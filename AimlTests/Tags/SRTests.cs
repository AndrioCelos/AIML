using Aiml.Tags;
using NUnit.Framework.Constraints;

namespace Aiml.Tests.Tags;
[TestFixture]
public class SRTests {
	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAIML(AimlTest.ParseXmlDocument(@"
<aiml>
	<category>
		<pattern>test</pattern>
		<template>Hello world!</template>
	</category>
</aiml>"));
		test.RequestProcess.star.Add("test");

		var tag = new SR();
		Assert.AreEqual("Hello world!", tag.Evaluate(test.RequestProcess));
	}

	[Test]
	public void EvaluateWithLimitedRecursion() {
		var test = new AimlTest() { ExpectingWarning = true };
		test.Bot.AimlLoader.LoadAIML(AimlTest.ParseXmlDocument(@"
<aiml>
	<category>
		<pattern>*</pattern>
		<template><sr/></template>
	</category>
</aiml>"));
		test.RequestProcess.star.Add("test");

		var tag = new SR();
		var result = test.AssertWarning(() => tag.Evaluate(test.RequestProcess));
		Assert.That(result, new EndsWithConstraint(test.Bot.Config.RecursionLimitMessage));
	}
}
