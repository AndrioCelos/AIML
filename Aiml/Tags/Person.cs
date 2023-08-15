namespace Aiml.Tags;
/// <summary>Applies bot-defined second-person substitutions to the content and returns the result.</summary>
/// <remarks>
///		<para>This element can also be used without content, in which case it is shorthand for <c><![CDATA[<person><star/></person>]]></c>.</para>
///		<para>This element is defined by the AIML 1.1 specification.</para>
/// </remarks>
/// <seealso cref="Person2"/>
public sealed class Person(TemplateElementCollection? children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) {
		var text = this.EvaluateChildrenOrStar(process);
		return process.Bot.Config.PersonSubstitutions.Apply(text);
	}
}
