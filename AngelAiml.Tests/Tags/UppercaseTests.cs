using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class UppercaseTests {
	[Test]
	public void Evaluate() {
		var tag = new Uppercase(new("hello WORLD says I."));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess).ToString(), Is.EqualTo("HELLO WORLD SAYS I."));
	}
}
