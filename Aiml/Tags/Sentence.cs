using System.Text;

namespace Aiml.Tags;
/// <summary>Converts the content to sentence case by converting the first letter of each sentence to uppercase and other letters to lowercase.</summary>
/// <remarks>This element is defined by the AIML 1.1 specification.</remarks>
/// <seealso cref="Formal"/><seealso cref="Lowercase"/><seealso cref="Uppercase"/>
public sealed class Sentence(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) {
		var value = new StringBuilder(this.EvaluateChildren(process));

		int i;
		for (i = 0; i < value.Length; ++i) {
			if (char.IsLetterOrDigit(value[i])) {
				if (char.IsLower(value[i])) value[i] = char.ToUpper(value[i]);
				break;
			}
		}
		for (++i; i < value.Length; ++i)
			if (char.IsUpper(value[i])) value[i] = char.ToLower(value[i]);

		return value.ToString();
	}
}
