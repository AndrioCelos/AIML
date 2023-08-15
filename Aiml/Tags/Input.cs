namespace Aiml.Tags;
/// <summary>Returns a current or previous input sentence from the current session.</summary>
/// <remarks>
///		<para>This element has the following attribute:</para>
///		<list type="table">
///			<item>
///				<term><c>index</c></term>
///				<description>a number specifying which sentence to return. 1 returns the current input sentence, 2 returns the previous sentence, and so on.
///					If omitted, 1 is used.</description>
///			</item>
///		</list>
///		<para>This element has no content.</para>
///		<para>This element is defined by the AIML 1.1 specification.</para>
/// </remarks>
/// <seealso cref="Request"/><seealso cref="Response"/><seealso cref="That"/>
public sealed class Input(TemplateElementCollection? index) : TemplateNode {
	public TemplateElementCollection? Index { get; set; } = index;

	public override string Evaluate(RequestProcess process) {
		string? indexText; var index = 1;
		indexText = this.Index?.Evaluate(process);

		if (!string.IsNullOrWhiteSpace(indexText))
			index = int.Parse(indexText);

		return process.User.GetInput(index);
	}
}
