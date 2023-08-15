namespace Aiml.Tags;
/// <summary>Returns the entire text of a previous output from the bot, consisting of zero or more sentences.</summary>
/// <remarks>
///		<para>This element has the following attribute:</para>
///		<list type="table">
///			<item>
///				<term><c>index</c></term>
///				<description>a number specifying which line to return. 1 returns the previous response, and so on.
///					If omitted, 1 is used.</description>
///			</item>
///		</list>
///		<para>This element has no content.</para>
///		<para>This element is defined by the AIML 2.0 specification.</para>
/// </remarks>
/// <seealso cref="Input"/><seealso cref="Request"/><seealso cref="That"/>
public sealed class Response(TemplateElementCollection? index) : TemplateNode {
	public TemplateElementCollection? Index { get; set; } = index;

	public override string Evaluate(RequestProcess process) {
		var indexString = this.Index?.Evaluate(process);
		var index = !string.IsNullOrWhiteSpace(indexString) ? int.Parse(indexString) : 1;
		return process.User.GetResponse(index);
	}
}
