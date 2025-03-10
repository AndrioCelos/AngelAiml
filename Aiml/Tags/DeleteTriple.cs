using Microsoft.Extensions.Logging;

namespace Aiml.Tags;
/// <summary>Deletes triples from the bot's triple database.</summary>
/// <remarks>
///		<para>This element has the following attributes:</para>
///		<list type="table">
///			<item>
///				<term><c>subj</c>, <c>pred</c>, <c>obj</c></term>
///				<description>specify the triples to be deleted.</description>
///			</item>
///		</list>
///		<para>If only <c>subj</c> and <c>pred</c> is specified, it deletes all relations with the specified subject and predicate.
///			If only <c>subj</c> is specified, it deletes all relations with the specified subject.</para>
///		<para>If the triple does not exist, the triple database is unchanged.</para>
///		<para>This element has no other content.</para>
///		<para>This element is part of an extension to AIML derived from Program AB and Program Y.</para>
/// </remarks>
/// <seealso cref="AddTriple"/><seealso cref="Select"/><seealso cref="Uniq"/>
public sealed partial class DeleteTriple : TemplateNode {
	public TemplateElementCollection Subject { get; }
	public TemplateElementCollection? Predicate { get; }
	public TemplateElementCollection? Object { get; }

	public DeleteTriple(TemplateElementCollection subj, TemplateElementCollection? pred, TemplateElementCollection? obj) {
		Subject = subj;
		Predicate = pred;
		Object = obj;
		if (pred is null && obj is not null)
			throw new ArgumentException("<deletetriple> element cannot have 'obj' attribute without 'pred' attribute.", nameof(pred));
	}

	public override string Evaluate(RequestProcess process) {
		var subj = Subject.Evaluate(process).Trim();
		var pred = Predicate?.Evaluate(process).Trim();
		var obj = Object?.Evaluate(process).Trim();

		if (string.IsNullOrEmpty(subj)) {
			LogEmptySubject(GetLogger(process, true));
			return "";
		}

		if (string.IsNullOrEmpty(pred) || string.IsNullOrEmpty(obj)) {
			var count = string.IsNullOrEmpty(pred) ? process.Bot.Triples.RemoveAll(subj) : process.Bot.Triples.RemoveAll(subj, pred!);
			LogDeletedTriples(GetLogger(process), count, subj, pred, obj);
		} else if (process.Bot.Triples.Remove(subj, pred!, obj!))
			LogDeletedTriple(GetLogger(process), subj, pred, obj);
		else
			LogTripleNotFound(GetLogger(process), subj, pred, obj);

		return "";
	}

	#region Log templates

	[LoggerMessage(LogLevel.Warning, "In element <deletetriple>: Subject was empty.")]
	private static partial void LogEmptySubject(ILogger logger);

	[LoggerMessage(LogLevel.Debug, "In element <deletetriple>: Deleted {Count} triple(s) {{ Subject = {Subject}, Predicate = {Predicate}, Object = {Object} }}")]
	private static partial void LogDeletedTriples(ILogger logger, int count, string subject, string? predicate, string? @object);

	[LoggerMessage(LogLevel.Debug, "In element <deletetriple>: Deleted a triple. {{ Subject = {Subject}, Predicate = {Predicate}, Object = {Object} }}")]
	private static partial void LogDeletedTriple(ILogger logger, string subject, string? predicate, string? @object);

	[LoggerMessage(LogLevel.Debug, "In element <deletetriple>: No such triple exists. {{ Subject = {Subject}, Predicate = {Predicate}, Object = {Object} }}")]
	private static partial void LogTripleNotFound(ILogger logger, string subject, string? predicate, string? @object);

	#endregion
}
