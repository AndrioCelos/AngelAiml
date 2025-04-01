using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using AngelAiml.Maps;
using AngelAiml.Sets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AngelAiml;
public partial class Bot {
	public static Version Version { get; } = typeof(Bot).Assembly.GetName().Version!;

	public string ConfigDirectory { get; set; }
	public Config Config { get; set; } = new();

	public int Size { get; internal set; }
	public int Vocabulary {
		get {
			if (this.vocabulary is not null) return this.vocabulary.Value;
			var vocabulary = CalculateVocabulary();
			this.vocabulary = vocabulary;
			return vocabulary;
		}
	}
	public PatternNode Graphmaster { get; } = new(null, StringComparer.CurrentCultureIgnoreCase);

	public Dictionary<string, string> Properties => Config.BotProperties;
	public Dictionary<string, Set> Sets { get; } = new(StringComparer.CurrentCultureIgnoreCase);
	public Dictionary<string, Map> Maps { get; } = new(StringComparer.CurrentCultureIgnoreCase);
	public TripleCollection Triples { get; } = new(StringComparer.CurrentCultureIgnoreCase);

	public AimlLoader AimlLoader { get; }

	public event EventHandler<GossipEventArgs>? Gossip;
	public event EventHandler<PostbackRequestEventArgs>? PostbackRequest;
	public event EventHandler<PostbackResponseEventArgs>? PostbackResponse;

	public void OnGossip(GossipEventArgs e) => Gossip?.Invoke(this, e);
	public void OnPostbackRequest(PostbackRequestEventArgs e) => PostbackRequest?.Invoke(this, e);
	public void OnPostbackResponse(PostbackResponseEventArgs e) => PostbackResponse?.Invoke(this, e);

	internal readonly Random Random = new();
	public ILoggerFactory LoggerFactory { get; }
	internal readonly ILogger<Bot> logger;
	internal readonly Dictionary<Type, ILogger> loggers = [];
	private int? vocabulary;

	public Bot() : this("config", null) { }
	public Bot(ILoggerFactory loggerFactory) : this("config", loggerFactory) { }
	public Bot(string configDirectory) : this(configDirectory, null) { }
	public Bot(string configDirectory, ILoggerFactory? loggerFactory) {
		this.LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
		logger = loggerFactory.CreateLogger<Bot>();
		AimlLoader = new(this);
		ConfigDirectory = configDirectory;

		// Add predefined sets and maps.
		var inflector = new Inflector(StringComparer.CurrentCultureIgnoreCase);
		Sets.Add("number", new NumberSet());
		Sets.Add("word", new WordSet());
		Maps.Add("successor", new ArithmeticMap(1));
		Maps.Add("predecessor", new ArithmeticMap(-1));
		Maps.Add("singular", new SingularMap(inflector));
		Maps.Add("plural", new PluralMap(inflector));
	}
	internal Bot(Random random, ILoggerFactory loggerFactory) : this(loggerFactory) => Random = random;

	public void LoadAiml() => AimlLoader.LoadAimlFiles();

	public void LoadConfig() {
		Config = Config.FromFile(Path.Combine(ConfigDirectory, "config.json"));
		LoadConfig2();
	}

	private void LoadConfig2() {
		CheckDefaultProperties();

		var inflector = new Inflector(Config.StringComparer);
		Maps["singular"] = new SingularMap(inflector);
		Maps["plural"] = new PluralMap(inflector);

		Config.GenderSubstitutions = new(Config.SubstitutionsPreserveCase);
		Config.PersonSubstitutions = new(Config.SubstitutionsPreserveCase);
		Config.Person2Substitutions = new(Config.SubstitutionsPreserveCase);
		Config.NormalSubstitutions = new(Config.SubstitutionsPreserveCase);
		Config.DenormalSubstitutions = new(Config.SubstitutionsPreserveCase);

		Config.LoadPredicates(Path.Combine(ConfigDirectory, "botpredicates.json"));
		Config.LoadGender(Path.Combine(ConfigDirectory, "gender.json"));
		Config.LoadPerson(Path.Combine(ConfigDirectory, "person.json"));
		Config.LoadPerson2(Path.Combine(ConfigDirectory, "person2.json"));
		Config.LoadNormal(Path.Combine(ConfigDirectory, "normal.json"));
		Config.LoadDenormal(Path.Combine(ConfigDirectory, "denormal.json"));
		Config.LoadDefaultPredicates(Path.Combine(ConfigDirectory, "predicates.json"));

		Config.GenderSubstitutions.CompileRegex();
		Config.PersonSubstitutions.CompileRegex();
		Config.Person2Substitutions.CompileRegex();
		Config.NormalSubstitutions.CompileRegex();
		Config.DenormalSubstitutions.CompileRegex();

		if (Directory.Exists(Path.Combine(ConfigDirectory, Config.MapsDirectory)))
			LoadMaps(Path.Combine(ConfigDirectory, Config.MapsDirectory));
		if (Directory.Exists(Path.Combine(ConfigDirectory, Config.SetsDirectory)))
			LoadSets(Path.Combine(ConfigDirectory, Config.SetsDirectory));

		LoadTriples(Path.Combine(ConfigDirectory, "triples.txt"));
	}

