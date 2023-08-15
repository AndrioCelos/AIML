using System.Xml;

namespace Aiml.Tags;
/// <summary>Processes the content as AIML and permanently adds it to the bot's brain, globally for all users.</summary>
/// <remarks>
///		<para>Unlike other elements with content, the content of this element is not normally evaluated until the newly-added category is called.
///			However, the special child element <c>eval</c> is replaced with the result of evaluating its own content.</para>
///		<para>This element is defined by the AIML 2.0 specification.</para>
/// </remarks>
/// <seealso cref="AddTriple"/><seealso cref="Learn"/><seealso cref="Set"/>
public sealed class LearnF(XmlElement el) : TemplateNode {
	public XmlElement XmlElement { get; private set; } = el;

	public override string Evaluate(RequestProcess process) {
		// Evaluate <eval> tags.
		var el = (XmlElement) this.XmlElement.Clone();
		Learn.ProcessXml(el, process);

		// Learn the result.
		process.Log(LogLevel.Diagnostic, "In element <learnf>: learning new category: " + el.OuterXml);
		var loader = new AimlLoader(process.Bot);
		loader.ProcessCategory(process.Bot.Graphmaster, el, null);

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
		writer.Write(el.InnerXml.Trim('\r', '\n'));
		writer.WriteLine();
		writer.WriteLine();
		writer.Write("</aiml>");
		writer.Close();

		return string.Empty;
	}
}
