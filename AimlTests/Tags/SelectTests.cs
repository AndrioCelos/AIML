using System.Xml.Linq;
using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class SelectTests {
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
	public void EvaluateBacktracking() {
		var tag = new Select(new("?x"), new Clause[] {
			new(new("?x"), new("r"), new("?y"), true),
			new(new("?y"), new("r"), new("X"), true)
		});
		Assert.AreEqual("Aj94AUE=", tag.Evaluate(GetTest().RequestProcess).ToString());
	}

	[Test]
	public void EvaluateBacktrackingWithoutVars() {
		var tag = new Select(null, new Clause[] {
			new(new("?x"), new("r"), new("?y"), true),
			new(new("?y"), new("r"), new("X"), true)
		});
		Assert.AreEqual("Aj95AU4CP3gBQQ== Aj95AU8CP3gBQQ==", tag.Evaluate(GetTest().RequestProcess).ToString());
	}

	[Test]
	public void EvaluateNoMatch() {
		var tag = new Select(new("?x"), new Clause[] { new(new("A"), new("attr"), new("?x"), true) });
		Assert.AreEqual("nil", tag.Evaluate(GetTest().RequestProcess).ToString());
	}

	[Test]
	public void EvaluateWithNotQ() {
		var tag = new Select(new("?y"), new Clause[] {
			new(new("A"), new("r"), new("?y"), true),
			new(new("?y"), new("attr"), new("0"), false)
		});
		Assert.AreEqual("Aj95AU0= Aj95AU8=", tag.Evaluate(GetTest().RequestProcess).ToString());
	}

	[Test]
	public void FromXml() {
		const string xml = @"
<select>
	<vars>?x</vars>
	<q><subj>?x</subj><pred>r</pred><obj>?y</obj></q>
	<notq><subj>?y</subj><pred>attr</pred><obj>0</obj></notq>
</select>";
		var tag = Select.FromXml(XElement.Parse(xml), new AimlTest().Bot.AimlLoader);
		Assert.AreEqual("?x", tag.Variables?.ToString());
		Assert.AreEqual(2, tag.Clauses.Length);
		Assert.IsTrue(tag.Clauses[0].Affirm);
		Assert.AreEqual("?x", tag.Clauses[0].Subject.ToString());
		Assert.AreEqual("r", tag.Clauses[0].Predicate.ToString());
		Assert.AreEqual("?y", tag.Clauses[0].Object.ToString());
		Assert.IsFalse(tag.Clauses[1].Affirm);
		Assert.AreEqual("?y", tag.Clauses[1].Subject.ToString());
		Assert.AreEqual("attr", tag.Clauses[1].Predicate.ToString());
		Assert.AreEqual("0", tag.Clauses[1].Object.ToString());
	}

	[Test]
	public void FromXmlWithoutVars() {
		const string xml = @"
<select>
	<q><subj>?x</subj><pred>r</pred><obj>?y</obj></q>
	<notq><subj>?y</subj><pred>attr</pred><obj>0</obj></notq>
</select>";
		var tag = Select.FromXml(XElement.Parse(xml), new AimlTest().Bot.AimlLoader);
		Assert.IsNull(tag.Variables);
	}

	[Test]
	public void FromXmlWithoutClauses() {
		const string xml = "<select/>";
		Assert.Throws<ArgumentException>(() => Select.FromXml(XElement.Parse(xml), new AimlTest().Bot.AimlLoader));
	}

	[Test]
	public void FromXmlWithInvalidContent() {
		const string xml = "<select>foo</select>";
		Assert.Throws<AimlException>(() => Select.FromXml(XElement.Parse(xml), new AimlTest().Bot.AimlLoader));
	}

	[Test]
	public void FromXmlWithInvalidElement() {
		const string xml = "<select><foo/></select>";
		Assert.Throws<AimlException>(() => Select.FromXml(XElement.Parse(xml), new AimlTest().Bot.AimlLoader));
	}

	[Test]
	public void FromXmlWithNotQFirst() {
		const string xml = "<select><notq><subj>?x</subj><pred>attr</pred><obj>0</obj></notq></select>";
		Assert.Throws<ArgumentException>(() => Select.FromXml(XElement.Parse(xml), new AimlTest().Bot.AimlLoader));
	}
}
