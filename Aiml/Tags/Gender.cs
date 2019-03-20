using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Replaces male-gendered words in the content with the corresponding female-gendered words, and vice versa.
		/// </summary>
		/// <remarks>
		///     This element can also be used without content, in which case it is shorthand for <code><gender><star /></gender></code>.
		///     This element is defined by the AIML 1.1 specification.
		/// </remarks>
		public sealed class Gender : RecursiveTemplateTag {
			public Gender(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				string text = this.Children?.Evaluate(process) ?? "";
				return process.Bot.Config.GenderSubstitutions.Apply(text);
			}

			public static Gender FromXml(XmlNode node, AimlLoader loader) {
				return new Gender(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
