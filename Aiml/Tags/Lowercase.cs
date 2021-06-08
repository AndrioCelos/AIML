using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Converts the content to lowercase.
		/// </summary>
		/// <remarks>
		///     This element is defined by the AIML 1.1 specification.
		/// </remarks>
		/// <seealso cref="Formal"/><seealso cref="Sentence"/><seealso cref="Uppercase"/>
		public sealed class Lowercase : RecursiveTemplateTag {
			public Lowercase(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				return (this.Children?.Evaluate(process) ?? "").ToLower();
			}

			public static TemplateNode.Lowercase FromXml(XmlNode node, AimlLoader loader) {
				return new Lowercase(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
