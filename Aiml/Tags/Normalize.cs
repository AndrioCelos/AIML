using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Normalises the content and returns the result.
		/// </summary>
		/// <remarks>
		///     This element is defined by the AIML 2.0 specification.
		/// </remarks>
		public sealed class Normalize : RecursiveTemplateTag {
			public Normalize(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				return process.Bot.Normalize(this.Children?.Evaluate(process) ?? "");
			}

			public static Normalize FromXml(XmlNode node, AimlLoader loader) {
				return new Normalize(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
