using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class ExplodeTests {
	[Test]
	public void EvaluateWithSpaces() {
		var tag = new Explode(new("Hello world"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess).ToString(), Is.EqualTo("H e l l o w o r l d"));
	}

	[Test]
	public void EvaluateWithPunctuation() {
		var tag = new Explode(new("1.5"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess).ToString(), Is.EqualTo("1 5"));
	}
}
