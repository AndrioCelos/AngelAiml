using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace AngelAiml;
/// <summary>Represents a node in the AIML category tree.</summary>
public partial class PatternNode {
	internal readonly Dictionary<string, PatternNode> children;
	/// <summary>Returns a dictionary mapping words and wildcards to the corresponding child nodes.</summary>
	public IReadOnlyDictionary<string, PatternNode> Children { get; }
	internal readonly List<SetChild> setChildren;
	/// <summary>Returns a list of child nodes from <c>set</c> tags.</summary>
	public IReadOnlyList<SetChild> SetChildren { get; }
	/// <summary>Returns the template associated with this node's path, or null if none exists.</summary>
	public Template? Template { get; internal set; }

	public PatternNode(IEqualityComparer<string> comparer) : this(null, comparer) { }
	public PatternNode(Template? template, IEqualityComparer<string> comparer) {
		children = new Dictionary<string, PatternNode>(comparer);
		Children = new ReadOnlyDictionary<string, PatternNode>(children);
		setChildren = [];
		SetChildren = setChildren.AsReadOnly();
		Template = template;
	}

	public void AddChild(IEnumerable<PathToken> path, Template template)
		=> AddChild(path, template, out _);
	public void AddChild(IEnumerable<PathToken> path, Template template, out Template? existingTemplate) {
		var node = this;
		foreach (var token in path ?? throw new ArgumentNullException(nameof(path))) {
			node = node.GetOrAddChild(token);
		}
		existingTemplate = node.Template;
		node.Template ??= template ?? throw new ArgumentNullException(nameof(template));
	}

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
	public bool TryGetChild(PathToken token, [MaybeNullWhen(false)] out PatternNode node) {
#else
	public bool TryGetChild(PathToken token, out PatternNode node) {
#endif
		if (token.IsSet) {
			var child = SetChildren.FirstOrDefault(c => c.SetName == token.Text);
			if (child == null) {
				node = null!;
				return false;
			}
			node = child.Node;
			return true;
		} else
			return Children.TryGetValue(token.Text, out node);
	}
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
	public bool TryGetChild(IEnumerable<PathToken> path, [MaybeNullWhen(false)] out PatternNode node) {
#else
	public bool TryGetChild(IEnumerable<PathToken> path, out PatternNode node) {
#endif
		node = this;
		foreach (var token in path ?? throw new ArgumentNullException(nameof(path))) {
			if (!node.TryGetChild(token, out node)) return false;
		}
		return true;
	}

	public PatternNode GetOrAddChild(PathToken token) {
		if (token.IsSet) {
			var child = SetChildren.FirstOrDefault(c => c.SetName == token.Text);
			if (child == null) {
				var node = new PatternNode(children.Comparer);
				setChildren.Add(new SetChild(token.Text, node));
				return node;
			}
			return child.Node;
		}
		else {
			if (Children.TryGetValue(token.Text, out var node)) return node;
			node = new PatternNode(children.Comparer);
			children.Add(token.Text, node);
			return node;
		}
	}

	public Template? Search(RequestSentence sentence, RequestProcess process, string that, bool traceSearch) {
		if (process.RecursionDepth > sentence.Bot.Config.RecursionLimit) {
			LogRecursionLimitExceeded(process.Bot.GetLogger(typeof(PatternNode)), sentence.Request.User.ID, sentence.Request.Text);
			throw new RecursionLimitException();
		}

		// Generate the input path.
		var messageSplit = sentence.Text.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries);
		var thatSplit = that.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries);
		var topicSplit = sentence.Bot.Normalize(sentence.User.Topic).Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries);

		var inputPath = new string[messageSplit.Length + thatSplit.Length + topicSplit.Length + 2];
		var i = 0;
		messageSplit.CopyTo(inputPath, 0);
		i += messageSplit.Length;
		inputPath[i++] = "<that>";
		thatSplit.CopyTo(inputPath, i);
		i += thatSplit.Length;
		inputPath[i++] = "<topic>";
		topicSplit.CopyTo(inputPath, i);
		if (traceSearch)
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
			LogNormalizedPath(process.Bot.GetLogger(typeof(PatternNode)), string.Join(' ', inputPath));
#else
			LogNormalizedPath(process.Bot.GetLogger(typeof(PatternNode)), string.Join(" ", inputPath));
#endif

