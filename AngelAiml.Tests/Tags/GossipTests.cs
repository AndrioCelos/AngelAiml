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
		Assert.Multiple(() => {
			Assert.That(tag.Evaluate(test.RequestProcess), Is.EqualTo("Hello, world!"));
			Assert.That(gossipMessage, Is.EqualTo("Hello, world!"));
		});
	}
}
