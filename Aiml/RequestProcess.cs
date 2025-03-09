using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Aiml; 
/// <summary>Contains data that is used during request processing, but is not stored after the request completes.</summary>
public class RequestProcess {
	public RequestSentence Sentence { get; }
	public int RecursionDepth { get; }
	internal Template? template;
	internal readonly List<string> patternPathTokens = [];
	internal readonly List<string> star = [];
	internal readonly List<string> thatstar = [];
	internal readonly List<string> topicstar = [];
	internal Dictionary<string, TestResult>? testResults;
	public ReadOnlyDictionary<string, TestResult>? TestResults;
	public TimeSpan Duration => stopwatch.Elapsed;

	internal Stopwatch stopwatch = Stopwatch.StartNew();

	public Bot Bot => Sentence.Bot;
	public User User => Sentence.User;
	/// <summary>Returns a zero-indexed list of phrases matched by pattern wildcards.</summary>
	public IReadOnlyList<string> Star { get; }
	/// <summary>Returns a zero-indexed list of phrases matched by that pattern wildcards.</summary>
	public IReadOnlyList<string> ThatStar { get; }
	/// <summary>Returns a zero-indexed list of phrases matched by topic pattern wildcards.</summary>
	public IReadOnlyList<string> TopicStar { get; }

	/// <summary>Returns the full normalised path used to search the graph.</summary>
	public string Path => string.Join(" ", patternPathTokens);

	/// <summary>Returns the dictionary of local variables in this request.</summary>
	public Dictionary<string, string> Variables { get; }

	public RequestProcess(RequestSentence sentence, int recursionDepth, bool useTests) {
		Sentence = sentence;
		RecursionDepth = recursionDepth;
		Variables = new Dictionary<string, string>(sentence.Bot.Config.StringComparer);
		Star = star.AsReadOnly();
		ThatStar = thatstar.AsReadOnly();
		TopicStar = topicstar.AsReadOnly();
		if (useTests) {
			testResults = new Dictionary<string, TestResult>(sentence.Bot.Config.StringComparer);
			TestResults = new ReadOnlyDictionary<string, TestResult>(testResults);
		}
	}

	/// <summary>Returns the text matched by the pattern wildcard with the specified one-based index, or <see cref="Config.DefaultWildcard"/> if no such wildcard exists.</summary>
	public string GetStar(int num) => --num >= 0 && num < star.Count ? star[num] : Bot.Config.DefaultWildcard;
	/// <summary>Returns the text matched by the that pattern wildcard with the specified one-based index, or <see cref="Config.DefaultWildcard"/> if no such wildcard exists.</summary>
	public string GetThatStar(int num) => --num >= 0 && num < thatstar.Count ? thatstar[num] : Bot.Config.DefaultWildcard;
	/// <summary>Returns the text matched by the topic pattern wildcard with the specified one-based index, or <see cref="Config.DefaultWildcard"/> if no such wildcard exists.</summary>
	public string GetTopicStar(int num) => --num >= 0 && num < topicstar.Count ? topicstar[num] : Bot.Config.DefaultWildcard;

	internal void Finish() => stopwatch.Stop();

	internal bool CheckTimeout() => stopwatch.ElapsedMilliseconds >= Bot.Config.Timeout;

	internal List<string> GetStarList(MatchState matchState) => matchState switch {
		MatchState.Message => star,
		MatchState.That => thatstar,
		MatchState.Topic => topicstar,
		_ => throw new ArgumentException($"Invalid {nameof(MatchState)} value", nameof(matchState)),
	};

	/// <summary>Returns the value of the specified local variable for this request, or <see cref="Config.DefaultPredicate"/> if it is not bound.</summary>
	public string GetVariable(string name) => Variables.TryGetValue(name, out var value) ? value : Bot.Config.DefaultPredicate;

	/// <summary>Writes a message to the bot's loggers.</summary>
	public void Log(LogLevel level, string message) {
		if (level > LogLevel.Diagnostic || RecursionDepth < Bot.Config.LogRecursionLimit)
			Bot.Log(level, $"[{RecursionDepth}] {message}");
	}

	/// <summary>Processes the specified text as a sub-request of the current request and returns the response.</summary>
	public string Srai(string request) {
		var newRequest = new Request(request, User, Bot);
		return Bot.ProcessRequest(newRequest, false, false, RecursionDepth + 1, out _).ToString().Trim();
	}
}
