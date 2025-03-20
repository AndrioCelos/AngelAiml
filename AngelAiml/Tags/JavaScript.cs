using Microsoft.Extensions.Logging;

namespace AngelAiml.Tags;
/// <summary>This element is not implemented. It executes the <c>JSFAILED</c> category.</summary>
/// <remarks>This element is defined by the AIML 1.1 specification and deprecated by the AIML 2.0 specification.</remarks>
/// <seealso cref="Calculate"/>
public sealed partial class JavaScript(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) {
		LogNotImplemented(GetLogger(process, true));
		return new TemplateElementCollection(new Srai(new TemplateElementCollection("JSFAILED"))).Evaluate(process);
	}

	#region Log templates

	[LoggerMessage(LogLevel.Warning, "In element <javascript>: <javascript> element is not implemented.")]
	private static partial void LogNotImplemented(ILogger logger);

	#endregion
}
