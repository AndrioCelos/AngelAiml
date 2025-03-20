using System.Xml.Linq;
using AngelAiml.Tags;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class TestTests {
	// Testing the <test>s among the tests.
	private static AimlTest GetTest() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(XElement.Parse(@"
<aiml>
	<category>
		<pattern>*</pattern>
		<template><star/></template>
	</category>
</aiml>"));
		return test;
	}

	[Test]
	public void ParseWithConstant() {
		var tag = new Test(name: new("SampleTest"), expected: new("Hello world"), regex: null, children: new("Hello world"));
		Assert.Multiple(() => {
			Assert.That(tag.Name, Is.EqualTo("SampleTest"));
			Assert.That(tag.ExpectedResponse.ToString(), Is.EqualTo("Hello world"));
		});
		Assert.That(tag.UseRegex, Is.False);
		Assert.That(tag.Children.ToString(), Is.EqualTo("Hello world"));
	}

	[Test]
	public void ParseWithRegex() {
		var tag = new Test(name: new("SampleTest"), expected: null, regex: new("^Hello"), children: new("Hello world"));
		Assert.Multiple(() => {
			Assert.That(tag.Name, Is.EqualTo("SampleTest"));
			Assert.That(tag.ExpectedResponse.ToString(), Is.EqualTo("^Hello"));
		});
		Assert.That(tag.UseRegex, Is.True);
		Assert.That(tag.Children.ToString(), Is.EqualTo("Hello world"));
	}

	[Test]
	public void ParseWithConstantAndRegex() {
		Assert.Throws<ArgumentException>(() => new Test(name: new("SampleTest"), expected: new("Hello world"), regex: new("^Hello"), children: new("Hello world")));
	}

	[Test]
	public void ParseWithoutExpectation() {
		Assert.Throws<ArgumentException>(() => new Test(name: new("SampleTest"), expected: null, regex: null, children: new("Hello world")));
	}

	[Test]
	public void ParseWithNonConstantName() {
		Assert.Throws<ArgumentException>(() => new Test(name: new(new Star(null)), expected: new("Hello world"), regex: null, children: new("Hello world")));
	}

	[Test]
	public void EvaluateConstantPass() {
		var test = GetTest();
		test.RequestProcess.testResults = [ ];
		var tag = new Test(name: new("SampleTest"), expected: new("Hello world"), regex: null, children: new("Hello\nworld"));
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("Hello world"));
		Assert.That(test.RequestProcess.testResults["SampleTest"].Passed, Is.True);
	}

	[Test]
	public void EvaluateConstantFail() {
		var test = GetTest();
		test.RequestProcess.testResults = [ ];
		var tag = new Test(name: new("SampleTest"), expected: new("Hello world"), regex: null, children: new("Hell world"));
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("Hell world"));
		Assert.That(test.RequestProcess.testResults["SampleTest"].Passed, Is.False);
	}

	[Test]
	public void EvaluateRegexPass() {
		var test = GetTest();
		test.RequestProcess.testResults = [ ];
		var tag = new Test(name: new("SampleTest"), expected: null, regex: new("^Hello\n\\w"), children: new("Hello world"));
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("Hello world"));
		Assert.That(test.RequestProcess.testResults["SampleTest"].Passed, Is.True);
	}

	[Test]
	public void EvaluateRegexFail() {
		var test = GetTest();
		test.RequestProcess.testResults = [ ];
		var tag = new Test(name: new("SampleTest"), expected: null, regex: new("^Hello"), children: new("Hell world"));
		Assert.That(tag.Evaluate(test.RequestProcess).ToString(), Is.EqualTo("Hell world"));
		Assert.That(test.RequestProcess.testResults["SampleTest"].Passed, Is.False);
	}

	[Test]
	public void EvaluateRegexInvalid() {
		var test = GetTest();
		test.RequestProcess.testResults = [ ];
		var tag = new Test(name: new("SampleTest"), expected: null, regex: new("("), children: new("Hello world"));
		Assert.That(test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()), Is.EqualTo("Hello world"));
		Assert.That(test.RequestProcess.testResults["SampleTest"].Passed, Is.False);
	}

	[Test]
	public void Evaluate_TestsDisabled() {
		var test = GetTest();
		var tag = new Test(name: new("SampleTest"), expected: new("Hello world"), regex: null, children: new("Hello world"));
		Assert.Multiple(() => {
			Assert.That(test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()), Is.EqualTo("Hello world"));
			Assert.That(test.RequestProcess.testResults, Is.Null);
		});
	}
}
