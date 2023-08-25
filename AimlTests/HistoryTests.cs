namespace Aiml.Tests;

[TestFixture]
public class HistoryTests {
	[Test]
	public void Initialise() {
		var subject = new History<string>(4);
		Assert.AreEqual(4, subject.Capacity);
		Assert.AreEqual(0, subject.Count);
	}

	[Test]
	public void Add() {
		var subject = new History<string>(4) {
			"Item 1",
			"Item 2"
		};
		Assert.AreEqual(2, subject.Count);
		Assert.AreEqual("Item 2", subject[0]);
		Assert.AreEqual("Item 1", subject[1]);
	}

	[Test]
	public void Add_PastCapacity() {
		var subject = new History<string>(4) {
			"Item 1",
			"Item 2",
			"Item 3",
			"Item 4",
			"Item 5"
		};
		Assert.AreEqual(4, subject.Count);
		Assert.AreEqual("Item 5", subject[0]);
		Assert.AreEqual("Item 2", subject[3]);
		Assert.Throws<IndexOutOfRangeException>(() => _ = subject[4]);
	}
}