using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aiml;
/// <summary>Represents an AIML template-side node.</summary>
public abstract partial class TemplateNode {
	/// <summary>When overridden, returns the evaluated content of this node.</summary>
	public abstract string Evaluate(RequestProcess process);

	/// <summary>Returns the <see cref="ILogger"/> instance for this type and the specified request.</summary>
	protected ILogger GetLogger(RequestProcess process, bool ignoreRecusionLimit = false)
		=> ignoreRecusionLimit || process.RecursionDepth < process.Bot.Config.LogRecursionLimit
			? process.Bot.GetLogger(GetType())
			: NullLogger.Instance;

	/// <summary>Evaluates the specified child element collection and attempts to parse the result as an integer.</summary>
	protected bool TryParseIndex(RequestProcess process, TemplateElementCollection? attr, out int index) {
		if (attr is null) {
			index = 1;
			return true;
		}
		var s = attr.Evaluate(process);
		if (int.TryParse(s, out index) && index > 0)
			return true;
		LogInvalidIndex(GetLogger(process, true), s);
		return false;
	}

	#region Log templates

	[LoggerMessage(LogLevel.Warning, "'index' was not valid: {Index}")]
	private static partial void LogInvalidIndex(ILogger logger, string Index);

	#endregion
}

/// <summary>Represents a template-side tag that can recursively contain other nodes.</summary>
public abstract class RecursiveTemplateTag(TemplateElementCollection children) : TemplateNode {
	public TemplateElementCollection Children { get; } = children;

	public string EvaluateChildren(RequestProcess process) => Children?.Evaluate(process) ?? "";
	public string EvaluateChildrenOrStar(RequestProcess process)
		=> Children is not null && !Children.IsEmpty ? Children.Evaluate(process)
			: process.star.Count > 0 ? process.star[0] : process.Bot.Config.DefaultWildcard;

	public override string ToString() => $"<{GetType().Name.ToLowerInvariant()}>{Children}</{GetType().Name.ToLowerInvariant()}>";
}

/// <summary>Represents constant text in place of a template-side AIML tag.</summary>
public sealed partial class TemplateText : TemplateNode {
	internal static TemplateText Space { get; } = new(" ");

	public string Text { get; private set; }

	/// <summary>Initialises a new <see cref="TemplateText"/> instance with the specified text.</summary>
	public TemplateText(string text) : this(text, true) { }
	/// <summary>Initialises a new <see cref="TemplateText"/> instance with the specified text.</summary>
	/// <param name="reduceWhitespace">If set, consecutive whitespace will be reduced to a single space, as per HTML.</param>
	public TemplateText(string text, bool reduceWhitespace) {
		// Pandorabots reduces consecutive whitespace in text nodes to a single space character (like HTML).
		if (reduceWhitespace) text = WhitespaceRegex().Replace(text, " ");
		Text = text;
	}

	/// <summary>Returns this node's text.</summary>
	public override string Evaluate(RequestProcess process) => Text;

	public override string ToString() => Text;

#if NET8_0_OR_GREATER
	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();
#else
	private static readonly Regex whitespaceRegex = new(@"\s+", RegexOptions.Compiled);
	public static Regex WhitespaceRegex() => whitespaceRegex;
#endif
}

/// <summary>Represents a collection of <see cref="TemplateNode"/> instances, as contained by a <see cref="RecursiveTemplateTag"/> or attribute subtag.</summary>
public class TemplateElementCollection(params TemplateNode[] tags) : IReadOnlyList<TemplateNode>, IList<TemplateNode>, IList {
	private readonly TemplateNode[] tags = tags;

	public static TemplateElementCollection Empty { get; } = new();

