﻿using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class Person2Tests {
	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		test.Bot.Config.Person2Substitutions.Add(new(" me ", " them "));
		var tag = new Person2(new("It is me"));
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("It is them"));
	}
}
