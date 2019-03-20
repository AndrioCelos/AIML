using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Replaces words in the content with first-person aspect with the corresponding second-person aspect, and vice versa.
		/// </summary>
		/// <remarks>
		///     This element can also be used without content, in which case it is shorthand for <code><person><star /></person></code>.
		///     This element is defined by the AIML 1.1 specification.
		/// </remarks>
		public sealed class Person : RecursiveTemplateTag {
			public Person(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				string text = this.Children?.Evaluate(process) ?? "";
				return process.Bot.Config.PersonSubstitutions.Apply(text);
			}

			public static Person FromXml(XmlNode node, AimlLoader loader) {
				return new Person(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
