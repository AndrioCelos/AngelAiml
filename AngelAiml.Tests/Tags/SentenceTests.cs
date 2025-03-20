using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class SentenceTests {
	[Test]
	public void Evaluate() {
		var tag = new Sentence(new("hello WORLD says I."));
		Assert.AreEqual("Hello WORLD says I.", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}
}
