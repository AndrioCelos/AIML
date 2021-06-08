using System;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Returns a sentence previously output by the bot for the current session.
		/// </summary>
		/// <remarks>
		///		<para>This element has the following attribute:</para>
		///		<list type="table">
		///			<item>
		///				<term><c>index</c></term>
		///				<description>two numbers, comma-separated. <c>m,n</c> returns the nth last sentence of the mth last response. If omitted, <c>1,1</c> is used.</description>
		///			</item>
		///		</list>
		///     <para>This element has no content.</para>
		///     <para>This element is defined by the AIML 1.1 specification.</para>
		/// </remarks>
		/// <seealso cref="Input"/><seealso cref="Request"/><seealso cref="Response"/>
		public sealed class That : TemplateNode {
			public TemplateElementCollection Index { get; set; }

			public That(TemplateElementCollection index) {
				this.Index = index;
			}

			public override string Evaluate(RequestProcess process) {
				string indices = null; int responseIndex = 1; int sentenceIndex = 1;
				if (this.Index != null) indices = this.Index.Evaluate(process);

				if (!string.IsNullOrWhiteSpace(indices)) {
					// Parse the index attribute.
					string[] fields = indices.Split(',');
					if (fields.Length > 2) throw new ArgumentException("index attribute of a that tag evaluated to an invalid value (" + indices + ").");

					responseIndex = int.Parse(fields[0].Trim());
					if (fields.Length == 2)
						sentenceIndex = int.Parse(fields[1].Trim());
				}

				return process.User.GetThat(responseIndex, sentenceIndex);
			}

			public static TemplateNode.That FromXml(XmlNode node, AimlLoader loader) {
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

				return new That(index);
			}
		}
	}
}
