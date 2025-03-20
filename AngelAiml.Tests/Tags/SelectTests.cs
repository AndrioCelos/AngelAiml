using System.Xml.Linq;
using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class SelectTests {
	private static AimlTest GetTest() {
		var test = new AimlTest();
		test.Bot.Triples.Add("A", "r", "M");
		test.Bot.Triples.Add("A", "r", "N");
		test.Bot.Triples.Add("A", "r", "O");
		test.Bot.Triples.Add("N", "r", "X");
		test.Bot.Triples.Add("O", "r", "X");
		test.Bot.Triples.Add("O", "r", "Y");
		test.Bot.Triples.Add("M", "attr", "1");
		test.Bot.Triples.Add("N", "attr", "0");
		return test;
	}

	[Test]
	public void EvaluateBacktracking() {
		var tag = new Select(new("?x"), [
			new(new("?x"), new("r"), new("?y"), true),
			new(new("?y"), new("r"), new("X"), true)
		]);
		Assert.That(tag.Evaluate(GetTest().RequestProcess).ToString(), Is.EqualTo("Aj94AUE="));
	}

	[Test]
	public void EvaluateBacktrackingWithoutVars() {
		var tag = new Select(null, [
			new(new("?x"), new("r"), new("?y"), true),
			new(new("?y"), new("r"), new("X"), true)
		]);
		Assert.That(tag.Evaluate(GetTest().RequestProcess).ToString(), Is.EqualTo("Aj95AU4CP3gBQQ== Aj95AU8CP3gBQQ=="));
	}

	[Test]
	public void EvaluateNoMatch() {
		var tag = new Select(new("?x"), [new(new("A"), new("attr"), new("?x"), true)]);
		Assert.That(tag.Evaluate(GetTest().RequestProcess).ToString(), Is.EqualTo("nil"));
	}

	[Test]
	public void EvaluateWithNotQ() {
		var tag = new Select(new("?y"), [
			new(new("A"), new("r"), new("?y"), true),
			new(new("?y"), new("attr"), new("0"), false)
		]);
		Assert.That(tag.Evaluate(GetTest().RequestProcess).ToString(), Is.EqualTo("Aj95AU0= Aj95AU8="));
	}

	[Test]
	public void FromXml() {
		const string xml = @"
<select>
	<vars>?x</vars>
	<q><subj>?x</subj><pred>r</pred><obj>?y</obj></q>
	<notq><subj>?y</subj><pred>attr</pred><obj>0</obj></notq>
</select>";
		var tag = Select.FromXml(XElement.Parse(xml), new AimlTest().Bot.AimlLoader);
		Assert.Multiple(() => {
			Assert.That(tag.Variables?.ToString(), Is.EqualTo("?x"));
			Assert.That(tag.Clauses, Has.Length.EqualTo(2));
		});
		Assert.That(tag.Clauses[0].Affirm, Is.True);
		Assert.Multiple(() => {
			Assert.That(tag.Clauses[0].Subject.ToString(), Is.EqualTo("?x"));
			Assert.That(tag.Clauses[0].Predicate.ToString(), Is.EqualTo("r"));
			Assert.That(tag.Clauses[0].Object.ToString(), Is.EqualTo("?y"));
		});
		Assert.That(tag.Clauses[1].Affirm, Is.False);
		Assert.Multiple(() => {
			Assert.That(tag.Clauses[1].Subject.ToString(), Is.EqualTo("?y"));
			Assert.That(tag.Clauses[1].Predicate.ToString(), Is.EqualTo("attr"));
			Assert.That(tag.Clauses[1].Object.ToString(), Is.EqualTo("0"));
		});
	}

	[Test]
	public void FromXmlWithoutVars() {
		const string xml = @"
<select>
	<q><subj>?x</subj><pred>r</pred><obj>?y</obj></q>
	<notq><subj>?y</subj><pred>attr</pred><obj>0</obj></notq>
</select>";
		var tag = Select.FromXml(XElement.Parse(xml), new AimlTest().Bot.AimlLoader);
		Assert.That(tag.Variables, Is.Null);
	}

	[Test]
	public void FromXmlWithoutClauses() {
		const string xml = "<select/>";
		Assert.Throws<ArgumentException>(() => Select.FromXml(XElement.Parse(xml), new AimlTest().Bot.AimlLoader));
	}

	[Test]
	public void FromXmlWithInvalidContent() {
		const string xml = "<select>foo</select>";
		Assert.Throws<AimlException>(() => Select.FromXml(XElement.Parse(xml), new AimlTest().Bot.AimlLoader));
	}

	[Test]
	public void FromXmlWithInvalidElement() {
		const string xml = "<select><foo/></select>";
		Assert.Throws<AimlException>(() => Select.FromXml(XElement.Parse(xml), new AimlTest().Bot.AimlLoader));
	}

	[Test]
	public void FromXmlWithNotQFirst() {
		const string xml = "<select><notq><subj>?x</subj><pred>attr</pred><obj>0</obj></notq></select>";
		Assert.Throws<ArgumentException>(() => Select.FromXml(XElement.Parse(xml), new AimlTest().Bot.AimlLoader));
	}
}
