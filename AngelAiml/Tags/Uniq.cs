using Microsoft.Extensions.Logging;

namespace AngelAiml.Tags;
/// <summary><para>Returns an element of a single triple matching a clause.</para></summary>
/// <remarks>
///		<para>This element has the following attributes:</para>
///		<list type="table">
///			<item>
///				<term><c>subj</c>, <c>pred</c>, <c>obj</c></term>
///				<description>
///					<para>May contain a variable <c>?</c> or text.</para>
///					<para>Text asserts that a triple element matches the text.</para>
///					<para>Exactly one attribute should be <c>?</c>; the variable name is ignored. It indicates the element of the triple that is returned.</para>
///				</description>
///			</item>
///		</list>
///		<para>If no triple matches, <c>DefaultTriple</c> is returned. If more than one triple matches, a single arbitrarily-chosen match is used.</para>
///     <para>This element has no other content.</para>
///		<para>This element is part of an extension to AIML derived from Program AB and Program Y.</para>
/// </remarks>
/// <example>
///		<para>Examples:</para>
///		<code>
///			<![CDATA[<uniq><subj>York</subj><pred>isa</pred><obj>?object</obj></uniq>]]>
///		</code>
///		<para>This example may return 'City'.</para>
///		<code>
///			<![CDATA[<uniq><subj>?id</subj><pred>hasFirstName</pred><obj>Alan</obj></uniq>]]>
///		</code>
///		<para>This example may return a bot-defined text identifying a person named Alan, such as '10099'. The triple may be defined by the <c>addtriple</c> element.</para>
/// </example>
/// <seealso cref="DeleteTriple"/><seealso cref="DeleteTriple"/><seealso cref="Select"/>
public sealed partial class Uniq(TemplateElementCollection subj, TemplateElementCollection pred, TemplateElementCollection obj) : TemplateNode {
	public TemplateElementCollection Subject { get; } = subj;
	public TemplateElementCollection Predicate { get; } = pred;
	public TemplateElementCollection Object { get; } = obj;

	public override string Evaluate(RequestProcess process) {
		var subj = Subject.Evaluate(process);
		var pred = Predicate.Evaluate(process);
		var obj = Object.Evaluate(process);

		if (subj.IsClauseVariable()) subj = null;
		if (pred.IsClauseVariable()) pred = null;
		if (obj.IsClauseVariable()) obj = null;

		var variables = (subj is null ? 1 : 0) + (pred is null ? 1 : 0) + (obj is null ? 1 : 0);
		if (variables != 1) {
			LogInvalidClause(GetLogger(process, true), variables, subj, pred, obj);
			if (variables == 0) return process.Bot.Config.DefaultTriple;
		}

		// Find triples that match.
		var triple = process.Bot.Triples.Match(subj, pred, obj).FirstOrDefault();
		if (triple is null) {
			LogNoMatch(GetLogger(process), subj, pred, obj);
			return process.Bot.Config.DefaultTriple;
		}

		return obj is null ? triple.Object : subj is null ? triple.Subject : triple.Predicate;
	}

	#region Log templates

	[LoggerMessage(LogLevel.Warning, "In element <uniq>: The clause contains {NumVariables} variables; it should contain exactly one. {{ Subject = {Subject}, Predicate = {Predicate}, Object = {Object} }}")]
	private static partial void LogInvalidClause(ILogger logger, int numVariables, string? subject, string? predicate, string? @object);

	[LoggerMessage(LogLevel.Trace, "In element <uniq>: No matching triple was found. {{ Subject = {Subject}, Predicate = {Predicate}, Object = {Object} }}")]
	private static partial void LogNoMatch(ILogger logger, string? subject, string? predicate, string? @object);

	#endregion
}
