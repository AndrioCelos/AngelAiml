using System.Text.RegularExpressions;

namespace Aiml;
/// <summary>Transforms English words to singular or plural forms based on regular expression substitutions.</summary>
/// <remarks>
///		<para>This class is based on the class with the same name in JBoss DNA, which in turn was inspired by its namesake in Ruby on Rails.</para>
///		<para>JBoss DNA is available under the GNU General Public License, version 2.1.</para>
/// </remarks>
public class Inflector {
	public List<Rule> Plurals { get; } = [];
	public List<Rule> Singulars { get; } = [];
	public HashSet<string> Uncountables { get; }

	public Inflector() : this(StringComparer.CurrentCultureIgnoreCase) { }
	public Inflector(IEqualityComparer<string> comparer) {
		Uncountables = new HashSet<string>(comparer);

		Plurals.Add(new Rule("$", "s"));
		Plurals.Add(new Rule("s$", "s"));
		Plurals.Add(new Rule("(ax|test)is$", "$1es"));
		Plurals.Add(new Rule("(octop|vir)(?:us|i)$", "$1i"));
		Plurals.Add(new Rule("(?:alias|status)$", "$0es"));
		Plurals.Add(new Rule("bus$", "$0es"));
		Plurals.Add(new Rule("(?:buffal|tomat)o$", "$0es"));
		Plurals.Add(new Rule("([ti])(?:um|a)$", "$1a"));
		Plurals.Add(new Rule("sis$", "ses"));
		Plurals.Add(new Rule("(?:([^f])fe|([lr])f)$", "$1$2ves"));
		Plurals.Add(new Rule("hive$", "$0s"));
		Plurals.Add(new Rule("([^aeiouy]|qu)y$", "$1ies"));
		Plurals.Add(new Rule("(?:x|ch|ss|sh)$", "$0es"));
		Plurals.Add(new Rule("(matr|vert|ind)(?:ix|ex)$", "$1ices"));
		Plurals.Add(new Rule("([m|l])(?:ouse|ice)$", "$1ice"));
		Plurals.Add(new Rule("^ox$", "$0en"));
		Plurals.Add(new Rule("quiz$", "$0zes"));
		// Need to check for the following words that are already plural:
		Plurals.Add(new Rule("(?:oxen|octopi|viri|aliases|quizzes)$", "$0")); // special rules

		Singulars.Add(new Rule("s$", ""));
		Singulars.Add(new Rule("(?:s|si|u)s$", "$0")); // '-us' and '-ss' are already singular
		Singulars.Add(new Rule("news$", "$0"));
		Singulars.Add(new Rule("([ti])a$", "$1um"));
		Singulars.Add(new Rule("(analy|ba|diagno|parenthe|progno|synop|the)s[ei]s$", "$1sis"));
		Singulars.Add(new Rule("([^f])ves$", "$1fe"));
		Singulars.Add(new Rule("(hive)s$", "$1"));
		Singulars.Add(new Rule("(tive)s$", "$1"));
		Singulars.Add(new Rule("([lr])ves$", "$1f"));
		Singulars.Add(new Rule("([^aeiouy]|qu)ies$", "$1y"));
		Singulars.Add(new Rule("series$", "$0"));
		Singulars.Add(new Rule("(m)ovies$", "$1ovie"));
		Singulars.Add(new Rule("(x|ch|ss|sh)es$", "$1"));
		Singulars.Add(new Rule("([m|l])ice$", "$1ouse"));
		Singulars.Add(new Rule("(bus)es$", "$1"));
		Singulars.Add(new Rule("(o)es$", "$1"));
		Singulars.Add(new Rule("(shoe)s$", "$1"));
		Singulars.Add(new Rule("(cris|test)[ei]s$", "$1is"));
		Singulars.Add(new Rule("^(axe)s$", "$1"));  // Ambiguous between 'axe', 'ax' and 'axis', 'axe' was chosen.
		Singulars.Add(new Rule("(octop|vir)(?:i|us)$", "$1us"));
		Singulars.Add(new Rule("(alias|status)(?:es)?$", "$1"));  // 'alias' and 'status' are already singular, despite ending with 's'.
		Singulars.Add(new Rule("(ox)en", "$1"));
		Singulars.Add(new Rule("(vert|ind)ices$", "$1ex"));
		Singulars.Add(new Rule("(matr)ices$", "$1ix"));
		Singulars.Add(new Rule("(quiz)zes$", "$1"));

		AddIrregular("person", "people");
		AddIrregular("man", "men");
		AddIrregular("child", "children");
		AddIrregular("sex", "sexes");
		AddIrregular("move", "moves");
		AddIrregular("stadium", "stadiums");

		Uncountables.Add("equipment");
		Uncountables.Add("information");
		Uncountables.Add("rice");
		Uncountables.Add("money");
		Uncountables.Add("species");
		Uncountables.Add("series");
		Uncountables.Add("fish");
		Uncountables.Add("sheep");
	}

	public void AddIrregular(string singular, string plural) {
		var singularRemainder = singular[1..];
		var pluralRemainder = plural[1..];

		// Add rules that check whether the word is already in the required form.
		Singulars.Add(new Rule(singular + "$", "$0"));
		Plurals.Add(new Rule(plural + "$", "$0"));

		// Capturing the first character preserves its case.
		Singulars.Add(new Rule("(" + plural[0] + ")" + pluralRemainder + "$", $"$1{singularRemainder}"));
		Plurals.Add(new Rule("(" + singular[0] + ")" + singularRemainder + "$", $"$1{pluralRemainder}"));
	}

	public string Singularize(string word) => ApplyRules(word, Singulars);
	public string Pluralize(string word) => ApplyRules(word, Plurals);

	private string ApplyRules(string word, List<Rule> rules) {
		if (string.IsNullOrWhiteSpace(word)) return word;
		word = word.Trim();
		if (Uncountables.Contains(word)) return word;

		// Apply the rules in reverse order.
		for (var i = rules.Count - 1; i >= 0; --i) {
			var rule = rules[i];
			var result = rule.Apply(word);
			// If there's no match, rule.Apply returns the same instance.
			// If there is a match, and result is a different instance, we return it immediately.
			if (!ReferenceEquals(result, word)) return result;
		}
		return word;
	}

	public class Rule(Regex pattern, string replacement) {
		public Regex Pattern { get; } = pattern;
		public string Replacement { get; } = replacement;

		public Rule(string pattern, string replacement) : this(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase), replacement) { }

		public string Apply(string text) => Pattern.Replace(text, Replacement);

		public override string ToString() => "/" + Pattern.ToString().Replace("/", @"\/") + "/" + Replacement.Replace("/", @"\/") + "/i";
	}
}
