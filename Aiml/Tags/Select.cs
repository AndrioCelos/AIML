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
///		<para>This element is part of an extension to AIML derived from Program AB and Program Y.</para>
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
		if (clauses.Length == 0) throw new ArgumentException("A <select> element must contain at least one clause.", nameof(clauses));
		this.Variables = variables;
		this.Clauses = clauses;
	}

	public override string Evaluate(RequestProcess process) {
		var resolvedClauses = (from c in this.Clauses select c.Evaluate(process)).ToList();
		var visibleVars = this.Variables != null
			? this.Variables.Evaluate(process).Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries)
			: Array.Empty<string>();

		// Begin a depth-first search for matching tuples.
		var tuples = SelectFromRemainingClauses(process, null, resolvedClauses, 0);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
		return string.Join(' ', (from t in tuples select t.Encode(visibleVars)).Distinct());
#else
		return string.Join(" ", (from t in tuples select t.Encode(visibleVars)).Distinct());
#endif
	}

	private static IEnumerable<Tuple?> SelectFromRemainingClauses(RequestProcess process, Tuple? partial, IReadOnlyList<(string subj, string pred, string obj, bool affirm)> resolvedClauses, int startIndex) {
		if (startIndex >= resolvedClauses.Count) {
			yield return partial;
			yield break;
		}
		foreach (var tuple in SelectFromClause(process, partial, resolvedClauses, startIndex)) {
			foreach (var tuple2 in SelectFromRemainingClauses(process, tuple, resolvedClauses, startIndex + 1))
				yield return tuple2;
		}
	}

	private static IEnumerable<Tuple?> SelectFromClause(RequestProcess process, Tuple? partial, IReadOnlyList<(string subj, string pred, string obj, bool affirm)> resolvedClauses, int startIndex) {
		var (subj, pred, obj, affirm) = resolvedClauses[startIndex];

		// Fill in the clause from existing bindings from the tuple under consideration.
		if (partial is not null) {
			if (subj.IsClauseVariable() && partial[subj] is string s1) subj = s1;
			if (pred.IsClauseVariable() && partial[pred] is string s2) pred = s2;
			if (obj.IsClauseVariable() && partial[obj] is string s3) obj = s3;
		}

		// Find triples that match.
		var triples = process.Bot.Triples.Match(subj.IsClauseVariable() ? null : subj, pred.IsClauseVariable() ? null : pred, obj.IsClauseVariable() ? null : obj);
		if (!affirm) {
			// If the <notq> assertion succeeds, there are no variable bindings to add, so just keep the tuple under consideration.
			if (!triples.Any()) yield return partial;
		} else {
			// Add possible variable bindings from each matching triple.
			foreach (var triple in triples) {
				var tuple = partial;

				if (subj.IsClauseVariable()) tuple = new(subj, triple.Subject, tuple);
				if (pred.IsClauseVariable()) tuple = new(pred, triple.Predicate, tuple);
				if (obj.IsClauseVariable()) tuple = new(obj, triple.Object, tuple);
				yield return tuple;
			}
		}
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
