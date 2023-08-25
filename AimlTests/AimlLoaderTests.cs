using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Linq;
using Aiml.Tags;
using Aiml.Tests.TestExtension;
using NUnit.Framework.Constraints;

namespace Aiml.Tests;

[TestFixture]
public class AimlLoaderTests {
	private int oobExecuted;

	[OneTimeSetUp]
	public void Init() {
		AimlLoader.AddCustomOobHandler("testoob", el => oobExecuted++);
		AimlLoader.AddCustomOobHandler("testoob2", el => "Sample replacement");
		AimlLoader.AddCustomTag(typeof(TestCustomTag));
		AimlLoader.AddCustomTag("custom", typeof(TestCustomTag));
		AimlLoader.AddCustomTag("custom2", (el, l) => new TestCustomTag(el, new("Hello"), new("world")));
	}

	private static string GetExampleBotDir() {
		var solutionDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Environment.CurrentDirectory))));
		if (solutionDir is null) HandleMissingExampleBot();
		var botDir = Path.Combine(solutionDir, "ExampleBot");
		if (!Directory.Exists(botDir)) HandleMissingExampleBot();
		return botDir;
	}

	[DoesNotReturn]
	private static void HandleMissingExampleBot() {
		Assert.Inconclusive("ExampleBot files were not found.");
		throw new InvalidOperationException();
	}

	[Test]
	public void AddExtensionTest() {
		var extension = new TestExtension.TestExtension();
		AimlLoader.AddExtension(extension);
		Assert.AreEqual(1, extension.Initialised);
	}

	[Test]
	public void AddExtensionsTest() {
		AimlLoader.AddExtensions(Assembly.GetExecutingAssembly().Location);
		Assert.AreEqual(1, TestExtension.TestExtension.instances.Single().Initialised);
	}

	[Test]
	public void AddCustomTag_ImplicitName() {
		var test = new AimlTest();
		var el = test.Bot.AimlLoader.ParseElement(XElement.Parse("<testcustomtag value1='Hello'><value2>world</value2></testcustomtag>"));
		Assert.IsInstanceOf<TestCustomTag>(el);
		Assert.AreEqual("Hello world", el.Evaluate(test.RequestProcess));
	}

	[Test]
	public void AddCustomTag_ExplicitName() {
		var test = new AimlTest();
		var el = test.Bot.AimlLoader.ParseElement(XElement.Parse("<custom value1='Hello'><value2>world</value2></custom>"));
		Assert.IsInstanceOf<TestCustomTag>(el);
		Assert.AreEqual("Hello world", el.Evaluate(test.RequestProcess));
	}

	[Test]
	public void AddCustomTag_Delegate() {
		var test = new AimlTest();
		var el = test.Bot.AimlLoader.ParseElement(XElement.Parse("<custom2/>"));
		Assert.IsInstanceOf<TestCustomTag>(el);
		Assert.AreEqual("Hello world", el.Evaluate(test.RequestProcess));
	}

	[Test]
	public void AddCustomMediaElement() {
		AimlLoader.AddCustomMediaElement("custommedia", MediaElementType.Inline, (_, _) => new TestCustomRichMediaElement());
		var response = new Response(new AimlTest().RequestProcess.Sentence.Request, "<custommedia/>");
		var messages = response.ToMessages();
		Assert.AreEqual(1, messages.Length);
		Assert.AreEqual(1, messages[0].InlineElements.Count);
		Assert.IsInstanceOf<TestCustomRichMediaElement>(messages[0].InlineElements[0]);
	}

	[Test]
	public void AddCustomOobHandler_EmptyReplacement() {
		var response = new Response(new AimlTest().RequestProcess.Sentence.Request, "<oob><testoob/></oob>");
		response.ProcessOobElements();
		Assert.AreEqual("", response.ToString());
		Assert.AreEqual(1, oobExecuted);
	}

	[Test]
	public void AddCustomOobHandler_WithReplacement() {
		var response = new Response(new AimlTest().RequestProcess.Sentence.Request, "<oob><testoob2/></oob>");
		response.ProcessOobElements();
		Assert.AreEqual("Sample replacement", response.ToString());
	}

	[Test]
	public void AddCustomSraixServiceTest() {
		var test = new AimlTest();
		AimlLoader.AddCustomSraixService(new TestSraixService());
		test.RequestProcess.Variables["bar"] = "var";

		var el = XElement.Parse("<sraix customattr='Sample'/>");
		var tag = new SraiX(new(nameof(TestSraixService)), new("default"), el, new("arguments"));
		Assert.AreEqual("Success", tag.Evaluate(test.RequestProcess));
	}

	[Test]
	public void LoadAimlFiles_DefaultDirectory() {
		var test = new AimlTest(new Bot(GetExampleBotDir()));
		test.Bot.LoadConfig();
		test.Bot.AimlLoader.LoadAimlFiles();
		var template = AimlTest.GetTemplate(test.Bot.Graphmaster, "HI", "<that>", "*", "<topic>", "*");
		Assert.AreEqual("Hello, world!", template.Content.ToString());
		Assert.That(template.Uri, new EndsWithConstraint("helloworld.aiml"));
	}

	[Test]
	public void LoadAimlFiles_SpecifiedDirectory() {
		var dir = Path.Combine(GetExampleBotDir(), "aiml");
		if (!Directory.Exists(dir)) HandleMissingExampleBot();
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAimlFiles(dir);
		var template = AimlTest.GetTemplate(test.Bot.Graphmaster, "HI", "<that>", "*", "<topic>", "*");
		Assert.AreEqual("Hello, world!", template.Content.ToString());
		Assert.That(template.Uri, new EndsWithConstraint("helloworld.aiml"));
	}

	[Test]
	public void LoadAiml_File() {
		var file = Path.Combine(GetExampleBotDir(), "aiml", "helloworld.aiml");
		if (!File.Exists(file)) HandleMissingExampleBot();
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(file);
		var template = AimlTest.GetTemplate(test.Bot.Graphmaster, "HI", "<that>", "*", "<topic>", "*");
		Assert.AreEqual("Hello, world!", template.Content.ToString());
		Assert.That(template.Uri, new EndsWithConstraint("helloworld.aiml"));
	}

	[Test]
	public void LoadAiml_XDocument() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(XDocument.Parse(@"<?xml version='1.0' encoding='utf-16'?>
<aiml>
	<category>
		<pattern>TEST</pattern>
		<template>Hello world!</template>
	</category>
</aiml>"));
		Assert.AreEqual("Hello world!", AimlTest.GetTemplate(test.Bot.Graphmaster, "TEST", "<that>", "*", "<topic>", "*").Content.ToString());
	}

	[Test]
	public void LoadAiml_XElement() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(XElement.Parse(@"
<aiml>
	<category>
		<pattern>TEST</pattern>
		<template>Hello world!</template>
	</category>
</aiml>"));
		Assert.AreEqual("Hello world!", AimlTest.GetTemplate(test.Bot.Graphmaster, "TEST", "<that>", "*", "<topic>", "*").Content.ToString());
	}

	[Test]
	public void LoadAimlInto_XElement() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAimlInto(target, XElement.Parse(@"
<learnf>
	<category>
		<pattern>TEST</pattern>
		<template>Hello world!</template>
	</category>
</learnf>"));
		var template = target.Children["TEST"].Children["<that>"].Children["*"].Children["<topic>"].Children["*"].Template;
		Assert.IsNotNull(template);
		Assert.AreEqual("Hello world!", template!.Content.ToString());
	}

	[Test]
	public void LoadAiml_TopicElement() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(XDocument.Parse(@"<?xml version='1.0' encoding='utf-16'?>
<aiml>
	<topic name='testing'>
		<category>
			<pattern>TEST</pattern>
			<template>Hello world!</template>
		</category>
	</topic>
</aiml>"));
		Assert.AreEqual("Hello world!", AimlTest.GetTemplate(test.Bot.Graphmaster, "TEST", "<that>", "*", "<topic>", "TESTING").Content.ToString());
	}

	[Test]
	public void ProcessCategory_NoTopic() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><pattern>TEST</pattern><template>Hello world!</template></category>"));
		Assert.AreEqual("Hello world!", AimlTest.GetTemplate(target, "TEST", "<that>", "*", "<topic>", "*").Content.ToString());
	}

	[Test]
	public void ProcessCategory_InheritedTopic() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><pattern>TEST</pattern><template>Hello world!</template></category>"), "testing");
		Assert.AreEqual("Hello world!", AimlTest.GetTemplate(target, "TEST", "<that>", "*", "<topic>", "TESTING").Content.ToString());
	}

	[Test]
	public void ProcessCategory_SpecificTopic() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><pattern>TEST</pattern><topic>TESTING2</topic><template>Hello world!</template></category>"), "testing");
		Assert.AreEqual("Hello world!", AimlTest.GetTemplate(target, "TEST", "<that>", "*", "<topic>", "TESTING2").Content.ToString());
	}

	[Test]
	public void ProcessCategory_DuplicatePath() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><pattern>TEST</pattern><template>Hello world!</template></category>"), "testing");
		test.AssertWarning(() => test.Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><pattern>TEST</pattern><template>Hello world!</template></category>"), "testing"));
	}

	[Test]
	public void ProcessCategory_That() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><pattern>TEST</pattern><that>HELLO</that><template>Hello world!</template></category>"));
		Assert.AreEqual("Hello world!", AimlTest.GetTemplate(target, "TEST", "<that>", "HELLO", "<topic>", "*").Content.ToString());
	}

	[Test]
	public void ProcessCategory_NoPattern() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><template>Hello world!</template></category>")));
	}

	[Test]
	public void ProcessCategory_NoTemplate() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><pattern>TEST</pattern></category>")));
	}

	[Test]
	public void ProcessCategory_InvalidCategoryElement() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ProcessCategory(target, XElement.Parse("<category><foo/></category>")));
	}

	[Test]
	public void ParseElement_NoContent() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<star/>"));
		Assert.IsInstanceOf<Star>(el);
		Assert.IsNull(((Star) el).Index);
	}

	[Test]
	public void ParseElement_WithContent() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<srai>Hello, world!</srai>"));
		Assert.IsInstanceOf<Srai>(el);
		Assert.AreEqual("Hello, world!", ((Srai) el).Children.ToString());
	}

	[Test]
	public void ParseElement_InvalidContent() {
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<star>foo</star>")));
	}

	[Test]
	public void ParseElement_SpecialParserOrCustomTag() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<oob><foo/></oob>"));
		Assert.IsInstanceOf<Oob>(el);
		Assert.IsInstanceOf<Oob>(((Oob) el).Children.Single());
	}

	[Test]
	public void ParseElement_RichMediaElement() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<split/>"));
		Assert.IsInstanceOf<Oob>(el);
	}

	[Test]
	public void ParseElement_AttributeAsXmlAttribute() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<star index='2'/>"));
		Assert.IsInstanceOf<Star>(el);
		Assert.AreEqual("2", ((Star) el).Index?.ToString());
	}

	[Test]
	public void ParseElement_AttributeAsXmlElement() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<star><index>2</index></star>"));
		Assert.IsInstanceOf<Star>(el);
		Assert.AreEqual("2", ((Star) el).Index?.ToString());
	}

	[Test]
	public void ParseElement_InvalidAttribute() {
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<star foo='bar'/>")));
	}

	[Test]
	public void ParseElement_DuplicateAttribute() {
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<star index='2'><index>3</index></star>")));
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<star><index>2</index><index>3</index></star>")));
	}

	[Test]
	public void ParseElement_MissingAttribute() {
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<map>foo</map>")));
	}

	[Test]
	public void ParseElement_SpecialContent() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<random><li>1</li><li>2</li></random>"));
		Assert.IsInstanceOf<Aiml.Tags.Random>(el);
		var random = (Aiml.Tags.Random) el;
		Assert.AreEqual(2, random.Items.Length);
		Assert.AreEqual("1", random.Items[0].Children.ToString());
		Assert.AreEqual("2", random.Items[1].Children.ToString());
	}

	[Test]
	public void ParseElement_PassthroughXmlElement() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(XElement.Parse("<learn><category><pattern>foo</pattern><template/></category></learn>"));
		Assert.IsInstanceOf<Learn>(el);
		Assert.AreEqual("foo", ((Learn) el).Element.Value);
	}

	[Test]
	public void ParseElement_PassthroughXmlElement_Sraix() {
		var el = XElement.Parse("<sraix service='ExternalBotService' botname='Angelina'/>");
		var tag = new AimlTest().Bot.AimlLoader.ParseElement(el);
		Assert.IsInstanceOf<SraiX>(tag);
		Assert.AreSame(el, ((SraiX) tag).Element);
	}
}
