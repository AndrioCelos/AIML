using System;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Returns the value of a predicate for the current user, local variable or tuple variable.
		/// </summary>
		/// <remarks>
		///     This element has three forms:
		///         <code><get name="predicate" /></code> or <code><get><name>predicate</name></get></code>
		///             Returns the value of a predicate for the current user, or a default value if none is found.
		///             This form is defined by the AIML 1.1 specification.
		///         <code><get var="variable" /></code> or <code><get><var>variable</var></get></code>
		///             Returns the value of a local variable. Local variables are specific to the category in which they are set.
		///             This form is defined by the AIML 2.0 specification.
		///         <code><get var="?variable" /><tuple>tuple</tuple></code> or <code><get><var>variable</var><tuple>tuple</tuple></get></code>
		///             Returns the value of a tuple variable. See <see cref="Select" /> for more information.
		///             This form is not part of the AIML specification, and was derived from Program AB.
		///     This element has no content.
		/// </remarks>
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
