namespace Aiml.Tests;

[TestFixture]
public class TripleCollectionTests {
	[Test]
	public void Add_NewTriple() {
		var triple = new Triple("Alice", "age", "25");
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase);
		Assert.IsTrue(subject.Add(triple));
		Assert.AreEqual(1, subject.Count);
		Assert.AreSame(triple, subject.First());
	}
	[Test]
	public void Add_ExistingTriple() {
		var triple = new Triple("Alice", "age", "25");
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) { triple };
		Assert.IsFalse(subject.Add("Alice", "age", "25"));
		Assert.AreEqual(1, subject.Count);
		Assert.AreSame(triple, subject.First());
	}

	[Test]
	public void Remove_ExistingTriple() {
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) {
			{ "Alice", "age", "25" },
			{ "Alice", "friendOf", "Bob" }
		};
		Assert.IsTrue(subject.Remove("Alice", "age", "25"));
		Assert.AreEqual(1, subject.Count);
	}

	[Test]
	public void Remove_AbsentTriple() {
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) {
			{ "Alice", "age", "25" },
			{ "Alice", "friendOf", "Bob" }
		};
		Assert.IsFalse(subject.Remove("Alice", "friendOf", "Carol"));
		Assert.AreEqual(2, subject.Count);
	}

	[Test]
	public void RemoveAll_SubjectAndPredicate() {
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) {
			{ "Alice", "age", "25" },
			{ "Alice", "friendOf", "Bob" },
			{ "Alice", "friendOf", "Carol" },
			{ "Alice", "friendOf", "Dan" }
		};
		Assert.AreEqual(3, subject.RemoveAll("Alice", "friendOf"));
		Assert.AreEqual(1, subject.Count);
	}

	[Test]
	public void RemoveAll_SubjectAndPredicateAbsent() {
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) {
			{ "Alice", "age", "25" }
		};
		Assert.AreEqual(0, subject.RemoveAll("Alice", "friendOf"));
		Assert.AreEqual(1, subject.Count);
	}

	[Test]
	public void RemoveAll_Subject() {
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) {
			{ "Alice", "age", "25" },
			{ "Alice", "friendOf", "Bob" },
			{ "Alice", "friendOf", "Carol" },
			{ "Alice", "friendOf", "Dan" }
		};
		Assert.AreEqual(4, subject.RemoveAll("Alice"));
		Assert.AreEqual(0, subject.Count);
	}

	[Test]
	public void RemoveAll_SubjectAbsent() {
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) {
			{ "Alice", "age", "25" }
		};
		Assert.AreEqual(0, subject.RemoveAll("Bob", "friendOf"));
		Assert.AreEqual(1, subject.Count);
	}

	[Test()]
	public void ClearTest() {
		var subject = new TripleCollection(StringComparer.InvariantCultureIgnoreCase) {
			{ "Alice", "age", "25" },
			{ "Alice", "friendOf", "Bob" }
		};
		subject.Clear();
		Assert.AreEqual(0, subject.Count);
		Assert.IsEmpty(subject);
	}

	private static TripleCollection GetTestCollection() => new(StringComparer.InvariantCultureIgnoreCase) {
		{ "Alice", "age", "25" },
		{ "Alice", "friendOf", "Bob" },
		{ "Alice", "friendOf", "Carol" },
		{ "Alice", "friendOf", "Dan" },
		{ "Bob", "age", "25" },
		{ "Carol", "age", "27" },
		{ "Carol", "friendOf", "Erin" },
		{ "Dan", "age", "28" },
		{ "Dan", "friendOf", "Erin" }
	};

	[Test]
	public void Match_CaseInsensitive() {
		var result = GetTestCollection().Match("alice", "friendof", "bob").Single();
		Assert.AreEqual("Alice", result.Subject);
		Assert.AreEqual("friendOf", result.Predicate);
		Assert.AreEqual("Bob", result.Object);
	}

	[TestCase("Carol", "friendOf", "Erin", ExpectedResult = 1, TestName = "Match count (all properties; present)")]
	[TestCase("Bob", "friendOf", "Erin", ExpectedResult = 0, TestName = "Match count (all properties; absent)")]
	[TestCase("Alice", "friendOf", null, ExpectedResult = 3, TestName = "Match count (subj and pred; present)")]
	[TestCase("Eve", "friendOf", null, ExpectedResult = 0, TestName = "Match count (subj and pred; absent)")]
	[TestCase("Alice", null, null, ExpectedResult = 4, TestName = "Match count (subj only; present)")]
	[TestCase("Eve", null, null, ExpectedResult = 0, TestName = "Match count (subj only; absent)")]
	[TestCase(null, "age", "25", ExpectedResult = 2, TestName = "Match count (obj and pred; present)")]
	[TestCase(null, "age", "24", ExpectedResult = 0, TestName = "Match count (obj and pred; absent)")]
	[TestCase(null, null, "Erin", ExpectedResult = 2, TestName = "Match count (obj only; present)")]
	[TestCase(null, null, "Eve", ExpectedResult = 0, TestName = "Match count (obj only; absent)")]
	[TestCase(null, "friendOf", null, ExpectedResult = 5, TestName = "Match count (pred only; present)")]
	[TestCase(null, "blocked", null, ExpectedResult = 0, TestName = "Match count (pred only; absent)")]
	[TestCase(null, null, null, ExpectedResult = 9, TestName = "Match count (no properties; present)")]
	public int MatchCountTest(string? subj, string? pred, string? obj)
		=> GetTestCollection().Match(subj, pred, obj).Count();

	[TestCase(null, null, null, ExpectedResult = 0, TestName = "Match count (no properties; empty)")]
	public int MatchCountTestEmpty(string? subj, string? pred, string? obj)
		=> new TripleCollection(StringComparer.InvariantCultureIgnoreCase).Match(subj, pred, obj).Count();
}
