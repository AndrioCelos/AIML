using System.Text;

namespace Aiml.Tags;
/// <summary>Converts the content to title case by converting the first letter of each word to uppercase and other letters to lowercase.</summary>
/// <remarks>This element is defined by the AIML 1.1 specification.</remarks>
/// <seealso cref="Lowercase"/><seealso cref="Sentence"/><seealso cref="Uppercase"/>
public sealed class Formal(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) {
		var value = new StringBuilder(this.EvaluateChildren(process));

		var firstLetter = true;
		for (var i = 0; i < value.Length; ++i) {
			if (char.IsWhiteSpace(value[i]))
				firstLetter = true;
			else {
				if (firstLetter) {
					if (char.IsLower(value[i])) value[i] = char.ToUpper(value[i]);
					firstLetter = false;
				} else {
					if (char.IsUpper(value[i])) value[i] = char.ToLower(value[i]);
				}
			}
		}

		return value.ToString();
	}
}
