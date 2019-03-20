using System.Reflection;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		public sealed class Version : TemplateNode {
			public override string Evaluate(RequestProcess process) {
				return Assembly.GetExecutingAssembly().GetName().Version.ToString();
			}

			public static TemplateNode.Version FromXml(XmlNode node, AimlLoader loader) {
				// The version tag supports no properties.
				return new Version();
			}
		}
	}
}
