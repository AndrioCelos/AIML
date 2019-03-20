using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Aiml {
	public class Template {
		public Bot Bot { get; }
		public XmlNode XmlNode { get; }
		public TemplateElementCollection Content { get; }
		public string? FileName { get; set; }

		public Template(Bot bot, XmlNode xmlNode, TemplateElementCollection content, string? fileName) {
			this.Bot = bot;
			this.XmlNode = xmlNode;
			this.Content = content;
			this.FileName = fileName;
        }

		public IList<TemplateNode.Test> GetTests() {
			var tests = new List<TemplateNode.Test>();
			var collections = new Queue<TemplateElementCollection>();
			collections.Enqueue(this.Content);
			while (collections.Count > 0) {
				var collection = collections.Dequeue();
				foreach (var child in collection) {
					if (child is TemplateNode.Test test)
						tests.Add(test);
					else if (child is RecursiveTemplateTag tag && tag.Children != null)
						collections.Enqueue(tag.Children);
				}
			}
			return tests;
		}
	}
}
