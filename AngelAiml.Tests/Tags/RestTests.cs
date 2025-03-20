using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class RestTests {
	[Test]
	public void EvaluateWithZeroWords() {
		var tag = new Rest(new(""));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess).ToString(), Is.EqualTo("nil"));
	}

	[Test]
	public void EvaluateWithOneWord() {
		var tag = new Rest(new("1"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess).ToString(), Is.EqualTo("nil"));
	}

	[Test]
	public void EvaluateWithMultipleWords() {
		var tag = new Rest(new("1 2 3"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess).ToString(), Is.EqualTo("2 3"));
	}
}
