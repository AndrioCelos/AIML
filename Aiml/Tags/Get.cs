using System;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Returns the value of a predicate for the current user, local variable or tuple variable.
		/// </summary>
		/// <remarks>
		///     <para>This element has three forms:</para>
		///     <list type="bullet">
		///			<item>
		///				<term><c>&lt;get name='predicate'/&gt;</c></term>
		///				<description>Returns the value of the specified predicate for the current user, or <c>DefaultPredicate</c> if it is not bound.</description>
		///			</item>
		///			<item>
		///				<term><c>&lt;get var='variable'/&gt;</c></term>
		///				<description>Returns the value of a local variable for the containing category, or <c>DefaultPredicate</c> if it is not bound.</description>
		///			</item>
		///			<item>
		///				<term><c><![CDATA[<get var='?variable'><tuple>tuple</tuple></get>]]></c></term>
		///				<description>Returns the value of a tuple variable set by a <see cref="Select"/> element.</description>
		///			</item>
		///     </list>
		///     <para>This element has no content.</para>
		///     <para>This element is defined by the AIML 1.1 specification. Local variables are defined by the AIML 2.0 specification. Tuples are part of an extension to AIML derived from Program AB.</para>
		/// </remarks>
		/// <seealso cref="Select"/><seealso cref="Set"/>
		public sealed class Get : TemplateNode {
			public TemplateElementCollection Key { get; }
			public TemplateElementCollection TupleKey { get; }
			public bool LocalVar { get; }

			public Get(TemplateElementCollection key, bool local) : this(key, null, local) { }
			public Get(TemplateElementCollection key, TemplateElementCollection tuple, bool local) {
				this.Key = key;
				this.TupleKey = tuple;
				this.LocalVar = local;
			}

			public override string Evaluate(RequestProcess process) {
				string value = this.TupleKey?.Evaluate(process);
				if (!string.IsNullOrWhiteSpace(value)) {
					// Get a value from a tuple.
					int index; Tuple tuple;
					if (int.TryParse(value, out index) && index >= 0 && index < Tuple.Tuples.Count) {
						tuple = Tuple.Tuples[index];
						if (tuple.TryGetValue(this.Key.Evaluate(process), out value)) return value;
					}
					return process.Bot.Config.DefaultPredicate;
				}

				// Get a user variable or local variable.
				if (this.LocalVar) return process.GetVariable(this.Key.Evaluate(process));
				return process.User.GetPredicate(this.Key.Evaluate(process));
			}

			public static TemplateNode.Get FromXml(XmlNode node, AimlLoader loader) {
				// Search for XML attributes.
				XmlAttribute attribute;

				TemplateElementCollection key = null;
				bool localVar = false;
				TemplateElementCollection tupleKey = null;

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
				attribute = node.Attributes["tuple"];
				if (attribute != null) tupleKey = new TemplateElementCollection(attribute.Value);

				// Search for properties in elements.
				foreach (XmlNode node2 in node.ChildNodes) {
					if (node2.NodeType == XmlNodeType.Element) {
						if (node2.Name.Equals("name", StringComparison.InvariantCultureIgnoreCase)) {
							key = TemplateElementCollection.FromXml(node2, loader);
							localVar = false;
						} else if (node2.Name.Equals("var", StringComparison.InvariantCultureIgnoreCase)) {
							key = TemplateElementCollection.FromXml(node2, loader);
							localVar = true;
						} else if (node2.Name.Equals("tuple", StringComparison.InvariantCultureIgnoreCase)) {
							tupleKey = TemplateElementCollection.FromXml(node2, loader);
						}
					}
				}

				if (key == null) throw new AimlException("get tag is missing a name or var property.");

				return new Get(key, tupleKey, localVar);
			}
		}
	}
}
