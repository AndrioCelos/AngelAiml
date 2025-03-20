using AngelAiml.Tags;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class BotTests {
	[Test]
	public void Initialise() {
		var tag = new AngelAiml.Tags.Bot(new("foo"));
		Assert.That(tag.Name.ToString(), Is.EqualTo("foo"));
	}

	[Test]
	public void EvaluateWithBoundProperty() {
		var test = new AimlTest();
		test.Bot.Properties["foo"] = "test";

		var tag = new AngelAiml.Tags.Bot(new("foo"));
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("test"));
	}

	[Test]
	public void EvaluateWithUnboundProperty() {
		var test = new AimlTest();
		var tag = new AngelAiml.Tags.Bot(new("bar"));
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("unknown"));
	}
}
