using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Returns the part of the content after the first word, or <c>DefaultListItem</c> if the evaluated content does not have more than one word.
		/// </summary>
		/// <remarks>
		///     This element is part of the Pandorabots extension of AIML.
		/// </remarks>
		/// <seealso cref="First"/><seealso cref="Srai"/>
		public sealed class Rest : RecursiveTemplateTag {
			public Rest(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				string sentence = (this.Children?.Evaluate(process) ?? "").Trim();
				if (sentence == "") return process.Bot.Config.DefaultListItem;

				int delimiter = sentence.IndexOf(' ');
				if (delimiter == -1) return process.Bot.Config.DefaultListItem;
				return sentence.Substring(delimiter + 1).TrimStart();
			}

			public static Rest FromXml(XmlNode node, AimlLoader loader) {
				return new Rest(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
