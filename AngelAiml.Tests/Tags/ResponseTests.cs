using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class ResponseTests {
	[Test]
	public void Parse_Index() {
		var tag = new AngelAiml.Tags.Response(new("2"));
		Assert.AreEqual("2", tag.Index?.ToString());
	}

	[Test]
	public void Parse_Default() {
		var tag = new AngelAiml.Tags.Response(null);
		Assert.IsNull(tag.Index);
	}

	[Test]
	public void Evaluate_Index() {
		var test = new AimlTest();
		test.User.Responses.Add(new(new("", test.User, test.Bot), "Hello world. This is a test."));
		test.User.Responses.Add(new(new("", test.User, test.Bot), "Hello again."));

		var tag = new AngelAiml.Tags.Response(new("2"));
		Assert.AreEqual("Hello world. This is a test.", tag.Evaluate(test.RequestProcess).ToString());
	}

	[Test]
	public void Evaluate_Default() {
		var test = new AimlTest();
		test.User.Responses.Add(new(new("", test.User, test.Bot), "Hello world. This is a test."));
		test.User.Responses.Add(new(new("", test.User, test.Bot), "Hello again."));

		var tag = new AngelAiml.Tags.Response(null);
		Assert.AreEqual("Hello again.", tag.Evaluate(test.RequestProcess).ToString());
	}

	[Test]
	public void Evaluate_IndexOutOfRange() {
		var test = new AimlTest();
		var tag = new AngelAiml.Tags.Response(null);
		Assert.AreEqual(test.Bot.Config.DefaultHistory, tag.Evaluate(test.RequestProcess).ToString());
	}

	[Test]
	public void Evaluate_InvalidIndex() {
		var test = new AimlTest();
		var tag = new AngelAiml.Tags.Response(new("foo"));
		Assert.AreEqual(test.Bot.Config.DefaultHistory, test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()));
	}
}
