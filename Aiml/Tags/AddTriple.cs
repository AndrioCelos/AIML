using System;
using System.Linq;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		public sealed class AddTriple : TemplateNode {
			public TemplateElementCollection Subject { get; }
			public TemplateElementCollection Predicate { get; }
			public TemplateElementCollection Object { get; }

			public AddTriple(TemplateElementCollection subj, TemplateElementCollection pred, TemplateElementCollection obj) {
				this.Subject = subj;
				this.Predicate = pred;
				this.Object = obj;
			}

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
				int key = process.Bot.Triples.Add(clause.subj, clause.pred, clause.obj);
				process.Log(LogLevel.Diagnostic, $"In element <addtriple>: Added a new triple with key {key}.  Subject: {clause.subj}  Predicate: {clause.pred}  Object: {clause.obj}");
				return key.ToString();
			}

			public static TemplateNode.AddTriple FromXml(XmlNode node, AimlLoader loader) {
				// Search for XML attributes.
				XmlAttribute attribute;

				TemplateElementCollection? subj = null;
				TemplateElementCollection? pred = null;
				TemplateElementCollection? obj = null;

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

				return new AddTriple(subj, pred, obj);
			}
		}
	}
}
