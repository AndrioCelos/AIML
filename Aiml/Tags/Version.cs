using System.Reflection;
using System.Xml;

namespace Aiml; 
public partial class TemplateNode {
	/// <summary>Returns the version of the AIML interpreter.</summary>
	/// <remarks>
	///		<para>This element has no content.</para>
	///		<para>This element is part of an extension to AIML.</para>
	/// </remarks>
	public sealed class Version : TemplateNode {
		public override string Evaluate(RequestProcess process) => Assembly.GetExecutingAssembly().GetName().Version.ToString();

		public static Version FromXml(XmlNode node, AimlLoader loader) => new();  // The version tag supports no properties.
	}
}
