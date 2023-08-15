namespace Aiml.Tags;
/// <summary>Converts the content to uppercase.</summary>
/// <remarks>This element is defined by the AIML 1.1 specification.</remarks>
/// <seealso cref="Formal"/><seealso cref="Lowercase"/><seealso cref="Sentence"/>
public sealed class Uppercase(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) => this.EvaluateChildren(process).ToUpper();
}