		var result = Search(sentence, process, inputPath, 0, traceSearch, MatchState.Message);
		return result;
	}
	private Template? Search(RequestSentence sentence, RequestProcess process, string[] inputPath, int inputPathIndex, bool traceSearch, MatchState matchState) {
		if (traceSearch)
			LogSearch(process.Bot.GetLogger(typeof(PatternNode)), process.Path);

		var pathDepth = process.patternPathTokens.Count;

		if (process.CheckTimeout()) {
			LogRequestTimeout(process.Bot.GetLogger(typeof(PatternNode)), sentence.Request.User.ID, sentence.Request.Text);
			throw new TimeoutException();
		}

		bool tokensRemaining;
		if (inputPathIndex >= inputPath.Length) {
			// No tokens remaining in the input path. If this node has a template, return success.
			if (Template != null) return Template;
			// Otherwise, look for zero+ wildcards.
			tokensRemaining = false;
		} else
			tokensRemaining = true;

		// Reserve a space in the pattern path list here. This is so that further recursive calls will leave it alone.
		// If we find a template, we replace the placeholder with the correct token.
		process.patternPathTokens.Add("?");

		// Search for child nodes that match the input in priority order.
		// Priority exact match.
		if (tokensRemaining && children.TryGetValue("$" + inputPath[inputPathIndex], out var node)) {
			process.patternPathTokens[pathDepth] = "$" + inputPath[inputPathIndex];
			var result = node.Search(sentence, process, inputPath, inputPathIndex + 1, traceSearch, matchState);
			if (result != null) return result;
		}

		// Priority zero+ wildcard.
		if (children.TryGetValue("#", out node)) {
			process.patternPathTokens[pathDepth] = "#";
			var result = node.WildcardSearch(sentence, process, inputPath, inputPathIndex, traceSearch, matchState, 0);
			if (result != null) return result;
		}

		// Priority one+ wildcard.
		if (children.TryGetValue("_", out node)) {
			process.patternPathTokens[pathDepth] = "_";
			var result = node.WildcardSearch(sentence, process, inputPath, inputPathIndex, traceSearch, matchState, 1);
			if (result != null) return result;
		}

		// Exact match.
		if (tokensRemaining && children.TryGetValue(inputPath[inputPathIndex], out node)) {
			// matchState must only be advanced now so that wildcards capturing zero words works correctly.
			switch (matchState) {
				case MatchState.Message:
					if (inputPath[inputPathIndex] == "<that>") matchState = MatchState.That;
					break;
				case MatchState.That:
					if (inputPath[inputPathIndex] == "<topic>") matchState = MatchState.Topic;
					break;
			}
			process.patternPathTokens[pathDepth] = inputPath[inputPathIndex];
			var result = node.Search(sentence, process, inputPath, inputPathIndex + 1, traceSearch, matchState);
			if (result != null) return result;
		}

		// Sets. (The empty string cannot be matched by a set token.)
		if (tokensRemaining) {
			foreach (var child in setChildren) {
				process.patternPathTokens[pathDepth] = $"<set>{child.SetName}</set>";
				if (!sentence.Bot.Sets.TryGetValue(child.SetName, out var set)) {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
					LogMissingSet(process.Bot.GetLogger(typeof(PatternNode)), string.Join(' ', process.patternPathTokens));
#else
					LogMissingSet(process.Bot.GetLogger(typeof(PatternNode)), string.Join(" ", process.patternPathTokens));
#endif
					continue;
				}
				var star = process.GetStarList(matchState);
				var starIndex = star.Count;
				star.Add("");  // Reserving a space; see above.

				// Find the maximum number of words that this can match.
				var endIndex = inputPathIndex;
				for (var i = inputPathIndex + 1; i < inputPath.Length; i++) {
					if (i - inputPathIndex > set.MaxWords
						|| (matchState == MatchState.Message && inputPath[i - 1] == "<that>")
						|| (matchState == MatchState.That && inputPath[i - 1] == "<topic>"))
						break;
					endIndex = i;
				}

				// Set tags work similarly to wildcards, but are greedy.
				for (; endIndex >= inputPathIndex; endIndex--) {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
					var phrase = string.Join(' ', inputPath, inputPathIndex, endIndex - inputPathIndex);
#else
					var phrase = string.Join(" ", inputPath, inputPathIndex, endIndex - inputPathIndex);
#endif
					if (set.Contains(phrase)) {
						// Phrase found in the set. Now continue with the tree search.
						var result = child.Node.Search(sentence, process, inputPath, endIndex, traceSearch, matchState);
						if (result != null) {
							star[starIndex] = phrase;
							return result;
						}
					}
				}

				// No match; release the reserved space.
				star.RemoveAt(starIndex);
				Debug.Assert(star.Count == starIndex);
			}
		}

		// Zero+ wildcard.
		if (children.TryGetValue("^", out node)) {
			process.patternPathTokens[pathDepth] = "^";
			var result = node.WildcardSearch(sentence, process, inputPath, inputPathIndex, traceSearch, matchState, 0);
			if (result != null) return result;
		}

		// One+ wildcard.
		if (children.TryGetValue("*", out node)) {
			process.patternPathTokens[pathDepth] = "*";
			var result = node.WildcardSearch(sentence, process, inputPath, inputPathIndex, traceSearch, matchState, 1);
			if (result != null) return result;
		}

		// No match.
		process.patternPathTokens.RemoveAt(pathDepth);
		Debug.Assert(process.patternPathTokens.Count == pathDepth);
		return null;
	}

	/// <summary>Handles a wildcard node by taking words one by one until a template is found.</summary>
	private Template? WildcardSearch(RequestSentence subRequest, RequestProcess process, string[] inputPath, int startIndex, bool traceSearch, MatchState matchState, int minimumWords) {
		int endIndex;
		var star = process.GetStarList(matchState);
		var starIndex = star.Count;
		// Reserve a space in the star list. If a template is found, this slot will be filled with the matched phrase.
		// This function can call other wildcards recursively. The reservation ensures that the star list will be populated correctly.
		star.Add("");

		for (endIndex = startIndex + minimumWords; endIndex <= inputPath.Length; endIndex++) {
			var result = Search(subRequest, process, inputPath, endIndex, traceSearch, matchState);
			if (result != null) {
				star[starIndex] = endIndex == startIndex
					? process.Bot.Config.DefaultWildcard
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
					: string.Join(' ', inputPath, startIndex, endIndex - startIndex);
#else
					: string.Join(" ", inputPath, startIndex, endIndex - startIndex);
#endif
				return result;
			}
			if (endIndex >= inputPath.Length || (matchState == MatchState.Message && inputPath[endIndex] == "<that>") || (matchState == MatchState.That && inputPath[endIndex] == "<topic>"))
				break;  // Wildcards cannot match these tokens.
		}

		// No match; remove the reserved slot.
		star.RemoveAt(starIndex);
		Debug.Assert(star.Count == starIndex);
		return null;
	}

	/// <summary>Returns an enumerable that enumerates all templates of this <see cref="PatternNode"/> and its children.</summary>
	/// <seealso cref="Template"/>
	public IEnumerable<KeyValuePair<string, Template>> GetTemplates() {
		if (Template != null) yield return new KeyValuePair<string, Template>("", Template);

		foreach (var child in Children) {
			foreach (var entry in child.Value.GetTemplates()) {
				yield return new KeyValuePair<string, Template>(entry.Key == "" ? child.Key : child.Key + " " + entry.Key, entry.Value);
			}
		}

		foreach (var child in setChildren) {
			var key = "<set>" + child.SetName + "</set>";
			foreach (var entry in child.Node.GetTemplates()) {
				yield return new KeyValuePair<string, Template>(entry.Key == "" ? key : key + " " + entry.Key, entry.Value);
			}
		}
	}

	/// <summary>Represents a child node with a set tag.</summary>
	public class SetChild(string setName, PatternNode node) {
		public string SetName { get; } = setName;
		public PatternNode Node { get; } = node;
	}

	#region Log templates

	[LoggerMessage(LogLevel.Warning, "Recursion limit exceeded. User: {UserId}; raw input: \"{Input}\"")]
	private static partial void LogRecursionLimitExceeded(ILogger logger, string userId, string input);

	[LoggerMessage(LogLevel.Trace, "Normalized path: {Path}")]
	private static partial void LogNormalizedPath(ILogger logger, string path);

	[LoggerMessage(LogLevel.Trace, "Search: {Path}")]
	private static partial void LogSearch(ILogger logger, string path);

	[LoggerMessage(LogLevel.Warning, "Request timeout. User: {UserId}; raw input: \"{Input}\"")]
	private static partial void LogRequestTimeout(ILogger logger, string userId, string input);

	[LoggerMessage(LogLevel.Warning, "Reference to a missing set in pattern path '{Path}'.")]
	private static partial void LogMissingSet(ILogger logger, string path);

	#endregion
}
