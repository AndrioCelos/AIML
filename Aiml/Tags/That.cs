namespace Aiml.Tags;
/// <summary>Returns a sentence previously output by the bot for the current session.</summary>
/// <remarks>
///		<para>This element has the following attribute:</para>
///		<list type="table">
///			<item>
///				<term><c>index</c></term>
///				<description>two numbers, comma-separated. <c>m,n</c> returns the nth last sentence of the mth last response. If omitted, <c>1,1</c> is used.</description>
///			</item>
///		</list>
///		<para>This element has no content.</para>
///		<para>This element is defined by the AIML 1.1 specification.</para>
/// </remarks>
/// <seealso cref="Input"/><seealso cref="Request"/><seealso cref="Response"/>
public sealed class That(TemplateElementCollection? index) : TemplateNode {
	public TemplateElementCollection? Index { get; set; } = index;

	public override string Evaluate(RequestProcess process) {
		if (this.Index is null) return process.User.That;

		var indices = this.Index.Evaluate(process);
#if NET5_0_OR_GREATER
		var fields = indices.Split(',', StringSplitOptions.TrimEntries);
#else
		var fields = indices.Split(',');
		for (var i = 0; i < fields.Length; i++) fields[i] = fields[i].Trim();
#endif
		if (fields.Length != 2 || !int.TryParse(fields[0], out var responseIndex) || responseIndex <= 0 || !int.TryParse(fields[1], out var sentenceIndex) || sentenceIndex <= 0) {
			process.Log(LogLevel.Warning, $"In element <that>: 'index' was not valid: {indices}");
			return process.Bot.Config.DefaultHistory;
		}

		return process.User.GetThat(responseIndex, sentenceIndex);
	}
}
