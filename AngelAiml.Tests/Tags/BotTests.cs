using AngelAiml.Tags;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class BotTests {
	[Test]
	public void Initialise() {
		var tag = new AngelAiml.Tags.Bot(new("foo"));
		Assert.AreEqual("foo", tag.Name.ToString());
	}

	[Test]
	public void EvaluateWithBoundProperty() {
		var test = new AimlTest();
		test.Bot.Properties["foo"] = "test";

		var tag = new AngelAiml.Tags.Bot(new("foo"));
		Assert.AreEqual("test", tag.Evaluate(test.RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithUnboundProperty() {
		var test = new AimlTest();
		var tag = new AngelAiml.Tags.Bot(new("bar"));
		Assert.AreEqual("unknown", tag.Evaluate(test.RequestProcess).ToString());
	}
}
