using System.Xml;
using System.Xml.Linq;
using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class OobTests {
	[Test]
	public void Evaluate() {
		var tag = new Oob("oob", Enumerable.Empty<XAttribute>(), new("<testoob>foo</testoob>"));
		Assert.AreEqual("<oob><testoob>foo</testoob></oob>", tag.Evaluate(new AimlTest().RequestProcess));
	}

	[Test]
	public void FromXml() {
		var tag = Oob.FromXml(XElement.Parse("<oob><testoob><input/></testoob></oob>"), new AimlTest().Bot.AimlLoader);
		Assert.AreEqual("oob", tag.Name);
		Assert.IsEmpty(tag.Attributes);
		Assert.IsInstanceOf<Oob>(tag.Children[0]);
		Assert.IsInstanceOf<Input>(((Oob) tag.Children[0]).Children[0]);
	}

	[Test]
	public void FromXmlWithRichMediaElement() {
		Oob.FromXml(XElement.Parse("<card><title>Test</title></card>"), new AimlTest().Bot.AimlLoader);
	}

	[Test]
	public void FromXmlWithRichMediaElementAndInvalidAttributes() {
		Assert.Throws<AimlException>(() => Oob.FromXml(XElement.Parse("<card><foo/></card>"), new AimlTest().Bot.AimlLoader, "image", "title", "subtitle", "button"));
	}
}
