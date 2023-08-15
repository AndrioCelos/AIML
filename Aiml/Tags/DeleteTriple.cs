namespace Aiml.Tags;
/// <summary>Deletes a triple from the bot's triple database and returns an opaque identifier for the deleted triple.</summary>
/// <remarks>
///		<para>This element has the following attributes:</para>
///		<list type="table">
///			<item>
///				<term><c>subj</c>, <c>pred</c>, <c>obj</c></term>
///				<description>specify the triple to be deleted.</description>
///			</item>
///		</list>
///		<para>If the triple does not exist, the triple database is unchanged and <c>DefaultTriple</c> is returned.</para>
///		<para>This element has no content.</para>
///		<para>This element is part of an extension to AIML derived from Program AB.</para>
/// </remarks>
/// <seealso cref="AddTriple"/><seealso cref="Select"/><seealso cref="Uniq"/>
public sealed class DeleteTriple(TemplateElementCollection subj, TemplateElementCollection pred, TemplateElementCollection obj) : TemplateNode {
	public TemplateElementCollection Subject { get; } = subj;
	public TemplateElementCollection Predicate { get; } = pred;
	public TemplateElementCollection Object { get; } = obj;

	public override string Evaluate(RequestProcess process) {
		var subj = this.Subject.Evaluate(process);
		var pred = this.Predicate.Evaluate(process);
		var obj = this.Object.Evaluate(process);

		if (string.IsNullOrWhiteSpace(subj) || string.IsNullOrWhiteSpace(pred) || string.IsNullOrWhiteSpace(obj)) {
			process.Log(LogLevel.Warning, $"In element <deletetriple>: Could not delete triple with missing elements.  Subject: {subj}  Predicate: {pred}  Object: {obj}");
			return process.Bot.Config.DefaultTriple;
		}

		var triples = process.Bot.Triples.Match(subj, pred, obj);
		if (triples.Count == 0) {
			process.Log(LogLevel.Diagnostic, $"In element <deletetriple>: No such triple exists.  Subject: {subj}  Predicate: {pred}  Object: {obj}");
			return process.Bot.Config.DefaultTriple;
		}

		var index = triples.First();
		process.Bot.Triples.Remove(index);
		process.Log(LogLevel.Diagnostic, $"In element <deletetriple>: Deleted the triple with key {index}.  Subject: {subj}  Predicate: {pred}  Object: {obj}");
		return index.ToString();
	}
}
