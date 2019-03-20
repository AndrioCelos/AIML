using System;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Returns the entire text of a previous response.
		/// </summary>
		/// <remarks>
		///     The optional index property can contain an integer. It defaults to 1. The index-th last response is returned, corresponding to the index-th last request.
		///     This element has no content.
		///     This element is defined by the AIML 2.0 specification.
		/// </remarks>
		public sealed class Response : TemplateNode {
			public TemplateElementCollection Index { get; set; }

			public Response(TemplateElementCollection index) {
				this.Index = index;
			}

			public override string Evaluate(RequestProcess process) {
				string indexText = null; int index = 1;
				if (this.Index != null) indexText = this.Index.Evaluate(process);

				if (!string.IsNullOrWhiteSpace(indexText))
					index = int.Parse(indexText);

				return process.User.GetResponse(index);
			}

			public static Response FromXml(XmlNode node, AimlLoader loader) {
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

				return new Response(index);
			}
		}
	}
}