	private void CheckDefaultProperties() {
		if (!Config.BotProperties.ContainsKey("version"))
			Config.BotProperties.Add("version", Version.ToString(2));
	}

	private void LoadSets(string directory) {
		// TODO: Implement remote sets and maps
		LogLoadingSets(directory);

		foreach (var file in Directory.EnumerateFiles(directory, "*.txt")) {
			if (Sets.ContainsKey(Path.GetFileNameWithoutExtension(file))) {
				LogDuplicateSetName(Path.GetFileNameWithoutExtension(file));
				continue;
			}

			var set = new List<string>();

			using var reader = new StreamReader(file);
			var phraseBuilder = new StringBuilder();
			while (!reader.EndOfStream) {
				phraseBuilder.Clear();
				bool trailingBackslash = false, whitespace = false;

				while (true) {
					var c = reader.Read();
					switch (c) {
						case < 0 or '\r' or '\n':
							// End of stream or newline
							goto endOfPhrase;
						case '\\':
							c = reader.Read();
							if (c is < 0 or '\r' or '\n') {
								// A backslash at the end of a line indicates that the empty string should be included the set.
								// Empty lines are ignored.
								trailingBackslash = true;
							} else {
								if (whitespace) {
									if (phraseBuilder.Length > 0) phraseBuilder.Append(' ');
									whitespace = false;
								}
								phraseBuilder.Append((char) c);
							}
							break;
						case '#':
							// Comment
							do { c = (char) reader.Read(); } while (c is >= 0 and not '\r' and not '\n');
							goto endOfPhrase;
						default:
							// Reduce consecutive whitespace into a single space.
							// Defer appending the space until a non-whitespace character is read, so as to ignore trailing whitespace.
							if (char.IsWhiteSpace((char) c)) {
								whitespace = true;
							} else {
								if (whitespace) {
									if (phraseBuilder.Length > 0) phraseBuilder.Append(' ');
									whitespace = false;
								}
								phraseBuilder.Append((char) c);
							}
							break;
					}
				}
				endOfPhrase:
				var phrase = phraseBuilder.ToString();
				if (!trailingBackslash && string.IsNullOrWhiteSpace(phrase)) continue;
				set.Add(phrase);
			}

			Sets[Path.GetFileNameWithoutExtension(file)] = set.Count == 1 && set[0].StartsWith("map:")
				? new MapSet(set[0][4..], this)
				: new StringSet(set, Config.StringComparer);
			InvalidateVocabulary();
		}

		LogLoadedSets(Sets.Count);
	}

	private void LoadMaps(string directory) {
		LogLoadingMaps(directory);

		foreach (var file in Directory.EnumerateFiles(directory, "*.txt")) {
			if (Maps.ContainsKey(Path.GetFileNameWithoutExtension(file))) {
				LogDuplicateMapName(Path.GetFileNameWithoutExtension(file));
				continue;
			}

			var map = new Dictionary<string, string>(Config.StringComparer);

			var reader = new StreamReader(file);
			while (true) {
				var line = reader.ReadLine();
				if (line is null) break;

				// Remove comments.
				line = MapCommentRegex().Replace(line, "$1");
				if (string.IsNullOrWhiteSpace(line)) continue;

				var pos = line.IndexOf(':');
				if (pos < 0)
					LogMapSyntaxError(Path.GetFileNameWithoutExtension(file), line);
				else {
					var key = line[..pos].Trim();
					var value = line[(pos + 1)..].Trim();
#if NET6_0_OR_GREATER
					if (!map.TryAdd(key, value))
#else
					if (!map.ContainsKey(key))
						map.Add(key, value);
					else
#endif
						LogMapDuplicateKey(Path.GetFileNameWithoutExtension(file), key);
				}
			}

			Maps[Path.GetFileNameWithoutExtension(file)] = new StringMap(map, Config.StringComparer);
			InvalidateVocabulary();
		}

		LogLoadedMaps(Maps.Count);
	}

	private void LoadTriples(string filePath) {
		if (!File.Exists(filePath)) {
			LogTriplesFileNotFound(filePath);
			return;
		}

		LogLoadingTriples(filePath);
		using (var reader = new StreamReader(filePath)) {
			while (!reader.EndOfStream) {
				var line = reader.ReadLine();
				if (string.IsNullOrWhiteSpace(line)) continue;
				var fields = line.Split([':'], 3);
				if (fields.Length != 3)
					LogTriplesSyntaxError(line);
				else
					Triples.Add(fields[0], fields[1], fields[2]);
			}
		}

		LogLoadedTriples(Triples.Count);
	}

