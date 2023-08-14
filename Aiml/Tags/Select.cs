using System.Xml;

namespace Aiml;
public partial class TemplateNode {
	/// <summary>Selects tuples consisting of values for variables that fulfil a list of query conditions involving triples, and returns a space-separated list of tuple identifiers.</summary>
	/// <remarks>
	///		<para>This element has the following attribute:</para>
	///		<list type="table">
	///			<item>
	///				<term><c>vars</c></term>
	///				<description>a space-separated list of variables in the query to define as 'visible'. Results (tuples) are considered duplicate if all visible variables match.</description>
	///			</item>
	///		</list>
	///		<para>A select tag also contains one or more clauses in the form of <c>q</c> and <c>notq</c> elements.</para>
	///		<para>The search starts with an empty tuple (which contains no values). <c>q</c> clauses can add possible tuples that match the query.</para>
	///		<list type="table">
	///			<item>
	///				<term><c>q</c></term>
	///				<description>includes tuples for which this clause matches a triple.</description>
	///			</item>
	///			<item>
	///				<term><c>notq</c></term>
	///				<description>excludes tuples for which this clause matches a triple.</description>
	///			</item>
	///		</list>
	///		<para>Clauses have the following attributes:</para>
	///		<list type="table">
	///			<item>
	///				<term><c>subj</c>, <c>pred</c>, <c>obj</c></term>
	///				<description>
	///					<para>If the content starts with a <c>?</c>, it is considered a variable name.</para>
	///					<para>If the content is text, asserts that the triple element matches the text.</para>
	///					<para>If the content is a bound variable (by a previous clause), asserts that the triple element matches its value.</para>
	///				</description>
	///			</item>
	///		</list>
	///		<para>This element has no other content.</para>
	///		<para>This element is part of an extension to AIML derived from Program AB.</para>
	/// </remarks>
	/// <example>
	///		<para>Example:</para>
	///		<code><![CDATA[
	///			<set var='tuples'>
	///				<select>
	///					<vars>?x</vars>
	///					<q><subj>?x</subj><pred>hasSize</pred><obj>7</obj></q>
	///					<q><subj>?x</subj><pred>lifeArea</pred><obj>Physical</obj></q>
	///				</select>
	///			</set>
	///			<condition var='tuples'>
	///				<li value='nil' />
	///				<li>
	///					<think>
	///						<set var='head'><first><get var='tuples'/></first></set>
	///						<set var='tuples'><rest><get var='tuples'/></rest></set>
	///					</think>
	///					<get var='?x'><tuple><get var="head"/></tuple></get> <loop/>
	///				</li>
	///			</condition>
	///		]]></code>
	///		<para>In this example, the <see cref="Select"/> element returns a list of tuples that contain the names of subjects in the physical life area with size 7,
	///			and stores this list in a local variable.
	///			The <see cref="Condition"/> element iterates through this list and outputs the actual subject names to the user.</para>
	///		<para>Note that the 'nil' list item is the base case which ends the loop when no more tuples remain.</para>
	///		<code><![CDATA[
	///			<select>
	///				<vars>?x</vars>
	///				<q><subj>?x</subj><pred>hasSize</pred><obj>7</obj></q>
	///				<notq><subj>?x</subj><pred>isa</pred><obj>Person</obj></notq>
	///			</select>
	///		]]></code>
	///		<para>This example returns a list of tuples containing subjects of size 7 that are not people, such as 'Door'.</para>
	///		<code><![CDATA[
	///			<select>
	///				<vars>?x</vars>
	///				<q><subj>?x</subj><pred>fatherOf</pred><obj>?y</obj></q>
	///				<q><subj>?y</subj><pred>parentOf</pred><obj><star /></obj></q>
	///			</select>
	///		]]></code>
	///		<para>This example may return a list of tuples containing names of grandfathers of a user-specified person.
	///		Note that the star element is only evaluated once each time the select element is evaluated.</para>
	/// </example>
	/// <seealso cref="DeleteTriple"/><seealso cref="DeleteTriple"/><seealso cref="Get"/><seealso cref="Uniq"/>
	public sealed class Select : TemplateNode {
		public TemplateElementCollection Variables { get; }
		public Clause[] Clauses { get; }

		public Select(TemplateElementCollection variables, Clause[] clauses) {
			if (clauses.Length == 0) throw new ArgumentException("A select tag must contain at least one clause.", nameof(clauses));
			this.Variables = variables;
			this.Clauses = clauses;
		}

		public override string Evaluate(RequestProcess process) {
			// Evaluate the contents of clauses.
			foreach (var clause in this.Clauses) clause.Evaluate(process);

			var visibleVars = this.Variables != null
				? this.Variables.Evaluate(process).Split((char[]) null, StringSplitOptions.RemoveEmptyEntries)
				: Array.Empty<string>();

			// Start with an empty tuple.
			var tuple = new Tuple(new HashSet<string>(visibleVars, process.Bot.Config.StringComparer));
			var tuples = this.SelectFromRemainingClauses(process, tuple, 0);
			process.Log(LogLevel.Diagnostic, $"In element <select>: Found {tuples.Count} matching {(tuples.Count == 1 ? "tuple" : "tuples")}.");
			return tuples.Count > 0
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
				? string.Join(' ', tuples.Select(t => t.Index))
#else
				? string.Join(" ", tuples.Select(t => t.Index))
#endif
				: process.Bot.Config.DefaultTriple;
		}

		private HashSet<Tuple> SelectFromRemainingClauses(RequestProcess process, Tuple partial, int startIndex) {
			HashSet<Tuple> tuples;
			HashSet<Tuple> result;

			var clause = this.Clauses[startIndex].Clone();

			// Fill in the clause with values from the tuple under consideration.
			if (clause.subj.StartsWith("?") && partial.TryGetValue(clause.subj, out var value)) clause.subj = value;
			if (clause.pred.StartsWith("?") && partial.TryGetValue(clause.pred, out value)) clause.pred = value;
			if (clause.obj.StartsWith("?") && partial.TryGetValue(clause.obj, out value)) clause.obj = value;

			// Find triples that match.
			var triples = process.Bot.Triples.Match(clause);
			if (!clause.Affirm) {
				// If the notq assertion succeeds, we just add the tuple under consideration without filling in any variables.
				if (triples.Count != 0) return new HashSet<Tuple>();
				tuples = new() { partial };
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
			var clauses = new List<Clause>();

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
