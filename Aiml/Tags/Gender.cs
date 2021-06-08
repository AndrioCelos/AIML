using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Applies bot-defined pronoun substitutions to the content and returns the result.
		/// </summary>
		/// <remarks>
		///		<para>This element can also be used without content, in which case it is shorthand for <c><![CDATA[<gender><star/></gender>]]></c>.</para>
		///     <para>This element is defined by the AIML 1.1 specification.</para>
		/// </remarks>
		public sealed class Gender : RecursiveTemplateTag {
			public Gender(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				string text = this.Children?.Evaluate(process) ?? "";
				return process.Bot.Config.GenderSubstitutions.Apply(text);
			}

			public static Gender FromXml(XmlNode node, AimlLoader loader) {
				return new Gender(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
