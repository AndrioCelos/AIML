using System.Xml;

namespace Aiml;
public class Template(Bot bot, XmlNode xmlNode, TemplateElementCollection content, string? fileName) {
	public Bot Bot { get; } = bot;
	public XmlNode XmlNode { get; } = xmlNode;
	public TemplateElementCollection Content { get; } = content;
	public string? FileName { get; set; } = fileName;

	public IList<Tags.Test> GetTests() {
		var tests = new List<Tags.Test>();
		var collections = new Queue<TemplateElementCollection>();
		collections.Enqueue(this.Content);
		while (collections.Count > 0) {
			var collection = collections.Dequeue();
			foreach (var child in collection) {
				if (child is Tags.Test test)
					tests.Add(test);
				else if (child is RecursiveTemplateTag tag && tag.Children != null)
					collections.Enqueue(tag.Children);
			}
		}
		return tests;
	}
}
