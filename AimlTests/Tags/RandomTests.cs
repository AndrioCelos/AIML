using Aiml.Tags;
using NUnit.Framework.Constraints;

namespace Aiml.Tests.Tags;
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
		var items = new Aiml.Tags.Random.Li[] { new(new("1")), new(new("2")), new(new("3")) };
		var tag = new Aiml.Tags.Random(items);
		Assert.AreSame(items[0], tag.Pick(test.RequestProcess));
	}

	[Test]
	public void Evaluate() {
		var test = new AimlTest(new MockRandom());
		var items = new Aiml.Tags.Random.Li[] { new(new("1")), new(new("2")), new(new("3")) };
		var tag = new Aiml.Tags.Random(items);
		Assert.AreEqual("1", tag.Evaluate(test.RequestProcess));
		Assert.AreEqual("2", tag.Evaluate(test.RequestProcess));
	}
}
