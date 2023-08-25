using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class GenderTests {
	[Test]
	public void Evaluate() {
		var test = new AimlTest();
		test.Bot.Config.GenderSubstitutions.Add(new(" he ", " she "));
		test.Bot.Config.GenderSubstitutions.Add(new(" her ", " his "));
		var tag = new Gender(new("he is her friend"));
		Assert.AreEqual("she is his friend", tag.Evaluate(test.RequestProcess).ToString());
	}
}
