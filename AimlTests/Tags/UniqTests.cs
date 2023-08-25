using Aiml.Tags;
using NUnit.Framework.Constraints;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class UniqTests {
	private static AimlTest GetTest() {
		var test = new AimlTest();
		test.Bot.Triples.Add("A", "r", "M");
		test.Bot.Triples.Add("A", "r", "N");
		test.Bot.Triples.Add("A", "r", "O");
		test.Bot.Triples.Add("N", "r", "X");
		test.Bot.Triples.Add("O", "r", "X");
		test.Bot.Triples.Add("O", "r", "Y");
		test.Bot.Triples.Add("M", "attr", "1");
		test.Bot.Triples.Add("N", "attr", "0");
		return test;
	}

	[Test]
	public void Parse() {
		var tag = new Uniq(new("M"), new("attr"), new("?"));
		Assert.AreEqual("M", tag.Subject.ToString());
		Assert.AreEqual("attr", tag.Predicate.ToString());
		Assert.AreEqual("?", tag.Object.ToString());
	}

	[Test]
	public void Evaluate_Object() {
		var tag = new Uniq(new("M"), new("attr"), new("?"));
		Assert.AreEqual("1", tag.Evaluate(GetTest().RequestProcess).ToString());
	}

	[Test]
	public void Evaluate_Subject() {
		var tag = new Uniq(new("?"), new("r"), new("M"));
		Assert.AreEqual("A", tag.Evaluate(GetTest().RequestProcess).ToString());
	}

	[Test]
	public void Evaluate_NoVariable() {
		var test = GetTest();
		var tag = new Uniq(new("M"), new("attr"), new("1"));
		Assert.AreEqual(test.Bot.Config.DefaultTriple, test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()));
	}

	[Test]
	public void Evaluate_MultipleVariables() {
		var test = GetTest();
		var tag = new Uniq(new("?"), new("attr"), new("?"));
		Assert.That(test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()), new AnyOfConstraint(new object[] { "1", "0" }));
	}
}
