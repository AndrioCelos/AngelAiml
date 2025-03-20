namespace AngelAiml.Tests;

[TestFixture]
public class TripleCollectionTests {
	[Test]
	public void Add_NewTriple() {
		var triple = new Triple("Alice", "age", "25");
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase);
		Assert.That(subject.Add(triple), Is.True);
		Assert.That(subject, Has.Count.EqualTo(1));
		Assert.That(subject.First(), Is.SameAs(triple));
	}
	[Test]
	public void Add_ExistingTriple() {
		var triple = new Triple("Alice", "age", "25");
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) { triple };
		Assert.That(subject.Add("Alice", "age", "25"), Is.False);
		Assert.That(subject, Has.Count.EqualTo(1));
		Assert.That(subject.First(), Is.SameAs(triple));
	}

	[Test]
	public void Remove_ExistingTriple() {
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) {
			{ "Alice", "age", "25" },
			{ "Alice", "friendOf", "Bob" }
		};
		Assert.That(subject.Remove("Alice", "age", "25"), Is.True);
		Assert.That(subject, Has.Count.EqualTo(1));
	}

	[Test]
	public void Remove_AbsentTriple() {
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) {
			{ "Alice", "age", "25" },
			{ "Alice", "friendOf", "Bob" }
		};
		Assert.That(subject.Remove("Alice", "friendOf", "Carol"), Is.False);
		Assert.That(subject, Has.Count.EqualTo(2));
	}

	[Test]
	public void RemoveAll_SubjectAndPredicate() {
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) {
			{ "Alice", "age", "25" },
			{ "Alice", "friendOf", "Bob" },
			{ "Alice", "friendOf", "Carol" },
			{ "Alice", "friendOf", "Dan" }
		};
		Assert.Multiple(() => {
			Assert.That(subject.RemoveAll("Alice", "friendOf"), Is.EqualTo(3));
			Assert.That(subject, Has.Count.EqualTo(1));
		});
	}

	[Test]
	public void RemoveAll_SubjectAndPredicateAbsent() {
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) {
			{ "Alice", "age", "25" }
		};
		Assert.Multiple(() => {
			Assert.That(subject.RemoveAll("Alice", "friendOf"), Is.EqualTo(0));
			Assert.That(subject, Has.Count.EqualTo(1));
		});
	}

	[Test]
	public void RemoveAll_Subject() {
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) {
			{ "Alice", "age", "25" },
			{ "Alice", "friendOf", "Bob" },
			{ "Alice", "friendOf", "Carol" },
			{ "Alice", "friendOf", "Dan" }
		};
		Assert.Multiple(() => {
			Assert.That(subject.RemoveAll("Alice"), Is.EqualTo(4));
			Assert.That(subject.Count, Is.EqualTo(0));
		});
	}

	[Test]
	public void RemoveAll_SubjectAbsent() {
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) {
			{ "Alice", "age", "25" }
		};
		Assert.Multiple(() => {
			Assert.That(subject.RemoveAll("Bob", "friendOf"), Is.EqualTo(0));
			Assert.That(subject, Has.Count.EqualTo(1));
		});
	}

	[Test()]
	public void ClearTest() {
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) {
			{ "Alice", "age", "25" },
			{ "Alice", "friendOf", "Bob" }
		};
		subject.Clear();
		Assert.That(subject.Count, Is.EqualTo(0));
		Assert.That(subject, Is.Empty);
	}

	private static TripleCollection GetTestCollection() => new(StringComparer.InvariantCultureIgnoreCase) {
		{ "Alice", "age", "25" },
		{ "Alice", "friendOf", "Bob" },
		{ "Alice", "friendOf", "Carol" },
		{ "Alice", "friendOf", "Dan" },
		{ "Bob", "age", "25" },
		{ "Carol", "age", "27" },
		{ "Carol", "friendOf", "Erin" },
		{ "Dan", "age", "28" },
		{ "Dan", "friendOf", "Erin" }
	};

	[Test]
	public void Match_CaseInsensitive() {
		var result = GetTestCollection().Match("alice", "friendof", "bob").Single();
		Assert.Multiple(() => {
			Assert.That(result.Subject, Is.EqualTo("Alice"));
			Assert.That(result.Predicate, Is.EqualTo("friendOf"));
			Assert.That(result.Object, Is.EqualTo("Bob"));
		});
	}

	[TestCase("Carol", "friendOf", "Erin", ExpectedResult = 1, TestName = "Match count (all properties; present)")]
	[TestCase("Bob", "friendOf", "Erin", ExpectedResult = 0, TestName = "Match count (all properties; absent)")]
	[TestCase("Alice", "friendOf", null, ExpectedResult = 3, TestName = "Match count (subj and pred; present)")]
	[TestCase("Eve", "friendOf", null, ExpectedResult = 0, TestName = "Match count (subj and pred; absent)")]
	[TestCase("Alice", null, null, ExpectedResult = 4, TestName = "Match count (subj only; present)")]
	[TestCase("Eve", null, null, ExpectedResult = 0, TestName = "Match count (subj only; absent)")]
	[TestCase(null, "age", "25", ExpectedResult = 2, TestName = "Match count (obj and pred; present)")]
	[TestCase(null, "age", "24", ExpectedResult = 0, TestName = "Match count (obj and pred; absent)")]
	[TestCase(null, null, "Erin", ExpectedResult = 2, TestName = "Match count (obj only; present)")]
	[TestCase(null, null, "Eve", ExpectedResult = 0, TestName = "Match count (obj only; absent)")]
	[TestCase(null, "friendOf", null, ExpectedResult = 5, TestName = "Match count (pred only; present)")]
	[TestCase(null, "blocked", null, ExpectedResult = 0, TestName = "Match count (pred only; absent)")]
	[TestCase(null, null, null, ExpectedResult = 9, TestName = "Match count (no properties; present)")]
	public int MatchCountTest(string? subj, string? pred, string? obj)
		=> GetTestCollection().Match(subj, pred, obj).Count();

	[TestCase(null, null, null, ExpectedResult = 0, TestName = "Match count (no properties; empty)")]
	public int MatchCountTestEmpty(string? subj, string? pred, string? obj)
		=> new TripleCollection(StringComparer.InvariantCultureIgnoreCase).Match(subj, pred, obj).Count();
}
