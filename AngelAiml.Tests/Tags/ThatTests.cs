using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class ThatTests {
	private static AimlTest GetTest() {
		var test = new AimlTest();
		test.Bot.Config.ThatExcludeEmptyResponse = true;
		test.User.That = "Hello again.";
		test.User.Responses.Add(new(new("", test.User, test.Bot), "Hello world. This is a test."));
		test.User.Responses.Add(new(new("", test.User, test.Bot), "Hello again."));
		test.User.Responses.Add(new(new("", test.User, test.Bot), ""));
		return test;
	}

	[Test]
	public void ParseWithIndex() {
		var tag = new That(new("1,2"));
		Assert.That(tag.Index?.ToString(), Is.EqualTo("1,2"));
	}

	[Test]
	public void ParseWithDefault() {
		var tag = new That(null);
		Assert.That(tag.Index, Is.Null);
	}

	[Test]
	public void EvaluateWithIndex() {
		var test = GetTest();
		Assert.Multiple(() => {
			Assert.That(new That(new("2,1")).Evaluate(test.RequestProcess).ToString(), Is.EqualTo("Hello again."));
			Assert.That(new That(new("3,1")).Evaluate(test.RequestProcess).ToString(), Is.EqualTo("This is a test."));
			Assert.That(new That(new("3,2")).Evaluate(test.RequestProcess).ToString(), Is.EqualTo("Hello world."));
		});
	}

	[Test]
	public void EvaluateWithWhitespace() {
		var test = GetTest();
		Assert.That(new That(new(" 2 ,\n\t1 ")).Evaluate(test.RequestProcess).ToString(), Is.EqualTo("Hello again."));
	}

	[Test]
	public void EvaluateWithResponseOutOfRange() {
		var test = GetTest();
		Assert.That(new That(new("4,1")).Evaluate(GetTest().RequestProcess).ToString(), Is.EqualTo(test.Bot.Config.DefaultHistory));
	}

	[Test]
	public void EvaluateWithSentenceOutOfRange() {
		var test = GetTest();
		Assert.That(new That(new("1,2")).Evaluate(GetTest().RequestProcess).ToString(), Is.EqualTo(test.Bot.Config.DefaultHistory));
	}

	[Test]
	public void EvaluateWithDefault() {
		Assert.That(new That(null).Evaluate(GetTest().RequestProcess).ToString(), Is.EqualTo("Hello again."));
	}

	[Test]
	public void EvaluateWithInvalidIndex() {
		var test = GetTest();
		Assert.That(test.AssertWarning(() => new That(new("1")).Evaluate(test.RequestProcess).ToString()), Is.EqualTo(test.Bot.Config.DefaultHistory));
	}

	[Test]
	public void EvaluateWithInvalidResponse() {
		var test = GetTest();
		Assert.That(test.AssertWarning(() => new That(new("0,1")).Evaluate(test.RequestProcess).ToString()), Is.EqualTo(test.Bot.Config.DefaultHistory));
	}

	[Test]
	public void EvaluateWithInvalidSentence() {
		var test = GetTest();
		Assert.That(test.AssertWarning(() => new That(new("1,0")).Evaluate(test.RequestProcess).ToString()), Is.EqualTo(test.Bot.Config.DefaultHistory));
	}
}
