using Aiml.Tags;

namespace Aiml.Tests.Tags;
[TestFixture]
public class MapTests {
	[Test]
	public void Parse() {
		var tag = new Aiml.Tags.Map(new("successor"), new("0"));
		Assert.AreEqual("successor", tag.Name.ToString());
		Assert.AreEqual("0", tag.Children.ToString());
	}

	[Test]
	public void Evaluate() {
		var tag = new Aiml.Tags.Map(new("successor"), new("0"));
		Assert.AreEqual("1", tag.Evaluate(new AimlTest().RequestProcess));
	}

	[Test]
	public void EvaluateWithUnknownEntry() {
		var tag = new Aiml.Tags.Map(new("successor"), new("foo"));
		Assert.AreEqual("unknown", tag.Evaluate(new AimlTest().RequestProcess));
	}

	[Test]
	public void EvaluateWithUnknownMap() {
		var tag = new Aiml.Tags.Map(new("foo"), new("0"));
		Assert.AreEqual("unknown", tag.Evaluate(new AimlTest().RequestProcess));
	}
}
