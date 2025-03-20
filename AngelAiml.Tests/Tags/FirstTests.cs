using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class FirstTests {
	[Test]
	public void EvaluateWithZeroWords() {
		var tag = new First(new(""));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess).ToString(), Is.EqualTo("nil"));
	}

	[Test]
	public void EvaluateWithOneWord() {
		var tag = new First(new("1"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess).ToString(), Is.EqualTo("1"));
	}

	[Test]
	public void EvaluateWithMultipleWords() {
		var tag = new First(new("1 2 3"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess).ToString(), Is.EqualTo("1"));
	}
}
