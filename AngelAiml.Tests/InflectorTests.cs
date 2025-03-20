namespace AngelAiml.Tests;

[TestFixture]
public class InflectorTests {
	[Test]
	public void Singularize() {
		var subject = new Inflector(StringComparer.InvariantCultureIgnoreCase);
		Assert.Multiple(() => {
			Assert.That(subject.Singularize("bots"), Is.EqualTo("bot"));
			Assert.That(subject.Singularize("Axes"), Is.EqualTo("Axe"));
			Assert.That(subject.Singularize("tomatoes"), Is.EqualTo("tomato"));
			Assert.That(subject.Singularize("theses"), Is.EqualTo("thesis"));
			Assert.That(subject.Singularize("thesis"), Is.EqualTo("thesis"));
			Assert.That(subject.Singularize("elves"), Is.EqualTo("elf"));
			Assert.That(subject.Singularize("parties"), Is.EqualTo("party"));
			Assert.That(subject.Singularize("foxes"), Is.EqualTo("fox"));
			Assert.That(subject.Singularize("statuses"), Is.EqualTo("status"));
			Assert.That(subject.Singularize("Moxen"), Is.EqualTo("Mox"));
			Assert.That(subject.Singularize("people"), Is.EqualTo("person"));
			Assert.That(subject.Singularize("species"), Is.EqualTo("species"));
		});
	}

	[Test]
	public void Pluralize() {
		var subject = new Inflector(StringComparer.InvariantCultureIgnoreCase);
		Assert.Multiple(() => {
			Assert.That(subject.Pluralize("bot"), Is.EqualTo("bots"));
			Assert.That(subject.Pluralize("Axe"), Is.EqualTo("Axes"));
			Assert.That(subject.Pluralize("Axis"), Is.EqualTo("Axes"));
			Assert.That(subject.Pluralize("Test"), Is.EqualTo("Tests"));
			Assert.That(subject.Pluralize("missus"), Is.EqualTo("missus"));
			Assert.That(subject.Pluralize("news"), Is.EqualTo("news"));
			Assert.That(subject.Pluralize("thesis"), Is.EqualTo("theses"));
			Assert.That(subject.Pluralize("theses"), Is.EqualTo("theses"));
			Assert.That(subject.Pluralize("hive"), Is.EqualTo("hives"));
			Assert.That(subject.Pluralize("elf"), Is.EqualTo("elves"));
			Assert.That(subject.Pluralize("party"), Is.EqualTo("parties"));
			Assert.That(subject.Pluralize("series"), Is.EqualTo("series"));
			Assert.That(subject.Pluralize("movie"), Is.EqualTo("movies"));
			Assert.That(subject.Pluralize("fox"), Is.EqualTo("foxes"));
			Assert.That(subject.Pluralize("dormouse"), Is.EqualTo("dormice"));
			Assert.That(subject.Pluralize("bus"), Is.EqualTo("buses"));
			Assert.That(subject.Pluralize("horseshoe"), Is.EqualTo("horseshoes"));
			Assert.That(subject.Pluralize("crisis"), Is.EqualTo("crises"));
			Assert.That(subject.Pluralize("crises"), Is.EqualTo("crises"));
			Assert.That(subject.Pluralize("octopus"), Is.EqualTo("octopi"));
			Assert.That(subject.Pluralize("status"), Is.EqualTo("statuses"));
			Assert.That(subject.Pluralize("box"), Is.EqualTo("boxes"));
			Assert.That(subject.Pluralize("ox"), Is.EqualTo("oxen"));
			Assert.That(subject.Pluralize("person"), Is.EqualTo("people"));
			Assert.That(subject.Pluralize("species"), Is.EqualTo("species"));
		});
	}
}