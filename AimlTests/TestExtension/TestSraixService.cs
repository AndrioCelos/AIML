using System.Xml.Linq;

namespace Aiml.Tests.TestExtension;

internal class TestSraixService : ISraixService {
	public string Process(string text, XElement element, RequestProcess process) {
		Assert.AreEqual("arguments", text);
		Assert.AreEqual("Sample", element.Attribute("customattr")?.Value);
		Assert.AreEqual("var", process.GetVariable("bar"));
		return "Success";
	}
}

internal class TestFaultSraixService() : ISraixService {
	public string Process(string text, XElement element, RequestProcess process) => throw new Exception("Test exception");
}
