﻿using System.Xml.Linq;
using AngelAiml.Tags;
using NUnit.Framework.Constraints;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class SraiTests {
	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(XElement.Parse(@"
<aiml>
	<category>
		<pattern>test</pattern>
		<template>Hello world!</template>
	</category>
</aiml>"));

		var tag = new Srai(new("test"));
		Assert.That(tag.Evaluate(test.RequestProcess), Is.EqualTo("Hello world!"));
	}

	[Test]
	public void EvaluateWithLimitedRecursion() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(XElement.Parse(@"
<aiml>
	<category>
		<pattern>*</pattern>
		<template><sr/></template>
	</category>
</aiml>"));

		var tag = new Srai(new("test"));
		var result = test.AssertWarning(() => tag.Evaluate(test.RequestProcess));
		Assert.That(result, new EndsWithConstraint(test.Bot.Config.RecursionLimitMessage));
	}
}
