using AngelAiml.Tags;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class MapTests {
	[Test]
	public void Parse() {
		var tag = new AngelAiml.Tags.Map(new("successor"), new("0"));
		Assert.Multiple(() => {
			Assert.That(tag.Name.ToString(), Is.EqualTo("successor"));
			Assert.That(tag.Children.ToString(), Is.EqualTo("0"));
		});
	}

	[Test]
	public void Evaluate() {
		var tag = new AngelAiml.Tags.Map(new("successor"), new("0"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("1"));
	}

	[Test]
	public void EvaluateWithUnknownEntry() {
		var tag = new AngelAiml.Tags.Map(new("successor"), new("foo"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("unknown"));
	}

	[Test]
	public void EvaluateWithUnknownMap() {
		var tag = new AngelAiml.Tags.Map(new("foo"), new("0"));
		Assert.That(tag.Evaluate(new AimlTest().RequestProcess), Is.EqualTo("unknown"));
	}
}
