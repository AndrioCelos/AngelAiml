using System.Collections;

namespace AngelAiml.Sets;
/// <summary>Represents an immutable set populated from a local file.</summary>
public class StringSet : Set, IReadOnlyCollection<string> {
	private readonly HashSet<string> elements;
	/// <summary>The <see cref="StringComparer"/> used to calculate hash codes for the phrases in this set.</summary>
	public IEqualityComparer<string> Comparer => elements.Comparer;

	public override int MaxWords { get; }

	public int Count => elements.Count;

	/// <summary>Creates a new <see cref="StringSet"/> with elements copied from a given collection, with the given comparer.</summary>
	/// <param name="elements">The collection from which to copy elements into this set.</param>
	/// <param name="comparer">The <see cref="IEqualityComparer{string}"/> to be used to compare phrases</param>
	public StringSet(IEnumerable<string> elements, IEqualityComparer<string> comparer) {
		this.elements = new HashSet<string>(comparer);
		foreach (var phrase in elements) {
			var words = phrase.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries);
			this.elements.Add(string.Join(" ", words));
			MaxWords = Math.Max(MaxWords, words.Length);
		}
	}

	public override bool Contains(string phrase) => elements.Contains(phrase);
	public IEnumerator<string> GetEnumerator() => elements.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
