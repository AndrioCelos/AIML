namespace Aiml.Tags;
/// <summary>Deletes triples from the bot's triple database.</summary>
/// <remarks>
///		<para>This element has the following attributes:</para>
///		<list type="table">
///			<item>
///				<term><c>subj</c>, <c>pred</c>, <c>obj</c></term>
///				<description>specify the triples to be deleted.</description>
///			</item>
///		</list>
///		<para>If only <c>subj</c> and <c>pred</c> is specified, it deletes all relations with the specified subject and predicate.
///			If only <c>subj</c> is specified, it deletes all relations with the specified subject.</para>
///		<para>If the triple does not exist, the triple database is unchanged.</para>
///		<para>This element has no other content.</para>
///		<para>This element is part of an extension to AIML derived from Program AB and Program Y.</para>
/// </remarks>
/// <seealso cref="AddTriple"/><seealso cref="Select"/><seealso cref="Uniq"/>
public sealed class DeleteTriple : TemplateNode {
	public TemplateElementCollection Subject { get; }
	public TemplateElementCollection? Predicate { get; }
	public TemplateElementCollection? Object { get; }

	public DeleteTriple(TemplateElementCollection subj, TemplateElementCollection? pred, TemplateElementCollection? obj) {
		this.Subject = subj;
		this.Predicate = pred;
		this.Object = obj;
		if (pred is null && obj is not null)
			throw new AimlException("<deletetriple> element cannot have 'obj' attribute without 'pred' attribute.");
	}

	public override string Evaluate(RequestProcess process) {
		var subj = this.Subject.Evaluate(process).Trim();
		var pred = this.Predicate?.Evaluate(process).Trim();
		var obj = this.Object?.Evaluate(process).Trim();

		if (string.IsNullOrEmpty(subj)) {
			process.Log(LogLevel.Warning, "In element <deletetriple>: Subject was empty.");
			return "";
		}

		if (string.IsNullOrEmpty(pred) || string.IsNullOrEmpty(obj)) {
			var count = string.IsNullOrEmpty(pred) ? process.Bot.Triples.RemoveAll(subj) : process.Bot.Triples.RemoveAll(subj, pred);
			process.Log(LogLevel.Diagnostic, $"In element <deletetriple>: Deleted {count} {(count == 1 ? "triple" : "triples")}. {{ Subject = {subj}, Predicate = {pred}, Object = {obj} }}");
		} else if (process.Bot.Triples.Remove(subj, pred, obj))
			process.Log(LogLevel.Diagnostic, $"In element <deletetriple>: Deleted a triple. {{ Subject = {subj}, Predicate = {pred}, Object = {obj} }}");
		else
			process.Log(LogLevel.Diagnostic, $"In element <deletetriple>: No such triple exists. {{ Subject = {subj}, Predicate = {pred}, Object = {obj} }}");

		return "";
	}
}
