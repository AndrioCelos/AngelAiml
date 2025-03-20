using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class ResponseTests {
	[Test]
	public void Parse_Index() {
		var tag = new AngelAiml.Tags.Response(new("2"));
		Assert.That(tag.Index?.ToString(), Is.EqualTo("2"));
	}

	[Test]
	public void Parse_Default() {
		var tag = new AngelAiml.Tags.Response(null);
		Assert.That(tag.Index, Is.Null);
	}

	[Test]
	public void Evaluate_Index() {
		var test = new AimlTest();
		test.User.Responses.Add(new(new("", test.User, test.Bot), "Hello world. This is a test."));
		test.User.Responses.Add(new(new("", test.User, test.Bot), "Hello again."));

		var tag = new AngelAiml.Tags.Response(new("2"));
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("Hello world. This is a test."));
	}

	[Test]
	public void Evaluate_Default() {
		var test = new AimlTest();
		test.User.Responses.Add(new(new("", test.User, test.Bot), "Hello world. This is a test."));
		test.User.Responses.Add(new(new("", test.User, test.Bot), "Hello again."));

		var tag = new AngelAiml.Tags.Response(null);
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("Hello again."));
	}

	[Test]
	public void Evaluate_IndexOutOfRange() {
		var test = new AimlTest();
		var tag = new AngelAiml.Tags.Response(null);
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo(test.Bot.Config.DefaultHistory));
	}

	[Test]
	public void Evaluate_InvalidIndex() {
		var test = new AimlTest();
		var tag = new AngelAiml.Tags.Response(new("foo"));
		Assert.That(test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()), Is.EqualTo(test.Bot.Config.DefaultHistory));
	}
}
