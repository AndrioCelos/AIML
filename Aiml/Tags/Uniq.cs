using System.Xml;

namespace Aiml;
public partial class TemplateNode {
	/// <summary><para>Returns an element of a single triple matching a clause.</para></summary>
	/// <remarks>
	///		<para>This element has the following attributes:</para>
	///		<list type="table">
	///			<item>
	///				<term><c>subj</c>, <c>pred</c>, <c>obj</c></term>
	///				<description>
	///					<para>May contain a variable starting with <c>?</c> or text.</para>
	///					<para>Text asserts that a triple element matches the text.</para>
	///					<para>Exactly one attribute should be a variable; the variable name is ignored and may be simply <c>?</c>. It indicates the element of the triple that is returned.</para>
	///				</description>
	///			</item>
	///		</list>
	///		<para>If no triple matches, <c>DefaultTriple</c> is returned. If more than one triple matches, a single arbitrarily-chosen match is used.</para>
	///     <para>This element has no other content.</para>
	///     <para>This element is not part of the AIML specification, and was derived from Program AB.</para>
	/// </remarks>
	/// <example>
	///		<para>Examples:</para>
	///		<code>
	///			<![CDATA[<uniq><subj>York</subj><pred>isa</pred><obj>?object</obj></uniq>]]>
	///		</code>
	///		<para>This example may return 'City'.</para>
	///		<code>
	///			<![CDATA[<uniq><subj>?id</subj><pred>hasFirstName</pred><obj>Alan</obj></uniq>]]>
	///		</code>
	///		<para>This example may return a bot-defined text identifying a person named Alan, such as '10099'. The triple may be defined by the <c>addtriple</c> element.</para>
	/// </example>
	/// <seealso cref="DeleteTriple"/><seealso cref="DeleteTriple"/><seealso cref="Select"/>
	public sealed class Uniq(TemplateElementCollection subj, TemplateElementCollection pred, TemplateElementCollection obj) : TemplateNode {
		public Clause Clause { get; } = new Clause(subj, pred, obj, true);

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
