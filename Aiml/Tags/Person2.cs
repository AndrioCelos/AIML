using System.Xml;

namespace Aiml; 
public partial class TemplateNode {
	/// <summary>Applies bot-defined third-person substitutions to the content and returns the result.</summary>
	/// <remarks>
	///		<para>This element can also be used without content, in which case it is shorthand for <c><![CDATA[<person2><star/></person2>]]></c>.</para>
	///		<para>This element is defined by the AIML 1.1 specification.</para>
	/// </remarks>
	/// <seealso cref="Person"/>
	public sealed class Person2(TemplateElementCollection children) : RecursiveTemplateTag(children) {
		public override string Evaluate(RequestProcess process) {
			var text = this.Children == null || this.Children.Count == 0
				? process.star.Count > 0 && !string.IsNullOrEmpty(process.star[0]) ? process.star[0] : process.Bot.Config.DefaultWildcard
				: this.Children.Evaluate(process);
			return process.Bot.Config.Person2Substitutions.Apply(text);
		}

		public static Person2 FromXml(XmlNode node, AimlLoader loader) => new(TemplateElementCollection.FromXml(node, loader));
	}
}
