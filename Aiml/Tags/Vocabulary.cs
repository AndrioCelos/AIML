using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Returns the total number of words in pattern paths and AIML sets.
		/// </summary>
		/// <remarks>
		///     This element has no content.
		///     This element is defined by the AIML 2.0 specification.
		/// </remarks>
		public sealed class Vocabulary : TemplateNode {
			public override string Evaluate(RequestProcess process) {
				return process.Bot.Vocabulary.ToString();
			}

			public static Vocabulary FromXml(XmlNode node, AimlLoader loader) {
				return new Vocabulary();  // The size tag supports no properties.
			}
		}
	}
}
