using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Aiml.Tags;
/// <summary>Runs an AIML unit test and returns the element's content.</summary>
/// <remarks>
///		<para>A unit test consists of processing the content as chat input to the bot, and checking the response against the specified expected response.
///			The test is case-sensitive, but leading and trailing whitespace is ignored.</para>
///		<para>This element has the following attributes:</para>
///		<list type="table">
///			<item>
///				<term><c>name</c></term>
///				<description>the name of the test. May not be an XML subtag.</description>
///			</item>
///			<item>
///				<term><c>expected</c></term>
///				<description>the expected response message from the test.</description>
///			</item>
///			<item>
///				<term><c>regex</c></term>
///				<description>a regular expression that must match the response. Whitespace characters will match any sequence of whitespace.</description>
///			</item>
///		</list>
///		<para>This element is part of an extension to AIML.</para>
/// </remarks>
public sealed partial class Test(string name, TemplateElementCollection expectedResponse, bool regex, TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public string Name { get; } = name;
	public bool UseRegex { get; } = regex;
	public TemplateElementCollection ExpectedResponse { get; } = expectedResponse;

	[AimlLoaderContructor]
	public Test(TemplateElementCollection name, TemplateElementCollection? expected, TemplateElementCollection? regex, TemplateElementCollection children)
		: this(name.Single() is TemplateText text ? text.Text : throw new ArgumentException("<test> attribute 'name' must be constant.", nameof(name)),
			  expected ?? regex ?? throw new ArgumentException("<test> element must have an 'expected' or 'regex' attribute."), regex is not null, children) {
		if (expected is not null && regex is not null)
			throw new ArgumentException("<test> element cannot have both 'expected' and 'regex' attributes.");
	}

	public override string Evaluate(RequestProcess process) {
		LogRunningTest(GetLogger(process, true), Name);
		var text = EvaluateChildren(process);
		LogRequest(GetLogger(process), text);
		var newRequest = new Aiml.Request(text, process.User, process.Bot);
		text = process.Bot.ProcessRequest(newRequest, false, false, process.RecursionDepth + 1, out var duration).ToString().Trim();
		LogResponse(GetLogger(process), text);

		if (process.testResults != null) {
			var expectedResponse = ExpectedResponse.Evaluate(process).Trim();
			TestResult result;
			if (UseRegex) {
				var pattern = WhitespaceRegex().Replace(expectedResponse, @"\s+");
				try {
					var regex = new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5));
					result = regex.IsMatch(text.Trim())
						? TestResult.Pass(duration)
						: TestResult.Failure($"Expected regex: {expectedResponse}\nActual response: {text}", duration);
				} catch (ArgumentException ex) {
					LogInvalidRegex(GetLogger(process, true), ex.Message, pattern);
					result = TestResult.Failure($"Regex was invalid: {pattern}", duration);
				} catch (RegexMatchTimeoutException) {
					LogRegexTimeout(GetLogger(process, true), pattern);
					result = TestResult.Failure("Regex check timed out", duration);
				}
			} else {
				result = process.Bot.Config.CaseSensitiveStringComparer.Equals(text, expectedResponse)
					? TestResult.Pass(duration)
					: TestResult.Failure($"Expected response: {expectedResponse}\nActual response: {text}", duration);
			}
			process.testResults[Name] = result;
		} else
			LogDisabled(GetLogger(process, true));

		return text;
	}

#if NET8_0_OR_GREATER
	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();
#else
	private static readonly Regex whitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);
	private static Regex WhitespaceRegex() => whitespaceRegex;
#endif

	#region Log templates

	[LoggerMessage(LogLevel.Information, "In element <test>: running test {Name}")]
	private static partial void LogRunningTest(ILogger logger, string name);

	[LoggerMessage(LogLevel.Trace, "In element <test>: processing text '{Request}'.")]
	private static partial void LogRequest(ILogger logger, string request);

	[LoggerMessage(LogLevel.Trace, "In element <test>: the request returned '{Response}'.")]
	private static partial void LogResponse(ILogger logger, string response);

	[LoggerMessage(LogLevel.Warning, "In element <test>: Regex was invalid: {Message}: {Pattern}")]
	private static partial void LogInvalidRegex(ILogger logger, string message, string pattern);

	[LoggerMessage(LogLevel.Warning, "In element <test>: Regex check timed out: {Pattern}")]
	private static partial void LogRegexTimeout(ILogger logger, string pattern);

	[LoggerMessage(LogLevel.Warning, "In element <test>: Tests are not being used.")]
	private static partial void LogDisabled(ILogger logger);

	#endregion
}
