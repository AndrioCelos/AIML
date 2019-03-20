using System;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		public sealed class Bot : TemplateNode {
			public TemplateElementCollection Key { get; private set; }

			public Bot(TemplateElementCollection key) {
				this.Key = key;
			}

			public override string Evaluate(RequestProcess process) {
				return process.Bot.GetProperty(this.Key.Evaluate(process));
			}

			public static TemplateNode.Bot FromXml(XmlNode node, AimlLoader loader) {
				// Search for XML attributes.
				XmlAttribute attribute;

				TemplateElementCollection? key = null;

				attribute = node.Attributes["name"];
				if (attribute != null) key = new TemplateElementCollection(attribute.Value);

				// Search for properties in elements.
				foreach (XmlNode node2 in node.ChildNodes) {
					if (node2.NodeType == XmlNodeType.Element) {
						if (node2.Name.Equals("name", StringComparison.InvariantCultureIgnoreCase)) {
							key = TemplateElementCollection.FromXml(node2, loader);
						}
					}
				}

				if (key == null) throw new AimlException("bot tag is missing a name property.");

				return new Bot(key);
			}
		}
	}
}
