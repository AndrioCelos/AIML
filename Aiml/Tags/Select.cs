using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Selects values for variables that fulfil query conditions involving triples, and returns a space-separated list of tuple keys.
		/// </summary>
		/// <remarks>
		///     A select tag contains a vars element and one or more clauses.
		///     q and notq elements form clauses.
		///     The search starts with an empty tuple (which contains no values). q clauses can add more possible tuples that match the query.
		///         vars
		///             Defines variables used in the query as 'visible'. Not used.
		///         q
		///             Includes tuples for which this clause matches a triple.
		///         notq
		///             Excludes tuples for which this clause matches a triple.
		///             If the first clause is a notq, and any triples match the clause, the result will automatically be empty as all tuples being considered are excluded.
		///     Each clause contains three children: subj, pred and obj.
		///     Each refers to an element of a triple, and can contain a variable name starting with '?', or text:
		///         Text asserts that a triple element matches the text.
		///         A variable set by a previous clause asserts that a triple element matches its value.
		///         An unbound variable makes no assertion, and is set to possible values that satisfy the clause. (All possible values are considered and compared with subsequent clauses in turn.)
		///     This element can only contain <vars>, <q> and <notq> elements as direct children.
		///     This element is not part of the AIML specification, and was derived from Program AB.
		/// </remarks>
		/// <example>
		///     Example:
		///     <code>
		///         <set var="tuples">
		///             <select>
		///                 <vars>?x</vars>
		///                 <q><subj>?x</subj><pred>hasSize</pred><obj>7</obj></q>
		///                 <q><subj>?x</subj><pred>lifeArea</pred><obj>Physical</obj></q>
		///             </select>
		///         </set>
		///         <condition var="tuples">
		///             <li value="NIL" />
		///             <li>
		///                 <think>
		///                     <set var="head"><first><get var="tuples"/></first></set>
		///                     <set var="tuples"><rest><get var="tuples"/></rest></set>
		///                 </think>
		///                 <get var="?x"><tuple><get var="head"/></tuple></get> <loop/>
		///             </li>
		///         </condition>
		///     </code>
		///     In this example, the select element returns a list of tuples that contain the names of subjects in the physical life area with (relative) size 7,
		///     and stores this list in a local variable.
		///     The condition element iterates through this list and outputs the actual subject names to the user.
		///     Note that the 'NIL' list item is the 'base case' which ends the loop when no more tuples remain.
		///     <code>
		///         <select>
		///             <vars>?x</vars>
		///             <q><subj>?x</subj><pred>hasSize</pred><obj>7</obj></q>
		///             <notq><subj>?x</subj><pred>isa</pred><obj>Person</obj></notq>
		///         </select>
		///     </code>
		///     This example returns a list of tuples containing subjects of size 7 that are not people, such as 'Door'.
		///     <code>
		///         <select>
		///             <vars>?x</vars>
		///             <q><subj>?x</subj><pred>fatherOf</pred><obj>?y</obj></q>
		///             <q><subj>?y</subj><pred>parentOf</pred><obj><star /></obj></q>
		///         </select>
		///     </code>
		///     This example may return a list of tuples containing names of grandfathers of a user-specified person.
		///     Note that the star element is only evaluated once each time the select element is evaluated.
		///     Of course, the people and relationships must be defined as triples for this to work.
		/// </example>
		public sealed class Select : TemplateNode {
			public TemplateElementCollection Variables { get; }
			public Clause[] Clauses { get; }

			public Select(TemplateElementCollection variables, Clause[] clauses) {
				if (clauses.Length == 0) throw new ArgumentException("A select tag must contain at least one clause.", "clauses");
				this.Variables = variables;
				this.Clauses = clauses;
			}

			public override string Evaluate(RequestProcess process) {
				// Evaluate the contents of clauses.
				foreach (var clause in this.Clauses) clause.Evaluate(process);

				string[] visibleVars;
				if (this.Variables == null) visibleVars = new string[0];
				else visibleVars = this.Variables.Evaluate(process).Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);

				// Start with an empty tuple.
				Tuple tuple = new Tuple(new HashSet<string>(visibleVars, process.Bot.Config.StringComparer));
				var tuples = this.SelectFromRemainingClauses(process, tuple, 0);
				process.Log(LogLevel.Diagnostic, $"In element <select>: Found {tuples.Count} matching {(tuples.Count == 1 ? "tuple" : "tuples")}.");
				if (tuples.Count == 0) return process.Bot.Config.DefaultTriple;
				return string.Join(" ", tuples.Select(t => t.Index));
			}

			private HashSet<Tuple> SelectFromRemainingClauses(RequestProcess process, Tuple partial, int startIndex) {
				HashSet<Tuple> tuples;
				HashSet<Tuple> result;

				Clause clause = this.Clauses[startIndex].Clone();

				// Fill in the clause with values from the tuple under consideration.
				string value;
				if (clause.subj.StartsWith("?") && partial.TryGetValue(clause.subj, out value)) clause.subj = value;
				if (clause.pred.StartsWith("?") && partial.TryGetValue(clause.pred, out value)) clause.pred = value;
				if (clause.obj.StartsWith("?") && partial.TryGetValue(clause.obj, out value)) clause.obj = value;

				// Find triples that match.
				var triples = process.Bot.Triples.Match(clause);
				if (!clause.Affirm) {
					// If the notq assertion succeeds, we just add the tuple under consideration without filling in any variables.
					if (triples.Count != 0) return new HashSet<Tuple>();
					tuples = new HashSet<Tuple>();
					tuples.Add(partial);
				} else {
					// Add possible tuples from each matching triple.
					tuples = new HashSet<Tuple>();
					foreach (var tripleIndex in triples) {
						var tuple = new Tuple(partial);

						if (clause.subj.StartsWith("?")) tuple.Add(clause.subj, process.Bot.Triples[tripleIndex].Subject);
						if (clause.pred.StartsWith("?")) tuple.Add(clause.pred, process.Bot.Triples[tripleIndex].Predicate);
						if (clause.obj.StartsWith("?")) tuple.Add(clause.obj, process.Bot.Triples[tripleIndex].Object);

						tuples.Add(tuple);
					}
				}

				var nextClause = startIndex + 1;
				if (nextClause == this.Clauses.Length) return tuples;

				// Recurse into the remaining clauses for each possible tuple.
				result = new HashSet<Tuple>();

				// TODO: This recursive strategy involving sets has a minor quirk.
				// For a query (q: a isA b, notq: b isA x), for subjects a that have more than one predicate isA,
				// the results depend on the order in which the triples are defined.
				foreach (var tuple in tuples) {
					result.UnionWith(this.SelectFromRemainingClauses(process, tuple, nextClause));
				}
				return result;
			}

			public static Select FromXml(XmlNode node, AimlLoader loader) {
				List<Clause> clauses = new List<Clause>();

				// Search for XML attributes.
				XmlAttribute attribute;

				TemplateElementCollection variables = null;

				attribute = node.Attributes["vars"];
				if (attribute != null) variables = new TemplateElementCollection(attribute.Value);

				foreach (XmlNode node2 in node.ChildNodes) {
					if (node2.NodeType == XmlNodeType.Element) {
						if (node2.Name.Equals("vars", StringComparison.InvariantCultureIgnoreCase))
							variables = TemplateElementCollection.FromXml(node2, loader);
						else if (node2.Name.Equals("q", StringComparison.InvariantCultureIgnoreCase))
							clauses.Add(Clause.FromXml(node2, true, loader));
						else if (node2.Name.Equals("notq", StringComparison.InvariantCultureIgnoreCase))
							clauses.Add(Clause.FromXml(node2, false, loader));
					}
				}

				return new Select(variables, clauses.ToArray());
			}
		}
	}
}
