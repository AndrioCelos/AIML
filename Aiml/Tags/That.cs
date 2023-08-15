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
		var responseIndex = 1; var sentenceIndex = 1;
		var indices = this.Index?.Evaluate(process);

		if (!string.IsNullOrWhiteSpace(indices)) {
			// Parse the index attribute.
			var fields = indices!.Split(',');
			if (fields.Length > 2) throw new ArgumentException("index attribute of a that tag evaluated to an invalid value (" + indices + ").");

			responseIndex = int.Parse(fields[0].Trim());
			if (fields.Length == 2)
				sentenceIndex = int.Parse(fields[1].Trim());
		}

		return process.User.GetThat(responseIndex, sentenceIndex);
	}
}
