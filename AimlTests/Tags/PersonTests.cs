using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class PersonTests {
	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		test.Bot.Config.PersonSubstitutions.Add(new(" me ", " you "));
		var tag = new Person(new("It is me"));
		Assert.AreEqual("It is you", tag.Evaluate(test.RequestProcess).ToString());
	}
}
