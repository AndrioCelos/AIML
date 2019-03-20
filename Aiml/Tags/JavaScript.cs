using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		public sealed class JavaScript : RecursiveTemplateTag {
			public JavaScript(TemplateElementCollection children) : base(children) { }

			public override string Evaluate(RequestProcess process) {
				// TODO: implement this.
				return new TemplateElementCollection(new Srai(new TemplateElementCollection("JSFAILED"))).Evaluate(process);
			}

			public static JavaScript FromXml(XmlNode node, AimlLoader loader) {
				return new JavaScript(TemplateElementCollection.FromXml(node, loader));
			}
		}
	}
}
