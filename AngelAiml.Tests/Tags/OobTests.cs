using System.Xml.Linq;
using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class OobTests {
	[Test]
	public void Evaluate() {
		var tag = new Oob("oob", Enumerable.Empty<XAttribute>(), new("<testoob>foo</testoob>"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("<oob><testoob>foo</testoob></oob>"));
	}

	[Test]
	public void FromXml() {
		var tag = Oob.FromXml(XElement.Parse("<oob><testoob><input/></testoob></oob>"), new AimlTest().Bot.AimlLoader);
		Assert.Multiple(() => {
			Assert.That(tag.Name, Is.EqualTo("oob"));
			Assert.That(tag.Attributes, Is.Empty);
			Assert.That(tag.Children[0], Is.InstanceOf<Oob>());
			Assert.That(((Oob) tag.Children[0]).Children[0], Is.InstanceOf<Input>());
		});
	}

	[Test]
	public void FromXmlWithRichMediaElement() {
		Oob.FromXml(XElement.Parse("<card><title>Test</title></card>"), new AimlTest().Bot.AimlLoader);
	}

	[Test]
	public void FromXmlWithRichMediaElementAndInvalidAttributes() {
		Assert.Throws<AimlException>(() => Oob.FromXml(XElement.Parse("<card><foo/></card>"), new AimlTest().Bot.AimlLoader, "image", "title", "subtitle", "button"));
	}
}
