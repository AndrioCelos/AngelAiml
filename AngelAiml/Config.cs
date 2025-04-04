﻿using System.Globalization;
using Newtonsoft.Json;

namespace AngelAiml;
public class Config {
	/// <summary>The value returned by the <c>get</c> and <c>bot</c> template elements for an unbound predicate, if no default value is defined for it.</summary>
	public string DefaultPredicate = "unknown";
	/// <summary>The value returned by the <c>map</c> template element if the map cannot be found or the input key is not in the map.</summary>
	public string DefaultMap = "unknown";
	/// <summary>The value returned by the <c>request</c> and <c>response</c> template elements if the index is beyond the history.</summary>
	public string DefaultHistory = "nil";
	/// <summary>The value returned by the <c>first</c> and <c>rest</c> template elements if the sought words do not exist.</summary>
	public string DefaultListItem = "nil";
	/// <summary>The value returned by the <c>uniq</c> template element if no matching triple is found.</summary>
	public string DefaultTriple = "nil";
	/// <summary>The value returned by the <c>star</c>, <c>thatstar</c> and <c>topicstar</c> template elements for a zero-length match.</summary>
	public string DefaultWildcard = "nil";

	/// <summary>The maximum number of requests and responses that will be remembered for each user.</summary>
	public int HistorySize = 16;

	/// <summary>The response the bot will give to input that doesn't match any AIML category.</summary>
	public string DefaultResponse = "I have no answer for that.";
	/// <summary>The maximum time, in milliseconds, a request should be allowed to run for.</summary>
	public double Timeout = 10e+3;
	/// <summary>The response to a request that times out.</summary>
	public string TimeoutMessage = "That query took too long for me to process.";
	/// <summary>The maximum allowed number of recursive <c>srai</c> template element calls.</summary>
	public int RecursionLimit = 50;
	/// <summary>The response to a request that exceeds the recursion limit.</summary>
	public string RecursionLimitMessage = "Too much recursion in AIML.";
	/// <summary>The maximum allowed number of loops on a single <c>condition</c> template element.</summary>
	public int LoopLimit = 100;
	/// <summary>The response to a request that exceeds the loop limit.</summary>
	public string LoopLimitMessage = "Too much looping in condition.";
	/// <summary>Whether to enable the <see cref="Tags.System"/> template element.</summary>
	public bool EnableSystem = false;
	/// <summary>The return value of the <see cref="Tags.System"/> template element when it is disabled or the command fails.</summary>
	public string SystemFailedMessage = "Failed to execute a system command.";

	public int DefaultDelay = 1000;

	/// <summary>
	/// The locale that should be used by default for string comparisons and the <c>date</c> template element.
	/// Defaults to the system's current locale.
	/// </summary>
	public CultureInfo Locale {
		get;
		set {
			field = value ?? CultureInfo.CurrentCulture;
			StringComparer = StringComparer.Create(Locale, true);
			CaseSensitiveStringComparer = StringComparer.Create(Locale, false);
			RebuildDictionaries();
		}
	} = CultureInfo.CurrentCulture;
	[JsonIgnore]
	/// <summary>Returns the <see cref="StringComparer"/> used for set and map comparisons. This is changed by setting the <see cref="Locale"/> property.</summary>
	public StringComparer StringComparer { get; private set; } = StringComparer.CurrentCultureIgnoreCase;

	[JsonIgnore]
	/// <summary>Returns the <see cref="StringComparer"/> used for test comparisons. This is changed by setting the <see cref="Locale"/> property.</summary>
	public StringComparer CaseSensitiveStringComparer { get; private set; } = StringComparer.CurrentCulture;

	/// <summary>The maximum allowed number of recursive <c>srai</c> template elements that will have diagnostic messages logged.</summary>
	public int LogRecursionLimit = 2;

	/// <summary>The directory in which to look for AIML files. Defaults to '$ConfigDirectory/aiml'.</summary>
	public string AimlDirectory = "aiml";
	/// <summary>The directory in which to write logs. Defaults to '$ConfigDirectory/logs'.</summary>
	public string LogDirectory = "logs";
	/// <summary>The directory in which to look for sets. Defaults to '$ConfigDirectory/sets'.</summary>
	public string SetsDirectory = "sets";
	/// <summary>The directory in which to look for maps. Defaults to '$ConfigDirectory/maps'.</summary>
	public string MapsDirectory = "maps";

	/// <summary>The file path to which to save categories learned by the <c>learnf</c> template element.</summary>
	public string LearnfFile { get; set; } = "learnf.aiml";

