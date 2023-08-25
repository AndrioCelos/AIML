using System.Text;
using System.Xml;
using System.Xml.Linq;
using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class LearnFTests {
	[Test]
	public void Parse() {
		var el = XElement.Parse("<learnf><category><pattern>TEST LEARNF</pattern><template>Original: <eval><input/></eval>; Current: <input/></template></category></learnf>");
		var tag = new LearnF(el);
		Assert.AreSame(el, tag.Element);
	}

	[TestCase("<learnf/>", TestName = "Parse (no category)")]
	[TestCase("<learnf><category><template/></category></learnf>", TestName = "Parse (no pattern)")]
	[TestCase("<learnf><category><pattern>TEST</pattern></category></learnf>", TestName = "Parse (no template)")]
	[TestCase("<learnf><category><foo/></category></learnf>", TestName = "Parse (invalid category element)")]
	[TestCase("<learnf><pattern>TEST</pattern></learnf>", TestName = "Parse (invalid AIML element)")]
	public void ParseInvalid(string xml) {
		Assert.Throws<ArgumentException>(() => new LearnF(XElement.Parse(xml)));
	}

	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		var tag = new LearnF(XElement.Parse("<learnf><category><pattern>TEST LEARNF</pattern><template>Original: <eval><input/></eval>; Current: <input/></template></category></learnf>"));
		test.User.Requests.Add(new("TEST", test.User, test.Bot));
		tag.Evaluate(test.RequestProcess);

		test.User.Requests.Add(new("TEST LEARNF", test.User, test.Bot));
		Assert.AreEqual("Original: TEST; Current: TEST LEARNF", AimlTest.GetTemplate(test.Bot.Graphmaster, "TEST", "LEARNF", "<that>", "*", "<topic>", "*").Content.Evaluate(new(new(new("TEST LEARNF", test.User, test.Bot), "TEST LEARNF"), 0, false)));
	}

	[Test]
	public void Evaluate_DoesNotModifyOriginalElement() {
		var test = new AimlTest();
		var tag = new LearnF(XElement.Parse("<learnf><category><pattern>TEST LEARNF</pattern><template>Original: <eval><input/></eval>; Current: <input/></template></category></learnf>"));
		var xml = LearnTests.GetOuterXml(tag.Element);

		test.User.Requests.Add(new("TEST", test.User, test.Bot));
		tag.Evaluate(test.RequestProcess);

		Assert.AreEqual(xml, LearnTests.GetOuterXml(tag.Element));
	}

	[Test]
	public void EvaluateWithThatAndTopic() {
		var test = new AimlTest();
		var tag = new LearnF(XElement.Parse("<learnf><category><pattern>TEST LEARNF</pattern><that>LEARNED</that><topic>TESTS</topic><template>Original: <eval><input/></eval>; Current: <input/></template></category></learnf>"));
		test.User.Requests.Add(new("TEST", test.User, test.Bot));
		tag.Evaluate(test.RequestProcess);
		AimlTest.GetTemplate(test.Bot.Graphmaster, "TEST", "LEARNF", "<that>", "LEARNED", "<topic>", "TESTS");
	}
}
