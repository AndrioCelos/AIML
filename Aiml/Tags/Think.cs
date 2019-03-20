using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Processes the content without returning any text.
		/// </summary>
		/// <remarks>
		///     This element can be used to set variables or apply transformation to text, and only output the final result.
		///     This element is defined by the AIML 1.1 specification.
		/// </remarks>
		public sealed class Think : RecursiveTemplateTag {
			public Think(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				this.Children.Evaluate(process);
				return "";
			}

			public static Think FromXml(XmlNode node, AimlLoader loader) {
				return new Think(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
