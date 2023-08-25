using Aiml.Tags;

namespace Aiml.Tests.Tags;
[TestFixture]
public class SizeTests {
	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAIML(AimlTest.ParseXmlDocument(@"
<aiml>
	<category>
		<pattern>SAMPLE CATEGORY</pattern>
		<template></template>
	</category>
</aiml>"));
		test.Bot.AimlLoader.LoadAIML(AimlTest.ParseXmlDocument(@"
<aiml>
	<category>
		<pattern>*</pattern>
		<template></template>
	</category>
</aiml>"));

		var tag = new Size();
		Assert.AreEqual("2", tag.Evaluate(test.RequestProcess));
	}
}
