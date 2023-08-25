using Aiml.Tags;

namespace Aiml.Tests.Tags;

[TestFixture]
public class DeleteTripleTests {
	private static AimlTest GetTest() {
		var test = new AimlTest();
		test.Bot.Triples.Add("Alice", "age", "25");
		test.Bot.Triples.Add("Alice", "friendOf", "Bob");
		test.Bot.Triples.Add("Alice", "friendOf", "Carol");
		test.Bot.Triples.Add("Alice", "friendOf", "Dan");
		test.Bot.Triples.Add("Bob", "age", "25");
		test.Bot.Triples.Add("Carol", "age", "27");
		test.Bot.Triples.Add("Carol", "friendOf", "Erin");
		test.Bot.Triples.Add("Dan", "age", "28");
		test.Bot.Triples.Add("Dan", "friendOf", "Erin");
		return test;
	}

	[Test]
	public void ParseComplete() {
		var tag = new DeleteTriple(new("foo"), new("r"), new("bar"));
		Assert.AreEqual("foo", tag.Subject.ToString());
		Assert.AreEqual("r", tag.Predicate?.ToString());
		Assert.AreEqual("bar", tag.Object?.ToString());
	}

	[Test]
	public void ParseSubjectAndPredicateOnly() {
		var tag = new DeleteTriple(new("foo"), new("r"), null);
		Assert.AreEqual("foo", tag.Subject.ToString());
		Assert.AreEqual("r", tag.Predicate?.ToString());
		Assert.IsNull(tag.Object);
	}

	[Test]
	public void ParseSubjectOnly() {
		var tag = new DeleteTriple(new("foo"), null, null);
		Assert.AreEqual("foo", tag.Subject.ToString());
		Assert.IsNull(tag.Predicate);
		Assert.IsNull(tag.Object);
	}

	[Test]
	public void EvaluateComplete() {
		var test = GetTest();
		var tag = new DeleteTriple(new("Alice"), new("friendOf"), new("Bob"));
		tag.Evaluate(test.RequestProcess);
		Assert.AreEqual(8, test.Bot.Triples.Count);
	}

	[Test]
	public void EvaluateCompleteNonexistentTriple() {
		var test = GetTest();
		var tag = new DeleteTriple(new("Bob"), new("friendOf"), new("Erin"));
		tag.Evaluate(test.RequestProcess);
		Assert.AreEqual(9, test.Bot.Triples.Count);
	}

	[Test]
	public void EvaluateSubjectAndPredicateOnly() {
		var test = GetTest();
		var tag = new DeleteTriple(new("Alice"), new("friendOf"), null);
		tag.Evaluate(test.RequestProcess);
		Assert.AreEqual(6, test.Bot.Triples.Count);
	}

	[Test]
	public void EvaluateSubjectOnly() {
		var test = GetTest();
		var tag = new DeleteTriple(new("Alice"), null, null);
		tag.Evaluate(test.RequestProcess);
		Assert.AreEqual(5, test.Bot.Triples.Count);
	}
}
