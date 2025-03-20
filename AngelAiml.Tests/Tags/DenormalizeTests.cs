using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class DenormalizeTests {
	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		test.Bot.Config.DenormalSubstitutions.Add(new(" foo ", " bar "));
		var tag = new Denormalize(new("foo"));
		Assert.AreEqual("bar", tag.Evaluate(test.RequestProcess).ToString());
	}
}
