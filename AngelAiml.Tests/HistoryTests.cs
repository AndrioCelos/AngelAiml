namespace AngelAiml.Tests;

[TestFixture]
public class HistoryTests {
	[Test]
	public void Initialise() {
		var subject = new History<string>(4);
		Assert.Multiple(() => {
			Assert.That(subject.Capacity, Is.EqualTo(4));
			Assert.That(subject.Count, Is.EqualTo(0));
		});
	}

	[Test]
	public void Add() {
		var subject = new History<string>(4) {
			"Item 1",
			"Item 2"
		};
		Assert.That(subject, Has.Count.EqualTo(2));
		Assert.Multiple(() => {
			Assert.That(subject[0], Is.EqualTo("Item 2"));
			Assert.That(subject[1], Is.EqualTo("Item 1"));
		});
	}

	[Test]
	public void Add_PastCapacity() {
		var subject = new History<string>(4) {
			"Item 1",
			"Item 2",
			"Item 3",
			"Item 4",
			"Item 5"
		};
		Assert.That(subject, Has.Count.EqualTo(4));
		Assert.Multiple(() => {
			Assert.That(subject[0], Is.EqualTo("Item 5"));
			Assert.That(subject[3], Is.EqualTo("Item 2"));
		});
		Assert.Throws<IndexOutOfRangeException>(() => _ = subject[4]);
	}
}