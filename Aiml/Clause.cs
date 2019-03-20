using System;
using System.Xml;

namespace Aiml {
	/// <summary>
	/// Represents a clause inside a <c>select</c> or <c>uniq</c> tag.
	/// A clause is one of the assertions that makes up a select query. It asserts that there either exists or does not exist a triple which matches the result of the select query.
	/// </summary>
	public class Clause : ICloneable {
		/// <summary>
		/// The subject of a triple.
		/// A value that doesn't start with '?' is considered text that a triple must match.
		/// A bound variable asserts that the triple subject matches its value.
		/// An unbound variable makes no assertion, but will be bound with a value from any matching triple.
		/// </summary>
		public TemplateElementCollection? Subject;
		public TemplateElementCollection? Predicate;
		public TemplateElementCollection? Object;

		internal string? subj;
		internal string? pred;
		internal string? obj;

		/// <summary>True if a triple must match (for a <c>q</c> tag); false if no triple must match (for a <c>notq</c> tag).</summary>
		public bool Affirm;

		public Clause(TemplateElementCollection? subj, TemplateElementCollection? pred, TemplateElementCollection? obj, bool affirm) {
			this.Subject = subj;
			this.Predicate = pred;
			this.Object = obj;
			this.Affirm = affirm;
		}

		internal void Evaluate(RequestProcess process) {
			this.subj = this.Subject?.Evaluate(process);
			this.pred = this.Predicate?.Evaluate(process);
			this.obj = this.Object?.Evaluate(process);
		}

		public Clause Clone() => new Clause(this.Subject, this.Predicate, this.Object, this.Affirm) { subj = this.subj, pred = this.pred, obj = this.obj };
		object ICloneable.Clone() => this.Clone();

		public static Clause FromXml(XmlNode node, bool affirm, AimlLoader loader) {
			// Search for XML attributes.
			XmlAttribute attribute;

			TemplateElementCollection? subj = null;
			TemplateElementCollection? pred = null;
			TemplateElementCollection? obj = null;

			attribute = node.Attributes["subj"];
			if (attribute != null) subj = new TemplateElementCollection(attribute.Value);
			attribute = node.Attributes["pred"];
			if (attribute != null) pred = new TemplateElementCollection(attribute.Value);
			attribute = node.Attributes["subj"];
			if (attribute != null) obj = new TemplateElementCollection(attribute.Value);

			foreach (XmlNode node2 in node.ChildNodes) {
				if (node2.NodeType == XmlNodeType.Element) {
					if (node2.Name.Equals("subj", StringComparison.InvariantCultureIgnoreCase))
						subj = TemplateElementCollection.FromXml(node2, loader);
					else if (node2.Name.Equals("pred", StringComparison.InvariantCultureIgnoreCase))
						pred = TemplateElementCollection.FromXml(node2, loader);
					else if (node2.Name.Equals("obj", StringComparison.InvariantCultureIgnoreCase))
						obj = TemplateElementCollection.FromXml(node2, loader);
				}
			}

			return new Clause(subj, pred, obj, affirm);
		}
	}
}
