namespace Aiml.Tags;
/// <summary>Recurses the content into a new request and returns the result.</summary>
/// <remarks>
///		<para>The content is evaluated and then processed as if it had been entered by the user, including normalisation and other pre-processing.</para>
///		<para>It is unknown what 'sr' stands for, but it's probably 'symbolic reduction'.</para>
///		<para>This element is defined by the AIML 1.1 specification.</para>
/// </remarks>
/// <seealso cref="SR"/><seealso cref="SraiX"/>
public sealed class Srai(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) {
		var text = this.EvaluateChildren(process);
		process.Log(LogLevel.Diagnostic, "In element <srai>: processing text '" + text + "'.");
		text = process.Srai(text);
		process.Log(LogLevel.Diagnostic, "In element <srai>: the request returned '" + text + "'.");
		return text;
	}
}
