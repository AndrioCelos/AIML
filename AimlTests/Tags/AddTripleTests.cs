using Aiml.Tags;

namespace Aiml.Tests.Tags;

[TestFixture]
public class AddTripleTests {
	[Test]
	public void Initialise() {
		var tag = new AddTriple(new("foo"), new("r"), new("bar"));
		Assert.AreEqual("foo", tag.Subject.ToString());
		Assert.AreEqual("r", tag.Predicate.ToString());
		Assert.AreEqual("bar", tag.Object.ToString());
	}

	[Test]
	public void EvaluateWithNewTriple() {
		var test = new AimlTest();
		var tag = new AddTriple(new("foo"), new("r"), new("bar"));
		tag.Evaluate(test.RequestProcess);
		Assert.AreEqual("{ Subject = foo, Predicate = r, Object = bar }", test.Bot.Triples.Single().ToString());
	}

	[Test]
	public void EvaluateWithExistingTriple() {
		var test = new AimlTest();
		test.Bot.Triples.Add("foo", "r", "bar");
		var tag = new AddTriple(new("foo"), new("r"), new("bar"));
		tag.Evaluate(test.RequestProcess);
		Assert.AreEqual("{ Subject = foo, Predicate = r, Object = bar }", test.Bot.Triples.Single().ToString());
	}

	[Test]
	public void EvaluateWithInvalidSubject() {
		var test = new AimlTest();
		var tag = new AddTriple(new(" "), new("r"), new("bar"));
		test.ExpectingWarning = true;
		tag.Evaluate(test.RequestProcess);
		Assert.IsEmpty(test.Bot.Triples);
		tag = new AddTriple(new("?foo"), new("r"), new("bar"));
		test.ExpectingWarning = true;
		tag.Evaluate(test.RequestProcess);
		Assert.IsEmpty(test.Bot.Triples);
	}

	[Test]
	public void EvaluateWithInvalidPredicate() {
		var test = new AimlTest();
		var tag = new AddTriple(new("foo"), new(" "), new("bar"));
		test.ExpectingWarning = true;
		tag.Evaluate(test.RequestProcess);
		Assert.IsEmpty(test.Bot.Triples);
		tag = new AddTriple(new("foo"), new("?r"), new("bar"));
		test.ExpectingWarning = true;
		tag.Evaluate(test.RequestProcess);
		Assert.IsEmpty(test.Bot.Triples);
	}

	[Test]
	public void EvaluateWithInvalidObject() {
		var test = new AimlTest();
		var tag = new AddTriple(new("foo"), new("r"), new(" "));
		test.ExpectingWarning = true;
		tag.Evaluate(test.RequestProcess);
		Assert.IsEmpty(test.Bot.Triples);
		tag = new AddTriple(new("foo"), new("r"), new("?bar"));
		test.ExpectingWarning = true;
		tag.Evaluate(test.RequestProcess);
		Assert.IsEmpty(test.Bot.Triples);
	}
}
