using System.Xml;
using System.Xml.Linq;

namespace Aiml.Tags;
/// <summary>Processes the content as AIML and permanently adds it to the bot's brain, globally for all users.</summary>
/// <remarks>
///		<para>Unlike other elements with content, the content of this element is not normally evaluated until the newly-added category is called.
///			However, the special child element <c>eval</c> is replaced with the result of evaluating its own content.</para>
///		<para>This element is defined by the AIML 2.0 specification.</para>
/// </remarks>
/// <seealso cref="AddTriple"/><seealso cref="Learn"/><seealso cref="Set"/>
public sealed class LearnF : TemplateNode {
	public XElement Element { get; private set; }

	public LearnF(XElement el) {
		this.Element = el;
		Learn.ValidateLearnElement(el);
	}

	public override string Evaluate(RequestProcess process) {
		// Evaluate <eval> tags.
		var el = new XElement(this.Element);
		Learn.ProcessXml(el, process);

		// Learn the result.
		process.Log(LogLevel.Diagnostic, "In element <learnf>: learning new category");
		process.Bot.AimlLoader.ForwardCompatible = false;
		process.Bot.AimlLoader.LoadAimlInto(process.Bot.Graphmaster, el);

		// Write it to a file.
		var newFile = !File.Exists(process.Bot.Config.LearnfFile) || new FileInfo(process.Bot.Config.LearnfFile).Length < 7;
		using var writer = new StreamWriter(File.Open("learnf.aiml", FileMode.OpenOrCreate, FileAccess.Write));
		using var xmlWriter = XmlWriter.Create(writer, new() { OmitXmlDeclaration = !newFile, Indent = true });
		if (newFile) {
			xmlWriter.WriteComment("This file contains AIML categories the bot has learned via <learnf> elements.");
			xmlWriter.WriteRaw("\n");
			xmlWriter.WriteStartElement("aiml");
			xmlWriter.WriteAttributeString("version", AimlLoader.AimlVersion.ToString());
			xmlWriter.WriteRaw("\n");
		} else {
			// Seek to just before the closing </aiml> tag.
			writer.BaseStream.Seek(-7, SeekOrigin.End);
			xmlWriter.WriteStartElement("aiml");
			xmlWriter.Flush();
			writer.BaseStream.Seek(-7, SeekOrigin.End);
		}

		xmlWriter.WriteComment($"Learned from {process.User.ID} via category '{process.Path}' on {DateTime.Now}.");
		foreach (var el2 in el.Elements())
			el2.WriteTo(xmlWriter);
		xmlWriter.WriteEndElement();

		return string.Empty;
	}
}
