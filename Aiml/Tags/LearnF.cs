using System.Xml;

namespace Aiml;
public partial class TemplateNode {
	/// <summary>Processes the content as AIML and permanently adds it to the bot's brain, globally for all users.</summary>
	/// <remarks>
	///		<para>Unlike other elements with content, the content of this element is not normally evaluated until the newly-added category is called.
	///			However, the special child element <c>eval</c> is replaced with the result of evaluating its own content.</para>
	///		<para>This element is defined by the AIML 2.0 specification.</para>
	/// </remarks>
	/// <seealso cref="AddTriple"/><seealso cref="Learn"/><seealso cref="Set"/>
	public sealed class LearnF(XmlNode node) : TemplateNode {
		public XmlNode Node { get; private set; } = node;

		public override string Evaluate(RequestProcess process) {
			// Evaluate <eval> tags.
			var node = this.Node.Clone();
			this.ProcessXml(node, process);

			// Learn the result.
			process.Log(LogLevel.Diagnostic, "In element <learnf>: learning new category: " + node.OuterXml);
			var loader = new AimlLoader(process.Bot);
			loader.ProcessCategory(process.Bot.Graphmaster, node, null);

			// Write it to a file.
			var newFile = !File.Exists(process.Bot.Config.LearnfFile) || new FileInfo(process.Bot.Config.LearnfFile).Length < 7;
			var writer = new StreamWriter(File.Open("learnf.aiml", FileMode.OpenOrCreate, FileAccess.Write));
			if (newFile) {
				writer.WriteLine("<!-- This file contains AIML categories the bot has learned via <learnf> elements. -->");
				writer.WriteLine();
				writer.WriteLine("<aiml version=\"2.0\">");
				writer.WriteLine();
			} else {
				// Seek to just before the closing </aiml> tag.
				writer.BaseStream.Seek(-7, SeekOrigin.End);
			}

			writer.WriteLine("<!-- Learned from " + process.User.ID + " via category '" + process.Path + "' on " + DateTime.Now + ". -->");
			writer.Write(node.InnerXml.Trim('\r', '\n'));
			writer.WriteLine();
			writer.WriteLine();
			writer.Write("</aiml>");
			writer.Close();

			return string.Empty;
		}

		public static LearnF FromXml(XmlNode node, AimlLoader loader) {
			var document = new XmlDocument { PreserveWhitespace = true };
			document.LoadXml(node.OuterXml);
			return new LearnF(document.DocumentElement);
		}

		private void ProcessXml(XmlNode node, RequestProcess process) {
			for (var i = 0; i < node.ChildNodes.Count; ++i) {
				var node2 = node.ChildNodes[i];
				if (node2.NodeType == XmlNodeType.Element) {
					if (node2.Name.Equals("eval", StringComparison.InvariantCultureIgnoreCase)) {
						var tags = TemplateElementCollection.FromXml(node2, process.Bot.AimlLoader);
						node2.ParentNode.ReplaceChild(node.OwnerDocument.CreateTextNode(tags.Evaluate(process)), node2);
					} else
						this.ProcessXml(node2, process);
				}
			}
		}
	}
}
