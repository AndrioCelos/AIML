using System.Xml.Linq;
using Aiml.Tags;

namespace Aiml.Tests.Tags;
[TestFixture]
public class TestTests {
	// Testing the <test>s among the tests.
	private static AimlTest GetTest() {
		var test = new AimlTest();
		test.Bot.AimlLoader.LoadAiml(XElement.Parse(@"
<aiml>
	<category>
		<pattern>*</pattern>
		<template><star/></template>
	</category>
</aiml>"));
		return test;
	}

	[Test]
	public void ParseWithConstant() {
		var tag = new Test(name: new("SampleTest"), expected: new("Hello world"), regex: null, children: new("Hello world"));
		Assert.AreEqual("SampleTest", tag.Name);
		Assert.AreEqual("Hello world", tag.ExpectedResponse.ToString());
		Assert.IsFalse(tag.UseRegex);
		Assert.AreEqual("Hello world", tag.Children.ToString());
	}

	[Test]
	public void ParseWithRegex() {
		var tag = new Test(name: new("SampleTest"), expected: null, regex: new("^Hello"), children: new("Hello world"));
		Assert.AreEqual("SampleTest", tag.Name);
		Assert.AreEqual("^Hello", tag.ExpectedResponse.ToString());
		Assert.IsTrue(tag.UseRegex);
		Assert.AreEqual("Hello world", tag.Children.ToString());
	}

	[Test]
	public void ParseWithConstantAndRegex() {
		Assert.Throws<ArgumentException>(() => new Test(name: new("SampleTest"), expected: new("Hello world"), regex: new("^Hello"), children: new("Hello world")));
	}

	[Test]
	public void ParseWithoutExpectation() {
		Assert.Throws<ArgumentException>(() => new Test(name: new("SampleTest"), expected: null, regex: null, children: new("Hello world")));
	}

	[Test]
	public void ParseWithNonConstantName() {
		Assert.Throws<ArgumentException>(() => new Test(name: new(new Star(null)), expected: new("Hello world"), regex: null, children: new("Hello world")));
	}

	[Test]
	public void EvaluateConstantPass() {
		var test = GetTest();
		test.RequestProcess.testResults = [ ];
		var tag = new Test(name: new("SampleTest"), expected: new("Hello world"), regex: null, children: new("Hello\nworld"));
		Assert.AreEqual("Hello world", tag.Evaluate(test.RequestProcess).ToString());
		Assert.IsTrue(test.RequestProcess.testResults["SampleTest"].Passed);
	}

	[Test]
	public void EvaluateConstantFail() {
		var test = GetTest();
		test.RequestProcess.testResults = [ ];
		var tag = new Test(name: new("SampleTest"), expected: new("Hello world"), regex: null, children: new("Hell world"));
		Assert.AreEqual("Hell world", tag.Evaluate(test.RequestProcess).ToString());
		Assert.IsFalse(test.RequestProcess.testResults["SampleTest"].Passed);
	}

	[Test]
	public void EvaluateRegexPass() {
		var test = GetTest();
		test.RequestProcess.testResults = [ ];
		var tag = new Test(name: new("SampleTest"), expected: null, regex: new("^Hello\n\\w"), children: new("Hello world"));
		Assert.AreEqual("Hello world", tag.Evaluate(test.RequestProcess).ToString());
		Assert.IsTrue(test.RequestProcess.testResults["SampleTest"].Passed);
	}

	[Test]
	public void EvaluateRegexFail() {
		var test = GetTest();
		test.RequestProcess.testResults = [ ];
		var tag = new Test(name: new("SampleTest"), expected: null, regex: new("^Hello"), children: new("Hell world"));
		Assert.AreEqual("Hell world", tag.Evaluate(test.RequestProcess).ToString());
		Assert.IsFalse(test.RequestProcess.testResults["SampleTest"].Passed);
	}

	[Test]
	public void EvaluateRegexInvalid() {
		var test = GetTest();
		test.RequestProcess.testResults = [ ];
		var tag = new Test(name: new("SampleTest"), expected: null, regex: new("("), children: new("Hello world"));
		Assert.AreEqual("Hello world", test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()));
		Assert.IsFalse(test.RequestProcess.testResults["SampleTest"].Passed);
	}

	[Test]
	public void Evaluate_TestsDisabled() {
		var test = GetTest();
		var tag = new Test(name: new("SampleTest"), expected: new("Hello world"), regex: null, children: new("Hello world"));
		Assert.AreEqual("Hello world", test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()));
		Assert.IsNull(test.RequestProcess.testResults);
	}
}
