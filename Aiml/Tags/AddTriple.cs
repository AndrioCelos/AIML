namespace Aiml.Tags;
/// <summary>Adds a triple to the bot's triple database if it is not already present.</summary>
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
///		<para>This element has no other content.</para>
///		<para>This element is part of an extension to AIML derived from Program AB and Program Y.</para>
/// </remarks>
/// <seealso cref="DeleteTriple"/><seealso cref="Learn"/><seealso cref="LearnF"/><seealso cref="Select"/><seealso cref="Uniq"/>
public sealed class AddTriple(TemplateElementCollection subj, TemplateElementCollection pred, TemplateElementCollection obj) : TemplateNode {
	public TemplateElementCollection Subject { get; } = subj;
	public TemplateElementCollection Predicate { get; } = pred;
	public TemplateElementCollection Object { get; } = obj;

	public override string Evaluate(RequestProcess process) {
		var subj = this.Subject.Evaluate(process).Trim();
		var pred = this.Predicate.Evaluate(process).Trim();
		var obj = this.Object.Evaluate(process).Trim();

		if (string.IsNullOrEmpty(subj) || string.IsNullOrEmpty(pred) || string.IsNullOrEmpty(obj)) {
			process.Log(LogLevel.Warning, $"In element <addtriple>: Could not add triple with missing elements. {{ Subject = {subj}, Predicate = {pred}, Object = {obj} }}");
			return "";
		}

		if (process.Bot.Triples.Add(subj, pred, obj))
			process.Log(LogLevel.Diagnostic, $"In element <addtriple>: Added a new triple. {{ Subject = {subj}, Predicate = {pred}, Object = {obj} }}");
		else
			process.Log(LogLevel.Diagnostic, $"In element <addtriple>: Triple already exists. {{ Subject = {subj}, Predicate = {pred}, Object = {obj} }}");
		return "";
	}
}
