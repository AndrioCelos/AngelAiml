﻿using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class StarTests {
	private static AimlTest GetTest() {
		var test = new AimlTest();
		test.RequestProcess.star.Add("foo");
		test.RequestProcess.star.Add("bar baz");
		return test;
	}

	[Test]
	public void ParseWithIndex() {
		var tag = new Star(new("2"));
		Assert.That(tag.Index?.ToString(), Is.EqualTo("2"));
	}

	[Test]
	public void ParseWithDefault() {
		var tag = new Star(null);
		Assert.That(tag.Index, Is.Null);
	}

	[Test]
	public void EvaluateWithIndex() {
		var tag = new Star(new("2"));
		Assert.That(tag.Evaluate(GetTest().RequestProcess).ToString(), Is.EqualTo("bar baz"));
	}

	[Test]
	public void EvaluateWithDefault() {
		var tag = new Star(null);
		Assert.That(tag.Evaluate(GetTest().RequestProcess).ToString(), Is.EqualTo("foo"));
	}

	[Test]
	public void EvaluateWithIndexOutOfRange() {
		var tag = new Star(new("3"));
		Assert.That(tag.Evaluate(GetTest().RequestProcess).ToString(), Is.EqualTo("nil"));
	}

	[Test]
	public void EvaluateWithInvalidIndex() {
		var test = GetTest();
		var tag = new Star(new("0"));
		Assert.That(test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()), Is.EqualTo("nil"));
	}

	[Test]
	public void EvaluateWithDefaultIndexAndNoWildcards() {
		var tag = new Star(null);
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess).ToString(), Is.EqualTo("nil"));
	}
}
