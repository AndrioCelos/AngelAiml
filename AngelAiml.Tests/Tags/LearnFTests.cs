using System.Xml.Linq;
using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class LearnFTests {
	[Test]
	public void Parse() {
		var el = XElement.Parse("<learnf><category><pattern>TEST LEARNF</pattern><template>Original: <eval><input/></eval>; Current: <input/></template></category></learnf>");
		var tag = new LearnF(el);
		Assert.That(tag.Element, Is.SameAs(el));
	}

	[TestCase("<learnf/>", TestName = "Parse (no category)")]
	[TestCase("<learnf><category><template/></category></learnf>", TestName = "Parse (no pattern)")]
	[TestCase("<learnf><category><pattern>TEST</pattern></category></learnf>", TestName = "Parse (no template)")]
	[TestCase("<learnf><category><foo/></category></learnf>", TestName = "Parse (invalid category element)")]
	[TestCase("<learnf><pattern>TEST</pattern></learnf>", TestName = "Parse (invalid AIML element)")]
	public void ParseInvalid(string xml) {
		Assert.Throws<ArgumentException>(() => new LearnF(XElement.Parse(xml)));
	}

	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		var tag = new LearnF(XElement.Parse("<learnf><category><pattern>TEST LEARNF</pattern><template>Original: <eval><input/></eval>; Current: <input/></template></category></learnf>"));
		test.User.Requests.Add(new("TEST", test.User, test.Bot));
		tag.Evaluate(test.RequestProcess);

		test.User.Requests.Add(new("TEST LEARNF", test.User, test.Bot));
		Assert.That(AimlTest.GetTemplate(test.Bot.Graphmaster, "TEST", "LEARNF", "<that>", "*", "<topic>", "*").Content.Evaluate(new(new(new("TEST LEARNF", test.User, test.Bot), "TEST LEARNF"), 0, false)), Is.EqualTo("Original: TEST; Current: TEST LEARNF"));
	}

	[Test]
	public void Evaluate_DoesNotModifyOriginalElement() {
		var test = new AimlTest();
		var tag = new LearnF(XElement.Parse("<learnf><category><pattern>TEST LEARNF</pattern><template>Original: <eval><input/></eval>; Current: <input/></template></category></learnf>"));
		var xml = LearnTests.GetOuterXml(tag.Element);

		test.User.Requests.Add(new("TEST", test.User, test.Bot));
		tag.Evaluate(test.RequestProcess);

		Assert.That(LearnTests.GetOuterXml(tag.Element), Is.EqualTo(xml));
	}

	[Test]
	public void EvaluateWithThatAndTopic() {
		var test = new AimlTest();
		var tag = new LearnF(XElement.Parse("<learnf><category><pattern>TEST LEARNF</pattern><that>LEARNED</that><topic>TESTS</topic><template>Original: <eval><input/></eval>; Current: <input/></template></category></learnf>"));
		test.User.Requests.Add(new("TEST", test.User, test.Bot));
		tag.Evaluate(test.RequestProcess);
		AimlTest.GetTemplate(test.Bot.Graphmaster, "TEST", "LEARNF", "<that>", "LEARNED", "<topic>", "TESTS");
	}
}
