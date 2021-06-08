using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     This element is not implemented. It executes the <c>JSFAILED</c> category.
		/// </summary>
		/// <remarks>
		///     This element is defined by the AIML 1.1 specification and deprecated by the AIML 2.0 specification.
		/// </remarks>
		/// <seealso cref="Calculate"/>
		public sealed class JavaScript : RecursiveTemplateTag {
			public JavaScript(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				// TODO: implement this.
				return new TemplateElementCollection(new Srai(new TemplateElementCollection("JSFAILED"))).Evaluate(process);
			}

			public static JavaScript FromXml(XmlNode node, AimlLoader loader) {
				return new JavaScript(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
