namespace Aiml.Tags;
/// <summary>Applies bot-defined normalisation substitutions to the content and removes leading, trailing and repeated whitespace, and returns the result.</summary>
/// <remarks>
///		This element is defined by the AIML 2.0 specification.
/// </remarks>
/// <seealso cref="Denormalize"/>
public sealed class Normalize(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) => process.Bot.Normalize(this.EvaluateChildren(process));
}
