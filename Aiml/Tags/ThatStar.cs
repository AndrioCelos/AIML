namespace Aiml.Tags;
/// <summary>Returns the text matched by a wildcard or <c>set</c> tag in a <c>that</c> pattern.</summary>
/// <remarks>
///		<para>This element has the following attribute:</para>
///		<list type="table">
///			<item>
///				<term><c>index</c></term>
///				<description>the one-based index of the wildcard or set tag to check. If omitted, 1 is used.</description>
///			</item>
///		</list>
///		<para>This element has no content.</para>
///		<para>This element is defined by the AIML 1.1 specification.</para>
/// </remarks>
/// <seealso cref="Star"/><seealso cref="TopicStar"/>
public sealed class ThatStar(TemplateElementCollection? index) : TemplateNode {
	public TemplateElementCollection? Index { get; private set; } = index;

	public override string Evaluate(RequestProcess process)
		=> TryParseIndex("thatstar", process, this.Index, out var index) ? process.GetThatStar(index) : process.Bot.Config.DefaultWildcard;
}