	internal ILogger GetLogger(Type categoryType) {
		if (loggers.TryGetValue(categoryType, out var logger)) return logger;
		if (LoggerFactory is null) return this.logger;

		logger = LoggerFactory.CreateLogger(categoryType.FullName!);
		loggers[categoryType] = logger;
		return logger;
	}

	internal void WriteGossip(RequestProcess process, string message) {
		var e = new GossipEventArgs(message);
		OnGossip(e);
		if (e.Handled) return;
		LogGossip(process.User.ID, message);
	}

	public Response Chat(Request request) => Chat(request, false);
	public Response Chat(Request request, bool trace) {
		LogChatRequest(request.User.ID, request.Text);
		request.User.AddRequest(request);

		var response = ProcessRequest(request, trace, false, 0, out _);

		if (!Config.BotProperties.TryGetValue("name", out var botName)) botName = "Robot";
		LogChatResponse(botName, request.User.ID, response.ToString());

		response.ProcessOobElements();
		request.User.AddResponse(response);
		return response;
	}
	internal Response Chat(Request request, bool trace, bool isPostback) {
		if (isPostback) LogChatRequestPostback(request.User.ID, request.Text);
		else LogChatRequest(request.User.ID, request.Text);

		request.User.AddRequest(request);

		var response = ProcessRequest(request, trace, false, 0, out _);

		if (!Config.BotProperties.TryGetValue("name", out var botName)) botName = "Robot";
		LogChatResponse(botName, request.User.ID, response.ToString());

		response.ProcessOobElements();
		request.User.AddResponse(response);
		return response;
	}

	internal Response ProcessRequest(Request request, bool trace, bool useTests, int recursionDepth, out TimeSpan duration) {
		var stopwatch = Stopwatch.StartNew();
		var that = request.User.That;
		var topic = Normalize(request.User.Topic);

		// Respond to each sentence separately.
		var builder = new StringBuilder();
		foreach (var sentence in request.Sentences) {
			var process = new RequestProcess(sentence, recursionDepth, useTests);

			LogNormalizedPath(sentence.Text, that, topic);

			string output;
			try {
				var template = request.User.Graphmaster.Search(sentence, process, that, trace);
				if (template != null) {
					process.template = template;
					LogMatchUserSpecificCategory(process.Path);
				} else {
					template = Graphmaster.Search(sentence, process, that, trace);
					if (template != null) {
						process.template = template;
						LogMatchCategory(process.Path, template.Uri, template.LineNumber);
					}
				}

				if (template != null) {
					output = template.Content.Evaluate(process);
				} else {
					LogNoMatch(sentence.Text, that, topic);
					output = Config.DefaultResponse;
				}
			} catch (TimeoutException) {
				output = Config.TimeoutMessage;
			} catch (RecursionLimitException) {
				output = Config.RecursionLimitMessage;
			} catch (LoopLimitException) {
				output = Config.LoopLimitMessage;
			}

			output = output.Trim();

			if (output.Length > 0) {
				if (builder.Length != 0) builder.Append(' ');
				builder.Append(output);
			}

			process.Finish();
		}

		duration = stopwatch.Elapsed;

		var response = new Response(request, builder.ToString());
		request.Response = response;
		return response;
	}

	public string[] SentenceSplit(string text, bool preserveMarks) {
		if (Config.Splitters.Length == 0) {
			var sentence = text.Trim();
			return sentence != "" ? [text.Trim()] : [];
		}

		int sentenceStart = 0, searchFrom = 0;
		var sentences = new List<string>();

		while (true) {
			string sentence;
			var pos2 = text.IndexOfAny(Config.Splitters, searchFrom);
			if (pos2 < 0) {
				sentence = text[sentenceStart..].Trim();
				if (sentence != "") sentences.Add(sentence);
				break;
			}
			if (pos2 < text.Length - 1 && !char.IsWhiteSpace(text[pos2 + 1]) && text[pos2 + 1] != '<') {
				// The sentence splitter must not be immediately followed by anything other than whitespace or an XML tag.
				searchFrom = pos2 + 1;
				continue;
			}
			sentence = text.Substring(sentenceStart, pos2 - sentenceStart + (preserveMarks ? 1 : 0)).Trim();
			if (sentence != "") sentences.Add(sentence);
			sentenceStart = pos2 + 1;
			searchFrom = sentenceStart;
		}

		return [.. sentences];
	}

	public string GetProperty(string predicate) => Config.BotProperties.GetValueOrDefault(predicate, Config.DefaultPredicate);

