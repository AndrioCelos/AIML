namespace Aiml.Tags;
/// <summary>Returns the number of AIML categories currently loaded.</summary>
/// <remarks>
///		<para>This element has no content.</para>
///		<para>This element is defined by the AIML 1.1 specification.</para>
/// </remarks>
/// <seealso cref="Vocabulary"/>
public sealed class Size : TemplateNode {
	public override string Evaluate(RequestProcess process) => process.Bot.Size.ToString();
}
