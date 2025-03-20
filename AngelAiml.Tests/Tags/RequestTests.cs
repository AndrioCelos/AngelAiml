using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class RequestTests {
	[Test]
	public void Parse_Index() {
		var tag = new AngelAiml.Tags.Request(new("2"));
		Assert.That(tag.Index?.ToString(), Is.EqualTo("2"));
	}

	[Test]
	public void Parse_Default() {
		var tag = new AngelAiml.Tags.Request(null);
		Assert.That(tag.Index, Is.Null);
	}

	[Test]
	public void Evaluate_Index() {
		var test = new AimlTest();
		test.User.Requests.Add(new("Hello world. This is a test.", test.User, test.Bot));
		test.User.Requests.Add(new("Hello again.", test.User, test.Bot));
		test.User.Requests.Add(new("current request", test.User, test.Bot));

		var tag = new AngelAiml.Tags.Request(new("2"));
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("Hello world. This is a test."));
	}

	[Test]
	public void Evaluate_Default() {
		var test = new AimlTest();
		test.User.Requests.Add(new("Hello world. This is a test.", test.User, test.Bot));
		test.User.Requests.Add(new("Hello again.", test.User, test.Bot));
		test.User.Requests.Add(new("current request", test.User, test.Bot));

		var tag = new AngelAiml.Tags.Request(null);
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("Hello again."));
	}

	[Test]
	public void Evaluate_IndexOutOfRange() {
		var test = new AimlTest();
		var tag = new AngelAiml.Tags.Request(null);
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo(test.Bot.Config.DefaultHistory));
	}

	[Test]
	public void Evaluate_InvalidIndex() {
		var test = new AimlTest();
		var tag = new AngelAiml.Tags.Request(new("foo"));
		Assert.That(test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()), Is.EqualTo(test.Bot.Config.DefaultHistory));
	}
}
