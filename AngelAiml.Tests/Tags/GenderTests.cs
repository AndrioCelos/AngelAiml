using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class GenderTests {
	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		test.Bot.Config.GenderSubstitutions.Add(new(" he ", " she "));
		test.Bot.Config.GenderSubstitutions.Add(new(" her ", " his "));
		var tag = new Gender(new("he is her friend"));
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("she is his friend"));
	}
}
