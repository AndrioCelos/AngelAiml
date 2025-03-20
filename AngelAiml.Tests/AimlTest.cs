using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace AngelAiml.Tests;
/// <summary>Creates a mock setup for AIML tests.</summary>
internal class AimlTest {
	public Bot Bot { get; }
	public User User { get; }
	public RequestProcess RequestProcess { get; private init; }

	public string SampleRequestSentenceText { [MemberNotNull(nameof(RequestProcess))] init => RequestProcess = new(new(new(value, User, Bot), value), 0, false); }

	internal bool expectingWarning;

	internal ILoggerFactory GetLoggerFactory() => LoggerFactory.Create(builder => builder.AddConsole().AddProvider(new FailOnWarningLoggerProvider(this)));

	/// <summary>Initialises a new <see cref="AimlTest"/> with a new bot with the default settings.</summary>
	public AimlTest() {
		Bot = new(GetLoggerFactory());
		User = new("tester", Bot);
		SampleRequestSentenceText = "TEST";
	}
	/// <summary>Initialises a new <see cref="AimlTest"/> from the specified <see cref="Bot"/>.</summary>
	public AimlTest(string botPath) {
		Bot = new(botPath, GetLoggerFactory());
		User = new("tester", Bot);
		SampleRequestSentenceText = "TEST";
	}
	/// <summary>Initialises a new <see cref="AimlTest"/> using the specified <see cref="Random"/>.</summary>
	public AimlTest(Random random) {
		Bot = new(random, GetLoggerFactory());
		User = new("tester", Bot);
		SampleRequestSentenceText = "TEST";
	}

	/// <summary>Asserts that the specified method causes a warning message to be logged.</summary>
	public void AssertWarning(Action action) {
		expectingWarning = true;
		action();
		if (expectingWarning)
			Assert.Fail("Expected warning was not raised.");
	}
	/// <summary>Asserts that the specified function causes a warning message to be logged.</summary>
	/// <returns>The return value of <paramref name="f"/>.</returns>
	public TResult AssertWarning<TResult>(Func<TResult> f) {
		expectingWarning = true;
		var result = f();
		if (expectingWarning)
			Assert.Fail("Expected warning was not raised.");
		return result;
	}

	internal static Template GetTemplate(PatternNode root, params string[] pathTokens) {
		var node = root;
		foreach (var token in pathTokens) {
			if (!node.Children.TryGetValue(token, out node)) {
				Assert.Fail($"Node '{token}' was not found.");
				throw new KeyNotFoundException("No match");
			}
		}
		if (node.Template is not null) return node.Template;
		Assert.Fail($"Node '{string.Join(' ', pathTokens)} is not a leaf.");
		throw new KeyNotFoundException("No match");
	}
}