	public string Normalize(string text) {
		text = Config.NormalSubstitutions.Apply(text);
		// Strip sentence delimiters from the end when normalising (from Pandorabots).
		for (var i = text.Length - 1; i >= 0; --i) {
			if (!Config.Splitters.Contains(text[i])) return text[..(i + 1)];
		}
		return text;
	}

	public string Denormalize(string text) => Config.DenormalSubstitutions.Apply(text);

	private int CalculateVocabulary() {
		static void TraversePatternNode(ICollection<string> words, PatternNode node) {
			foreach (var e in node.Children) {
				if (e.Key is not ("_" or "#" or "*" or "^" or "<that>" or "<topic>"))
					words.Add(e.Key.TrimStart('$'));
				TraversePatternNode(words, e.Value);
			}
		}

		var words = new HashSet<string>(Config.StringComparer);
		TraversePatternNode(words, Graphmaster);
		foreach (var set in Sets.Values) {
			switch (set) {
				case StringSet stringSet:
					foreach (var entry in stringSet) {
						words.UnionWith(entry.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries));
					}
					break;
				case MapSet mapSet:
					if (mapSet.Map is not StringMap stringMap) continue;
					foreach (var entry in stringMap.Keys) {
						words.UnionWith(entry.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries));
					}
					break;
			}
		}

		return words.Count;
	}

	internal void InvalidateVocabulary() => vocabulary = null;

#if NET8_0_OR_GREATER
	[GeneratedRegex(@"\\([\\#])|#.*")]
	private static partial Regex MapCommentRegex();
#else
	private static readonly Regex mapCommentRegex = new(@"\\([\\#])|#.*", RegexOptions.Compiled);
	private static Regex MapCommentRegex() => mapCommentRegex;
#endif

	#region Log templates

	[LoggerMessage(LogLevel.Information, "Loading sets from {Directory}.")]
	private partial void LogLoadingSets(string directory);

	[LoggerMessage(LogLevel.Warning, "Duplicate set name '{SetName}'.")]
	private partial void LogDuplicateSetName(string setName);

	[LoggerMessage(LogLevel.Information, "Loaded {Count} set(s).")]
	private partial void LogLoadedSets(int count);

	[LoggerMessage(LogLevel.Information, "Loading maps from {Directory}.")]
	private partial void LogLoadingMaps(string directory);

	[LoggerMessage(LogLevel.Warning, "Duplicate map name '{MapName}'.")]
	private partial void LogDuplicateMapName(string mapName);

	[LoggerMessage(LogLevel.Warning, "Map '{MapName}' contains a badly formatted line: {Line}.")]
	private partial void LogMapSyntaxError(string mapName, string line);

	[LoggerMessage(LogLevel.Warning, "Map '{MapName}' contains duplicate key '{Key}'.")]
	private partial void LogMapDuplicateKey(string mapName, string key);

	[LoggerMessage(LogLevel.Information, "Loaded {Count} map(s).")]
	private partial void LogLoadedMaps(int count);

	[LoggerMessage(LogLevel.Information, "Triples file ({FilePath}) was not found.")]
	private partial void LogTriplesFileNotFound(string filePath);

	[LoggerMessage(LogLevel.Information, "Loading triples from {FilePath}.")]
	private partial void LogLoadingTriples(string filePath);

	[LoggerMessage(LogLevel.Warning, "Triples file contains a badly formatted line: {Line}.")]
	private partial void LogTriplesSyntaxError(string line);

	[LoggerMessage(LogLevel.Information, "Loaded {Count} triple(s).")]
	private partial void LogLoadedTriples(int count);

	[LoggerMessage(LogLevel.Warning, "Gossip from {UserId}: {Message}")]
	private partial void LogGossip(string userId, string message);

	[LoggerMessage(LogLevel.Information, "{UserId}: {Message}")]
	private partial void LogChatRequest(string userId, string message);

	[LoggerMessage(LogLevel.Information, "{UserId} [Postback]: {Message}")]
	private partial void LogChatRequestPostback(string userId, string message);

	[LoggerMessage(LogLevel.Information, "{BotName} -> {UserId}: {Message}")]
	private partial void LogChatResponse(string botName, string userId, string message);

	[LoggerMessage(LogLevel.Trace, "Normalized path: {Message} <THAT> {That} <TOPIC> {Topic}")]
	private partial void LogNormalizedPath(string message, string that, string topic);

	[LoggerMessage(LogLevel.Trace, "Input matched user-specific category '{Path}'.")]
	private partial void LogMatchUserSpecificCategory(string path);

	[LoggerMessage(LogLevel.Trace, "Input matched category '{Path}' in {Uri} line {LineNumber}.")]
	private partial void LogMatchCategory(string path, string? uri, int lineNumber);

	[LoggerMessage(LogLevel.Warning, "No match for input {Message} <THAT> {That} <TOPIC> {Topic}")]
	private partial void LogNoMatch(string message, string that, string topic);

	#endregion
}
