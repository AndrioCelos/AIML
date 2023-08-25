using Aiml.Sets;
using Aiml.Tags;

namespace Aiml.Tests.Tags;
[TestFixture]
public class VocabularyTests {
	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		test.Bot.Properties["name"] = "Angelina";
		test.Bot.AimlLoader.LoadAIML(AimlTest.ParseXmlDocument(@"
<aiml>
	<category>
		<pattern>HELLO WORLD</pattern>
		<template>Hello world!</template>
	</category>
	<category>
		<pattern>HELLO <bot name='name'/></pattern>
		<template>Hello world!</template>
	</category>
	<category>
		<pattern>HELLO <set>testset</set></pattern>
		<template>Hello world!</template>
	</category>
</aiml>"));
		test.Bot.Sets["testset"] = new StringSet(new[] { "A", "B", "C D" }, test.Bot.Config.StringComparer);

		var tag = new Vocabulary();
		Assert.AreEqual("7", tag.Evaluate(test.RequestProcess));
	}
}
