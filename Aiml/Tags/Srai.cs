using System.Xml;

namespace Aiml; 
public partial class TemplateNode {
	/// <summary>Recurses the content into a new request and returns the result.</summary>
	/// <remarks>
	///		<para>The content is evaluated and then processed as if it had been entered by the user, including normalisation and other pre-processing.</para>
	///		<para>It is unknown what 'sr' stands for, but it's probably 'symbolic reduction'.</para>
	///		<para>This element is defined by the AIML 1.1 specification.</para>
	/// </remarks>
	/// <seealso cref="SR"/><seealso cref="SraiX"/>
	public sealed class Srai(TemplateElementCollection children) : RecursiveTemplateTag(children) {
		public override string Evaluate(RequestProcess process) {
			var text = this.Children?.Evaluate(process) ?? "";
			process.Log(LogLevel.Diagnostic, "In element <srai>: processing text '" + text + "'.");
			var newRequest = new Aiml.Request(text, process.User, process.Bot);
			text = process.Bot.ProcessRequest(newRequest, false, false, process.RecursionDepth + 1, out _).ToString().Trim();
			process.Log(LogLevel.Diagnostic, "In element <srai>: the request returned '" + text + "'.");
			return text;
		}

		public static Srai FromXml(XmlNode node, AimlLoader loader) => new(TemplateElementCollection.FromXml(node, loader));
	}
}
