﻿using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class TopicStarTests {
	private static AimlTest GetTest() {
		var test = new AimlTest();
		test.RequestProcess.topicstar.Add("foo");
		test.RequestProcess.topicstar.Add("bar baz");
		return test;
	}

	[Test]
	public void ParseWithIndex() {
		var tag = new TopicStar(new("2"));
		Assert.AreEqual("2", tag.Index?.ToString());
	}

	[Test]
	public void ParseWithDefault() {
		var tag = new TopicStar(null);
		Assert.IsNull(tag.Index);
	}

	[Test]
	public void EvaluateWithIndex() {
		var tag = new TopicStar(new("2"));
		Assert.AreEqual("bar baz", tag.Evaluate(GetTest().RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithDefault() {
		var tag = new TopicStar(null);
		Assert.AreEqual("foo", tag.Evaluate(GetTest().RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithIndexOutOfRange() {
		var tag = new TopicStar(new("3"));
		Assert.AreEqual("nil", tag.Evaluate(GetTest().RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithInvalidIndex() {
		var test = GetTest();
		var tag = new TopicStar(new("0"));
		Assert.AreEqual("nil", test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()));
	}

	[Test]
	public void EvaluateWithDefaultIndexAndNoWildcards() {
		var tag = new TopicStar(null);
		Assert.AreEqual("nil", tag.Evaluate(new AimlTest().RequestProcess).ToString());
	}
}
