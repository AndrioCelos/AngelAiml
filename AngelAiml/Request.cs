namespace AngelAiml;
public class Request {
	public string Text { get; private set; }
	public User User { get; private set; }
	public Bot Bot { get; private set; }
	public Response? Response { get; internal set; }
	private readonly RequestSentence[] sentences;
	public IReadOnlyList<RequestSentence> Sentences { get; }

	public Request(string text, User user, Bot bot) {
		Text = text;
		User = user;
		Bot = bot;

		var sentences = bot.SentenceSplit(text, false);
		this.sentences = new RequestSentence[sentences.Length];
		for (var i = 0; i < sentences.Length; ++i) {
			this.sentences[i] = new RequestSentence(this, sentences[i]);
		}
		Sentences = Array.AsReadOnly(this.sentences);
	}

	public RequestSentence GetLastSentence(int n) => sentences[^n];
}

public class RequestSentence(Request request, string text) {
	public Request Request { get; } = request;
	public Bot Bot => Request.Bot;
	public User User => Request.User;
	public string Text { get; } = request.Bot.Normalize(text);
}