	/// <summary>Indicates whether this <see cref="TemplateElementCollection"/> contains a <see cref="Tags.Loop"/> tag.</summary>
	public bool Loop { get; } = tags.OfType<Tags.Loop>().Any();
	public int Count => tags.Length;
	public bool IsEmpty => tags.All(t => t is TemplateText text && string.IsNullOrEmpty(text.Text));
	public bool IsWhitespace => tags.All(t => t is TemplateText text && string.IsNullOrWhiteSpace(text.Text));

	public TemplateElementCollection(IEnumerable<TemplateNode> tags) : this([.. tags]) { }
	public TemplateElementCollection(string text) : this([new TemplateText(text)]) { }

	public TemplateNode this[int index] => tags[index];

	/// <summary>Evaluates the contained tags and returns the result.</summary>
	public string Evaluate(RequestProcess process) {
		if (tags == null || tags.Length == 0) return string.Empty;
		var builder = new StringBuilder();
		foreach (var tag in tags) {
			var output = tag.Evaluate(process);

			// Condense consecutive spaces.
			if (builder.Length > 0 && char.IsWhiteSpace(builder[^1]))
				output = output.TrimStart();

			builder.Append(output);
		}
		return builder.ToString();
	}

	/// <summary>Returns a new TagCollection containing all nodes contained in a given XML node.</summary>
	/// <param name="el">The XML node whose children should be parsed.</param>
	/// <returns>A new TagCollection containing the results of calling Tag.Parse to construct child nodes from the XML node's children.</returns>
	public static TemplateElementCollection FromXml(XElement el, AimlLoader loader) {
		var tagList = new List<TemplateNode>();
		foreach (var node2 in el.Nodes()) {
			switch (node2) {
				case XText textNode:
					tagList.Add(new TemplateText(textNode.Value));
					break;
				case XElement childElement:
					tagList.Add(loader.ParseElement(childElement));
					break;
			}
		}
		return new TemplateElementCollection([.. tagList]);
	}

	public override string ToString() => string.Join(null, this);

	#region Interface implementations

	bool IList.IsFixedSize => true;
	bool IList.IsReadOnly => true;
	bool ICollection<TemplateNode>.IsReadOnly => true;
	bool ICollection.IsSynchronized => false;
	object ICollection.SyncRoot => this;

	TemplateNode IList<TemplateNode>.this[int index] { get => this[index]; set => throw new NotSupportedException(); }
	object? IList.this[int index] { get => this[index]; set => throw new NotSupportedException(); }

	public int IndexOf(TemplateNode tag) => Array.IndexOf(tags, tag);
	public bool Contains(TemplateNode tag) => IndexOf(tag) >= 0;

	public void CopyTo(TemplateNode[] target, int startIndex) {
		for (var i = 0; i < tags.Length; ++i)
			target[startIndex + i] = tags[i];
	}
	void ICollection.CopyTo(Array target, int startIndex) => tags.CopyTo(target, startIndex);

	public IEnumerator<TemplateNode> GetEnumerator() => ((IEnumerable<TemplateNode>) tags).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	bool ICollection<TemplateNode>.Remove(TemplateNode tag) => throw new NotSupportedException();
	void ICollection<TemplateNode>.Clear() => throw new NotSupportedException();
	void ICollection<TemplateNode>.Add(TemplateNode tag) => throw new NotSupportedException();
	void IList<TemplateNode>.RemoveAt(int index) => throw new NotSupportedException();
	void IList<TemplateNode>.Insert(int index, TemplateNode tag) => throw new NotSupportedException();
	void IList.Remove(object? tag) => throw new NotSupportedException();
	void IList.RemoveAt(int index) => throw new NotSupportedException();
	void IList.Insert(int index, object? tag) => throw new NotSupportedException();
	void IList.Clear() => throw new NotSupportedException();
	int IList.IndexOf(object? tag) => tag is TemplateNode node ? IndexOf(node) : -1;
	bool IList.Contains(object? tag) => tag is TemplateNode node && Contains(node);
	int IList.Add(object? tag) => throw new NotSupportedException();

	#endregion
}
