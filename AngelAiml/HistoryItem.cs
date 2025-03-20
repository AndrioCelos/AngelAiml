using System.Collections.ObjectModel;

namespace AngelAiml;
/// <summary>Represents an item in the bot's request or response history, containing one or more sentences.</summary>
public class HistoryItem {
	public string Text { get; }
	public IReadOnlyList<string> Sentences { get; }

	internal HistoryItem(string text, IList<string> sentences) {
		Text = text;
		Sentences = new ReadOnlyCollection<string>(sentences);
	}
}
