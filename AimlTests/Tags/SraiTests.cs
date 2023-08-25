using Aiml.Tags;
using NUnit.Framework.Constraints;

namespace Aiml.Tests.Tags;
[TestFixture]
public class SraiTests {
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

		var tag = new Srai(new("test"));
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

		var tag = new Srai(new("test"));
		var result = test.AssertWarning(() => tag.Evaluate(test.RequestProcess));
		Assert.That(result, new EndsWithConstraint(test.Bot.Config.RecursionLimitMessage));
	}
}
