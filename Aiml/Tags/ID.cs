using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Returns a text identifying the current user.
		/// </summary>
		/// <remarks>
		///     This element has no content.
		///     This element is defined by the AIML 1.1 specification.
		/// </remarks>
		public sealed class ID : TemplateNode {
			public override string Evaluate(RequestProcess process) {
				return process.User.ID;
			}

			public static TemplateNode.ID FromXml(XmlNode node, AimlLoader loader) {
				return new ID();  // The id tag supports no properties.
			}
		}
	}
}