	/// <summary>Defined strings that delimit sentences in requests and responses. Defaults to [ ".", "!", "?", ";" ].</summary>
	public char[] Splitters = ['.', '!', '?', ';'];

	/// <summary>Whether normalisation and other substitutions preserve the case of words.</summary>
	public bool SubstitutionsPreserveCase;
	/// <summary>Whether to have an empty response, after <see cref="Tags.Oob"/> tag processing, not change the path used for matching a that pattern.</summary>
	public bool ThatExcludeEmptyResponse = false;
	/// <summary>If this is true, using the <c>set</c> template element to set a predicate or variable to the default value will unbind it instead.</summary>
	public bool UnbindPredicatesWithDefaultValue = false;

	// These go in their own files.
	/// <summary>Defines default values for bot predicates, used by the <c>bot</c> template element.</summary>
	[JsonIgnore] public Dictionary<string, string> BotProperties { get; set; } = new(StringComparer.CurrentCultureIgnoreCase);
	/// <summary>Defines default values for user predicates, used by the <c>get</c> template element.</summary>
	[JsonIgnore] public Dictionary<string, string> DefaultPredicates { get; set; } = new(StringComparer.CurrentCultureIgnoreCase);
	/// <summary>Defines substitutions used by the <c>gender</c> template element.</summary>
	[JsonIgnore] public SubstitutionList GenderSubstitutions   { get; set; } = [];
	/// <summary>Defines substitutions used by the <c>person</c> template element.</summary>
	[JsonIgnore] public SubstitutionList PersonSubstitutions   { get; set; } = [];
	/// <summary>Defines substitutions used by the <c>person2</c> template element.</summary>
	[JsonIgnore] public SubstitutionList Person2Substitutions  { get; set; } = [];
	/// <summary>Defines substitutions used in the normalisation process.</summary>
	[JsonIgnore] public SubstitutionList NormalSubstitutions   { get; set; } = [];
	/// <summary>Defines substitutions used in the denormalisation process.</summary>
	[JsonIgnore] public SubstitutionList DenormalSubstitutions { get; set; } = [];

	private void RebuildDictionaries() {
		BotProperties = new Dictionary<string, string>(BotProperties, StringComparer);
		DefaultPredicates = new Dictionary<string, string>(DefaultPredicates, StringComparer);
	}

	public static Config FromFile(string file) {
		if (!File.Exists(file)) return new();
		using var reader = new JsonTextReader(new StreamReader(file));
		return new JsonSerializer().Deserialize<Config>(reader) ?? new();
	}

	private static void Load(string file, object target) {
		using var reader = new JsonTextReader(new StreamReader(file));
		new JsonSerializer().Populate(reader, target);
	}

	public void LoadPredicates(string file) => Load(file, BotProperties);
	public void LoadGender(string file) => Load(file, GenderSubstitutions);
	public void LoadPerson(string file) => Load(file, PersonSubstitutions);
	public void LoadPerson2(string file) => Load(file, Person2Substitutions);
	public void LoadNormal(string file) => Load(file, NormalSubstitutions);
	public void LoadDenormal(string file) => Load(file, DenormalSubstitutions);
	public void LoadDefaultPredicates(string file) => Load(file, DefaultPredicates);

	public string GetDefaultPredicate(string name) => DefaultPredicates.GetValueOrDefault(name, DefaultPredicate);

	public class SubstitutionConverter : JsonConverter {
		public override bool CanConvert(Type type) => type == typeof(Substitution);
		public override bool CanRead => true;
		public override bool CanWrite => true;

		public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
			var list = serializer.Deserialize<string[]>(reader) ?? throw new JsonSerializationException($"{nameof(Substitution)} cannot be null.");
			return list.Length switch {
				2 => new Substitution(list[0], list[1]),
				3 => new Substitution(list[0], list[1], list[2].Equals("regex", StringComparison.OrdinalIgnoreCase)),
				_ => throw new JsonSerializationException($"{nameof(Substitution)} must have exactly 2 or 3 elements.")
			};
		}

		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) {
			if (value == null) {
				writer.WriteNull();
				return;
			}
			var substitution = (Substitution) value;
			writer.WriteStartArray();
			writer.WriteValue(substitution.Pattern);
			writer.WriteValue(substitution.Replacement);
			if (substitution.IsRegex) writer.WriteValue("regex");
			writer.WriteEndArray();
		}
	}
}
