using Aiml.Tags;

namespace Aiml.Tests.Tags;
[TestFixture]
public class GetTests {
	private static AimlTest GetTest() {
		var test = new AimlTest();
		test.User.Predicates["foo"] = "sample predicate";
		test.Bot.Config.DefaultPredicates["bar"] = "sample default";
		test.RequestProcess.Variables["bar"] = "sample local";
		return test;
	}

	[Test]
	public void ParseWithName() {
		var tag = new Get(name: new("foo"), var: null, tuple: null);
		Assert.AreEqual("foo", tag.Key.ToString());
		Assert.IsNull(tag.TupleString);
		Assert.IsFalse(tag.LocalVar);
	}

	[Test]
	public void ParseWithVar() {
		var tag = new Get(name: null, var: new("bar"), tuple: null);
		Assert.AreEqual("bar", tag.Key.ToString());
		Assert.IsNull(tag.TupleString);
		Assert.IsTrue(tag.LocalVar);
	}

	[Test]
	public void ParseWithTupleName() {
		Assert.Throws<AimlException>(() => new Get(name: new("baz"), var: null, tuple: new("tuple")));
	}

	[Test]
	public void ParseWithTupleVar() {
		var tag = new Get(name: null, var: new("?x"), tuple: new("tuple"));
		Assert.AreEqual("?x", tag.Key.ToString());
		Assert.AreEqual("tuple", tag.TupleString?.ToString());
	}

	[Test]
	public void ParseWithNameAndVar() {
		Assert.Throws<AimlException>(() => new Get(name: new("foo"), var: new("bar"), tuple: null));
	}

	[Test]
	public void ParseWithNoAttributes() {
		Assert.Throws<AimlException>(() => new Get(name: null, var: null, tuple: null));
	}

	[Test]
	public void EvaluateWithBoundPredicate() {
		var tag = new Get(new("foo"), null, false);
		Assert.AreEqual("sample predicate", tag.Evaluate(GetTest().RequestProcess));
	}

	[Test]
	public void EvaluateWithUnboundPredicateWithDefault() {
		var test = new AimlTest();

		var tag = new Get(new("bar"), null, false);
		Assert.AreEqual("sample default", tag.Evaluate(GetTest().RequestProcess));
	}

	[Test]
	public void EvaluateWithUnboundPredicate() {
		var tag = new Get(new("baz"), null, false);
		Assert.AreEqual("unknown", tag.Evaluate(GetTest().RequestProcess));
	}

	[Test]
	public void EvaluateWithBoundLocalVariable() {
		var tag = new Get(new("bar"), null, true);
		Assert.AreEqual("sample local", tag.Evaluate(GetTest().RequestProcess));
	}

	[Test]
	public void EvaluateWithUnboundLocalVariable() {
		var tag = new Get(new("foo"), null, true);
		Assert.AreEqual("unknown", tag.Evaluate(GetTest().RequestProcess));
	}

	[Test]
	public void EvaluateWithBoundTupleVariable() {
		var tag = new Get(new("?x"), new(new Tuple("?y", "", new("?x", "sample tuple")).Encode(new[] { "?x", "?y" })), true);
		Assert.AreEqual("sample tuple", tag.Evaluate(GetTest().RequestProcess));
	}

	[Test]
	public void EvaluateWithUnboundTupleVariable() {
		var tag = new Get(new("?z"), new(new Tuple("?y", "", new("?x", "sample tuple")).Encode(new[] { "?x", "?y" })), true);
		Assert.AreEqual("unknown", tag.Evaluate(GetTest().RequestProcess));
	}
}
