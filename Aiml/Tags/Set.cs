using System.Xml;

namespace Aiml;
public partial class TemplateNode {
	/// <summary>Sets the value of a predicate or a local variable to the content, and returns the content.</summary>
	/// <remarks>
	///		<para>This element has two forms:</para>
	///		<list type="bullet">
	///			<item>
	///				<term><c>&lt;set name='predicate'&gt;value&lt;/set&gt;</c></term>
	///				<description>Sets a predicate for the current user.</description>
	///			</item>
	///			<item>
	///				<term><c>&lt;set var='variable'&gt;value&lt;/set&gt;</c></term>
	///				<description>Sets a local variable for the containing category.</description>
	///			</item>
	///		</list>
	///		<para>This element is defined by the AIML 1.1 specification. Local variables are defined by the AIML 2.0 specification.</para>
	/// </remarks>
	/// <seealso cref="AddTriple"/><seealso cref="Get"/>
	public sealed class Set(TemplateElementCollection key, bool local, TemplateElementCollection children) : RecursiveTemplateTag(children) {
		public TemplateElementCollection Key { get; private set; } = key;
		public bool LocalVar { get; private set; } = local;

		public override string Evaluate(RequestProcess process) {
			var key = this.Key.Evaluate(process);
			var value = (this.Children?.Evaluate(process) ?? "").Trim();

			var dictionary = this.LocalVar ? process.Variables : process.User.Predicates;
			if (process.Bot.Config.UnbindPredicatesWithDefaultValue &&
				value == (this.LocalVar ? process.Bot.Config.DefaultPredicate : process.Bot.Config.GetDefaultPredicate(key))) {
				dictionary.Remove(key);
				process.Log(LogLevel.Diagnostic, "In element <set>: Unbound " + (this.LocalVar ? "local variable" : "predicate") + " '" + key + "' with default value '" + value + "'.");
			} else {
				dictionary[key] = value;
				process.Log(LogLevel.Diagnostic, "In element <set>: Set " + (this.LocalVar ? "local variable" : "predicate") + " '" + key + "' to '" + value + "'.");
			}

			return value;
		}

		public static Set FromXml(XmlNode node, AimlLoader loader) {
			// Search for XML attributes.
			XmlAttribute attribute;

			TemplateElementCollection key = null;
			var localVar = false;
			var children = new List<TemplateNode>();

			attribute = node.Attributes["name"];
			if (attribute != null) {
				key = new TemplateElementCollection(attribute.Value);
			} else {
				attribute = node.Attributes["var"];
				if (attribute != null) {
					key = new TemplateElementCollection(attribute.Value);
					localVar = true;
				}
			}

			// Search for properties in elements.
			foreach (XmlNode node2 in node.ChildNodes) {
				if (node2.NodeType == XmlNodeType.Whitespace) {
					children.Add(new TemplateText(" "));
				} else if (node2.NodeType is XmlNodeType.Text or XmlNodeType.SignificantWhitespace) {
					children.Add(new TemplateText(node2.InnerText));
				} else if (node2.NodeType == XmlNodeType.Element) {
					if (node2.Name.Equals("name", StringComparison.InvariantCultureIgnoreCase)) {
						key = TemplateElementCollection.FromXml(node2, loader);
						localVar = false;
					} else if (node2.Name.Equals("var", StringComparison.InvariantCultureIgnoreCase)) {
						key = TemplateElementCollection.FromXml(node2, loader);
						localVar = true;
					} else
						children.Add(loader.ParseElement(node2));
				}
			}

			return key != null
				? new Set(key, localVar, new TemplateElementCollection(children.ToArray()))
				: throw new AimlException("set tag is missing a name or var property.");
		}
	}
}
