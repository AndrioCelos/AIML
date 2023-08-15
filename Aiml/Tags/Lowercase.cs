namespace Aiml.Tags;
/// <summary>Converts the content to lowercase.</summary>
/// <remarks>This element is defined by the AIML 1.1 specification.</remarks>
/// <seealso cref="Formal"/><seealso cref="Sentence"/><seealso cref="Uppercase"/>
public sealed class Lowercase(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) => this.EvaluateChildren(process).ToLower();
}
