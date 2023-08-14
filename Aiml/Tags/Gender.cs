using System.Xml;

namespace Aiml; 
public partial class TemplateNode {
	/// <summary>Applies bot-defined pronoun substitutions to the content and returns the result.</summary>
	/// <remarks>
	///		<para>This element can also be used without content, in which case it is shorthand for <c><![CDATA[<gender><star/></gender>]]></c>.</para>
	///		<para>This element is defined by the AIML 1.1 specification.</para>
	/// </remarks>
	public sealed class Gender(TemplateElementCollection children) : RecursiveTemplateTag(children) {
		public override string Evaluate(RequestProcess process) {
			var text = this.Children?.Evaluate(process) ?? "";
			return process.Bot.Config.GenderSubstitutions.Apply(text);
		}

		public static Gender FromXml(XmlNode node, AimlLoader loader) => new(TemplateElementCollection.FromXml(node, loader));
	}
}
