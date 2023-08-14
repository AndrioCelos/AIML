using System.Xml;

namespace Aiml; 
public partial class TemplateNode {
	/// <summary>Returns an application-defined string identifying the current user.</summary>
	/// <remarks>
	///		<para>This element has no content.</para>
	///		<para>This element is defined by the AIML 1.1 specification.</para>
	/// </remarks>
	public sealed class ID : TemplateNode {
		public override string Evaluate(RequestProcess process) => process.User.ID;
		public static ID FromXml(XmlNode node, AimlLoader loader) => new();  // The id tag supports no properties.
	}
}
