namespace Aiml.Tests;

[TestFixture]
public class InflectorTests {
	[Test]
	public void Singularize() {
		var subject = new Inflector(StringComparer.InvariantCultureIgnoreCase);
		Assert.AreEqual("bot", subject.Singularize("bots"));
		Assert.AreEqual("Axe", subject.Singularize("Axes"));
		Assert.AreEqual("tomato", subject.Singularize("tomatoes"));
		Assert.AreEqual("thesis", subject.Singularize("theses"));
		Assert.AreEqual("thesis", subject.Singularize("thesis"));
		Assert.AreEqual("elf", subject.Singularize("elves"));
		Assert.AreEqual("party", subject.Singularize("parties"));
		Assert.AreEqual("fox", subject.Singularize("foxes"));
		Assert.AreEqual("status", subject.Singularize("statuses"));
		Assert.AreEqual("Mox", subject.Singularize("Moxen"));
		Assert.AreEqual("person", subject.Singularize("people"));
		Assert.AreEqual("species", subject.Singularize("species"));
	}

	[Test]
	public void Pluralize() {
		var subject = new Inflector(StringComparer.InvariantCultureIgnoreCase);
		Assert.AreEqual("bots", subject.Pluralize("bot"));
		Assert.AreEqual("Axes", subject.Pluralize("Axe"));
		Assert.AreEqual("Axes", subject.Pluralize("Axis"));
		Assert.AreEqual("Tests", subject.Pluralize("Test"));
		Assert.AreEqual("missus", subject.Pluralize("missus"));
		Assert.AreEqual("news", subject.Pluralize("news"));
		Assert.AreEqual("theses", subject.Pluralize("thesis"));
		Assert.AreEqual("theses", subject.Pluralize("theses"));
		Assert.AreEqual("hives", subject.Pluralize("hive"));
		Assert.AreEqual("elves", subject.Pluralize("elf"));
		Assert.AreEqual("parties", subject.Pluralize("party"));
		Assert.AreEqual("series", subject.Pluralize("series"));
		Assert.AreEqual("movies", subject.Pluralize("movie"));
		Assert.AreEqual("foxes", subject.Pluralize("fox"));
		Assert.AreEqual("dormice", subject.Pluralize("dormouse"));
		Assert.AreEqual("buses", subject.Pluralize("bus"));
		Assert.AreEqual("horseshoes", subject.Pluralize("horseshoe"));
		Assert.AreEqual("crises", subject.Pluralize("crisis"));
		Assert.AreEqual("crises", subject.Pluralize("crises"));
		Assert.AreEqual("octopi", subject.Pluralize("octopus"));
		Assert.AreEqual("statuses", subject.Pluralize("status"));
		Assert.AreEqual("boxes", subject.Pluralize("box"));
		Assert.AreEqual("oxen", subject.Pluralize("ox"));
		Assert.AreEqual("people", subject.Pluralize("person"));
		Assert.AreEqual("species", subject.Pluralize("species"));
	}
}