using Microsoft.Extensions.Logging;

namespace AngelAiml.Tags;
/// <summary>Recurses the content into a new request and returns the result.</summary>
/// <remarks>
///		<para>The content is evaluated and then processed as if it had been entered by the user, including normalisation and other pre-processing.</para>
///		<para>It is unknown what 'sr' stands for, but it's probably 'symbolic reduction'.</para>
///		<para>This element is defined by the AIML 1.1 specification.</para>
/// </remarks>
/// <seealso cref="SR"/><seealso cref="SraiX"/>
public sealed partial class Srai(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) {
		var text = EvaluateChildren(process);
		LogRequest(GetLogger(process), text);
		text = process.Srai(text);
		LogResponse(GetLogger(process), text);
		return text;
	}

	#region Log templates

	[LoggerMessage(LogLevel.Debug, "In element <srai>: processing text '{Request}'.")]
	private static partial void LogRequest(ILogger logger, string request);

	[LoggerMessage(LogLevel.Debug, "In element <srai>: the request returned '{Response}'.")]
	private static partial void LogResponse(ILogger logger, string response);

	#endregion
}
