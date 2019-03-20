using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Randomly selects and returns one of a choice of child elements.
		/// </summary>
		/// <remarks>
		///     This element can only contain <li> elements as direct children.
		///     This element is defined by the AIML 1.1 specification.
		/// </remarks>
		public sealed class Random : TemplateNode {
			private li[] items;

			public Random(li[] items) {
				if (items.Length == 0) throw new AimlException("Random element must contain at least one item.");
				this.items = items;
			}

			public li Pick() {
				return this.items[new global::System.Random().Next(this.items.Length)];
			}

			public override string Evaluate(RequestProcess process) {
				StringBuilder builder = new StringBuilder();
				li item;

				do {
					item = this.Pick();
					if (builder.Length != 0) builder.Append(" ");
					builder.Append(item.Evaluate(process));
				} while (item.Children.Loop);

				return builder.ToString();
			}

			public static Random FromXml(XmlNode node, AimlLoader loader) {
				List<li> items = new List<li>();

				// Search for items.
				foreach (XmlNode node2 in node.ChildNodes) {
					if (node2.NodeType == XmlNodeType.Element) {
						if (node2.Name.Equals("li", StringComparison.InvariantCultureIgnoreCase))
							items.Add(li.Parse(node2, loader));
					}
				}

				if (items.Count == 0)
					return new Random(items.ToArray());

				foreach (var item in items) {
					if (!item.Children.Loop)
						return new Random(items.ToArray());
				}
				throw new AimlException("Infinite loop: every <li> has a loop.");
			}

			public class li : RecursiveTemplateTag {
				public li(TemplateElementCollection children) : base(children) { }

				public override string Evaluate(RequestProcess process) {
					return this.Children?.Evaluate(process) ?? "";
				}

				public static TemplateNode.Random.li Parse(XmlNode node, AimlLoader loader) {
					return new li(TemplateElementCollection.FromXml(node, loader));
				}
			}

		}
	}
}
