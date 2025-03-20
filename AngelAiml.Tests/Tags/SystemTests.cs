using AngelAiml.Tags;
using NUnit.Framework.Internal;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class SystemTests {
	[Test]
	public void Evaluate_DisabledByDefault() {
		var test = new AimlTest();
		var tag = new AngelAiml.Tags.System(new("foo"));
		Assert.That(test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()), Is.EqualTo(test.Bot.Config.SystemFailedMessage));
	}
}
