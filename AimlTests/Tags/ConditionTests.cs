using Aiml.Tags;

namespace Aiml.Tests.Tags;
[TestFixture]
public class ConditionTests {
	private static AimlTest GetTest() {
		var test = new AimlTest();
		test.User.Predicates["foo"] = "predicate";
		test.Bot.Config.DefaultPredicates["bar"] = "default";
		test.RequestProcess.Variables["bar"] = "var";
		test.RequestProcess.Variables["n"] = "3";
		return test;
	}

	[Test]
	public void ParseType1WithName() {
		var tag = new Condition(name: new("foo"), var: null, value: new("sample predicate"), items: Array.Empty<Condition.Li>(), children: new("match"));
		Assert.AreEqual(1, tag.Items.Count);
		Assert.AreEqual("foo", tag.Items[0].Key?.ToString());
		Assert.IsFalse(tag.Items[0].LocalVar);
		Assert.AreEqual("sample predicate", tag.Items[0].Value?.ToString());
		Assert.AreEqual("match", tag.Items[0].Children?.ToString());
	}

	[Test]
	public void ParseType1WithVar() {
		var tag = new Condition(name: null, var: new("bar"), value: new("sample predicate"), items: Array.Empty<Condition.Li>(), children: new("match"));
		Assert.AreEqual(1, tag.Items.Count);
		Assert.AreEqual("bar", tag.Items[0].Key?.ToString());
		Assert.IsTrue(tag.Items[0].LocalVar);
		Assert.AreEqual("sample predicate", tag.Items[0].Value?.ToString());
		Assert.AreEqual("match", tag.Items[0].Children?.ToString());
	}

	[Test]
	public void ParseType2WithName() {
		var tag = new Condition(name: new("foo"), var: null, value: null, items: new Condition.Li[] {
			new(name: new("baz"), var: null, value: new("value 1"), children: new("match 1")),
			new(name: null, var: new("bar"), value: new("value 2"), children: new("match 2")),
			new(new("value 3"), new("match 3")),
			new(new("match 4"))
		}, children: TemplateElementCollection.Empty);
		Assert.AreEqual(4, tag.Items.Count);

		Assert.AreEqual("baz", tag.Items[0].Key?.ToString());
		Assert.IsFalse(tag.Items[0].LocalVar);
		Assert.AreEqual("value 1", tag.Items[0].Value?.ToString());
		Assert.AreEqual("match 1", tag.Items[0].Children?.ToString());

		Assert.AreEqual("bar", tag.Items[1].Key?.ToString());
		Assert.IsTrue(tag.Items[1].LocalVar);
		Assert.AreEqual("value 2", tag.Items[1].Value?.ToString());
		Assert.AreEqual("match 2", tag.Items[1].Children?.ToString());

		Assert.AreEqual("foo", tag.Items[2].Key?.ToString());
		Assert.IsFalse(tag.Items[2].LocalVar);
		Assert.AreEqual("value 3", tag.Items[2].Value?.ToString());
		Assert.AreEqual("match 3", tag.Items[2].Children?.ToString());

		Assert.IsNull(tag.Items[3].Key);
		Assert.IsNull(tag.Items[3].Value);
		Assert.AreEqual("match 4", tag.Items[3].Children?.ToString());
	}

	[Test]
	public void ParseType2WithVar() {
		var tag = new Condition(name: null, var: new("foo"), value: null, items: new Condition.Li[] {
			new(name: new("baz"), var: null, value: new("value 1"), children: new("match 1")),
			new(name: null, var: new("bar"), value: new("value 2"), children: new("match 2")),
			new(new("value 3"), new("match 3")),
			new(new("match 4"))
		}, children: TemplateElementCollection.Empty);
		Assert.AreEqual(4, tag.Items.Count);

		Assert.AreEqual("baz", tag.Items[0].Key?.ToString());
		Assert.IsFalse(tag.Items[0].LocalVar);
		Assert.AreEqual("value 1", tag.Items[0].Value?.ToString());
		Assert.AreEqual("match 1", tag.Items[0].Children?.ToString());

		Assert.AreEqual("bar", tag.Items[1].Key?.ToString());
		Assert.IsTrue(tag.Items[1].LocalVar);
		Assert.AreEqual("value 2", tag.Items[1].Value?.ToString());
		Assert.AreEqual("match 2", tag.Items[1].Children?.ToString());

		Assert.AreEqual("foo", tag.Items[2].Key?.ToString());
		Assert.IsTrue(tag.Items[2].LocalVar);
		Assert.AreEqual("value 3", tag.Items[2].Value?.ToString());
		Assert.AreEqual("match 3", tag.Items[2].Children?.ToString());

		Assert.IsNull(tag.Items[3].Key);
		Assert.IsNull(tag.Items[3].Value);
		Assert.AreEqual("match 4", tag.Items[3].Children?.ToString());
	}

