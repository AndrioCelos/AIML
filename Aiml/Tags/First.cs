namespace Aiml.Tags;
/// <summary>Returns the first word of its content, or <c>DefaultListItem</c> if the evaluated content is empty.</summary>
/// <remarks>This element is part of the Pandorabots extension of AIML.</remarks>
/// <seealso cref="Rest"/><seealso cref="Srai"/>
public sealed class First(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) {
		var sentence = this.EvaluateChildren(process).Trim();
		if (sentence == "") return process.Bot.Config.DefaultListItem;

		var delimiter = sentence.IndexOf(' ');
		return delimiter < 0 ? sentence : sentence[..delimiter];
	}
}
