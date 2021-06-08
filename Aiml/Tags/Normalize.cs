using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Applies bot-defined normalisation substitutions to the content and removes leading, trailing and repeated whitespace, and returns the result.
		/// </summary>
		/// <remarks>
		///     This element is defined by the AIML 2.0 specification.
		/// </remarks>
		/// <seealso cref="Denormalize"/>
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
