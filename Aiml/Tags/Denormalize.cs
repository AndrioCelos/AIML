using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		public sealed class Denormalize : RecursiveTemplateTag {
			public Denormalize(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				return process.Bot.Denormalize(this.Children?.Evaluate(process) ?? "");
			}

			public static Denormalize FromXml(XmlNode node, AimlLoader loader) {
				return new Denormalize(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
