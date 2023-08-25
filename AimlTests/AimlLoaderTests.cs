using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Aiml.Tags;
using Aiml.Tests.TestExtension;
using Newtonsoft.Json.Bson;
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
		AimlLoader.AddCustomTag("custom2", (el, l) => new TestCustomTag(el, el.Attributes, new("Hello"), new("world")));
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
		var el = test.Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<testcustomtag value1='Hello'><value2>world</value2></testcustomtag>"));
		Assert.IsInstanceOf<TestCustomTag>(el);
		Assert.AreEqual("Hello world", el.Evaluate(test.RequestProcess));
	}

	[Test]
	public void AddCustomTag_ExplicitName() {
		var test = new AimlTest();
		var el = test.Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<custom value1='Hello'><value2>world</value2></custom>"));
		Assert.IsInstanceOf<TestCustomTag>(el);
		Assert.AreEqual("Hello world", el.Evaluate(test.RequestProcess));
	}

	[Test]
	public void AddCustomTag_Delegate() {
		var test = new AimlTest();
		var el = test.Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<custom2/>"));
		Assert.IsInstanceOf<TestCustomTag>(el);
		Assert.AreEqual("Hello world", el.Evaluate(test.RequestProcess));
	}

	[Test]
	public void AddCustomMediaElement() {
		AimlLoader.AddCustomMediaElement("custommedia", MediaElementType.Inline, el => new TestCustomRichMediaElement());
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

		var el = AimlTest.ParseXmlElement("<sraix customattr='Sample'/>");
		var tag = new Aiml.Tags.SraiX(new(nameof(TestSraixService)), new("default"), el.Attributes, new("arguments"));
		Assert.AreEqual("Success", tag.Evaluate(test.RequestProcess));
	}

	[Test]
	public void LoadAimlFiles_DefaultDirectory() {
		var test = new AimlTest(new Bot(GetExampleBotDir()));
		test.Bot.LoadConfig();
		test.Bot.AimlLoader.LoadAimlFiles();
		var template = AimlTest.GetTemplate(test.Bot.Graphmaster, "HI", "<that>", "*", "<topic>", "*");
		Assert.AreEqual("Hello, world!", template.Content.ToString());
		Assert.That(template.FileName, new EndsWithConstraint("helloworld.aiml"));
	}

	[Test]
	public void LoadAimlFiles_SpecifiedDirectory() {
		var dir = Path.Combine(GetExampleBotDir(), "aiml");
		if (!Directory.Exists(dir)) HandleMissingExampleBot();
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAimlFiles(dir);
		var template = AimlTest.GetTemplate(test.Bot.Graphmaster, "HI", "<that>", "*", "<topic>", "*");
		Assert.AreEqual("Hello, world!", template.Content.ToString());
		Assert.That(template.FileName, new EndsWithConstraint("helloworld.aiml"));
	}

	[Test]
	public void LoadAIML_File() {
		var file = Path.Combine(GetExampleBotDir(), "aiml", "helloworld.aiml");
		if (!File.Exists(file)) HandleMissingExampleBot();
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAIML(file);
		var template = AimlTest.GetTemplate(test.Bot.Graphmaster, "HI", "<that>", "*", "<topic>", "*");
		Assert.AreEqual("Hello, world!", template.Content.ToString());
		Assert.That(template.FileName, new EndsWithConstraint("helloworld.aiml"));
	}

	[Test]
	public void LoadAIML_XmlDocument() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAIML(AimlTest.ParseXmlDocument(@"
<aiml>
	<category>
		<pattern>TEST</pattern>
		<template>Hello world!</template>
	</category>
</aiml>"));
		Assert.AreEqual("Hello world!", AimlTest.GetTemplate(test.Bot.Graphmaster, "TEST", "<that>", "*", "<topic>", "*").Content.ToString());
	}

	[Test]
	public void LoadAIML_XmlDocumentWithFileName() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAIML(AimlTest.ParseXmlDocument(@"
<aiml>
	<category>
		<pattern>TEST</pattern>
		<template>Hello world!</template>
	</category>
</aiml>"), "test.aiml");
		var template = AimlTest.GetTemplate(test.Bot.Graphmaster, "TEST", "<that>", "*", "<topic>", "*");
		Assert.AreEqual("test.aiml", template.FileName);
		Assert.AreEqual("Hello world!", template.Content.ToString());
	}

	[Test]
	public void LoadAIML_XmlElement() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAIML(target, AimlTest.ParseXmlElement(@"
<aiml>
	<category>
		<pattern>TEST</pattern>
		<template>Hello world!</template>
	</category>
</aiml>"));
		var template = target.Children["TEST"].Children["<that>"].Children["*"].Children["<topic>"].Children["*"].Template;
		Assert.IsNotNull(template);
		Assert.AreEqual("Hello world!", template!.Content.ToString());
	}

	[Test]
	public void LoadAIML_XmlElementWithFileName() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAIML(target, AimlTest.ParseXmlElement(@"
<aiml>
	<category>
		<pattern>TEST</pattern>
		<template>Hello world!</template>
	</category>
</aiml>"), "test.aiml");
		var template = target.Children["TEST"].Children["<that>"].Children["*"].Children["<topic>"].Children["*"].Template;
		Assert.IsNotNull(template);
		Assert.AreEqual("test.aiml", template!.FileName);
		Assert.AreEqual("Hello world!", template.Content.ToString());
	}

	[Test]
	public void LoadAIML_TopicElement() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAIML(AimlTest.ParseXmlDocument(@"
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
		test.Bot.AimlLoader.ProcessCategory(target, AimlTest.ParseXmlElement("<category><pattern>TEST</pattern><template>Hello world!</template></category>"), null);
		Assert.AreEqual("Hello world!", AimlTest.GetTemplate(target, "TEST", "<that>", "*", "<topic>", "*").Content.ToString());
	}

	[Test]
	public void ProcessCategory_InheritedTopic() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.ProcessCategory(target, AimlTest.ParseXmlElement("<category><pattern>TEST</pattern><template>Hello world!</template></category>"), "testing", null);
		Assert.AreEqual("Hello world!", AimlTest.GetTemplate(target, "TEST", "<that>", "*", "<topic>", "TESTING").Content.ToString());
	}

	[Test]
	public void ProcessCategory_SpecificTopic() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.ProcessCategory(target, AimlTest.ParseXmlElement("<category><pattern>TEST</pattern><topic>TESTING2</topic><template>Hello world!</template></category>"), "testing", null);
		Assert.AreEqual("Hello world!", AimlTest.GetTemplate(target, "TEST", "<that>", "*", "<topic>", "TESTING2").Content.ToString());
	}

	[Test]
	public void ProcessCategory_That() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var test = new AimlTest();
		test.Bot.AimlLoader.ProcessCategory(target, AimlTest.ParseXmlElement("<category><pattern>TEST</pattern><that>HELLO</that><template>Hello world!</template></category>"), null);
		Assert.AreEqual("Hello world!", AimlTest.GetTemplate(target, "TEST", "<that>", "HELLO", "<topic>", "*").Content.ToString());
	}

	[Test]
	public void ProcessCategory_NoPattern() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ProcessCategory(target, AimlTest.ParseXmlElement("<category><template>Hello world!</template></category>"), null));
	}

	[Test]
	public void ProcessCategory_NoTemplate() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ProcessCategory(target, AimlTest.ParseXmlElement("<category><pattern>TEST</pattern></category>"), null));
	}

	[Test]
	public void ProcessCategory_InvalidCategoryElement() {
		var target = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ProcessCategory(target, AimlTest.ParseXmlElement("<category><foo/></category>"), null));
	}

	[Test]
	public void ParseElement_NoContent() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<star/>"));
		Assert.IsInstanceOf<Star>(el);
		Assert.IsNull(((Star) el).Index);
	}

	[Test]
	public void ParseElement_WithContent() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<srai>Hello, world!</srai>"));
		Assert.IsInstanceOf<Srai>(el);
		Assert.AreEqual("Hello, world!", ((Srai) el).Children.ToString());
	}

	[Test]
	public void ParseElement_InvalidContent() {
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<star>foo</star>")));
	}

	[Test]
	public void ParseElement_SpecialParserOrCustomTag() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<oob><foo/></oob>"));
		Assert.IsInstanceOf<Oob>(el);
		Assert.IsInstanceOf<Oob>(((Oob) el).Children.Single());
	}

	[Test]
	public void ParseElement_RichMediaElement() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<split/>"));
		Assert.IsInstanceOf<Oob>(el);
	}

	[Test]
	public void ParseElement_AttributeAsXmlAttribute() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<star index='2'/>"));
		Assert.IsInstanceOf<Star>(el);
		Assert.AreEqual("2", ((Star) el).Index?.ToString());
	}

	[Test]
	public void ParseElement_AttributeAsXmlElement() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<star><index>2</index></star>"));
		Assert.IsInstanceOf<Star>(el);
		Assert.AreEqual("2", ((Star) el).Index?.ToString());
	}

	[Test]
	public void ParseElement_InvalidAttribute() {
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<star foo='bar'/>")));
	}

	[Test]
	public void ParseElement_DuplicateAttribute() {
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<star index='2'><index>3</index></star>")));
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<star><index>2</index><index>3</index></star>")));
	}

	[Test]
	public void ParseElement_MissingAttribute() {
		Assert.Throws<AimlException>(() => new AimlTest().Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<map>foo</map>")));
	}

	[Test]
	public void ParseElement_SpecialContent() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<random><li>1</li><li>2</li></random>"));
		Assert.IsInstanceOf<Aiml.Tags.Random>(el);
		var random = (Aiml.Tags.Random) el;
		Assert.AreEqual(2, random.Items.Length);
		Assert.AreEqual("1", random.Items[0].Children.ToString());
		Assert.AreEqual("2", random.Items[1].Children.ToString());
	}

	[Test]
	public void ParseElement_PassthroughXmlElement() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<learn><category><pattern>foo</pattern><template/></category></learn>"));
		Assert.IsInstanceOf<Learn>(el);
		Assert.AreEqual("foo", ((Learn) el).XmlElement.InnerText);
	}

	[Test]
	public void ParseElement_PassthroughXmlAttributeCollection() {
		var el = new AimlTest().Bot.AimlLoader.ParseElement(AimlTest.ParseXmlElement("<sraix service='ExternalBotService' botname='Angelina'/>"));
		Assert.IsInstanceOf<SraiX>(el);
		Assert.AreEqual("Angelina", ((SraiX) el).Attributes["botname"]?.Value);
	}
}
