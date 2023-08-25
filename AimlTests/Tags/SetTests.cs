using Aiml.Tags;

namespace Aiml.Tests.Tags;
[TestFixture]
public class SetTests {
	[Test]
	public void ParseWithName() {
		var tag = new Aiml.Tags.Set(name: new("foo"), var: null, children: new("predicate"));
		Assert.AreEqual("foo", tag.Key.ToString());
		Assert.IsFalse(tag.LocalVar);
		Assert.AreEqual("predicate", tag.Children.ToString());
	}

	[Test]
	public void ParseWithVar() {
		var tag = new Aiml.Tags.Set(name: null, var: new("bar"), children: new("variable"));
		Assert.AreEqual("bar", tag.Key.ToString());
		Assert.IsTrue(tag.LocalVar);
		Assert.AreEqual("variable", tag.Children.ToString());
	}

	[Test]
	public void ParseWithNameAndVar() {
		Assert.Throws<AimlException>(() => new Aiml.Tags.Set(name: new("foo"), var: new("bar"), children: new("variable")));
	}

	[Test]
	public void ParseWithNoAttributes() {
		Assert.Throws<AimlException>(() => new Aiml.Tags.Set(name: null, var: null, children: new("variable")));
	}

	[Test]
	public void EvaluateWithName() {
		var test = new AimlTest();
		var tag = new Aiml.Tags.Set(new("foo"), false, new("predicate"));
		Assert.AreEqual("predicate", tag.Evaluate(test.RequestProcess));
		Assert.AreEqual("predicate", test.User.GetPredicate("foo"));
	}

	[Test]
	public void EvaluateWithVar() {
		var test = new AimlTest();
		var tag = new Aiml.Tags.Set(new("bar"), true, new("variable"));
		Assert.AreEqual("variable", tag.Evaluate(test.RequestProcess));
		Assert.AreEqual("variable", test.RequestProcess.GetVariable("bar"));
	}

	[Test]
	public void EvaluateWithSpecificDefaultValue() {
		var test = new AimlTest();
		test.Bot.Config.DefaultPredicates["foo"] = "default";
		test.Bot.Config.UnbindPredicatesWithDefaultValue = true;
		test.User.Predicates["foo"] = "bar";
		var tag = new Aiml.Tags.Set(new("foo"), false, new("default"));
		Assert.AreEqual("default", tag.Evaluate(test.RequestProcess));
		Assert.IsFalse(test.User.Predicates.ContainsKey("foo"));
	}

	[Test]
	public void EvaluateWithGenericDefaultValue() {
		var test = new AimlTest();
		test.Bot.Config.UnbindPredicatesWithDefaultValue = true;
		test.User.Predicates["foo"] = "bar";
		var tag = new Aiml.Tags.Set(new("foo"), false, new("unknown"));
		Assert.AreEqual("unknown", tag.Evaluate(test.RequestProcess));
		Assert.IsFalse(test.User.Predicates.ContainsKey("foo"));
	}

	[Test]
	public void EvaluateWithVarDefaultValue() {
		var test = new AimlTest();
		test.Bot.Config.UnbindPredicatesWithDefaultValue = true;
		test.RequestProcess.Variables["bar"] = "baz";
		var tag = new Aiml.Tags.Set(new("bar"), true, new("unknown"));
		Assert.AreEqual("unknown", tag.Evaluate(test.RequestProcess));
		Assert.IsFalse(test.RequestProcess.Variables.ContainsKey("bar"));
	}
}
