using AngelAiml.Media;

namespace AngelAiml.Tests;

[TestFixture]
public class ResponseTests {
	[Test]
	public void GetLastSentenceTest() {
		var subject = new Response(new AimlTest().RequestProcess.Sentence.Request, "Hello, world! This is a test.");
		Assert.Multiple(() => {
			Assert.That(subject.GetLastSentence(), Is.EqualTo("This is a test."));
			Assert.That(subject.GetLastSentence(1), Is.EqualTo("This is a test."));
			Assert.That(subject.GetLastSentence(2), Is.EqualTo("Hello, world!"));
		});
	}

	[Test]
	public void ToMessages_Button_PostbackTextOnly() {
		var subject = new Response(new AimlTest().RequestProcess.Sentence.Request, "Hello, world!<split/><list><item>This is a test.</item></list><button>Hello!</button>");
		var messages = subject.ToMessages();
		Assert.That(messages, Has.Length.EqualTo(2));

		Assert.That(messages[0].InlineElements, Has.Count.EqualTo(1));
		Assert.Multiple(() => {
			Assert.That(messages[0].InlineElements[0], Is.InstanceOf<MediaText>());
			Assert.That(((MediaText) messages[0].InlineElements[0]).Text, Is.EqualTo("Hello, world!"));
			Assert.That(messages[0].Separator, Is.InstanceOf<Split>());

			Assert.That(messages[1].InlineElements, Has.Count.EqualTo(1));
		});
		Assert.Multiple(() => {
			Assert.That(messages[1].InlineElements[0], Is.InstanceOf<Media.List>());
			Assert.That(messages[1].BlockElements, Has.Count.EqualTo(1));
		});
		Assert.Multiple(() => {
			Assert.That(messages[1].BlockElements[0], Is.InstanceOf<Button>());
			Assert.That(messages[1].Separator, Is.Null);
		});
	}

	[Test]
	public void ToMessages() {
		var subject = new Response(new AimlTest().RequestProcess.Sentence.Request, "Hello, world! <button>Hello!</button>");
		var messages = subject.ToMessages();
		Assert.That(messages[0].BlockElements[0], Is.InstanceOf<Button>());
		Assert.Multiple(() => {
			Assert.That(((Button) messages[0].BlockElements[0]).Text, Is.EqualTo("Hello!"));
			Assert.That(((Button) messages[0].BlockElements[0]).Postback, Is.EqualTo("Hello!"));
		});
		Assert.That(((Button) messages[0].BlockElements[0]).Url, Is.Null);
	}
}
