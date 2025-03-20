using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class GossipTests {
	[Test]
	public void Evaluate() {
		string? gossipMessage = null;
		var test = new AimlTest();
		test.Bot.Gossip += (_, e) => { gossipMessage = e.Message; e.Handled = true; };
		var tag = new Gossip(new("Hello, world!"));
		Assert.AreEqual("Hello, world!", tag.Evaluate(test.RequestProcess));
		Assert.AreEqual("Hello, world!", gossipMessage);
	}
}
