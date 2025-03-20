using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class ThinkTests {
	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		var tag = new Think(new(new AngelAiml.Tags.Set(new("foo"), false, new("predicate"))));
		Assert.IsEmpty(tag.Evaluate(test.RequestProcess));
		Assert.AreEqual("predicate", test.User.GetPredicate("foo"));
	}
}
