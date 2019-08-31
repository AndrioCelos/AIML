using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Returns one of a choice of child elements depending on the results of matching a predicate against a pattern.
		/// </summary>
		/// <remarks>
		///     TODO: fill this in.
		///     This element is defined by the AIML 1.1 specification.
		/// </remarks>
		public sealed class Condition : TemplateNode {
			private readonly li[] items;
			public ReadOnlyCollection<li> Items { get; }

			public Condition(TemplateElementCollection key, bool localVar, TemplateElementCollection value, TemplateElementCollection children) : this(key, localVar, new li[] { new li(value, children) }) { }
			public Condition(TemplateElementCollection? key, bool localVar, li[] items) {
				if (items.Length == 0) throw new AimlException("Condition element must contain at least one item.");
				foreach (var item in items) {
					if (item.Key == null && item.Value != null) {
						item.Key = key;
						item.LocalVar = localVar;
					}
				}
				this.items = items;
				this.Items = new ReadOnlyCollection<li>(items);
			}
			public Condition(li[] items) : this(null, false, items) { }

			public li? Pick(RequestProcess process) {
				string value;

				foreach (var item in this.items) {
					string? key = item.Key?.Evaluate(process);
					string? checkValue = item.Value?.Evaluate(process);

					Dictionary<string, string> dictionary;
					if (item.LocalVar) dictionary = process.Variables;
					else dictionary = process.User.Predicates;

					if (key != null && checkValue != null) {
						if (checkValue == "*") {
							// '*' is a match if the predicate is bound at all.
							if (item.LocalVar) {
								if (process.Variables.TryGetValue(key, out value)) {
									process.Log(LogLevel.Diagnostic, $"In element <condition>: Local variable {key} matches *.");
									return item;
								}
							} else {
								if (process.User.Predicates.TryGetValue(key, out value)) {
									process.Log(LogLevel.Diagnostic, $"In element <condition>: Local variable {key} matches *.");
									return item;
								}
							}
						} else {
							if (item.LocalVar) {
								if (process.Bot.Config.StringComparer.Equals(process.GetVariable(key), checkValue)) {
									process.Log(LogLevel.Diagnostic, $"In element <condition>: Local variable {key} matches {checkValue}.");
									return item;
								}
							} else {
								if (process.Bot.Config.StringComparer.Equals(checkValue, process.User.GetPredicate(key))) {
									process.Log(LogLevel.Diagnostic, $"In element <condition>: {(item.LocalVar ? "Local variable" : "Predicate")} {key} matches {checkValue}.");
									return item;
								}
							}
						}
						// No match; keep looking.
						process.Log(LogLevel.Diagnostic, $"In element <condition>: {(item.LocalVar ? "Local variable" : "Predicate")} {key} does not match {checkValue}.");
					} else if (key == null && checkValue == null) {
						// Default case.
						return item;
					} else {
						process.Log(LogLevel.Warning, "In element <condition>: Missing name, var or value attribute in <li>.");
					}
				}

				return null;
			}

			public override string Evaluate(RequestProcess process) {
				StringBuilder builder = new StringBuilder();

				li item; int loops = 0;
				do {
					++loops;
					if (loops > process.Bot.Config.LoopLimit) {
						process.Log(LogLevel.Warning, "Loop limit exceeded. User: " + process.User.ID + "; path: \"" + process.Path + "\"");
						throw new LoopLimitException();
					}

					item = this.Pick(process);
					if (item == null) return string.Empty;
					builder.Append(item.Children?.Evaluate(process));
				} while (item.Children != null && item.Children.Loop);

				return builder.ToString();
			}

			public static Condition FromXml(XmlNode node, AimlLoader loader) {
				// Search for XML attributes.
				XmlAttribute attribute;

				TemplateElementCollection? name = null;
				TemplateElementCollection? value = null;
				bool localVar = false;
				List<TemplateNode> children = new List<TemplateNode>();
				List<li> items = new List<li>();

				attribute = node.Attributes["name"];
				if (attribute != null) name = new TemplateElementCollection(attribute.Value);
				attribute = node.Attributes["var"];
				if (attribute != null) {
					name = new TemplateElementCollection(attribute.Value);
					localVar = true;
				}
				attribute = node.Attributes["value"];
				if (attribute != null) value = new TemplateElementCollection(attribute.Value);

				// Search for properties in elements.
				foreach (XmlNode node2 in node.ChildNodes) {
					if (node2.NodeType == XmlNodeType.Whitespace) {
						children.Add(new TemplateText(" "));
					} else if (node2.NodeType == XmlNodeType.Text || node2.NodeType == XmlNodeType.SignificantWhitespace) {
						children.Add(new TemplateText(node2.InnerText));
					} else if (node2.NodeType == XmlNodeType.Element) {
							if (node2.Name.Equals("name", StringComparison.InvariantCultureIgnoreCase)) {
								name = TemplateElementCollection.FromXml(node2, loader);
								localVar = false;
							} else if (node2.Name.Equals("var", StringComparison.InvariantCultureIgnoreCase)) {
								name = TemplateElementCollection.FromXml(node2, loader);
								localVar = true;
							} else if (node2.Name.Equals("value", StringComparison.InvariantCultureIgnoreCase))
								value = TemplateElementCollection.FromXml(node2, loader);
							else if (node2.Name.Equals("li", StringComparison.InvariantCultureIgnoreCase))
								items.Add(li.Parse(node2, loader));
							else
								children.Add(loader.ParseElement(node2));
					}
				}

				if (items.Count == 0) {
					if (name == null || value == null) throw new AimlException("<condition> tag is missing attributes or <li> tags.");
					return new Condition(name, localVar, value, new TemplateElementCollection(children.ToArray()));
				}

				if (items.Any(i => i.Value == null)) {
					bool infiniteLoop = true;
					foreach (var item in items) {
						if (item.Children == null || !item.Children.Loop) {
							infiniteLoop = false;
							break;
						}
					}
					if (infiniteLoop) throw new AimlException("Infinite loop: every <li> has a loop (and there is a default <li>).");
				}

				return new Condition(name, localVar, items.ToArray());
			}

			public class li : RecursiveTemplateTag {
				public TemplateElementCollection? Key { get; internal set; }
				public TemplateElementCollection? Value { get; }
				public bool LocalVar { get; internal set; }

				public li(TemplateElementCollection key, bool localVar, TemplateElementCollection value, TemplateElementCollection children) : base(children) {
					this.Key = key;
					this.Value = value;
					this.LocalVar = localVar;
				}
				public li(TemplateElementCollection value, TemplateElementCollection children) : this(null, false, value, children) { }
				public li(TemplateElementCollection children) : this(null, false, null, children) { }

				public override string Evaluate(RequestProcess process) {
					return this.Children?.Evaluate(process) ?? "";
				}

				public static TemplateNode.Condition.li Parse(XmlNode node, AimlLoader loader) {
					// Search for XML attributes.
					XmlAttribute attribute;

					TemplateElementCollection name = null;
					TemplateElementCollection value = null;
					bool localVar = false;
					List<TemplateNode> children = new List<TemplateNode>();

					attribute = node.Attributes["name"];
					if (attribute != null) name = new TemplateElementCollection(attribute.Value);
					attribute = node.Attributes["var"];
					if (attribute != null) {
						name = new TemplateElementCollection(attribute.Value);
						localVar = true;
					}
					attribute = node.Attributes["value"];
					if (attribute != null) value = new TemplateElementCollection(attribute.Value);

					// Search for properties in elements.
					foreach (XmlNode node2 in node.ChildNodes) {
						if (node2.NodeType == XmlNodeType.Whitespace) {
							children.Add(new TemplateText(" "));
						} else if (node2.NodeType == XmlNodeType.Text || node2.NodeType == XmlNodeType.SignificantWhitespace) {
							children.Add(new TemplateText(node2.InnerText));
						} else if (node2.NodeType == XmlNodeType.Element) {
							if (node2.Name.Equals("name", StringComparison.InvariantCultureIgnoreCase)) {
								name = TemplateElementCollection.FromXml(node2, loader);
								localVar = false;
							} else if (node2.Name.Equals("var", StringComparison.InvariantCultureIgnoreCase)) {
								name = TemplateElementCollection.FromXml(node2, loader);
								localVar = true;
							} else if (node2.Name.Equals("value", StringComparison.InvariantCultureIgnoreCase))
								value = TemplateElementCollection.FromXml(node2, loader);
							else
								children.Add(loader.ParseElement(node2));
						}
					}

					return new li(name, localVar, value, new TemplateElementCollection(children.ToArray()));
				}

				public override string ToString() {
					return "<li>" + this.Children.ToString() + "</li>";
				}
			}

		}
	}
}
