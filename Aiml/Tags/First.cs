using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Returns the first word of its content, or <c>DefaultListItem</c> if the evaluated content is empty.
		/// </summary>
		/// <remarks>
		///     This element is part of the Pandorabots extension of AIML.
		/// </remarks>
		/// <seealso cref="Rest"/><seealso cref="Srai"/>
		public sealed class First : RecursiveTemplateTag {
			public First(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				string sentence = (this.Children?.Evaluate(process) ?? "").Trim();
				if (sentence == "") return process.Bot.Config.DefaultListItem;

				int delimiter = sentence.IndexOf(' ');
				if (delimiter == -1) return sentence;
				return sentence.Substring(0, delimiter);
			}

			public static First FromXml(XmlNode node, AimlLoader loader) {
				return new First(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
