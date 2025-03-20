using AngelAiml.Tags;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class ConditionTests {
	private static AimlTest GetTest() {
		var test = new AimlTest();
		test.User.Predicates["foo"] = "predicate";
		test.Bot.Config.DefaultPredicates["bar"] = "default";
		test.RequestProcess.Variables["bar"] = "var";
		test.RequestProcess.Variables["n"] = "3";
		return test;
	}

	[Test]
	public void ParseType1WithName() {
		var tag = new Condition(name: new("foo"), var: null, value: new("sample predicate"), items: Array.Empty<Condition.Li>(), children: new("match"));
		Assert.That(tag.Items, Has.Count.EqualTo(1));
		Assert.That(tag.Items[0].Key?.ToString(), Is.EqualTo("foo"));
		Assert.That(tag.Items[0].LocalVar, Is.False);
		Assert.Multiple(() => {
			Assert.That(tag.Items[0].Value?.ToString(), Is.EqualTo("sample predicate"));
			Assert.That(tag.Items[0].Children?.ToString(), Is.EqualTo("match"));
		});
	}

	[Test]
	public void ParseType1WithVar() {
		var tag = new Condition(name: null, var: new("bar"), value: new("sample predicate"), items: Array.Empty<Condition.Li>(), children: new("match"));
		Assert.That(tag.Items, Has.Count.EqualTo(1));
		Assert.That(tag.Items[0].Key?.ToString(), Is.EqualTo("bar"));
		Assert.That(tag.Items[0].LocalVar, Is.True);
		Assert.Multiple(() => {
			Assert.That(tag.Items[0].Value?.ToString(), Is.EqualTo("sample predicate"));
			Assert.That(tag.Items[0].Children?.ToString(), Is.EqualTo("match"));
		});
	}

	[Test]
	public void ParseType2WithName() {
		var tag = new Condition(name: new("foo"), var: null, value: null, items: [
			new(name: new("baz"), var: null, value: new("value 1"), children: new("match 1")),
			new(name: null, var: new("bar"), value: new("value 2"), children: new("match 2")),
			new(new("value 3"), new("match 3")),
			new(new("match 4"))
		], children: TemplateElementCollection.Empty);
		Assert.That(tag.Items, Has.Count.EqualTo(4));

		Assert.That(tag.Items[0].Key?.ToString(), Is.EqualTo("baz"));
		Assert.That(tag.Items[0].LocalVar, Is.False);
		Assert.Multiple(() => {
			Assert.That(tag.Items[0].Value?.ToString(), Is.EqualTo("value 1"));
			Assert.That(tag.Items[0].Children?.ToString(), Is.EqualTo("match 1"));

			Assert.That(tag.Items[1].Key?.ToString(), Is.EqualTo("bar"));
		});
		Assert.That(tag.Items[1].LocalVar, Is.True);
		Assert.Multiple(() => {
			Assert.That(tag.Items[1].Value?.ToString(), Is.EqualTo("value 2"));
			Assert.That(tag.Items[1].Children?.ToString(), Is.EqualTo("match 2"));

			Assert.That(tag.Items[2].Key?.ToString(), Is.EqualTo("foo"));
		});
		Assert.That(tag.Items[2].LocalVar, Is.False);
		Assert.Multiple(() => {
			Assert.That(tag.Items[2].Value?.ToString(), Is.EqualTo("value 3"));
			Assert.That(tag.Items[2].Children?.ToString(), Is.EqualTo("match 3"));
		});

		Assert.Multiple(() => {
			Assert.That(tag.Items[3].Key, Is.Null);
			Assert.That(tag.Items[3].Value, Is.Null);
			Assert.That(tag.Items[3].Children?.ToString(), Is.EqualTo("match 4"));
		});
	}

	[Test]
	public void ParseType2WithVar() {
		var tag = new Condition(name: null, var: new("foo"), value: null, items: [
			new(name: new("baz"), var: null, value: new("value 1"), children: new("match 1")),
			new(name: null, var: new("bar"), value: new("value 2"), children: new("match 2")),
			new(new("value 3"), new("match 3")),
			new(new("match 4"))
		], children: TemplateElementCollection.Empty);
		Assert.That(tag.Items, Has.Count.EqualTo(4));

		Assert.That(tag.Items[0].Key?.ToString(), Is.EqualTo("baz"));
		Assert.That(tag.Items[0].LocalVar, Is.False);
		Assert.Multiple(() => {
			Assert.That(tag.Items[0].Value?.ToString(), Is.EqualTo("value 1"));
			Assert.That(tag.Items[0].Children?.ToString(), Is.EqualTo("match 1"));

			Assert.That(tag.Items[1].Key?.ToString(), Is.EqualTo("bar"));
		});
		Assert.That(tag.Items[1].LocalVar, Is.True);
		Assert.Multiple(() => {
			Assert.That(tag.Items[1].Value?.ToString(), Is.EqualTo("value 2"));
			Assert.That(tag.Items[1].Children?.ToString(), Is.EqualTo("match 2"));

			Assert.That(tag.Items[2].Key?.ToString(), Is.EqualTo("foo"));
		});
		Assert.That(tag.Items[2].LocalVar, Is.True);
		Assert.Multiple(() => {
			Assert.That(tag.Items[2].Value?.ToString(), Is.EqualTo("value 3"));
			Assert.That(tag.Items[2].Children?.ToString(), Is.EqualTo("match 3"));
		});

		Assert.Multiple(() => {
			Assert.That(tag.Items[3].Key, Is.Null);
			Assert.That(tag.Items[3].Value, Is.Null);
			Assert.That(tag.Items[3].Children?.ToString(), Is.EqualTo("match 4"));
		});
	}

	[Test]
	public void ParseType3() {
		var tag = new Condition(name: null, var: null, value: null, items: [
			new(name: new("baz"), var: null, value: new("value 1"), children: new("match 1")),
			new(name: null, var: new("bar"), value: new("value 2"), children: new("match 2")),
			new(new("match 3"))
		], children: TemplateElementCollection.Empty);
		Assert.That(tag.Items, Has.Count.EqualTo(3));

		Assert.That(tag.Items[0].Key?.ToString(), Is.EqualTo("baz"));
		Assert.That(tag.Items[0].LocalVar, Is.False);
		Assert.Multiple(() => {
			Assert.That(tag.Items[0].Value?.ToString(), Is.EqualTo("value 1"));
			Assert.That(tag.Items[0].Children?.ToString(), Is.EqualTo("match 1"));

			Assert.That(tag.Items[1].Key?.ToString(), Is.EqualTo("bar"));
		});
		Assert.That(tag.Items[1].LocalVar, Is.True);
		Assert.Multiple(() => {
			Assert.That(tag.Items[1].Value?.ToString(), Is.EqualTo("value 2"));
			Assert.That(tag.Items[1].Children?.ToString(), Is.EqualTo("match 2"));
		});

		Assert.Multiple(() => {
			Assert.That(tag.Items[2].Key, Is.Null);
			Assert.That(tag.Items[2].Value, Is.Null);
			Assert.That(tag.Items[2].Children?.ToString(), Is.EqualTo("match 3"));
		});
	}

	[Test]
	public void ParseWithLiAfterDefault() {
		Assert.Throws<ArgumentException>(() => new Condition(name: null, var: null, value: null, items: [
			new(new("match 2")),
			new(name: new("baz"), var: null, value: new("value 1"), children: new("match 1"))
		], children: TemplateElementCollection.Empty));
	}

	[Test]
	public void ParseWithNameAndVar() {
		Assert.Throws<ArgumentException>(() => new Condition(name: new("foo"), var: new("bar"), value: new("value 1"), items: Array.Empty<Condition.Li>(), children: TemplateElementCollection.Empty));
	}

	[Test]
	public void ParseLiWithNameAndVar() {
		Assert.Throws<ArgumentException>(() => new Condition.Li(name: new("foo"), var: new("bar"), value: new("value 1"), children: TemplateElementCollection.Empty));
	}

	[Test]
	public void ParseType1WithNoVariable() {
		Assert.Throws<ArgumentException>(() => new Condition(name: null, var: null, value: new("value 1"), items: Array.Empty<Condition.Li>(), children: TemplateElementCollection.Empty));
	}

	[Test]
	public void ParseType1WithNoValue() {
		Assert.Throws<ArgumentException>(() => new Condition(name: new("foo"), var: null, value: null, items: Array.Empty<Condition.Li>(), children: TemplateElementCollection.Empty));
	}

	[Test]
	public void ParseType3WithNoValue() {
		Assert.Throws<ArgumentException>(() => new Condition(name: null, var: null, value: null, items: [
			new(name: new("foo"), var: null, value: null, children: TemplateElementCollection.Empty)
		], children: TemplateElementCollection.Empty));
	}

	[Test]
	public void PickWithNameMatch() {
		var items = new Condition.Li[] { new(new("foo"), false, new("predicate"), new(TemplateElementCollection.Empty)) };
		Assert.That(new Condition(items).Pick(GetTest().RequestProcess), Is.SameAs(items[0]));
	}

	[Test]
	public void PickWithNameNoMatch() {
		var items = new Condition.Li[] { new(new("foo"), false, new("var"), new(TemplateElementCollection.Empty)) };
		Assert.That(new Condition(items).Pick(GetTest().RequestProcess), Is.Null);
	}

	[Test]
	public void PickWithNameWildcardMatch() {
		var items = new Condition.Li[] { new(new("foo"), false, new("*"), new(TemplateElementCollection.Empty)) };
		Assert.That(new Condition(items).Pick(GetTest().RequestProcess), Is.SameAs(items[0]));
	}

	[Test]
	public void PickWithNameWildcardNoMatch() {
		var items = new Condition.Li[] { new(new("bar"), false, new("*"), new(TemplateElementCollection.Empty)) };
		Assert.That(new Condition(items).Pick(GetTest().RequestProcess), Is.Null);
	}

	[Test]
	public void PickWithVarMatch() {
		var items = new Condition.Li[] { new(new("bar"), true, new("var"), new(TemplateElementCollection.Empty)) };
		Assert.That(new Condition(items).Pick(GetTest().RequestProcess), Is.SameAs(items[0]));
	}

	[Test]
	public void PickWithVarNoMatch() {
		var items = new Condition.Li[] { new(new("bar"), true, new("predicate"), new(TemplateElementCollection.Empty)) };
		Assert.That(new Condition(items).Pick(GetTest().RequestProcess), Is.Null);
	}

	[Test]
	public void PickWithVarWildcardMatch() {
		var items = new Condition.Li[] { new(new("bar"), true, new("*"), new(TemplateElementCollection.Empty)) };
		Assert.That(new Condition(items).Pick(GetTest().RequestProcess), Is.SameAs(items[0]));
	}

	[Test]
	public void PickWithVarWildcardNoMatch() {
		var items = new Condition.Li[] { new(new("foo"), true, new("*"), new(TemplateElementCollection.Empty)) };
		Assert.That(new Condition(items).Pick(GetTest().RequestProcess), Is.Null);
	}

	[Test]
	public void PickWithDefault() {
		var items = new Condition.Li[] { new(new("bar"), false, new("*"), TemplateElementCollection.Empty), new(TemplateElementCollection.Empty) };
		Assert.That(new Condition(items).Pick(GetTest().RequestProcess), Is.SameAs(items[1]));
	}

	[Test]
	public void EvaluateWithSingleMatch() {
		var tag = new Condition([new(new("foo"), false, new("predicate"), new("match"))]);
		Assert.That(tag.Evaluate(GetTest().RequestProcess), Is.EqualTo("match"));
	}

	[Test]
	public void EvaluateWithLoop() {
		var tag = new Condition([
			new(new("n"), true, new("0"), TemplateElementCollection.Empty),
			new(null, false, null, new(
				new AngelAiml.Tags.Set(new("n"), true, new(new AngelAiml.Tags.Map(new("predecessor"), new(new Get(new("n"), null, true))))),
				new Loop()
			))
		]);
		Assert.That(tag.Evaluate(GetTest().RequestProcess), Is.EqualTo("210"));
	}

	[Test]
	public void EvaluateWithLoopType1() {
		var tag = new Condition([
			new(new("n"), true, new("3"), new(
				new AngelAiml.Tags.Set(new("n"), true, new(new AngelAiml.Tags.Map(new("predecessor"), new(new Get(new("n"), null, true))))),
				new Loop()
			))
		]);
		Assert.That(tag.Evaluate(GetTest().RequestProcess), Is.EqualTo("2"));
	}

	[Test]
	public void EvaluateWithInfiniteLoop() {
		var test = new AimlTest();
		var tag = new Condition([
			new(new("n"), true, new("0"), TemplateElementCollection.Empty),
			new(null, false, null, new(new Loop()))
		]);
		Assert.Throws<LoopLimitException>(() => test.AssertWarning(() => tag.Evaluate(test.RequestProcess)));
	}

	[Test]
	public void EvaluateWithNoMatch() {
		var tag = new Condition([new(new("foo"), false, new("var"), new("match"))]);
		Assert.That(tag.Evaluate(GetTest().RequestProcess), Is.Empty);
	}
}
