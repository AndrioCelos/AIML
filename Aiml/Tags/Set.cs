using System;
using System.Collections.Generic;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Sets the value of a predicate for the current user or a local variable to the content.
		/// </summary>
		/// <remarks>
		///     This element has two forms:
		///         <code><set name="predicate" /></code> or <code><set><name>predicate</name></set></code>
		///             Sets the value of a predicate for the current user.
		///             This form is defined by the AIML 1.1 specification.
		///         <code><set var="variable" /></code> or <code><set><var>variable</var></set></code>
		///             Sets the value of a local variable. Local variables are specific to the category in which they are set.
		///             This form is defined by the AIML 2.0 specification.
		/// </remarks>
		public sealed class Set : RecursiveTemplateTag {
			public TemplateElementCollection Key { get; private set; }
			public bool LocalVar { get; private set; }

			public Set(TemplateElementCollection key, bool local, TemplateElementCollection children) : base(children) {
				this.Key = key;
				this.LocalVar = local;
			}

			public override string Evaluate(RequestProcess process) {
				string key = this.Key.Evaluate(process);
				string value = (this.Children?.Evaluate(process) ?? "").Trim();

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
				bool localVar = false;
				List<TemplateNode> children = new List<TemplateNode>();

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
					} else if (node2.NodeType == XmlNodeType.Text || node2.NodeType == XmlNodeType.SignificantWhitespace) {
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

				if (key == null) throw new AimlException("set tag is missing a name or var property.");

				return new Set(key, localVar, new TemplateElementCollection(children.ToArray()));
			}
		}
	}
}
