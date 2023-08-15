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
		// Does the triple already exist?
		var clause = new Clause(this.Subject, this.Predicate, this.Object, true);
		clause.Evaluate(process);

		if (string.IsNullOrWhiteSpace(clause.subj) || string.IsNullOrWhiteSpace(clause.pred) || string.IsNullOrWhiteSpace(clause.obj)) {
			process.Log(LogLevel.Diagnostic, $"In element <addtriple>: Could not add triple with missing elements.  Subject: {clause.subj}  Predicate: {clause.pred}  Object: {clause.obj}");
			return process.Bot.Config.DefaultTriple;
		}

		var triples = process.Bot.Triples.Match(clause);
		if (triples.Count != 0) {
			process.Log(LogLevel.Diagnostic, $"In element <addtriple>: Triple already exists at key {triples.First()}.  Subject: {clause.subj}  Predicate: {clause.pred}  Object: {clause.obj}");
			return triples.First().ToString();
		}

		// Add the triple.
		var key = process.Bot.Triples.Add(clause.subj, clause.pred, clause.obj);
		process.Log(LogLevel.Diagnostic, $"In element <addtriple>: Added a new triple with key {key}.  Subject: {clause.subj}  Predicate: {clause.pred}  Object: {clause.obj}");
		return key.ToString();
	}
}
