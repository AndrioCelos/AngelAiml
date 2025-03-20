using AngelAiml.Tags;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class GetTests {
	private static AimlTest GetTest() {
		var test = new AimlTest();
		test.User.Predicates["foo"] = "sample predicate";
		test.Bot.Config.DefaultPredicates["bar"] = "sample default";
		test.RequestProcess.Variables["bar"] = "sample local";
		return test;
	}

	[Test]
	public void ParseWithName() {
		var tag = new Get(name: new("foo"), var: null, tuple: null);
		Assert.Multiple(() => {
			Assert.That(tag.Key.ToString(), Is.EqualTo("foo"));
			Assert.That(tag.TupleString, Is.Null);
		});
		Assert.That(tag.LocalVar, Is.False);
	}

	[Test]
	public void ParseWithVar() {
		var tag = new Get(name: null, var: new("bar"), tuple: null);
		Assert.Multiple(() => {
			Assert.That(tag.Key.ToString(), Is.EqualTo("bar"));
			Assert.That(tag.TupleString, Is.Null);
		});
		Assert.That(tag.LocalVar, Is.True);
	}

	[Test]
	public void ParseWithTupleName() {
		Assert.Throws<ArgumentException>(() => new Get(name: new("baz"), var: null, tuple: new("tuple")));
	}

	[Test]
	public void ParseWithTupleVar() {
		var tag = new Get(name: null, var: new("?x"), tuple: new("tuple"));
		Assert.Multiple(() => {
			Assert.That(tag.Key.ToString(), Is.EqualTo("?x"));
			Assert.That(tag.TupleString?.ToString(), Is.EqualTo("tuple"));
		});
	}

	[Test]
	public void ParseWithNameAndVar() {
		Assert.Throws<ArgumentException>(() => new Get(name: new("foo"), var: new("bar"), tuple: null));
	}

	[Test]
	public void ParseWithNoAttributes() {
		Assert.Throws<ArgumentException>(() => new Get(name: null, var: null, tuple: null));
	}

	[Test]
	public void EvaluateWithBoundPredicate() {
		var tag = new Get(new("foo"), null, false);
		Assert.That(tag.Evaluate(GetTest().RequestProcess), Is.EqualTo("sample predicate"));
	}

	[Test]
	public void EvaluateWithUnboundPredicateWithDefault() {
		var tag = new Get(new("bar"), null, false);
		Assert.That(tag.Evaluate(GetTest().RequestProcess), Is.EqualTo("sample default"));
	}

	[Test]
	public void EvaluateWithUnboundPredicate() {
		var tag = new Get(new("baz"), null, false);
		Assert.That(tag.Evaluate(GetTest().RequestProcess), Is.EqualTo("unknown"));
	}

	[Test]
	public void EvaluateWithBoundLocalVariable() {
		var tag = new Get(new("bar"), null, true);
		Assert.That(tag.Evaluate(GetTest().RequestProcess), Is.EqualTo("sample local"));
	}

	[Test]
	public void EvaluateWithUnboundLocalVariable() {
		var tag = new Get(new("foo"), null, true);
		Assert.That(tag.Evaluate(GetTest().RequestProcess), Is.EqualTo("unknown"));
	}

	[Test]
	public void EvaluateWithBoundTupleVariable() {
		var tag = new Get(new("?x"), new(new Tuple("?y", "", new("?x", "sample tuple")).Encode(["?x", "?y"])), true);
		Assert.That(tag.Evaluate(GetTest().RequestProcess), Is.EqualTo("sample tuple"));
	}

	[Test]
	public void EvaluateWithUnboundTupleVariable() {
		var tag = new Get(new("?z"), new(new Tuple("?y", "", new("?x", "sample tuple")).Encode(["?x", "?y"])), true);
		Assert.That(tag.Evaluate(GetTest().RequestProcess), Is.EqualTo("unknown"));
	}
}
