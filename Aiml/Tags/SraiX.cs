using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Aiml.Tags;
/// <summary>Sends the content to an external service and returns the response from the service.</summary>
/// <remarks>
///		<para>This element has the following attributes:</para>
///		<list type="table">
///			<item>
///				<term><c>service</c></term>
///				<description>the name of the external service to use, from <see cref="Bot.SraixServices"/>.</description>
///			</item>
///			<item>
///				<term><c>default</c></term>
///				<description>returned if the service call fails.
///					If omitted, the <c>SRAIXFAILED</c> category is queried and two predicates are set:
///					<list type="bullet">
///						<item><term>SraixException</term><description>the exception type name.</description></item>
///						<item><term>SraixExceptionMessage</term><description>the exception message.</description></item>
///					</list>
///				</description>
///			</item>
///		</list>
///		<para>This element is defined by the AIML 2.0 specification. This implementation is non-standard.</para>
/// </remarks>
/// <seealso cref="Srai"/>
public sealed partial class SraiX(TemplateElementCollection service, TemplateElementCollection? @default, XElement element, TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public TemplateElementCollection ServiceName { get; } = service;
	public XElement Element { get; } = element;
	public TemplateElementCollection? DefaultReply { get; } = @default;

	public override string Evaluate(RequestProcess process) {
		var serviceName = ServiceName.Evaluate(process);
		try {
			if (AimlLoader.sraixServices.TryGetValue(serviceName, out var service)) {
				var text = Children?.Evaluate(process) ?? "";
				LogRequest(GetLogger(process), serviceName, text);
				text = service.Process(text, Element, process);
				LogResponse(GetLogger(process), text);
				return text;
			} else {
				process.User.Predicates["SraixException"] = nameof(KeyNotFoundException);
				process.User.Predicates["SraixExceptionMessage"] = $"No service named '{serviceName}' is known.";
				LogServiceNotFound(GetLogger(process, true), serviceName);
				return (DefaultReply ?? new TemplateElementCollection(new Srai(new TemplateElementCollection("SRAIXFAILED")))).Evaluate(process);
			}
		} catch (Exception ex) {
			process.User.Predicates["SraixException"] = ex.GetType().Name;
			process.User.Predicates["SraixExceptionMessage"] = ex.Message;
			LogServiceException(GetLogger(process, true), ex, serviceName);
			return (DefaultReply ?? new TemplateElementCollection(new Srai(new TemplateElementCollection("SRAIXFAILED")))).Evaluate(process);
		}
	}

	#region Log templates

	[LoggerMessage(LogLevel.Debug, "In element <sraix>: querying service '{Service}' to process text '{Request}'.")]
	private static partial void LogRequest(ILogger logger, string service, string request);

	[LoggerMessage(LogLevel.Debug, "In element <sraix>: the request returned '{Response}'.")]
	private static partial void LogResponse(ILogger logger, string response);

	[LoggerMessage(LogLevel.Warning, "In element <sraix>: no service named '{Service}' is known.")]
	private static partial void LogServiceNotFound(ILogger logger, string service);

	[LoggerMessage(LogLevel.Warning, "In element <sraix>: exception in service {Service}")]
	private static partial void LogServiceException(ILogger logger, Exception ex, string service);

	#endregion
}
