using AngelAiml.Tags;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class SetTests {
	[Test]
	public void ParseWithName() {
		var tag = new AngelAiml.Tags.Set(name: new("foo"), var: null, children: new("predicate"));
		Assert.That(tag.Key.ToString(), Is.EqualTo("foo"));
		Assert.That(tag.LocalVar, Is.False);
		Assert.That(tag.Children.ToString(), Is.EqualTo("predicate"));
	}

	[Test]
	public void ParseWithVar() {
		var tag = new AngelAiml.Tags.Set(name: null, var: new("bar"), children: new("variable"));
		Assert.That(tag.Key.ToString(), Is.EqualTo("bar"));
		Assert.That(tag.LocalVar, Is.True);
		Assert.That(tag.Children.ToString(), Is.EqualTo("variable"));
	}

	[Test]
	public void ParseWithNameAndVar() {
		Assert.Throws<ArgumentException>(() => new AngelAiml.Tags.Set(name: new("foo"), var: new("bar"), children: new("variable")));
	}

	[Test]
	public void ParseWithNoAttributes() {
		Assert.Throws<ArgumentException>(() => new AngelAiml.Tags.Set(name: null, var: null, children: new("variable")));
	}

	[Test]
	public void EvaluateWithName() {
		var test = new AimlTest();
		var tag = new AngelAiml.Tags.Set(new("foo"), false, new("predicate"));
		Assert.Multiple(() => {
			Assert.That(tag.Evaluate(test.RequestProcess), Is.EqualTo("predicate"));
			Assert.That(test.User.GetPredicate("foo"), Is.EqualTo("predicate"));
		});
	}

	[Test]
	public void EvaluateWithVar() {
		var test = new AimlTest();
		var tag = new AngelAiml.Tags.Set(new("bar"), true, new("variable"));
		Assert.Multiple(() => {
			Assert.That(tag.Evaluate(test.RequestProcess), Is.EqualTo("variable"));
			Assert.That(test.RequestProcess.GetVariable("bar"), Is.EqualTo("variable"));
		});
	}

	[Test]
	public void EvaluateWithSpecificDefaultValue() {
		var test = new AimlTest();
		test.Bot.Config.DefaultPredicates["foo"] = "default";
		test.Bot.Config.UnbindPredicatesWithDefaultValue = true;
		test.User.Predicates["foo"] = "bar";
		var tag = new AngelAiml.Tags.Set(new("foo"), false, new("default"));
		Assert.That(tag.Evaluate(test.RequestProcess), Is.EqualTo("default"));
		Assert.That(test.User.Predicates.ContainsKey("foo"), Is.False);
	}

	[Test]
	public void EvaluateWithGenericDefaultValue() {
		var test = new AimlTest();
		test.Bot.Config.UnbindPredicatesWithDefaultValue = true;
		test.User.Predicates["foo"] = "bar";
		var tag = new AngelAiml.Tags.Set(new("foo"), false, new("unknown"));
		Assert.That(tag.Evaluate(test.RequestProcess), Is.EqualTo("unknown"));
		Assert.That(test.User.Predicates.ContainsKey("foo"), Is.False);
	}

	[Test]
	public void EvaluateWithVarDefaultValue() {
		var test = new AimlTest();
		test.Bot.Config.UnbindPredicatesWithDefaultValue = true;
		test.RequestProcess.Variables["bar"] = "baz";
		var tag = new AngelAiml.Tags.Set(new("bar"), true, new("unknown"));
		Assert.That(tag.Evaluate(test.RequestProcess), Is.EqualTo("unknown"));
		Assert.That(test.RequestProcess.Variables.ContainsKey("bar"), Is.False);
	}
}
