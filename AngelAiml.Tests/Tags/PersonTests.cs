﻿using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class PersonTests {
	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		test.Bot.Config.PersonSubstitutions.Add(new(" me ", " you "));
		var tag = new Person(new("It is me"));
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("It is you"));
	}
}
