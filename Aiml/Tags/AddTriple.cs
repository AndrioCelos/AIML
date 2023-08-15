namespace Aiml.Tags;
/// <summary>Adds a triple to the bot's triple database and returns an opaque identifier for the new triple.</summary>
/// <remarks>
///		<para>This element has the following attributes:</para>
///		<list type="table">
///			<item>
///				<term><c>subj</c>, <c>pred</c>, <c>obj</c></term>
///				<description>specify the triple to be added.</description>
///			</item>
///		</list>
///		<para>
///			If the triple already exists, the triple database is unchanged and the identifier of the existing triple is returned.
///			If the triple cannot be added, <c>DefaultTriple</c> is returned.
///		</para>
///		<para>This element has no content.</para>
///		<para>This element is part of an extension to AIML derived from Program AB.</para>
/// </remarks>
/// <seealso cref="DeleteTriple"/><seealso cref="Learn"/><seealso cref="LearnF"/><seealso cref="Select"/><seealso cref="Uniq"/>
public sealed class AddTriple(TemplateElementCollection subj, TemplateElementCollection pred, TemplateElementCollection obj) : TemplateNode {
	public TemplateElementCollection Subject { get; } = subj;
	public TemplateElementCollection Predicate { get; } = pred;
	public TemplateElementCollection Object { get; } = obj;

	public override string Evaluate(RequestProcess process) {
		var subj = this.Subject.Evaluate(process);
		var pred = this.Predicate.Evaluate(process);
		var obj = this.Object.Evaluate(process);

		if (string.IsNullOrWhiteSpace(subj) || string.IsNullOrWhiteSpace(pred) || string.IsNullOrWhiteSpace(obj)) {
			process.Log(LogLevel.Warning, $"In element <addtriple>: Could not add triple with missing elements.  Subject: {subj}  Predicate: {pred}  Object: {obj}");
			return process.Bot.Config.DefaultTriple;
		}

		// Add the triple.
		if (process.Bot.Triples.Add(subj, pred, obj, out var key))
			process.Log(LogLevel.Diagnostic, $"In element <addtriple>: Added a new triple with key {key}.  Subject: {subj}  Predicate: {pred}  Object: {obj}");
		else
			process.Log(LogLevel.Diagnostic, $"In element <addtriple>: Triple already exists at key {key}.  Subject: {subj}  Predicate: {pred}  Object: {obj}");
		return key.ToString();
	}
}
