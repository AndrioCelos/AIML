using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Applies bot-defined third-person substitutions to the content and returns the result.
		/// </summary>
		/// <remarks>
		///		<para>This element can also be used without content, in which case it is shorthand for <c><![CDATA[<person2><star/></person2>]]></c>.</para>
		///     <para>This element is defined by the AIML 1.1 specification.</para>
		/// </remarks>
		/// <seealso cref="Person"/>
		public sealed class Person2 : RecursiveTemplateTag {
			public Person2(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				string text;
				if (this.Children == null || this.Children.Count == 0)
					text = process.star.Count > 0 && !string.IsNullOrEmpty(process.star[0]) ? process.star[0] : process.Bot.Config.DefaultWildcard;
				else
					text = this.Children.Evaluate(process);
				return process.Bot.Config.Person2Substitutions.Apply(text);
			}

			public static Person2 FromXml(XmlNode node, AimlLoader loader) {
				return new Person2(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
