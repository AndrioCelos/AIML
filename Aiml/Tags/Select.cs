using System.Xml;

namespace Aiml.Tags;
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
		var resolvedClauses = (from c in this.Clauses select c.Evaluate(process)).ToList();
		var visibleVars = this.Variables != null
			? this.Variables.Evaluate(process).Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries)
			: Array.Empty<string>();

		// Start with an empty tuple.
		var tuple = new Tuple(new HashSet<string>(visibleVars, process.Bot.Config.StringComparer));
		var tuples = this.SelectFromRemainingClauses(process, tuple, resolvedClauses, 0);
		process.Log(LogLevel.Diagnostic, $"In element <select>: Found {tuples.Count} matching {(tuples.Count == 1 ? "tuple" : "tuples")}.");
		return tuples.Count > 0
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
			? string.Join(' ', tuples.Select(t => t.Index))
#else
			? string.Join(" ", tuples.Select(t => t.Index))
#endif
			: process.Bot.Config.DefaultTriple;
	}

	private HashSet<Tuple> SelectFromRemainingClauses(RequestProcess process, Tuple partial, IReadOnlyList<(string subj, string pred, string obj, bool affirm)> resolvedClauses, int startIndex) {
		HashSet<Tuple> tuples;

		var (subj, pred, obj, affirm) = resolvedClauses[startIndex];

		// Fill in the clause with values from the tuple under consideration.
		if (subj.StartsWith("?") && partial.TryGetValue(subj, out var value)) subj = value;
		if (pred.StartsWith("?") && partial.TryGetValue(pred, out value)) pred = value;
		if (obj.StartsWith("?") && partial.TryGetValue(obj, out value)) obj = value;

		// Find triples that match.
		var triples = process.Bot.Triples.Match(subj, pred, obj);
		if (!affirm) {
			// If the notq assertion succeeds, we just add the tuple under consideration without filling in any variables.
			if (triples.Count != 0) return new HashSet<Tuple>();
			tuples = new() { partial };
		} else {
			// Add possible tuples from each matching triple.
			tuples = new HashSet<Tuple>();
			foreach (var tripleIndex in triples) {
				var tuple = new Tuple(partial);

				if (subj.StartsWith("?")) tuple.Add(subj, process.Bot.Triples[tripleIndex].Subject);
				if (pred.StartsWith("?")) tuple.Add(pred, process.Bot.Triples[tripleIndex].Predicate);
				if (obj.StartsWith("?")) tuple.Add(obj, process.Bot.Triples[tripleIndex].Object);

				tuples.Add(tuple);
			}
		}

		var nextClause = startIndex + 1;
		if (nextClause == this.Clauses.Length) return tuples;

		// Recurse into the remaining clauses for each possible tuple.
		var result = new HashSet<Tuple>();

		// TODO: This recursive strategy involving sets has a minor quirk.
		// For a query (q: a isA b, notq: b isA x), for subjects a that have more than one predicate isA,
		// the results depend on the order in which the triples are defined.
		foreach (var tuple in tuples) {
			result.UnionWith(this.SelectFromRemainingClauses(process, tuple, resolvedClauses, nextClause));
		}
		return result;
	}

	public static Select FromXml(XmlElement element, AimlLoader loader) {
		var clauses = new List<Clause>();

		var vars = element.Attributes["vars"] is XmlAttribute attribute ? new TemplateElementCollection(attribute.Value) : null;

		foreach (XmlNode node in element.ChildNodes) {
			if (node is XmlElement childElement) {
				switch (childElement.Name.ToLowerInvariant()) {
					case "vars":
						if (vars is not null) throw new AimlException("<select> element vars attribute provided multiple times.");
						vars = TemplateElementCollection.FromXml(childElement, loader);
						break;
					case "q":
					case "notq":
						clauses.Add(loader.ParseChildElementInternal<Clause>(childElement));
						break;
					default:
						throw new AimlException("<select> element cannot have content.");
				}
			} else if (node.NodeType is XmlNodeType.Text or XmlNodeType.CDATA)
				throw new AimlException("<select> element cannot have content.");
		}

		return new Select(vars ?? throw new AimlException("Missing required attribute vars in <select> element"), clauses.ToArray());
	}
}
