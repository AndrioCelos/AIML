namespace Aiml.Tags;
/// <summary>Applies bot-defined third-person substitutions to the content and returns the result.</summary>
/// <remarks>
///		<para>This element can also be used without content, in which case it is shorthand for <c><![CDATA[<person2><star/></person2>]]></c>.</para>
///		<para>This element is defined by the AIML 1.1 specification.</para>
/// </remarks>
/// <seealso cref="Person"/>
public sealed class Person2(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) {
		var text = this.EvaluateChildrenOrStar(process);
		return process.Bot.Config.Person2Substitutions.Apply(text);
	}
}
