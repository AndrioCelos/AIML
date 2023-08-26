using Aiml.Tags;
using NUnit.Framework.Internal;

namespace Aiml.Tests.Tags;
[TestFixture]
public class SystemTests {
	[Test]
	public void Evaluate_DisabledByDefault() {
		var test = new AimlTest();
		var tag = new Aiml.Tags.System(new("foo"));
		Assert.AreEqual(test.Bot.Config.SystemFailedMessage, test.AssertWarning(() => tag.Evaluate(test.RequestProcess).ToString()));
	}
}
