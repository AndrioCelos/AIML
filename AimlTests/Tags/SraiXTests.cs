using System.Xml.Linq;
using Aiml.Tags;
using Aiml.Tests.TestExtension;

namespace Aiml.Tests.Tags;
[TestFixture]
public class SraiXTests {
	[OneTimeSetUp]
	public void Init() {
		if (!AimlLoader.sraixServices.ContainsKey(nameof(TestSraixService)))
			AimlLoader.AddCustomSraixService(new TestSraixService());
		AimlLoader.AddCustomSraixService(new TestFaultSraixService());
	}

	[Test]
	public void ParseWithDefault() {
		var el = XElement.Parse("<sraix/>");
		var tag = new SraiX(new(nameof(TestSraixService)), new("default"), el, new("arguments"));
		Assert.AreEqual(nameof(TestSraixService), tag.ServiceName.ToString());
		Assert.AreEqual("default", tag.DefaultReply?.ToString());
		Assert.AreSame(el, tag.Element);
		Assert.AreEqual(nameof(TestSraixService), tag.ServiceName.ToString());
	}

	[Test]
	public void ParseWithoutDefault() {
		var el = XElement.Parse("<sraix/>");
		var tag = new SraiX(new(nameof(TestSraixService)), null, el, new("arguments"));
		Assert.IsNull(tag.DefaultReply);
	}

	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		test.RequestProcess.Variables["bar"] = "var";

		var el = XElement.Parse("<sraix customattr='Sample'/>");
		var tag = new SraiX(new(nameof(TestSraixService)), new("default"), el, new("arguments"));
		Assert.AreEqual("Success", tag.Evaluate(test.RequestProcess));
	}

	[Test]
	public void EvaluateFullName() {
		var test = new AimlTest();
		test.RequestProcess.Variables["bar"] = "var";

		var el = XElement.Parse("<sraix customattr='Sample'/>");
		var tag = new SraiX(new($"{nameof(Aiml)}.{nameof(Tests)}.{nameof(TestExtension)}.{nameof(TestSraixService)}"), new("default"), el, new("arguments"));
		Assert.AreEqual("Success", tag.Evaluate(test.RequestProcess));
	}

	[Test]
	public void EvaluateInvalidServiceWithDefault() {
		var test = new AimlTest();
		var el = XElement.Parse("<sraix/>");
		var tag = new SraiX(new("InvalidService"), new("default"), el, new("arguments"));
		Assert.AreEqual("default", test.AssertWarning(() => tag.Evaluate(test.RequestProcess)));
	}

	[Test]
	public void EvaluateInvalidServiceWithoutDefault() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(XElement.Parse(@"
<aiml>
	<category>
		<pattern>SRAIXFAILED ^</pattern>
		<template>Failure template</template>
	</category>
</aiml>"));
		var el = XElement.Parse("<sraix/>");
		var tag = new SraiX(new("InvalidService"), null, el, new("arguments"));
		Assert.AreEqual("Failure template", test.AssertWarning(() => tag.Evaluate(test.RequestProcess)));
	}

	[Test]
	public void EvaluateFaultedServiceWithDefault() {
		var test = new AimlTest();
		var el = XElement.Parse("<sraix/>");
		var tag = new SraiX(new(nameof(TestFaultSraixService)), new("default"), el, new("arguments"));
		Assert.AreEqual("default", test.AssertWarning(() => tag.Evaluate(test.RequestProcess)));
	}

	[Test]
	public void EvaluateFaultedServiceWithoutDefault() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(XElement.Parse(@"
<aiml>
	<category>
		<pattern>SRAIXFAILED ^</pattern>
		<template>Failure template</template>
	</category>
</aiml>"));
		var el = XElement.Parse("<sraix/>");
		var tag = new SraiX(new(nameof(TestFaultSraixService)), null, el, new("arguments"));
		Assert.AreEqual("Failure template", test.AssertWarning(() => tag.Evaluate(test.RequestProcess)));
	}
}
