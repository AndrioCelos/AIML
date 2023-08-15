namespace Aiml.Tags;
/// <summary>Returns the value of a bot property.</summary>
/// <remarks>
///		<para>This element has the following attribute:</para>
///		<list type="table">
///			<item>
///				<term><c>name</c></term>
///				<description>the property to get.</description>
///			</item>
///		</list>
///		<para>This element has no content.</para>
///		<para>This element is defined by the AIML 1.1 specification.</para>
/// </remarks>
public sealed class Bot(TemplateElementCollection name) : TemplateNode {
	public TemplateElementCollection Name { get; private set; } = name;

	public override string Evaluate(RequestProcess process) => process.Bot.GetProperty(this.Name.Evaluate(process));
}
