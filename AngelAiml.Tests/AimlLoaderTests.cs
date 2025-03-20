using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Linq;
using AngelAiml.Tags;
using AngelAiml.Tests.TestExtension;
using NUnit.Framework.Constraints;

namespace AngelAiml.Tests;

[TestFixture]
public class AimlLoaderTests {
	private int oobExecuted;

	[OneTimeSetUp]
	public void Init() {
		AimlLoader.AddCustomOobHandler("testoob", (el, r) => oobExecuted++);
		AimlLoader.AddCustomOobHandler("testoob2", (el, r) => "Sample replacement");
		AimlLoader.AddCustomTag(typeof(TestCustomTag));
		AimlLoader.AddCustomTag("custom", typeof(TestCustomTag));
		AimlLoader.AddCustomTag("custom2", (el, l) => new TestCustomTag(el, new("Hello"), new("world")));
	}

	private static string GetExampleBotDir() {
		var solutionDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory))));
		if (solutionDir is null) HandleMissingExampleBot();
		var botDir = Path.Combine(solutionDir, "ExampleBot");
		if (!Directory.Exists(botDir)) HandleMissingExampleBot();
		return botDir;
	}

	[DoesNotReturn]
	private static void HandleMissingExampleBot() {
		Assert.Inconclusive("ExampleBot files were not found.");
		throw new InvalidOperationException();
	}

	[Test]
	public void AddExtensionTest() {
		var extension = new TestExtension.TestExtension();
		AimlLoader.AddExtension(extension);
		Assert.That(extension.Initialised, Is.EqualTo(1));
	}

	[Test]
	public void AddExtensionsTest() {
		AimlLoader.AddExtensions(Assembly.GetExecutingAssembly().Location);
		Assert.That(TestExtension.TestExtension.instances.Single().Initialised, Is.EqualTo(1));
	}

	[Test]
	public void AddCustomTag_ImplicitName() {
		var test = new AimlTest();
		var el = test.Bot.AimlLoader.ParseElement(XElement.Parse("<testcustomtag value1='Hello'><value2>world</value2></testcustomtag>"));
		Assert.That(el, Is.InstanceOf<TestCustomTag>());
		Assert.That(el.Evaluate(test.RequestProcess), Is.EqualTo("Hello world"));
	}

	[Test]
	public void AddCustomTag_ExplicitName() {
		var test = new AimlTest();
		var el = test.Bot.AimlLoader.ParseElement(XElement.Parse("<custom value1='Hello'><value2>world</value2></custom>"));
		Assert.That(el, Is.InstanceOf<TestCustomTag>());
		Assert.That(el.Evaluate(test.RequestProcess), Is.EqualTo("Hello world"));
	}

	[Test]
	public void AddCustomTag_Delegate() {
		var test = new AimlTest();
		var el = test.Bot.AimlLoader.ParseElement(XElement.Parse("<custom2/>"));
		Assert.That(el, Is.InstanceOf<TestCustomTag>());
		Assert.That(el.Evaluate(test.RequestProcess), Is.EqualTo("Hello world"));
	}

	[Test]
	public void AddCustomMediaElement() {
		AimlLoader.AddCustomMediaElement("custommedia", MediaElementType.Inline, (_, _) => new TestCustomRichMediaElement());
		var response = new Response(new AimlTest().RequestProcess.Sentence.Request, "<custommedia/>");
		var messages = response.ToMessages();
		Assert.That(messages, Has.Length.EqualTo(1));
		Assert.That(messages[0].InlineElements, Has.Count.EqualTo(1));
		Assert.That(messages[0].InlineElements[0], Is.InstanceOf<TestCustomRichMediaElement>());
	}

	[Test]
	public void AddCustomOobHandler_EmptyReplacement() {
		var response = new Response(new AimlTest().RequestProcess.Sentence.Request, "<oob><testoob/></oob>");
		response.ProcessOobElements();
		Assert.Multiple(() => {
			Assert.That(response.ToString(), Is.EqualTo(""));
			Assert.That(oobExecuted, Is.EqualTo(1));
		});
	}

	[Test]
	public void AddCustomOobHandler_WithReplacement() {
		var response = new Response(new AimlTest().RequestProcess.Sentence.Request, "<oob><testoob2/></oob>");
		response.ProcessOobElements();
		Assert.That(response.ToString(), Is.EqualTo("Sample replacement"));
	}

	[Test]
	public void AddCustomSraixServiceTest() {
		var test = new AimlTest();
		AimlLoader.AddCustomSraixService(new TestSraixService());
		test.RequestProcess.Variables["bar"] = "var";

		var el = XElement.Parse("<sraix customattr='Sample'/>");
		var tag = new SraiX(new(nameof(TestSraixService)), new("default"), el, new("arguments"));
		Assert.That(tag.Evaluate(test.RequestProcess), Is.EqualTo("Success"));
	}

	[Test]
	public void LoadAimlFiles_DefaultDirectory() {
		var test = new AimlTest(GetExampleBotDir());
		test.Bot.LoadConfig();
		test.Bot.AimlLoader.LoadAimlFiles();
		var template = AimlTest.GetTemplate(test.Bot.Graphmaster, "HI", "<that>", "*", "<topic>", "*");
		Assert.Multiple(() => {
			Assert.That(template.Content.ToString(), Is.EqualTo("Hello, world!"));
			Assert.That(template.Uri, new EndsWithConstraint("helloworld.aiml"));
		});
	}

	[Test]
	public void LoadAimlFiles_SpecifiedDirectory() {
		var dir = Path.Combine(GetExampleBotDir(), "aiml");
		if (!Directory.Exists(dir)) HandleMissingExampleBot();
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAimlFiles(dir);
		var template = AimlTest.GetTemplate(test.Bot.Graphmaster, "HI", "<that>", "*", "<topic>", "*");
		Assert.Multiple(() => {
			Assert.That(template.Content.ToString(), Is.EqualTo("Hello, world!"));
			Assert.That(template.Uri, new EndsWithConstraint("helloworld.aiml"));
		});
	}

	[Test]
	public void LoadAiml_File() {
		var file = Path.Combine(GetExampleBotDir(), "aiml", "helloworld.aiml");
		if (!File.Exists(file)) HandleMissingExampleBot();
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(file);
		var template = AimlTest.GetTemplate(test.Bot.Graphmaster, "HI", "<that>", "*", "<topic>", "*");
		Assert.Multiple(() => {
			Assert.That(template.Content.ToString(), Is.EqualTo("Hello, world!"));
			Assert.That(template.Uri, new EndsWithConstraint("helloworld.aiml"));
		});
	}

	[Test]
	public void LoadAiml_XDocument() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(XDocument.Parse(@"<?xml version='1.0' encoding='utf-16'?>
<aiml>
	<category>
		<pattern>TEST</pattern>
		<template>Hello world!</template>
	</category>
</aiml>"));
		Assert.That(AimlTest.GetTemplate(test.Bot.Graphmaster, "TEST", "<that>", "*", "<topic>", "*").Content.ToString(), Is.EqualTo("Hello world!"));
	}

	[Test]
	public void LoadAiml_XDocumentWithWhitespace() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(XDocument.Parse(@"<?xml version='1.0' encoding='utf-16'?>
<aiml>
	<category>
		<pattern>TEST</pattern>
		<template>
			<sr/>
			<sr/>
		</template>
	</category>
</aiml>", LoadOptions.PreserveWhitespace));
		var intermediateWhitespaceNode = AimlTest.GetTemplate(test.Bot.Graphmaster, "TEST", "<that>", "*", "<topic>", "*").Content.SkipWhile(t => t is not SR).Skip(1).First();
		Assert.Multiple(() => {
			Assert.That(intermediateWhitespaceNode, Is.InstanceOf<TemplateText>());
			Assert.That(((TemplateText) intermediateWhitespaceNode).Text, Is.EqualTo(" "));
		});
	}

	[Test]
	public void LoadAiml_XElement() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(XElement.Parse(@"
<aiml>
	<category>
		<pattern>TEST</pattern>
		<template>Hello world!</template>
	</category>
</aiml>"));
		Assert.That(AimlTest.GetTemplate(test.Bot.Graphmaster, "TEST", "<that>", "*", "<topic>", "*").Content.ToString(), Is.EqualTo("Hello world!"));
	}

	[Test]
	public void LoadAimlInto_XElement() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAimlInto(target, XElement.Parse(@"
<learnf>
	<category>
		<pattern>TEST</pattern>
		<template>Hello world!</template>
	</category>
</learnf>"));
		var template = target.Children["TEST"].Children["<that>"].Children["*"].Children["<topic>"].Children["*"].Template;
		Assert.That(template, Is.Not.Null);
		Assert.That(template!.Content.ToString(), Is.EqualTo("Hello world!"));
	}

	[Test]
	public void LoadAiml_TopicElement() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(XDocument.Parse(@"<?xml version='1.0' encoding='utf-16'?>
<aiml>
	<topic name='testing'>
		<category>
			<pattern>TEST</pattern>
			<template>Hello world!</template>
		</category>
	</topic>
</aiml>"));
		Assert.That(AimlTest.GetTemplate(test.Bot.Graphmaster, "TEST", "<that>", "*", "<topic>", "TESTING").Content.ToString(), Is.EqualTo("Hello world!"));
	}

	[Test]
	public void ProcessCategory_NoTopic() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><pattern>TEST</pattern><template>Hello world!</template></category>"));
		Assert.That(AimlTest.GetTemplate(target, "TEST", "<that>", "*", "<topic>", "*").Content.ToString(), Is.EqualTo("Hello world!"));
	}

	[Test]
	public void ProcessCategory_InheritedTopic() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><pattern>TEST</pattern><template>Hello world!</template></category>"), "testing");
		Assert.That(AimlTest.GetTemplate(target, "TEST", "<that>", "*", "<topic>", "TESTING").Content.ToString(), Is.EqualTo("Hello world!"));
	}

	[Test]
	public void ProcessCategory_SpecificTopic() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><pattern>TEST</pattern><topic>TESTING2</topic><template>Hello world!</template></category>"), "testing");
		Assert.That(AimlTest.GetTemplate(target, "TEST", "<that>", "*", "<topic>", "TESTING2").Content.ToString(), Is.EqualTo("Hello world!"));
	}

	[Test]
	public void ProcessCategory_DuplicatePath() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><pattern>TEST</pattern><template>Hello world!</template></category>"), "testing");
		test.AssertWarning(() => test.Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><pattern>TEST</pattern><template>Hello world!</template></category>"), "testing"));
	}

	[Test]
	public void ProcessCategory_That() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><pattern>TEST</pattern><that>HELLO</that><template>Hello world!</template></category>"));
		Assert.That(AimlTest.GetTemplate(target, "TEST", "<that>", "HELLO", "<topic>", "*").Content.ToString(), Is.EqualTo("Hello world!"));
	}

	[Test]
	public void ProcessCategory_NoPattern() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><template>Hello world!</template></category>")));
	}

	[Test]
	public void ProcessCategory_NoTemplate() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><pattern>TEST</pattern></category>")));
	}

	[Test]
	public void ProcessCategory_InvalidCategoryElement() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><foo/></category>")));
	}

	[Test]
	public void ParseElement_NoContent() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<star/>"));
		Assert.Multiple(() => {
			Assert.That(el, Is.InstanceOf<Star>());
			Assert.That(((Star) el).Index, Is.Null);
		});
	}

	[Test]
	public void ParseElement_WithContent() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<srai>Hello, world!</srai>"));
		Assert.Multiple(() => {
			Assert.That(el, Is.InstanceOf<Srai>());
			Assert.That(((Srai) el).Children.ToString(), Is.EqualTo("Hello, world!"));
		});
	}

	[Test]
	public void ParseElement_InvalidContent() => Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<star>foo</star>")));

	[Test]
	public void ParseElement_SpecialParserOrCustomTag() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<oob><foo/></oob>"));
		Assert.Multiple(() => {
			Assert.That(el, Is.InstanceOf<Oob>());
			Assert.That(((Oob) el).Children.Single(), Is.InstanceOf<Oob>());
		});
	}

	[Test]
	public void ParseElement_RichMediaElement() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<split/>"));
		Assert.That(el, Is.InstanceOf<Oob>());
	}

	[Test]
	public void ParseElement_AttributeAsXmlAttribute() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<star index='2'/>"));
		Assert.Multiple(() => {
			Assert.That(el, Is.InstanceOf<Star>());
			Assert.That(((Star) el).Index?.ToString(), Is.EqualTo("2"));
		});
	}

	[Test]
	public void ParseElement_AttributeAsXmlElement() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<star><index>2</index></star>"));
		Assert.Multiple(() => {
			Assert.That(el, Is.InstanceOf<Star>());
			Assert.That(((Star) el).Index?.ToString(), Is.EqualTo("2"));
		});
	}

	[Test]
	public void ParseElement_InvalidAttribute() => Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<star foo='bar'/>")));

	[Test]
	public void ParseElement_DuplicateAttribute() {
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<star index='2'><index>3</index></star>")));
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<star><index>2</index><index>3</index></star>")));
	}

	[Test]
	public void ParseElement_MissingAttribute() => Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<map>foo</map>")));

	[Test]
	public void ParseElement_SpecialContent() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<random><li>1</li><li>2</li></random>"));
		Assert.That(el, Is.InstanceOf<AngelAiml.Tags.Random>());
		var random = (AngelAiml.Tags.Random) el;
		Assert.That(random.Items, Has.Length.EqualTo(2));
		Assert.Multiple(() => {
			Assert.That(random.Items[0].Children.ToString(), Is.EqualTo("1"));
			Assert.That(random.Items[1].Children.ToString(), Is.EqualTo("2"));
		});
	}

	[Test]
	public void ParseElement_PassthroughXmlElement() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<learn><category><pattern>foo</pattern><template/></category></learn>"));
		Assert.Multiple(() => {
			Assert.That(el, Is.InstanceOf<Learn>());
			Assert.That(((Learn) el).Element.Value, Is.EqualTo("foo"));
		});
	}

	[Test]
	public void ParseElement_PassthroughXmlElement_Sraix() {
		var el = XElement.Parse("<sraix service='ExternalBotService' botname='Angelina'/>");
		var tag = new AimlTest().Bot.AimlLoader.ParseElement(el);
		Assert.Multiple(() => {
			Assert.That(tag, Is.InstanceOf<SraiX>());
			Assert.That(((SraiX) tag).Element, Is.SameAs(el));
		});
	}
}
