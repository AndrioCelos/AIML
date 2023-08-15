namespace Aiml.Tags;
/// <summary>Applies bot-defined denormalisation substitutions to the content and returns the result.</summary>
/// <remarks>
///		<para>This is a specific set of substitutions and not the inverse of <see cref="Normalize"/>.</para>
///		<para>This element is defined by the AIML 2.0 specification.</para>
/// </remarks>
/// <seealso cref="Normalize"/>
public sealed class Denormalize(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) => process.Bot.Denormalize(this.EvaluateChildren(process));
}
