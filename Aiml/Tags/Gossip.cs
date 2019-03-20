using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		public sealed class Gossip : RecursiveTemplateTag {
			public Gossip(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				var message = this.Children?.Evaluate(process) ?? "";
				process.Bot.WriteGossip(process, message);
				return message;
			}

			public static Gossip FromXml(XmlNode node, AimlLoader loader) {
				return new Gossip(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
