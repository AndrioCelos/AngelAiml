using Microsoft.Extensions.Logging;

namespace AngelAiml.Tags;
/// <summary>Recurses the text matched by the first message wildcard into a new request and returns the result.</summary>
/// <remarks>
///		<para>This element is shorthand for <c><![CDATA[<srai><star/></srai>]]></c>.</para>
///		<para>This element has no content.</para>
///		<para>This element is defined by the AIML 1.1 specification.</para>
/// </remarks>
/// <seealso cref="Srai"/>
public sealed partial class SR : TemplateNode {
	public override string Evaluate(RequestProcess process) {
		var text = process.star.Count > 0 ? process.star[0] : process.Bot.Config.DefaultWildcard;
		LogRequest(GetLogger(process), text);
		var newRequest = new AngelAiml.Request(text, process.User, process.Bot);
		text = process.Bot.ProcessRequest(newRequest, false, false, process.RecursionDepth + 1, out _).ToString();
		LogResponse(GetLogger(process), text);
		return text;
	}

	#region Log templates

	[LoggerMessage(LogLevel.Debug, "In element <sr>: processing text '{Request}'.")]
	private static partial void LogRequest(ILogger logger, string request);

	[LoggerMessage(LogLevel.Debug, "In element <sr>: the request returned '{Response}'.")]
	private static partial void LogResponse(ILogger logger, string response);

	#endregion
}
