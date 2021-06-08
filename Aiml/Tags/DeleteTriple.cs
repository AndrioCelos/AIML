using System;
using System.Linq;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Deletes a triple from the bot's triple database and returns an opaque identifier for the deletedtriple.
		/// </summary>
		/// <remarks>
		///     This element supports the following attributes, specifying the parts of the triple to be deleted:
		///         subject
		///         predicate
		///         object
		///     If the triple does not exist, the triple database is unchanged and DefaultTriple returned.
		///     This element has no content.
		///		This element is not part of the AIML specification, and was derived from Program AB.
		/// </remarks>
		/// <seealso cref="AddTriple"/><seealso cref="Select"/><seealso cref="Uniq"/>
		public sealed class DeleteTriple : TemplateNode {
			public TemplateElementCollection Subject { get; }
			public TemplateElementCollection Predicate { get; }
			public TemplateElementCollection Object { get; }

			public DeleteTriple(TemplateElementCollection subj, TemplateElementCollection pred, TemplateElementCollection obj) {
				this.Subject = subj;
				this.Predicate = pred;
				this.Object = obj;
			}

			public override string Evaluate(RequestProcess process) {
				var clause = new Clause(this.Subject, this.Predicate, this.Object, true);
				clause.Evaluate(process);

				if (string.IsNullOrWhiteSpace(clause.subj) || string.IsNullOrWhiteSpace(clause.pred) || string.IsNullOrWhiteSpace(clause.obj)) {
					process.Log(LogLevel.Diagnostic, $"In element <deletetriple>: Could not delete triple with missing elements.  Subject: {clause.subj}  Predicate: {clause.pred}  Object: {clause.obj}");
					return process.Bot.Config.DefaultTriple;
				}

				var triples = process.Bot.Triples.Match(clause);
				if (triples.Count == 0) {
					process.Log(LogLevel.Diagnostic, $"In element <deletetriple>: No such triple exists.  Subject: {clause.subj}  Predicate: {clause.pred}  Object: {clause.obj}");
					return process.Bot.Config.DefaultTriple;
				}

				var index = triples.Single();
				process.Bot.Triples.Remove(index);
				process.Log(LogLevel.Diagnostic, $"In element <deletetriple>: Deleted the triple with key {index}.  Subject: {clause.subj}  Predicate: {clause.pred}  Object: {clause.obj}");
				return index.ToString();
			}

			public static DeleteTriple FromXml(XmlNode node, AimlLoader loader) {
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

				return new DeleteTriple(subj, pred, obj);
			}
		}
	}
}
