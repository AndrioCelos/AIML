using System.Xml;
using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class OobTests {
	[Test]
	public void Evaluate() {
		var tag = new Oob("oob", null, new("<testoob>foo</testoob>"));
		Assert.AreEqual("<oob><testoob>foo</testoob></oob>", tag.Evaluate(new AimlTest().RequestProcess));
	}

	[Test]
	public void FromXml() {
		var test = new AimlTest();
		var xmlDocument = new XmlDocument();
		xmlDocument.LoadXml("<oob><testoob><input/></testoob></oob>");
		var tag = Oob.FromXml(xmlDocument.DocumentElement!, test.Bot.AimlLoader);
		Assert.AreEqual("oob", tag.Name);
		Assert.IsEmpty(tag.Attributes);
		Assert.IsInstanceOf<Oob>(tag.Children[0]);
		Assert.IsInstanceOf<Input>(((Oob) tag.Children[0]).Children[0]);
	}

	[Test]
	public void FromXmlWithRichMediaElement() {
		var test = new AimlTest();
		var xmlDocument = new XmlDocument();
		xmlDocument.LoadXml("<card><title>Test</title></card>");
		Oob.FromXml(xmlDocument.DocumentElement!, test.Bot.AimlLoader);
	}

	[Test]
	public void FromXmlWithRichMediaElementAndInvalidAttributes() {
		var test = new AimlTest();
		var xmlDocument = new XmlDocument();
		xmlDocument.LoadXml("<card><foo/></card>");
		Assert.Throws<AimlException>(() => Oob.FromXml(xmlDocument.DocumentElement!, test.Bot.AimlLoader, "image", "title", "subtitle", "button"));
	}
}
