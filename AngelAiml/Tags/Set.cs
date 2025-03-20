using Microsoft.Extensions.Logging;

namespace AngelAiml.Tags;
/// <summary>Sets the value of a predicate or a local variable to the content, and returns the content.</summary>
/// <remarks>
///		<para>This element has two forms:</para>
///		<list type="bullet">
///			<item>
///				<term><c>&lt;set name='predicate'&gt;value&lt;/set&gt;</c></term>
///				<description>Sets a predicate for the current user.</description>
///			</item>
///			<item>
///				<term><c>&lt;set var='variable'&gt;value&lt;/set&gt;</c></term>
///				<description>Sets a local variable for the containing category.</description>
///			</item>
///		</list>
///		<para>This element is defined by the AIML 1.1 specification. Local variables are defined by the AIML 2.0 specification.</para>
/// </remarks>
/// <seealso cref="AddTriple"/><seealso cref="Get"/>
public sealed partial class Set(TemplateElementCollection key, bool local, TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public TemplateElementCollection Key { get; private set; } = key;
	public bool LocalVar { get; private set; } = local;

	[AimlLoaderContructor]
	public Set(TemplateElementCollection? name, TemplateElementCollection? var, TemplateElementCollection children)
		: this(var ?? name ?? throw new ArgumentException("<set> element must have a 'name' or 'var' attribute"), var is not null, children) {
		if (name is not null && var is not null)
			throw new ArgumentException("<set> element cannot have both 'name' and 'var' attributes.");
	}

	public override string Evaluate(RequestProcess process) {
		var key = Key.Evaluate(process);
		var value = EvaluateChildren(process).Trim();

		var dictionary = LocalVar ? process.Variables : process.User.Predicates;
		if (process.Bot.Config.UnbindPredicatesWithDefaultValue &&
			value == (LocalVar ? process.Bot.Config.DefaultPredicate : process.Bot.Config.GetDefaultPredicate(key))) {
			dictionary.Remove(key);
			if (LocalVar)
				LogUnboundLocalVariable(GetLogger(process), key, value);
			else
				LogUnboundPredicate(GetLogger(process), key, value);
		} else {
			dictionary[key] = value;
			if (LocalVar)
				LogBoundLocalVariable(GetLogger(process), key, value);
			else
				LogBoundPredicate(GetLogger(process), key, value);
		}

		return value;
	}

	#region Log templates

	[LoggerMessage(LogLevel.Trace, "In element <set>: Unbound local variable '{Variable}' with default value '{Value}'.")]
	private static partial void LogUnboundLocalVariable(ILogger logger, string variable, string value);

	[LoggerMessage(LogLevel.Trace, "In element <set>: Unbound predicate '{Predicate}' with default value '{Value}'.")]
	private static partial void LogUnboundPredicate(ILogger logger, string predicate, string value);

	[LoggerMessage(LogLevel.Trace, "In element <set>: Set local variable '{Variable}' to '{Value}'.")]
	private static partial void LogBoundLocalVariable(ILogger logger, string variable, string value);

	[LoggerMessage(LogLevel.Trace, "In element <set>: Set predicate '{Predicate}' to '{Value}'.")]
	private static partial void LogBoundPredicate(ILogger logger, string predicate, string value);

	#endregion
}
