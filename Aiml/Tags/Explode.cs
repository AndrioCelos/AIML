namespace Aiml.Tags;
/// <summary>Splits the content into individual characters, separated by a space. Existing whitespace characters are skipped.</summary>
/// <remarks>This element is defined by the AIML 2.0 specification.</remarks>
public sealed class Explode(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) {
		var value = this.EvaluateChildren(process);
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		return string.Join(' ', value.Where(char.IsLetterOrDigit));
#else
		return string.Join(" ", value.Where(char.IsLetterOrDigit));
#endif
	}
}
