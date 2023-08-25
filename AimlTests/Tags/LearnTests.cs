using System.Text;
using System.Xml;
using System.Xml.Linq;
using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class LearnTests {
	[Test]
	public void Parse() {
		var el = XElement.Parse("<learn><category><pattern>TEST LEARN</pattern><template>Original: <eval><input/></eval>; Current: <input/></template></category></learn>");
		var tag = new Learn(el);
		Assert.AreSame(el, tag.Element);
	}

	[TestCase("<learn/>", TestName = "Parse (no category)")]
	[TestCase("<learn><category><template/></category></learn>", TestName = "Parse (no pattern)")]
	[TestCase("<learn><category><pattern>TEST</pattern></category></learn>", TestName = "Parse (no template)")]
	[TestCase("<learn><category><foo/></category></learn>", TestName = "Parse (invalid category element)")]
	[TestCase("<learn><pattern>TEST</pattern></learn>", TestName = "Parse (invalid AIML element)")]
	public void ParseInvalid(string xml) {
		Assert.Throws<ArgumentException>(() => new Learn(XElement.Parse(xml)));
	}

	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		var tag = new Learn(XElement.Parse("<learn><category><pattern>TEST LEARN</pattern><template>Original: <eval><input/></eval>; Current: <input/></template></category></learn>"));
		test.User.Requests.Add(new("TEST", test.User, test.Bot));
		tag.Evaluate(test.RequestProcess);

		test.User.Requests.Add(new("TEST LEARN", test.User, test.Bot));
		Assert.AreEqual("Original: TEST; Current: TEST LEARN", AimlTest.GetTemplate(test.User.Graphmaster, "TEST", "LEARN", "<that>", "*", "<topic>", "*").Content.Evaluate(new(new(new("TEST LEARN", test.User, test.Bot), "TEST LEARN"), 0, false)));
	}

	[Test]
	public void Evaluate_DoesNotModifyOriginalElement() {
		var test = new AimlTest();
		var tag = new Learn(XElement.Parse("<learn><category><pattern>TEST LEARN</pattern><template>Original: <eval><input/></eval>; Current: <input/></template></category></learn>"));
		var xml = GetOuterXml(tag.Element);
		test.User.Requests.Add(new("TEST", test.User, test.Bot));
		tag.Evaluate(test.RequestProcess);

		Assert.AreEqual(xml, GetOuterXml(tag.Element));
	}

	[Test]
	public void EvaluateWithThatAndTopic() {
		var test = new AimlTest();
		var tag = new Learn(XElement.Parse("<learn><category><pattern>TEST LEARN</pattern><that>LEARNED</that><topic>TESTS</topic><template>Original: <eval><input/></eval>; Current: <input/></template></category></learn>"));
		test.User.Requests.Add(new("TEST", test.User, test.Bot));
		tag.Evaluate(test.RequestProcess);
		AimlTest.GetTemplate(test.User.Graphmaster, "TEST", "LEARN", "<that>", "LEARNED", "<topic>", "TESTS");
	}

	internal static string GetOuterXml(XElement element) {
		var builder = new StringBuilder();
		using var writer = XmlWriter.Create(builder);
		element.WriteTo(writer);
		writer.Flush();
		return builder.ToString();
	}
}
