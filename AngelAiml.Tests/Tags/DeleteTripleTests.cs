using AngelAiml.Tags;

namespace AngelAiml.Tests.Tags;

[TestFixture]
public class DeleteTripleTests {
	private static AimlTest GetTest() {
		var test = new AimlTest();
		test.Bot.Triples.Add("Alice", "age", "25");
		test.Bot.Triples.Add("Alice", "friendOf", "Bob");
		test.Bot.Triples.Add("Alice", "friendOf", "Carol");
		test.Bot.Triples.Add("Alice", "friendOf", "Dan");
		test.Bot.Triples.Add("Bob", "age", "25");
		test.Bot.Triples.Add("Carol", "age", "27");
		test.Bot.Triples.Add("Carol", "friendOf", "Erin");
		test.Bot.Triples.Add("Dan", "age", "28");
		test.Bot.Triples.Add("Dan", "friendOf", "Erin");
		return test;
	}

	[Test]
	public void ParseComplete() {
		var tag = new DeleteTriple(new("foo"), new("r"), new("bar"));
		Assert.Multiple(() => {
			Assert.That(tag.Subject.ToString(), Is.EqualTo("foo"));
			Assert.That(tag.Predicate?.ToString(), Is.EqualTo("r"));
			Assert.That(tag.Object?.ToString(), Is.EqualTo("bar"));
		});
	}

	[Test]
	public void ParseSubjectAndPredicateOnly() {
		var tag = new DeleteTriple(new("foo"), new("r"), null);
		Assert.Multiple(() => {
			Assert.That(tag.Subject.ToString(), Is.EqualTo("foo"));
			Assert.That(tag.Predicate?.ToString(), Is.EqualTo("r"));
		});
		Assert.That(tag.Object, Is.Null);
	}

	[Test]
	public void ParseSubjectOnly() {
		var tag = new DeleteTriple(new("foo"), null, null);
		Assert.Multiple(() => {
			Assert.That(tag.Subject.ToString(), Is.EqualTo("foo"));
			Assert.That(tag.Predicate, Is.Null);
			Assert.That(tag.Object, Is.Null);
		});
	}

	[Test]
	public void EvaluateComplete() {
		var test = GetTest();
		var tag = new DeleteTriple(new("Alice"), new("friendOf"), new("Bob"));
		tag.Evaluate(test.RequestProcess);
		Assert.That(test.Bot.Triples, Has.Count.EqualTo(8));
	}

	[Test]
	public void EvaluateCompleteNonexistentTriple() {
		var test = GetTest();
		var tag = new DeleteTriple(new("Bob"), new("friendOf"), new("Erin"));
		tag.Evaluate(test.RequestProcess);
		Assert.That(test.Bot.Triples, Has.Count.EqualTo(9));
	}

	[Test]
	public void EvaluateSubjectAndPredicateOnly() {
		var test = GetTest();
		var tag = new DeleteTriple(new("Alice"), new("friendOf"), null);
		tag.Evaluate(test.RequestProcess);
		Assert.That(test.Bot.Triples, Has.Count.EqualTo(6));
	}

	[Test]
	public void EvaluateSubjectOnly() {
		var test = GetTest();
		var tag = new DeleteTriple(new("Alice"), null, null);
		tag.Evaluate(test.RequestProcess);
		Assert.That(test.Bot.Triples, Has.Count.EqualTo(5));
	}
}
