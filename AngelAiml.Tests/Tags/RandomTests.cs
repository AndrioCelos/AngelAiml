using AngelAiml.Tags;

namespace AngelAiml.Tests.Tags;
[TestFixture]
public class RandomTests {
	private class MockRandom : System.Random {
		private int num;

		public override int Next() => num++;
		public override int Next(int maxValue) => num++ % maxValue;
		public override int Next(int minValue, int maxValue) => num++ % (maxValue - minValue) + minValue;
	}

	[Test]
	public void Pick() {
		var test = new AimlTest(new MockRandom());
		var items = new AngelAiml.Tags.Random.Li[] { new(new("1")), new(new("2")), new(new("3")) };
		var tag = new AngelAiml.Tags.Random(items);
		Assert.That(tag.Pick(test.RequestProcess), Is.SameAs(items[0]));
	}

	[Test]
	public void Evaluate() {
		var test = new AimlTest(new MockRandom());
		var items = new AngelAiml.Tags.Random.Li[] { new(new("1")), new(new("2")), new(new("3")) };
		var tag = new AngelAiml.Tags.Random(items);
		Assert.That(tag.Evaluate(test.RequestProcess), Is.EqualTo("1"));
		Assert.That(tag.Evaluate(test.RequestProcess), Is.EqualTo("2"));
	}
}
