namespace Aiml.Tags;
/// <summary>Returns the entire text of a previous input to the bot, consisting of one or more sentences.</summary>
/// <remarks>
///		<para>This element has the following attribute:</para>
///		<list type="table">
///			<item>
///				<term><c>index</c></term>
///				<description>a number specifying which line to return. 1 returns the previous request, and so on.
///					If omitted, 1 is used.</description>
///			</item>
///		</list>
///		<para>This element has no content.</para>
///		<para>This element is defined by the AIML 2.0 specification.</para>
/// </remarks>
/// <seealso cref="Input"/><seealso cref="Response"/><seealso cref="That"/>
public sealed class Request(TemplateElementCollection? index) : TemplateNode {
	public TemplateElementCollection? Index { get; set; } = index;

	public override string Evaluate(RequestProcess process)
		=> TryParseIndex("request", process, this.Index, out var index) ? process.User.GetRequest(index) : process.Bot.Config.DefaultHistory;
}
