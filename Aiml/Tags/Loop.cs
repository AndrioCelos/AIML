using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     When used in a li element, causes the condition or random element to be re-evaluated.
		/// </summary>
		/// <remarks>
		///     This element has no content.
		///     This element is defined by the AIML 2.0 specification.
		/// </remarks>
		public sealed class Loop : TemplateNode {
			public Loop() { }

			public override string Evaluate(RequestProcess process) {
				return string.Empty;
			}

			public static Loop FromXml(XmlNode node, AimlLoader loader) {
				return new Loop();
			}
		}
	}
}
