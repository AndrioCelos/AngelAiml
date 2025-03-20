using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class ThinkTests {
	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		var tag = new Think(new(new AngelAiml.Tags.Set(new("foo"), false, new("predicate"))));
		Assert.Multiple(() => {
			Assert.That(tag.Evaluate(test.RequestProcess), Is.Empty);
			Assert.That(test.User.GetPredicate("foo"), Is.EqualTo("predicate"));
		});
	}
}
