namespace AngelAiml.Tests;

[TestFixture]
public class SubstitutionListTests {
	[Test]
	public void Apply_AdjacentWords() {
		var subject = new SubstitutionList() { new(" foo ", " bar ") };
		Assert.That(subject.Apply("A foo foo z"), Is.EqualTo("A bar bar z"));
	}

	[Test]
	public void Apply_LastWord() {
		var subject = new SubstitutionList() { new(" foo ", " bar ") };
		Assert.That(subject.Apply("A foo"), Is.EqualTo("A bar"));
	}

	[Test]
	public void Apply_FirstWordSentenceCase() {
		var subject = new SubstitutionList() { new(" foo ", " bar ") };
		Assert.That(subject.Apply("foo bar"), Is.EqualTo("bar bar"));
	}

	[Test]
	public void Apply_SentenceCase() {
		var subject = new SubstitutionList(true) { new(" foo ", " bar ") };
		Assert.That(subject.Apply(" Foo "), Is.EqualTo(" Bar "));
	}

	[Test]
	public void Apply_Uppercase() {
		var subject = new SubstitutionList(true) { new(" foo ", " bar ") };
		Assert.That(subject.Apply(" FOO "), Is.EqualTo(" BAR "));
	}

	[Test]
	public void Apply_ChainedSubstitutions() {
		// Multiple substitutions should not be applied to the same word.
		// This matches Pandorabots, which is different from Program AB's substitutions.
		var subject = new SubstitutionList() { new(" you ", " me "), new(" with me ", " with you "), new(" me ", " you ") };
		Assert.That(subject.Apply("Test with you and me talking"), Is.EqualTo("Test with me and you talking"));
	}

	[Test]
	public void Apply_NoMatch() {
		var subject = new SubstitutionList() { new(" foo ", " bar ") };
		var s = " bar ";
		Assert.That(subject.Apply(s), Is.SameAs(s));
	}
}
