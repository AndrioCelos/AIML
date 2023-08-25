using System.Xml;

namespace Aiml.Tests.TestExtension;

internal class TestSraixService : ISraixService {
	public string Process(string text, XmlAttributeCollection attributes, RequestProcess process) {
		Assert.AreEqual("arguments", text);
		Assert.AreEqual("Sample", attributes["customattr"]?.Value);
		Assert.AreEqual("var", process.GetVariable("bar"));
		return "Success";
	}
}

internal class TestFaultSraixService() : ISraixService {
	public string Process(string text, XmlAttributeCollection attributes, RequestProcess process) => throw new Exception("Test exception");
}