	[Test]
	public void ParseType3() {
		var tag = new Condition(name: null, var: null, value: null, items: new Condition.Li[] {
			new(name: new("baz"), var: null, value: new("value 1"), children: new("match 1")),
			new(name: null, var: new("bar"), value: new("value 2"), children: new("match 2")),
			new(new("match 3"))
		}, children: TemplateElementCollection.Empty);
		Assert.AreEqual(3, tag.Items.Count);

		Assert.AreEqual("baz", tag.Items[0].Key?.ToString());
		Assert.IsFalse(tag.Items[0].LocalVar);
		Assert.AreEqual("value 1", tag.Items[0].Value?.ToString());
		Assert.AreEqual("match 1", tag.Items[0].Children?.ToString());

		Assert.AreEqual("bar", tag.Items[1].Key?.ToString());
		Assert.IsTrue(tag.Items[1].LocalVar);
		Assert.AreEqual("value 2", tag.Items[1].Value?.ToString());
		Assert.AreEqual("match 2", tag.Items[1].Children?.ToString());

		Assert.IsNull(tag.Items[2].Key);
		Assert.IsNull(tag.Items[2].Value);
		Assert.AreEqual("match 3", tag.Items[2].Children?.ToString());
	}

	[Test]
	public void ParseWithLiAfterDefault() {
		Assert.Throws<ArgumentException>(() => new Condition(name: null, var: null, value: null, items: new Condition.Li[] {
			new(new("match 2")),
			new(name: new("baz"), var: null, value: new("value 1"), children: new("match 1"))
		}, children: TemplateElementCollection.Empty));
	}

	[Test]
	public void ParseWithNameAndVar() {
		Assert.Throws<ArgumentException>(() => new Condition(name: new("foo"), var: new("bar"), value: new("value 1"), items: Array.Empty<Condition.Li>(), children: TemplateElementCollection.Empty));
	}

	[Test]
	public void ParseLiWithNameAndVar() {
		Assert.Throws<ArgumentException>(() => new Condition.Li(name: new("foo"), var: new("bar"), value: new("value 1"), children: TemplateElementCollection.Empty));
	}

	[Test]
	public void ParseType1WithNoVariable() {
		Assert.Throws<ArgumentException>(() => new Condition(name: null, var: null, value: new("value 1"), items: Array.Empty<Condition.Li>(), children: TemplateElementCollection.Empty));
	}

	[Test]
	public void ParseType1WithNoValue() {
		Assert.Throws<ArgumentException>(() => new Condition(name: new("foo"), var: null, value: null, items: Array.Empty<Condition.Li>(), children: TemplateElementCollection.Empty));
	}

	[Test]
	public void ParseType3WithNoValue() {
		Assert.Throws<ArgumentException>(() => new Condition(name: null, var: null, value: null, items: new Condition.Li[] {
			new(name: new("foo"), var: null, value: null, children: TemplateElementCollection.Empty)
		}, children: TemplateElementCollection.Empty));
	}

	[Test]
	public void PickWithNameMatch() {
		var items = new Condition.Li[] { new(new("foo"), false, new("predicate"), new(TemplateElementCollection.Empty)) };
		Assert.AreSame(items[0], new Condition(items).Pick(GetTest().RequestProcess));
	}

