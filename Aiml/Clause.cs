using System.Xml.Linq;

namespace Aiml;
/// <summary>
/// Represents a clause inside a <c>select</c> or <c>uniq</c> tag.
/// A clause is one of the assertions that makes up a select query. It asserts that there either exists or does not exist a triple which matches the result of the select query.
/// </summary>
public class Clause(TemplateElementCollection subj, TemplateElementCollection pred, TemplateElementCollection obj, bool affirm)  {
	/// <summary>
	/// The subject of a triple.
	/// A value that doesn't start with '?' is considered text that a triple must match.
	/// A bound variable asserts that the triple subject matches its value.
	/// An unbound variable makes no assertion, but will be bound with a value from any matching triple.
	/// </summary>
	public TemplateElementCollection Subject { get; } = subj;
	public TemplateElementCollection Predicate { get; } = pred;
	public TemplateElementCollection Object { get; } = obj;

	/// <summary>True if a triple must match (for a <c>q</c> tag); false if no triple must match (for a <c>notq</c> tag).</summary>
	public bool Affirm = affirm;

	[AimlLoaderContructor]
	public Clause(XElement el, TemplateElementCollection subj, TemplateElementCollection pred, TemplateElementCollection obj) : this(subj, pred, obj, el.Name.LocalName.Equals("q", StringComparison.OrdinalIgnoreCase)) { }

	internal (string subj, string pred, string obj, bool affirm) Evaluate(RequestProcess process)
		=> (this.Subject.Evaluate(process), this.Predicate.Evaluate(process), this.Object.Evaluate(process), this.Affirm);
}
