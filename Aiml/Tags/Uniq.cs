using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Returns an element of a single triple matching a clause.
		/// </summary>
		/// <remarks>
		///     This element contains three properties: subj, pred and obj.
		///     Each refers to an element of a triple, and can contain a variable name starting with '?', or text:
		///         Text asserts that a triple element matches the text.
		///         A variable name indicates the element of the triple that is returned. Only the latest-occurring variable (object, then predicate, then subject) is returned.
		///     This element is not part of the AIML specification, and was derived from Program AB.
		/// </remarks>
		/// <example>
		///     Examples:
		///     <code>
		///         <uniq><subj>York</subj><pred>isa</pred><obj>?object</obj></uniq>
		///     </code>
		///     This example may return 'City'.
		///     <code>
		///         <uniq><subj>?id</subj><pred>hasFirstName</pred><obj>Alan</obj></uniq>
		///     </code>
		///     This example may return a bot-defined text identifying a person named Alan, such as '10099'. The triple may be defined by the <addtriple> element.
		/// </example>
		public sealed class Uniq : TemplateNode {
			public Clause Clause { get; }

			public Uniq(TemplateElementCollection subj, TemplateElementCollection pred, TemplateElementCollection obj) {
				this.Clause = new Clause(subj, pred, obj, true);
			}

			public override string Evaluate(RequestProcess process) {
				this.Clause.Evaluate(process);

				// Find triples that match.
				var triples = process.Bot.Triples.Match(this.Clause);
				if (triples.Count == 0) {
					process.Log(LogLevel.Diagnostic, $"In element <uniq>: No matching triple exists.  Subject: {this.Clause.subj}  Predicate: {this.Clause.pred}  Object: {this.Clause.obj}");
					return process.Bot.Config.DefaultTriple;
				} else if (triples.Count > 1)
					process.Log(LogLevel.Diagnostic, $"In element <uniq>: Found {triples.Count} matching triples.  Subject: {this.Clause.subj}  Predicate: {this.Clause.pred}  Object: {this.Clause.obj}");

				var tripleIndex = triples.First();
				var triple = process.Bot.Triples[tripleIndex];

				process.Log(LogLevel.Diagnostic, $"In element <uniq>: Found triple {tripleIndex}.  Subject: {triple.Subject}  Predicate: {triple.Predicate}  Object: {triple.Object}");

				// Get the result.
				if (this.Clause.obj.StartsWith("?")) return triple.Object;
				if (this.Clause.pred.StartsWith("?")) return triple.Predicate;
				if (this.Clause.subj.StartsWith("?")) return triple.Subject;
				process.Log(LogLevel.Warning, $"In element <uniq>: The clause contains no variables.  Subject: {this.Clause.subj}  Predicate: {this.Clause.pred}  Object: {this.Clause.obj}");
				return process.Bot.Config.DefaultTriple;
			}

			public static Uniq FromXml(XmlNode node, AimlLoader loader) {
				List<Clause> clauses = new List<Clause>();

				// Search for XML attributes.
				XmlAttribute attribute;

				TemplateElementCollection subj = null;
				TemplateElementCollection pred = null;
				TemplateElementCollection obj = null;

				attribute = node.Attributes["subj"];
				if (attribute != null) subj = new TemplateElementCollection(attribute.Value);
				attribute = node.Attributes["pred"];
				if (attribute != null) pred = new TemplateElementCollection(attribute.Value);
				attribute = node.Attributes["obj"];
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

				return new Uniq(subj, pred, obj);
			}
		}
	}
}
