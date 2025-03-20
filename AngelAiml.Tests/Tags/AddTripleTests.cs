using AngelAiml.Tags;

namespace AngelAiml.Tests.Tags;

[TestFixture]
public class AddTripleTests {
	[Test]
	public void Initialise() {
		var tag = new AddTriple(new("foo"), new("r"), new("bar"));
		Assert.Multiple(() => {
			Assert.That(tag.Subject.ToString(), Is.EqualTo("foo"));
			Assert.That(tag.Predicate.ToString(), Is.EqualTo("r"));
			Assert.That(tag.Object.ToString(), Is.EqualTo("bar"));
		});
	}

	[Test]
	public void EvaluateWithNewTriple() {
		var test = new AimlTest();
		var tag = new AddTriple(new("foo"), new("r"), new("bar"));
		tag.Evaluate(test.RequestProcess);
		Assert.That(test.Bot.Triples.Single().ToString(), Is.EqualTo("{ Subject = foo, Predicate = r, Object = bar }"));
	}

	[Test]
	public void EvaluateWithExistingTriple() {
		var test = new AimlTest();
		test.Bot.Triples.Add("foo", "r", "bar");
		var tag = new AddTriple(new("foo"), new("r"), new("bar"));
		tag.Evaluate(test.RequestProcess);
		Assert.That(test.Bot.Triples.Single().ToString(), Is.EqualTo("{ Subject = foo, Predicate = r, Object = bar }"));
	}

	[Test]
	public void EvaluateWithInvalidSubject() {
		var test = new AimlTest();
		var tag = new AddTriple(new(" "), new("r"), new("bar"));
		test.AssertWarning(() => tag.Evaluate(test.RequestProcess));
		Assert.That(test.Bot.Triples, Is.Empty);
		tag = new AddTriple(new("?foo"), new("r"), new("bar"));
		test.AssertWarning(() => tag.Evaluate(test.RequestProcess));
		Assert.That(test.Bot.Triples, Is.Empty);
	}

	[Test]
	public void EvaluateWithInvalidPredicate() {
		var test = new AimlTest();
		var tag = new AddTriple(new("foo"), new(" "), new("bar"));
		test.AssertWarning(() => tag.Evaluate(test.RequestProcess));
		Assert.That(test.Bot.Triples, Is.Empty);
		tag = new AddTriple(new("foo"), new("?r"), new("bar"));
		test.AssertWarning(() => tag.Evaluate(test.RequestProcess));
		Assert.That(test.Bot.Triples, Is.Empty);
	}

	[Test]
	public void EvaluateWithInvalidObject() {
		var test = new AimlTest();
		var tag = new AddTriple(new("foo"), new("r"), new(" "));
		test.AssertWarning(() => tag.Evaluate(test.RequestProcess));
		Assert.That(test.Bot.Triples, Is.Empty);
		tag = new AddTriple(new("foo"), new("r"), new("?bar"));
		test.AssertWarning(() => tag.Evaluate(test.RequestProcess));
		Assert.That(test.Bot.Triples, Is.Empty);
	}
}
