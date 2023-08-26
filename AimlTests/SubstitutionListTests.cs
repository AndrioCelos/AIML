namespace Aiml.Tests;

[TestFixture]
public class SubstitutionListTests {
	[Test]
	public void Apply_AdjacentWords() {
		var subject = new SubstitutionList() { new(" foo ", " bar ") };
		Assert.AreEqual("A bar bar z", subject.Apply("A foo foo z"));
	}

	[Test]
	public void Apply_LastWord() {
		var subject = new SubstitutionList() { new(" foo ", " bar ") };
		Assert.AreEqual("A bar", subject.Apply("A foo"));
	}

	[Test]
	public void Apply_FirstWordSentenceCase() {
		var subject = new SubstitutionList() { new(" foo ", " bar ") };
		Assert.AreEqual("bar bar", subject.Apply("foo bar"));
	}

	[Test]
	public void Apply_SentenceCase() {
		var subject = new SubstitutionList(true) { new(" foo ", " bar ") };
		Assert.AreEqual(" Bar ", subject.Apply(" Foo "));
	}

	[Test]
	public void Apply_Uppercase() {
		var subject = new SubstitutionList(true) { new(" foo ", " bar ") };
		Assert.AreEqual(" BAR ", subject.Apply(" FOO "));
	}

	[Test]
	public void Apply_ChainedSubstitutions() {
		// Multiple substitutions should not be applied to the same word.
		// This matches Pandorabots, which is different from Program AB's substitutions.
		var subject = new SubstitutionList() { new(" you ", " me "), new(" with me ", " with you "), new(" me ", " you ") };
		Assert.AreEqual("Test with me and you talking", subject.Apply("Test with you and me talking"));
	}

	[Test]
	public void Apply_NoMatch() {
		var subject = new SubstitutionList() { new(" foo ", " bar ") };
		var s = " bar ";
		Assert.AreSame(s, subject.Apply(s));
	}
}
