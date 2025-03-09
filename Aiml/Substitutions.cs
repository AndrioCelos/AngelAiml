using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Aiml;
public class SubstitutionList : IList<Substitution> {
	private readonly List<Substitution> substitutions = [];
	private Regex? regex;
	private readonly bool preserveCase;

	private static readonly Regex substitutionRegex = new(@"\$(?:(\d+)|\{([^}]*)\}|([$&`'+_]))", RegexOptions.Compiled);

	public SubstitutionList() { }
	public SubstitutionList(bool preserveCase) => this.preserveCase = preserveCase;

	public Substitution this[int index] {
		get => substitutions[index];
		set {
			substitutions[index] = value;
			regex = null;
		}
	}

#if NET5_0_OR_GREATER
	[MemberNotNull(nameof(regex))]
#endif
	public void CompileRegex() {
		var groupIndex = 1;
		var builder = new StringBuilder("(");
		foreach (var item in substitutions) {
			item.groupIndex = groupIndex;
			if (builder.Length != 1) builder.Append(")|(");
			builder.Append(item.Pattern);
			if (item.IsRegex) {
				// To work out the number of capturing groups in the pattern, run it against the empty string.
				// The '|' ensures we will get a successful match; otherwise we would not get group information.
				var match = Regex.Match("", "|" + item.Pattern);
				groupIndex += match.Groups.Count;  // Deliberately counting group 0.
			} else {
				++groupIndex;
			}
		}
		builder.Append(')');
		regex = new Regex(builder.ToString(), RegexOptions.Compiled | RegexOptions.IgnoreCase);
	}

	public string Apply(string text) {
		if (substitutions.Count == 0) return text;
		if (regex == null) CompileRegex();
		return regex!.Replace(text, match => {
			foreach (var substitution in substitutions) {
				if (match.Groups[substitution.groupIndex].Success) {
					var replacement = substitution.Replacement;

					if (substitution.IsRegex) {
						// Process substitution tokens in the replacement.
						replacement = substitutionRegex.Replace(replacement, m =>
							m.Groups[1].Success ? match.Groups[substitution.groupIndex + int.Parse(m.Groups[1].Value)].Value :
							m.Groups[2].Success ? (int.TryParse(m.Groups[2].Value, out var n) ? match.Groups[substitution.groupIndex + n].Value : match.Groups[m.Groups[2].Value].Value) :
							m.Groups[3].Value[0] switch {
								'$' => "$",
								'&' => match.Value,
								'`' => text[..(match.Index - 1)],
								'\'' => text[(match.Index + match.Length - 1)..],
								'+' => match.Groups[^1].Value,
								'_' => text,
								_ => m.Value
							});
					}

					if (substitution.startSpace && !match.Value.StartsWith(" ")) replacement = replacement.TrimStart();
					if (substitution.endSpace   && !match.Value.EndsWith(" ")  ) replacement = replacement.TrimEnd();

					if (preserveCase) {
						if (char.IsUpper(match.Value.FirstOrDefault(char.IsLetter))) {
							if (match.Value.Where(char.IsLetter).All(char.IsUpper)) {
								// Uppercase
								replacement = replacement.ToUpper();
							} else {
								// Sentence case
								var builder = new StringBuilder(replacement);
								for (var i = 0; i < builder.Length; i++) {
									if (char.IsLetter(builder[i])) {
										builder[i] = char.ToUpper(builder[i]);
										replacement = builder.ToString();
										break;
									}
								}
							}
						}
					}

					return replacement;
				}
			}
			return "";
		});
	}

	public int Count => substitutions.Count;
	public bool IsReadOnly => false;

	public void Add(Substitution item) {
		substitutions.Add(item);
		regex = null;
	}
	public void AddRange(IEnumerable<Substitution> items) {
		substitutions.AddRange(items);
		regex = null;
	}
	public void Insert(int index, Substitution item) {
		substitutions.Insert(index, item);
		regex = null;
	}
	public bool Remove(Substitution item) {
		var result = substitutions.Remove(item);
		if (result) regex = null;
		return result;
	}
	public void RemoveAt(int index) {
		substitutions.RemoveAt(index);
		regex = null;
	}
	public void Clear() {
		substitutions.Clear();
		regex = null;
	}

	public bool Contains(Substitution item) => substitutions.Contains(item);
	public void CopyTo(Substitution[] array, int arrayIndex) => substitutions.CopyTo(array, arrayIndex);
	public IEnumerator<Substitution> GetEnumerator() => substitutions.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	public int IndexOf(Substitution item) => substitutions.IndexOf(item);
}

[JsonArray, JsonConverter(typeof(Config.SubstitutionConverter))]
public class Substitution {
	public bool IsRegex { get; }
	public string Pattern { get; }
	public string Replacement { get; }
	internal int groupIndex;
	internal bool startSpace;
	internal bool endSpace;

	public Substitution(string original, string replacement, bool regex) {
		IsRegex = regex;
		if (regex) {
			Pattern = original.Trim();
			Replacement = replacement;
		} else {
			Pattern = Regex.Escape(original.Trim());
			Replacement = replacement.Replace("$", "$$");
		}
		// Spaces surrounding the pattern indicate word boundaries.
		if (original.StartsWith(" ")) {
			// If there's a space there, it will match the space. If there isn't a space there, such as if it overlaps a previous substitution, it will still match.
			Pattern = @"(?: |(?<!\S))" + Pattern;
			if (replacement.StartsWith(" ")) startSpace = true;
		}
		if (original.EndsWith(" ")) {
			Pattern += @"(?: |(?!\S))";
			if (replacement.EndsWith(" ")) endSpace = true;
		}
	}
	public Substitution(string original, string replacement) : this(original, replacement, false) { }
}
