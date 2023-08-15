namespace Aiml.Tags;
/// <summary>Returns the part of the content after the first word, or <c>DefaultListItem</c> if the evaluated content does not have more than one word.</summary>
/// <remarks>This element is part of the Pandorabots extension of AIML.</remarks>
/// <seealso cref="First"/><seealso cref="Srai"/>
public sealed class Rest(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) {
		var sentence = this.EvaluateChildren(process).Trim();
		if (sentence == "") return process.Bot.Config.DefaultListItem;

		var delimiter = sentence.IndexOf(' ');
		if (delimiter == -1) return process.Bot.Config.DefaultListItem;
		return sentence[(delimiter + 1)..].TrimStart();
	}
}
