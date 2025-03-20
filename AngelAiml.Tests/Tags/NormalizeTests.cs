using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class NormalizeTests {
	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		test.Bot.Config.NormalSubstitutions.Add(new(" foo ", " bar "));
		var tag = new Normalize(new("foo"));
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("bar"));
	}
}
