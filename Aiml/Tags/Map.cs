using System;
using System.Collections.Generic;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		public sealed class Map : RecursiveTemplateTag {
			public TemplateElementCollection Name { get; set; }

			public Map(TemplateElementCollection name, TemplateElementCollection children) : base(children) {
				this.Name = name;
			}

			public override string Evaluate(RequestProcess process) {
				if (process.Bot.Maps.TryGetValue(this.Name.Evaluate(process), out var map))
					return map[this.Children?.Evaluate(process) ?? ""] ?? process.Bot.Config.DefaultMap;

				return process.Bot.Config.DefaultMap;
			}

			public static Map FromXml(XmlNode node, AimlLoader loader) {
				// Search for XML attributes.
				XmlAttribute attribute;

				TemplateElementCollection name = null;
				List<TemplateNode> children = new List<TemplateNode>();

				attribute = node.Attributes["name"];
				if (attribute != null) name = new TemplateElementCollection(attribute.Value);

				// Search for properties in elements.
				foreach (XmlNode node2 in node.ChildNodes) {
					if (node2.NodeType == XmlNodeType.Whitespace) {
						children.Add(new TemplateText(" "));
					} else if (node2.NodeType == XmlNodeType.Text || node2.NodeType == XmlNodeType.SignificantWhitespace) {
						children.Add(new TemplateText(node2.InnerText));
					} else if (node2.NodeType == XmlNodeType.Element) {
						if (node2.Name.Equals("name", StringComparison.InvariantCultureIgnoreCase))
							name = TemplateElementCollection.FromXml(node2, loader);
						else
							children.Add(loader.ParseElement(node2));
					}
				}

				if (name == null) throw new AimlException("map tag is missing a name property.");

				return new Map(name, new TemplateElementCollection(children.ToArray()));
			}
		}
	}
}
