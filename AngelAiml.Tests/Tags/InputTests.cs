using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class InputTests {
	[Test]
	public void ParseWithIndex() {
		var tag = new Input(new("2"));
		Assert.That(tag.Index?.ToString(), Is.EqualTo("2"));
	}

	[Test]
	public void ParseWithDefault() {
		var tag = new Input(null);
		Assert.That(tag.Index, Is.Null);
	}

	[Test]
	public void EvaluateWithIndex() {
		var test = new AimlTest();
		test.User.Requests.Add(new("Hello world. This is a test.", test.User, test.Bot));
		test.User.Requests.Add(new("Hello again.", test.User, test.Bot));

		var tag = new Input(new("2"));
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("This is a test"));
	}

	[Test]
	public void EvaluateWithDefault() {
		var test = new AimlTest();
		test.User.Requests.Add(new("Hello world. This is a test.", test.User, test.Bot));
		test.User.Requests.Add(new("Hello again.", test.User, test.Bot));

		var tag = new Input(null);
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("Hello again"));
	}
}
