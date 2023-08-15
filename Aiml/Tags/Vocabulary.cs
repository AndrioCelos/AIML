namespace Aiml.Tags;
/// <summary>Returns the total number of words in the pattern graph and AIML sets.</summary>
/// <remarks>
///		<para>This element has no content.</para>
///		<para>This element is defined by the AIML 2.0 specification.</para>
/// </remarks>
/// <seealso cref="Size"/>
public sealed class Vocabulary : TemplateNode {
	public override string Evaluate(RequestProcess process) => process.Bot.Vocabulary.ToString();
}
