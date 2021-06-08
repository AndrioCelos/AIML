using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Evaluates the content but returns the empty string.
		/// </summary>
		/// <remarks>
		///     <para>This element can be used to set variables or make external queries without including intermediate results in output.</para>
		///     <para>This element is defined by the AIML 1.1 specification.</para>
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
