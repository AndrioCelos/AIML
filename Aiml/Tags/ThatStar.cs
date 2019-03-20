using System;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		public sealed class ThatStar : TemplateNode {
			public TemplateElementCollection Index { get; private set; }

			public ThatStar() : this(new TemplateElementCollection("1")) { }
			public ThatStar(TemplateElementCollection index) {
				this.Index = index;
			}

			public override string Evaluate(RequestProcess process) {
				int index = int.Parse(this.Index.Evaluate(process));

				if (process.thatstar.Count < index) return process.Bot.Config.DefaultWildcard;
				var match = process.thatstar[index - 1];
				return match == "" ? process.Bot.Config.DefaultWildcard : match;
			}

			public static TemplateNode.ThatStar FromXml(XmlNode node, AimlLoader loader) {
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

				return new ThatStar(index);
			}
		}
	}
}
