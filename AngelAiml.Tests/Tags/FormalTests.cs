using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class FormalTests {
	[Test]
	public void Evaluate() {
		var tag = new Formal(new("hello WORLD says I."));
		Assert.AreEqual("Hello World Says I.", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}
}
