namespace Aiml.Tags;
/// <summary>This element is not implemented. It executes the <c>JSFAILED</c> category.</summary>
/// <remarks>This element is defined by the AIML 1.1 specification and deprecated by the AIML 2.0 specification.</remarks>
/// <seealso cref="Calculate"/>
public sealed class JavaScript(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) {
		process.Log(LogLevel.Warning, "In element <javascript>: <javascript> element is not implemented.");
		return new TemplateElementCollection(new Srai(new TemplateElementCollection("JSFAILED"))).Evaluate(process);
	}
}
