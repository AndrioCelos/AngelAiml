using System.Xml.Linq;
using AngelAiml.Tags;
using AngelAiml.Tests.TestExtension;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class SraiXTests {
	[OneTimeSetUp]
	public void Init() {
		if (!AimlLoader.sraixServices.ContainsKey(nameof(TestSraixService)))
			AimlLoader.AddCustomSraixService(new TestSraixService());
		AimlLoader.AddCustomSraixService(new TestFaultSraixService());
	}

	[Test]
	public void ParseWithDefault() {
		var el = XElement.Parse("<sraix/>");
		var tag = new SraiX(new(nameof(TestSraixService)), new("default"), el, new("arguments"));
		Assert.Multiple(() => {
			Assert.That(tag.ServiceName.ToString(), Is.EqualTo(nameof(TestSraixService)));
			Assert.That(tag.DefaultReply?.ToString(), Is.EqualTo("default"));
		});
		Assert.Multiple(() => {
			Assert.That(tag.Element, Is.SameAs(el));
			Assert.That(tag.ServiceName.ToString(), Is.EqualTo(nameof(TestSraixService)));
		});
	}

	[Test]
	public void ParseWithoutDefault() {
		var el = XElement.Parse("<sraix/>");
		var tag = new SraiX(new(nameof(TestSraixService)), null, el, new("arguments"));
		Assert.That(tag.DefaultReply, Is.Null);
	}

	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		test.RequestProcess.Variables["bar"] = "var";

		var el = XElement.Parse("<sraix customattr='Sample'/>");
		var tag = new SraiX(new(nameof(TestSraixService)), new("default"), el, new("arguments"));
		Assert.That(tag.Evaluate(test.RequestProcess), Is.EqualTo("Success"));
	}

	[Test]
	public void EvaluateFullName() {
		var test = new AimlTest();
		test.RequestProcess.Variables["bar"] = "var";

		var el = XElement.Parse("<sraix customattr='Sample'/>");
		var tag = new SraiX(new($"{nameof(AngelAiml)}.{nameof(Tests)}.{nameof(TestExtension)}.{nameof(TestSraixService)}"), new("default"), el, new("arguments"));
		Assert.That(tag.Evaluate(test.RequestProcess), Is.EqualTo("Success"));
	}

	[Test]
	public void EvaluateInvalidServiceWithDefault() {
		var test = new AimlTest();
		var el = XElement.Parse("<sraix/>");
		var tag = new SraiX(new("InvalidService"), new("default"), el, new("arguments"));
		Assert.That(test.AssertWarning(() => tag.Evaluate(test.RequestProcess)), Is.EqualTo("default"));
	}

	[Test]
	public void EvaluateInvalidServiceWithoutDefault() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(XElement.Parse(@"
<aiml>
	<category>
		<pattern>SRAIXFAILED ^</pattern>
		<template>Failure template</template>
	</category>
</aiml>"));
		var el = XElement.Parse("<sraix/>");
		var tag = new SraiX(new("InvalidService"), null, el, new("arguments"));
		Assert.That(test.AssertWarning(() => tag.Evaluate(test.RequestProcess)), Is.EqualTo("Failure template"));
	}

	[Test]
	public void EvaluateFaultedServiceWithDefault() {
		var test = new AimlTest();
		var el = XElement.Parse("<sraix/>");
		var tag = new SraiX(new(nameof(TestFaultSraixService)), new("default"), el, new("arguments"));
		Assert.That(test.AssertWarning(() => tag.Evaluate(test.RequestProcess)), Is.EqualTo("default"));
	}

	[Test]
	public void EvaluateFaultedServiceWithoutDefault() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(XElement.Parse(@"
<aiml>
	<category>
		<pattern>SRAIXFAILED ^</pattern>
		<template>Failure template</template>
	</category>
</aiml>"));
		var el = XElement.Parse("<sraix/>");
		var tag = new SraiX(new(nameof(TestFaultSraixService)), null, el, new("arguments"));
		Assert.That(test.AssertWarning(() => tag.Evaluate(test.RequestProcess)), Is.EqualTo("Failure template"));
	}
}
