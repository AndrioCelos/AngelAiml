using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace AngelAiml.Maps;
/// <summary>Represents an immutable map populated from a dictionary read from a file.</summary>
/// <param name="dictionary">The dictionary from which to copy elements.</param>
/// <param name="comparer">The <see cref="IEqualityComparer{string}"/> to be used to compare phrases.</param>
public class StringMap(IDictionary<string, string> dictionary, IEqualityComparer<string> comparer) : Map, IReadOnlyDictionary<string, string> {
	private readonly Dictionary<string, string> dictionary = new(dictionary, comparer);

    public override string? this[string key] => dictionary.TryGetValue(key, out var value) ? value : null;
    string IReadOnlyDictionary<string, string>.this[string key] => dictionary[key];

    public int Count => dictionary.Count;
	public IEnumerable<string> Keys => dictionary.Keys;
	public IEnumerable<string> Values => dictionary.Values;

	public bool ContainsKey(string key) => dictionary.ContainsKey(key);
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => dictionary.GetEnumerator();
#if NET5_0_OR_GREATER
	public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value) => dictionary.TryGetValue(key, out value);
#else
	public bool TryGetValue(string key, out string value) => dictionary.TryGetValue(key, out value);
#endif
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
