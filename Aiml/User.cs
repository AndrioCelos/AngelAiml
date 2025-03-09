namespace Aiml;
public class User {
	public string ID { get; }
	public Bot Bot { get; }
	public History<Request> Requests { get; }
	public History<Response> Responses { get; }
	public Dictionary<string, string> Predicates { get; }
	public PatternNode Graphmaster { get; }

	public string That { get; internal set; }

	public string Topic {
		get => GetPredicate("topic");
		set => Predicates["topic"] = value;
	}

	public User(string ID, Bot bot) {
		if (string.IsNullOrEmpty(ID)) throw new ArgumentException("The user ID cannot be empty", nameof(ID));
		this.ID = ID;
		Bot = bot;
		That = bot.Config.DefaultHistory;
		Requests = new History<Request>(bot.Config.HistorySize);
		Responses = new History<Response>(bot.Config.HistorySize);
		Predicates = new Dictionary<string, string>(StringComparer.Create(bot.Config.Locale, true));
		Graphmaster = new PatternNode(null, bot.Config.StringComparer);
	}

	/// <summary>Returns the last sentence output from the bot to this user.</summary>
	public string GetThat() => That;
	/// <summary>Returns the last sentence in the <paramref name='n'/>th last message from the bot to this user.</summary>
	public string GetThat(int n) => GetThat(n, 1);
	/// <summary>Returns the <paramref name='n'/>th last sentence in the <paramref name='n'/>th last message from the bot to this user.</summary>
	public string GetThat(int n, int sentence)
		=> n >= 1 && n <= Responses.Count && sentence >= 1 && Responses[n - 1] is var response && sentence <= Responses[n - 1].Sentences.Count
			? response.GetLastSentence(sentence)
			: Bot.Config.DefaultHistory;

	public string GetInput() => GetInput(1, 1);
	public string GetInput(int n) => GetInput(n, 1);
	public string GetInput(int n, int sentence)
		=> n >= 1 && n <= Requests.Count && sentence >= 1 && Requests[n - 1] is var response && sentence <= response.Sentences.Count
			? response.GetLastSentence(sentence).Text
			: Bot.Config.DefaultHistory;

	public string GetRequest() => GetRequest(1);
	public string GetRequest(int n) => n >= 1 & n <= Requests.Count ? Requests[n].Text : Bot.Config.DefaultHistory;
	// Unlike <input>, the <request> tag does not count the request currently being processed.

	public string GetResponse() => GetResponse(1);
	public string GetResponse(int n) => n >= 1 & n <= Responses.Count
		? Responses[n - 1].ToString()
		: Bot.Config.DefaultHistory;

	public void AddResponse(Response response) {
		Responses.Add(response);
		var that = Bot.SentenceSplit(response.Text, false).Select(Bot.Normalize).LastOrDefault(s => !string.IsNullOrWhiteSpace(s));
		if (that is not null)
			That = that;
		else if (Bot.Config.ThatExcludeEmptyResponse)
			That = that ?? Bot.Config.DefaultPredicate;
	}
	public void AddRequest(Request request) => Requests.Add(request);

	public string GetPredicate(string key)
		=> Predicates.TryGetValue(key, out var value) || Bot.Config.DefaultPredicates.TryGetValue(key, out value)
			? value
			: Bot.Config.DefaultPredicate;

	public Response Postback(string text) {
		var request = new Request(text, this, Bot);
		Bot.OnPostbackRequest(new(request));
		var response = Bot.Chat(request, false, true);
		Bot.OnPostbackResponse(new(response));
		return response;
	}
}
