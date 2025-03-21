﻿using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class UppercaseTests {
	[Test]
	public void Evaluate() {
		var tag = new Uppercase(new("hello WORLD says I."));
		Assert.AreEqual("HELLO WORLD SAYS I.", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}
}
