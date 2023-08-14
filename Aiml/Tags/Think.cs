using System.Xml;

namespace Aiml; 
public partial class TemplateNode {
	/// <summary>Evaluates the content but returns the empty string.</summary>
	/// <remarks>
	///		<para>This element can be used to set variables or make external queries without including intermediate results in output.</para>
	///		<para>This element is defined by the AIML 1.1 specification.</para>
	/// </remarks>
	public sealed class Think(TemplateElementCollection children) : RecursiveTemplateTag(children) {
		public override string Evaluate(RequestProcess process) {
			this.Children.Evaluate(process);
			return "";
		}

		public static Think FromXml(XmlNode node, AimlLoader loader) => new(TemplateElementCollection.FromXml(node, loader));
	}
}
