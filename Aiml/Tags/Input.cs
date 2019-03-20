using System;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Returns a sentence previously said to the bot for the current session.
		/// </summary>
		/// <remarks>
		///     The optional index property can contain up to two integers, separated by a comma. Both default to 1. An index of "m,n" returns the nth sentence in the mth last input.
		///     This element has no content.
		///     This element is defined by the AIML 1.1 specification.
		/// </remarks>
		public sealed class Input : TemplateNode {
			public TemplateElementCollection Index { get; set; }

			public Input(TemplateElementCollection index) {
				this.Index = index;
			}

			public override string Evaluate(RequestProcess process) {
				string? indexText = null; int index = 1;
				indexText = this.Index?.Evaluate(process);

				if (!string.IsNullOrWhiteSpace(indexText))
					index = int.Parse(indexText);

				return process.User.GetInput(index);
			}

			public static TemplateNode.Input FromXml(XmlNode node, AimlLoader loader) {
				// Search for XML attributes.
				XmlAttribute attribute;

				TemplateElementCollection index = null;

				attribute = node.Attributes["index"];
				if (attribute != null) index = new TemplateElementCollection(attribute.Value);

				// Search for properties in elements.
				foreach (XmlNode node2 in node.ChildNodes) {
					if (node2.NodeType == XmlNodeType.Element) {
						if (node2.Name.Equals("index", StringComparison.InvariantCultureIgnoreCase))
							index = TemplateElementCollection.FromXml(node2, loader);
					}
				}

				return new Input(index);
			}
		}
	}
}
