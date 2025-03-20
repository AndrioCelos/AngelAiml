using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class UniqTests {
	private static AimlTest GetTest() {
		var test = new AimlTest();
		test.Bot.Triples.Add("A", "r", "M");
		test.Bot.Triples.Add("A", "r", "N");
		test.Bot.Triples.Add("A", "r", "O");
		test.Bot.Triples.Add("N", "r", "X");
		test.Bot.Triples.Add("O", "r", "X");
		test.Bot.Triples.Add("O", "r", "Y");
		test.Bot.Triples.Add("M", "attr", "1");
		test.Bot.Triples.Add("N", "attr", "0");
		return test;
	}

	[Test]
	public void Parse() {
		var tag = new Uniq(new("M"), new("attr"), new("?"));
		Assert.Multiple(() => {
			Assert.That(tag.Subject.ToString(), Is.EqualTo("M"));
			Assert.That(tag.Predicate.ToString(), Is.EqualTo("attr"));
			Assert.That(tag.Object.ToString(), Is.EqualTo("?"));
		});
	}

	[Test]
	public void Evaluate_Object() {
		var tag = new Uniq(new("M"), new("attr"), new("?"));
		Assert.That(tag.Evaluate(GetTest().RequestProcess).ToString(), Is.EqualTo("1"));
	}

	[Test]
	public void Evaluate_Subject() {
		var tag = new Uniq(new("?"), new("r"), new("M"));
		Assert.That(tag.Evaluate(GetTest().RequestProcess).ToString(), Is.EqualTo("A"));
	}

	[Test]
	public void Evaluate_NoVariable() {
		var test = GetTest();
		var tag = new Uniq(new("M"), new("attr"), new("1"));
		Assert.That(test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()), Is.EqualTo(test.Bot.Config.DefaultTriple));
	}

	[Test]
	public void Evaluate_MultipleVariables() {
		var test = GetTest();
		var tag = new Uniq(new("?"), new("attr"), new("?"));
		Assert.That(test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()), Is.AnyOf(["0", "1"]));
	}
}
