using System.Linq;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Splits the content into individual characters, separated by a space. Existing whitespace characters are skipped.
		/// </summary>
		/// <remarks>
		///     This element is defined by the AIML 2.0 specification.
		/// </remarks>
		public sealed class Explode : RecursiveTemplateTag {
			public Explode(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				string value = this.Children?.Evaluate(process) ?? "";
				return string.Join(" ", value.Where(c => !char.IsWhiteSpace(c)));
			}

			public static Explode FromXml(XmlNode node, AimlLoader loader) {
				return new Explode(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