	[Test]
	public void PickWithNameNoMatch() {
		var items = new Condition.Li[] { new(new("foo"), false, new("var"), new(TemplateElementCollection.Empty)) };
		Assert.IsNull(new Condition(items).Pick(GetTest().RequestProcess));
	}

	[Test]
	public void PickWithNameWildcardMatch() {
		var items = new Condition.Li[] { new(new("foo"), false, new("*"), new(TemplateElementCollection.Empty)) };
		Assert.AreSame(items[0], new Condition(items).Pick(GetTest().RequestProcess));
	}

	[Test]
	public void PickWithNameWildcardNoMatch() {
		var items = new Condition.Li[] { new(new("bar"), false, new("*"), new(TemplateElementCollection.Empty)) };
		Assert.IsNull(new Condition(items).Pick(GetTest().RequestProcess));
	}

	[Test]
	public void PickWithVarMatch() {
		var items = new Condition.Li[] { new(new("bar"), true, new("var"), new(TemplateElementCollection.Empty)) };
		Assert.AreSame(items[0], new Condition(items).Pick(GetTest().RequestProcess));
	}

	[Test]
	public void PickWithVarNoMatch() {
		var items = new Condition.Li[] { new(new("bar"), true, new("predicate"), new(TemplateElementCollection.Empty)) };
		Assert.IsNull(new Condition(items).Pick(GetTest().RequestProcess));
	}

	[Test]
	public void PickWithVarWildcardMatch() {
		var items = new Condition.Li[] { new(new("bar"), true, new("*"), new(TemplateElementCollection.Empty)) };
		Assert.AreSame(items[0], new Condition(items).Pick(GetTest().RequestProcess));
	}

	[Test]
	public void PickWithVarWildcardNoMatch() {
		var items = new Condition.Li[] { new(new("foo"), true, new("*"), new(TemplateElementCollection.Empty)) };
		Assert.IsNull(new Condition(items).Pick(GetTest().RequestProcess));
	}

	[Test]
	public void PickWithDefault() {
		var items = new Condition.Li[] { new(new("bar"), false, new("*"), TemplateElementCollection.Empty), new(TemplateElementCollection.Empty) };
		Assert.AreSame(items[1], new Condition(items).Pick(GetTest().RequestProcess));
	}

	[Test]
	public void EvaluateWithSingleMatch() {
		var tag = new Condition(new Condition.Li[] { new(new("foo"), false, new("predicate"), new("match")) });
		Assert.AreEqual("match", tag.Evaluate(GetTest().RequestProcess));
	}

	[Test]
	public void EvaluateWithLoop() {
		var tag = new Condition(new Condition.Li[] {
			new(new("n"), true, new("0"), TemplateElementCollection.Empty),
			new(null, false, null, new(
				new Aiml.Tags.Set(new("n"), true, new(new Aiml.Tags.Map(new("predecessor"), new(new Get(new("n"), null, true))))),
				new Loop()
			))
		});
		Assert.AreEqual("210", tag.Evaluate(GetTest().RequestProcess));
	}

	[Test]
	public void EvaluateWithLoopType1() {
		var tag = new Condition(new Condition.Li[] {
			new(new("n"), true, new("3"), new(
				new Aiml.Tags.Set(new("n"), true, new(new Aiml.Tags.Map(new("predecessor"), new(new Get(new("n"), null, true))))),
				new Loop()
			))
		});
		Assert.AreEqual("2", tag.Evaluate(GetTest().RequestProcess));
	}

	[Test]
	public void EvaluateWithInfiniteLoop() {
		var test = new AimlTest();
		var tag = new Condition(new Condition.Li[] {
			new(new("n"), true, new("0"), TemplateElementCollection.Empty),
			new(null, false, null, new(new Loop()))
		});
		Assert.Throws<LoopLimitException>(() => test.AssertWarning(() => tag.Evaluate(test.RequestProcess)));
	}

	[Test]
	public void EvaluateWithNoMatch() {
		var tag = new Condition(new Condition.Li[] { new(new("foo"), false, new("var"), new("match")) });
		Assert.IsEmpty(tag.Evaluate(GetTest().RequestProcess));
	}
}
