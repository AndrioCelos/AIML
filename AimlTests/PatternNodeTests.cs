using Aiml.Sets;

namespace Aiml.Tests;

[TestFixture]
public class PatternNodeTests {
	[Test]
	public void AddChild() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		node.AddChild(new PathToken[] { new("TEST"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("testing"), new("2") }, template);
		Assert.IsNull(node.Children["TEST"].Children["<that>"].Children["*"].Children["<topic>"].Children["testing"].Template);

		var template2 = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		node.AddChild(new PathToken[] { new("TEST"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("testing") }, template2);
		Assert.AreSame(template, node.Children["TEST"].Children["<that>"].Children["*"].Children["<topic>"].Children["testing"].Children["2"].Template);
		Assert.AreSame(template2, node.Children["TEST"].Children["<that>"].Children["*"].Children["<topic>"].Children["testing"].Template);
	}

	[Test]
	public void AddChild_Set() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		node.AddChild(new PathToken[] { new("number", true), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template);

		var child = node.SetChildren.Single();
		Assert.AreEqual("number", child.SetName);
		Assert.AreSame(template, child.Node.Children["<that>"].Children["*"].Children["<topic>"].Children["*"].Template);
	}

	[Test]
	public void Search() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		node.AddChild(new PathToken[] { new("TEST"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template);

		var test = new AimlTest("test");
		var foundTemplate = node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false);
		Assert.AreSame(template, foundTemplate);
		Assert.AreEqual(0, test.RequestProcess.Star.Count);
		Assert.AreEqual(1, test.RequestProcess.ThatStar.Count);
		Assert.AreEqual(1, test.RequestProcess.TopicStar.Count);
	}

	[Test]
	public void Search_Wildcard() {
		// ^ should be able to match multiple words and take precedence over *.
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		var template2 = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		node.AddChild(new PathToken[] { new("*"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template);
		node.AddChild(new PathToken[] { new("^"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template2);

		var test = new AimlTest("1 2 3");
		Assert.AreSame(template2, node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false));
		Assert.AreEqual(1, test.RequestProcess.Star.Count);
		Assert.AreEqual("1 2 3", test.RequestProcess.Star[0]);
	}

	[Test]
	public void Search_Sets() {
		// Sets should be able to match multiple words and take precedence over *.
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		var template2 = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		node.AddChild(new PathToken[] { new("testset", true), new("*"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template);
		node.AddChild(new PathToken[] { new("*"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template2);

		var test = new AimlTest("test entry 1");
		test.Bot.Sets.Add("testset", new StringSet(new[] { "test entry" }, StringComparer.InvariantCultureIgnoreCase));
		Assert.AreSame(template, node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false));
		Assert.AreEqual(2, test.RequestProcess.Star.Count);
		Assert.AreEqual("test entry", test.RequestProcess.Star[0]);
		Assert.AreEqual("1", test.RequestProcess.Star[1]);
	}

	[Test]
	public void Search_SetsTakeLongestMatch() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		node.AddChild(new PathToken[] { new("testset", true), new("*"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template);

		var test = new AimlTest("foo bar baz");
		test.Bot.Sets.Add("testset", new StringSet(new[] { "foo", "foo bar" }, StringComparer.InvariantCultureIgnoreCase));
		Assert.AreSame(template, node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false));
		Assert.AreEqual(2, test.RequestProcess.Star.Count);
		Assert.AreEqual("foo bar", test.RequestProcess.Star[0]);
		Assert.AreEqual("baz", test.RequestProcess.Star[1]);
	}

	[Test]
	public void Search_OptionalWildcard() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		var template2 = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		node.AddChild(new PathToken[] { new("TEST"), new("^"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template);
		node.AddChild(new PathToken[] { new("TEST"), new("*"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template2);

		var test = new AimlTest("TEST");
		Assert.AreSame(template, node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false));
		Assert.AreEqual(1, test.RequestProcess.Star.Count);
		Assert.AreEqual("nil", test.RequestProcess.Star[0]);
	}

	[Test]
	public void Search_PriorityWildcard() {
		// # should take precedence over exact match.
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		var template2 = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		node.AddChild(new PathToken[] { new("#"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template);
		node.AddChild(new PathToken[] { new("TEST"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template2);

		var test = new AimlTest("test");
		Assert.AreSame(template, node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false));
		Assert.AreEqual(1, test.RequestProcess.Star.Count);
		Assert.AreEqual("test", test.RequestProcess.Star[0]);
	}

	[Test]
	public void Search_PriorityWildcardExampleFromAimlSpec() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		var template2 = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		var template3 = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		node.AddChild(new PathToken[] { new("_"), new("ANGELINA"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template);
		node.AddChild(new PathToken[] { new("$WHO"), new("IS"), new("ANGELINA"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template2);
		node.AddChild(new PathToken[] { new("HELLO"), new("ANGELINA"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template3);

		var test = new AimlTest("Hello Angelina");  // Should match _ ANGELINA.
		var foundTemplate = node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false);
		Assert.AreSame(template, foundTemplate);
		Assert.AreEqual(1, test.RequestProcess.Star.Count);
		Assert.AreEqual(1, test.RequestProcess.ThatStar.Count);
		Assert.AreEqual(1, test.RequestProcess.TopicStar.Count);

		test = new AimlTest("Who is Angelina");  // Should match $WHO IS ANGELINA.
		foundTemplate = node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false);
		Assert.AreSame(template2, foundTemplate);
		Assert.AreEqual(0, test.RequestProcess.Star.Count);
		Assert.AreEqual(1, test.RequestProcess.ThatStar.Count);
		Assert.AreEqual(1, test.RequestProcess.TopicStar.Count);
	}

	[Test]
	public void Search_AdjacentWildcardsAreUngreedy() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		node.AddChild(new PathToken[] { new("*"), new("*"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template);

		var test = new AimlTest("1 2 3");
		Assert.AreSame(template, node.Search(test.RequestProcess.Sentence, test.RequestProcess, "unknown", false));
		Assert.AreEqual(2, test.RequestProcess.Star.Count);
		Assert.AreEqual("1", test.RequestProcess.Star[0]);
		Assert.AreEqual("2 3", test.RequestProcess.Star[1]);
	}

	[Test]
	public void GetTemplatesTest() {
		var node = new PatternNode(StringComparer.InvariantCultureIgnoreCase);
		var template = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		var template2 = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		var template3 = new Template(new AimlTest().Bot, AimlTest.ParseXmlElement("<template/>"), TemplateElementCollection.Empty, null);
		node.AddChild(new PathToken[] { new("_"), new("ANGELINA"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template);
		node.AddChild(new PathToken[] { new("$WHO"), new("IS"), new("ANGELINA"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template2);
		node.AddChild(new PathToken[] { new("HELLO"), new("ANGELINA"), PathToken.ThatSeparator, new("*"), PathToken.TopicSeparator, new("*") }, template3);

		var templates = node.GetTemplates().Select(e => e.Value).ToList();
		Assert.AreEqual(3, templates.Count);
		Assert.Contains(template, templates);
		Assert.Contains(template2, templates);
		Assert.Contains(template3, templates);
	}
}