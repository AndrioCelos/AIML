using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		public sealed class First : RecursiveTemplateTag {
			public First(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				string sentence = (this.Children?.Evaluate(process) ?? "").Trim();
				if (sentence == "") return process.Bot.Config.DefaultPredicate;

				int delimiter = sentence.IndexOf(' ');
				if (delimiter == -1) return sentence;
				if (sentence == "") return process.Bot.Config.DefaultListItem;
				return sentence.Substring(0, delimiter);
			}

			public static First FromXml(XmlNode node, AimlLoader loader) {
				return new First(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
