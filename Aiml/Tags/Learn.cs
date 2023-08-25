using System.Xml.Linq;

namespace Aiml.Tags;
/// <summary>Processes the content as AIML and adds it to the bot's brain, temporarily and for the current user only.</summary>
/// <remarks>
///		<para>Unlike other elements with content, the content of this element is not normally evaluated until the newly-added category is called.
///			However, the special child element <c>eval</c> is replaced with the result of evaluating its own content.</para>
///		<para>This element is defined by the AIML 2.0 specification.</para>
/// </remarks>
/// <seealso cref="AddTriple"/><seealso cref="LearnF"/><seealso cref="Set"/>
public sealed class Learn : TemplateNode {
	public XElement Element { get; }

	public Learn(XElement el) {
		this.Element = el;
		ValidateLearnElement(el);
	}

	internal static void ValidateLearnElement(XElement el) {
		var hasCategory = false;
		foreach (var childNode in el.Nodes()) {
			switch (childNode) {
				case XText:
					throw new ArgumentException($"Invalid node of type {childNode.NodeType} in <{el.Name}> element.", nameof(el));
				case XElement el2:
					if (!el2.Name.LocalName.Equals("category", StringComparison.OrdinalIgnoreCase))
						throw new ArgumentException($"Invalid element <{el2.Name}> in <{el.Name}> element.", nameof(el));
					hasCategory = true;
					bool hasPattern = false, hasThat = false, hasTopic = false, hasTemplate = false;
					foreach (var childNode2 in el2.Nodes()) {
						switch (childNode2) {
							case XText:
								throw new ArgumentException($"Invalid node of type {childNode.NodeType} in category of <{el.Name}> element.", nameof(el));
							case XElement el3:
								switch (el3.Name.LocalName.ToLowerInvariant()) {
									case "pattern":
										if (hasPattern) throw new ArgumentException($"Multiple <pattern> elements in category of <{el.Name}> element.", nameof(el));
										hasPattern = true;
										break;
									case "that":
										if (hasThat) throw new ArgumentException($"Multiple <that> elements in category of <{el.Name}> element.", nameof(el));
										hasThat = true;
										break;
									case "topic":
										if (hasTopic) throw new ArgumentException($"Multiple <topic> elements in category of <{el.Name}> element.", nameof(el));
										hasTopic = true;
										break;
									case "template":
										if (hasTemplate) throw new ArgumentException($"Multiple <template> elements in category of <{el.Name}> element.", nameof(el));
										hasTemplate = true;
										break;
									default:
										throw new ArgumentException($"Invalid element <{el3.Name}> in category of <{el.Name}> element.", nameof(el));
								}
								break;
						}
					}
					if (!hasPattern) throw new ArgumentException($"Missing <pattern> element in category of <{el.Name}> element.", nameof(el));
					if (!hasTemplate) throw new ArgumentException($"Missing <template> element in category of <{el.Name}> element.", nameof(el));
					break;
			}
		}
		if (!hasCategory) throw new ArgumentException($"Empty <{el.Name}> element.", nameof(el));
	}

	public override string Evaluate(RequestProcess process) {
		// Evaluate <eval> tags.
		var el = new XElement(this.Element);
		ProcessXml(el, process);

		// Learn the result.
		process.Log(LogLevel.Diagnostic, $"In element <learn>: learning new category for {process.User.ID}");
		process.Bot.AimlLoader.ForwardCompatible = false;
		process.Bot.AimlLoader.LoadAimlInto(process.User.Graphmaster, el);

		return string.Empty;
	}

	internal static void ProcessXml(XElement el, RequestProcess process) {
		for (var childNode = el.FirstNode; childNode is not null; childNode = childNode.NextNode) {
			if (childNode is XElement childElement) {
				if (childElement.Name.LocalName.Equals("eval", StringComparison.OrdinalIgnoreCase)) {
					var tags = TemplateElementCollection.FromXml(childElement, process.Bot.AimlLoader!);
					var newElement = new XText(tags.Evaluate(process));
					childNode.ReplaceWith(newElement);
					childNode = newElement;
				} else
					ProcessXml(childElement, process);
			}
		}
	}
}
