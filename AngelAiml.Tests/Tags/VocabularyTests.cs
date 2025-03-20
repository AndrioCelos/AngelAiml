using System.Xml.Linq;
using AngelAiml.Sets;
using AngelAiml.Tags;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class VocabularyTests {
	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		test.Bot.Properties["name"] = "Angelina";
		test.Bot.AimlLoader.LoadAiml(XElement.Parse(@"
<aiml>
	<category>
		<pattern>HELLO WORLD</pattern>
		<template>Hello world!</template>
	</category>
	<category>
		<pattern>HELLO <bot name='name'/></pattern>
		<template>Hello world!</template>
	</category>
	<category>
		<pattern>HELLO <set>testset</set></pattern>
		<template>Hello world!</template>
	</category>
</aiml>"));
		test.Bot.Sets["testset"] = new StringSet(["A", "B", "C D"], test.Bot.Config.StringComparer);

		var tag = new Vocabulary();
		Assert.That(tag.Evaluate(test.RequestProcess), Is.EqualTo("7"));
	}
}
