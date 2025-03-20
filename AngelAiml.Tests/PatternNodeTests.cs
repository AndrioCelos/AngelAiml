using AngelAiml.Sets;

namespace AngelAiml.Tests;

[TestFixture]
public class PatternNodeTests {
	[Test]
	public void AddChild() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(TemplateElementCollection.Empty);
		node.AddChild([new("TEST"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("testing"), new("2")], template);
		Assert.That(node.Children["TEST"].Children["<that>"].Children["*"].Children["<topic>"].Children["testing"].Template, Is.Null);

		var template2 = new Template(TemplateElementCollection.Empty);
		node.AddChild([new("TEST"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("testing")], template2);
		Assert.Multiple(() => {
			Assert.That(node.Children["TEST"].Children["<that>"].Children["*"].Children["<topic>"].Children["testing"].Children["2"].Template, Is.SameAs(template));
			Assert.That(node.Children["TEST"].Children["<that>"].Children["*"].Children["<topic>"].Children["testing"].Template, Is.SameAs(template2));
		});
	}

	[Test]
	public void AddChild_Set() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(TemplateElementCollection.Empty);
		node.AddChild([new("number", true), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template);

		var child = node.SetChildren.Single();
		Assert.Multiple(() => {
			Assert.That(child.SetName, Is.EqualTo("number"));
			Assert.That(child.Node.Children["<that>"].Children["*"].Children["<topic>"].Children["*"].Template, Is.SameAs(template));
		});
	}

	[Test]
	public void Search() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(TemplateElementCollection.Empty);
		node.AddChild([new("TEST"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template);

		var test = new AimlTest() { SampleRequestSentenceText = "test" };
		var foundTemplate = node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false);
		Assert.That(foundTemplate, Is.SameAs(template));
		Assert.Multiple(() => {
			Assert.That(test.RequestProcess.Star.Count, Is.EqualTo(0));
			Assert.That(test.RequestProcess.ThatStar, Has.Count.EqualTo(1));
			Assert.That(test.RequestProcess.TopicStar, Has.Count.EqualTo(1));
		});
	}

	[Test]
	public void Search_Wildcard() {
		// ^ should be able to match multiple words and take precedence over *.
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(TemplateElementCollection.Empty);
		var template2 = new Template(TemplateElementCollection.Empty);
		node.AddChild([new("*"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template);
		node.AddChild([new("^"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template2);

		var test = new AimlTest() { SampleRequestSentenceText = "1 2 3" };
		Assert.Multiple(() => {
			Assert.That(node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false), Is.SameAs(template2));
			Assert.That(test.RequestProcess.Star, Has.Count.EqualTo(1));
		});
		Assert.That(test.RequestProcess.Star[0], Is.EqualTo("1 2 3"));
	}

	[Test]
	public void Search_Sets() {
		// Sets should be able to match multiple words and take precedence over *.
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(TemplateElementCollection.Empty);
		var template2 = new Template(TemplateElementCollection.Empty);
		node.AddChild([new("testset", true), new("*"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template);
		node.AddChild([new("*"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template2);

		var test = new AimlTest() { SampleRequestSentenceText = "test entry 1" };
		test.Bot.Sets.Add("testset", new StringSet(["test entry"], StringComparer.InvariantCultureIgnoreCase));
		Assert.Multiple(() => {
			Assert.That(node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false), Is.SameAs(template));
			Assert.That(test.RequestProcess.Star, Has.Count.EqualTo(2));
		});
		Assert.Multiple(() => {
			Assert.That(test.RequestProcess.Star[0], Is.EqualTo("test entry"));
			Assert.That(test.RequestProcess.Star[1], Is.EqualTo("1"));
		});
	}

	[Test]
	public void Search_SetsTakeLongestMatch() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(TemplateElementCollection.Empty);
		node.AddChild([new("testset", true), new("*"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template);

		var test = new AimlTest() { SampleRequestSentenceText = "foo bar baz" };
		test.Bot.Sets.Add("testset", new StringSet(["foo", "foo bar"], StringComparer.InvariantCultureIgnoreCase));
		Assert.Multiple(() => {
			Assert.That(node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false), Is.SameAs(template));
			Assert.That(test.RequestProcess.Star, Has.Count.EqualTo(2));
		});
		Assert.Multiple(() => {
			Assert.That(test.RequestProcess.Star[0], Is.EqualTo("foo bar"));
			Assert.That(test.RequestProcess.Star[1], Is.EqualTo("baz"));
		});
	}

	[Test]
	public void Search_OptionalWildcard() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(TemplateElementCollection.Empty);
		var template2 = new Template(TemplateElementCollection.Empty);
		node.AddChild([new("TEST"), new("^"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template);
		node.AddChild([new("TEST"), new("*"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template2);

		var test = new AimlTest() { SampleRequestSentenceText = "TEST" };
		Assert.Multiple(() => {
			Assert.That(node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false), Is.SameAs(template));
			Assert.That(test.RequestProcess.Star, Has.Count.EqualTo(1));
		});
		Assert.That(test.RequestProcess.Star[0], Is.EqualTo("nil"));
	}

	[Test]
	public void Search_PriorityWildcard() {
		// # should take precedence over exact match.
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(TemplateElementCollection.Empty);
		var template2 = new Template(TemplateElementCollection.Empty);
		node.AddChild([new("#"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template);
		node.AddChild([new("TEST"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template2);

		var test = new AimlTest() { SampleRequestSentenceText = "test" };
		Assert.Multiple(() => {
			Assert.That(node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false), Is.SameAs(template));
			Assert.That(test.RequestProcess.Star, Has.Count.EqualTo(1));
		});
		Assert.That(test.RequestProcess.Star[0], Is.EqualTo("test"));
	}

	[Test]
	public void Search_PriorityWildcardExampleFromAimlSpec() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(TemplateElementCollection.Empty);
		var template2 = new Template(TemplateElementCollection.Empty);
		var template3 = new Template(TemplateElementCollection.Empty);
		node.AddChild([new("_"), new("ANGELINA"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template);
		node.AddChild([new("$WHO"), new("IS"), new("ANGELINA"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template2);
		node.AddChild([new("HELLO"), new("ANGELINA"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template3);

		var test = new AimlTest() { SampleRequestSentenceText = "Hello Angelina" };  // Should match _ ANGELINA.
		var foundTemplate = node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false);
		Assert.That(foundTemplate, Is.SameAs(template));
		Assert.Multiple(() => {
			Assert.That(test.RequestProcess.Star, Has.Count.EqualTo(1));
			Assert.That(test.RequestProcess.ThatStar, Has.Count.EqualTo(1));
			Assert.That(test.RequestProcess.TopicStar, Has.Count.EqualTo(1));
		});

		test = new AimlTest() { SampleRequestSentenceText = "Who is Angelina" };  // Should match $WHO IS ANGELINA.
		foundTemplate = node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false);
		Assert.That(foundTemplate, Is.SameAs(template2));
		Assert.Multiple(() => {
			Assert.That(test.RequestProcess.Star.Count, Is.EqualTo(0));
			Assert.That(test.RequestProcess.ThatStar, Has.Count.EqualTo(1));
			Assert.That(test.RequestProcess.TopicStar, Has.Count.EqualTo(1));
		});
	}

	[Test]
	public void Search_AdjacentWildcardsAreUngreedy() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(TemplateElementCollection.Empty);
		node.AddChild([new("*"), new("*"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template);

		var test = new AimlTest() { SampleRequestSentenceText = "1 2 3" };
		Assert.Multiple(() => {
			Assert.That(node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false), Is.SameAs(template));
			Assert.That(test.RequestProcess.Star, Has.Count.EqualTo(2));
		});
		Assert.Multiple(() => {
			Assert.That(test.RequestProcess.Star[0], Is.EqualTo("1"));
			Assert.That(test.RequestProcess.Star[1], Is.EqualTo("2 3"));
		});
	}

	[Test]
	public void GetTemplatesTest() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(TemplateElementCollection.Empty);
		var template2 = new Template(TemplateElementCollection.Empty);
		var template3 = new Template(TemplateElementCollection.Empty);
		node.AddChild([new("_"), new("ANGELINA"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template);
		node.AddChild([new("$WHO"), new("IS"), new("ANGELINA"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template2);
		node.AddChild([new("HELLO"), new("ANGELINA"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*")], template3);

		var templates = node.GetTemplates().Select(e => e.Value).ToList();
		Assert.That(templates, Has.Count.EqualTo(3));
		Assert.That(templates, Contains.Item(template));
		Assert.That(templates, Contains.Item(template2));
		Assert.That(templates, Contains.Item(template3));
	}
}
