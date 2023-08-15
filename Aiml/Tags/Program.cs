namespace Aiml.Tags;
/// <summary>Returns the name and version of the AIML interpreter.</summary>
/// <remarks>
///		<para>This element has no content.</para>
///		<para>This element is defined by the AIML 2.0 specification.</para>
/// </remarks>
public sealed class Program : TemplateNode {
	public override string Evaluate(RequestProcess process) => $"Andrio Celos's AIML interpreter, version {typeof(Program).Assembly.GetName().Version:2}";
}
