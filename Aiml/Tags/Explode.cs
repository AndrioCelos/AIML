using System.Xml;

namespace Aiml;
public partial class TemplateNode {
	/// <summary>Splits the content into individual characters, separated by a space. Existing whitespace characters are skipped.</summary>
	/// <remarks>This element is defined by the AIML 2.0 specification.</remarks>
	public sealed class Explode(TemplateElementCollection children) : RecursiveTemplateTag(children) {
		public override string Evaluate(RequestProcess process) {
			var value = this.Children?.Evaluate(process) ?? "";
			return string.Join(" ", value.Where(c => !char.IsWhiteSpace(c)));
		}

		public static Explode FromXml(XmlNode node, AimlLoader loader) => new(TemplateElementCollection.FromXml(node, loader));
	}
}
