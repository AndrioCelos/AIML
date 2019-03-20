using System;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		public sealed class TopicStar : TemplateNode {
			public TemplateElementCollection Index { get; private set; }

			public TopicStar() : this(new TemplateElementCollection("1")) { }
			public TopicStar(TemplateElementCollection index) {
				this.Index = index;
			}

			public override string Evaluate(RequestProcess process) {
				int index = int.Parse(this.Index.Evaluate(process));

				if (process.topicstar.Count < index) return process.Bot.Config.DefaultWildcard;
				var match = process.topicstar[index - 1];
				return match == "" ? process.Bot.Config.DefaultWildcard : match;
			}

			public static TemplateNode.TopicStar FromXml(XmlNode node, AimlLoader loader) {
				// Search for XML attributes.
				XmlAttribute attribute;

				TemplateElementCollection index = new TemplateElementCollection("1");

				attribute = node.Attributes["index"];
				if (attribute != null) index = new TemplateElementCollection(attribute.Value);

				// Search for properties in elements.
				foreach (XmlNode node2 in node.ChildNodes) {
					if (node2.NodeType == XmlNodeType.Element) {
						if (node2.Name.Equals("index", StringComparison.InvariantCultureIgnoreCase))
							index = TemplateElementCollection.FromXml(node2, loader);
					}
				}

				return new TopicStar(index);
			}
		}
	}
}
