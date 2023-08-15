using System.Xml;

namespace Aiml;
/// <summary>
/// Represents a clause inside a <c>select</c> or <c>uniq</c> tag.
/// A clause is one of the assertions that makes up a select query. It asserts that there either exists or does not exist a triple which matches the result of the select query.
/// </summary>
public class Clause(TemplateElementCollection? subj, TemplateElementCollection? pred, TemplateElementCollection? obj, bool affirm) : ICloneable {
	/// <summary>
	/// The subject of a triple.
	/// A value that doesn't start with '?' is considered text that a triple must match.
	/// A bound variable asserts that the triple subject matches its value.
	/// An unbound variable makes no assertion, but will be bound with a value from any matching triple.
	/// </summary>
	public TemplateElementCollection? Subject = subj;
	public TemplateElementCollection? Predicate = pred;
	public TemplateElementCollection? Object = obj;

	internal string? subj;
	internal string? pred;
	internal string? obj;

	/// <summary>True if a triple must match (for a <c>q</c> tag); false if no triple must match (for a <c>notq</c> tag).</summary>
	public bool Affirm = affirm;

	[AimlLoaderContructor]
	public Clause(XmlElement el, TemplateElementCollection? subj, TemplateElementCollection? pred, TemplateElementCollection? obj) : this(subj, pred, obj, el.Name.Equals("q", StringComparison.OrdinalIgnoreCase)) { }

	internal void Evaluate(RequestProcess process) {
		this.subj = this.Subject?.Evaluate(process);
		this.pred = this.Predicate?.Evaluate(process);
		this.obj = this.Object?.Evaluate(process);
	}

	public Clause Clone() => new(this.Subject, this.Predicate, this.Object, this.Affirm) { subj = this.subj, pred = this.pred, obj = this.obj };
	object ICloneable.Clone() => this.Clone();
}
