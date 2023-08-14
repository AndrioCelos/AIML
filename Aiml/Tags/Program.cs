using System.Xml;

namespace Aiml; 
public partial class TemplateNode {
	/// <summary>Returns the name and version of the AIML interpreter.</summary>
	/// <remarks>
	///		<para>This element has no content.</para>
	///		<para>This element is defined by the AIML 2.0 specification.</para>
	/// </remarks>
	public sealed class Program : TemplateNode {
		public override string Evaluate(RequestProcess process) => $"Andrio Celos's AIML interpreter, version {typeof(Program).Assembly.GetName().Version:2}";

		public static Program FromXml(XmlNode node, AimlLoader loader) => new();  // The size tag supports no properties.
	}
}
