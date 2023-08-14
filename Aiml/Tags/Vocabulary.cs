using System.Xml;

namespace Aiml; 
public partial class TemplateNode {
	/// <summary>Returns the total number of words in the pattern graph and AIML sets.</summary>
	/// <remarks>
	///		<para>This element has no content.</para>
	///		<para>This element is defined by the AIML 2.0 specification.</para>
	/// </remarks>
	/// <seealso cref="Size"/>
	public sealed class Vocabulary : TemplateNode {
		public override string Evaluate(RequestProcess process) => process.Bot.Vocabulary.ToString();

		public static Vocabulary FromXml(XmlNode node, AimlLoader loader) => new();  // The size tag supports no properties.
	}
}
