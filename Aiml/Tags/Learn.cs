using System.Xml;

namespace Aiml.Tags;
/// <summary>Processes the content as AIML and adds it to the bot's brain, temporarily and for the current user only.</summary>
/// <remarks>
///		<para>Unlike other elements with content, the content of this element is not normally evaluated until the newly-added category is called.
///			However, the special child element <c>eval</c> is replaced with the result of evaluating its own content.</para>
///		<para>This element is defined by the AIML 2.0 specification.</para>
/// </remarks>
/// <seealso cref="AddTriple"/><seealso cref="LearnF"/><seealso cref="Set"/>
public sealed class Learn : TemplateNode {
	public XmlElement XmlElement { get; }

	public Learn(XmlElement el) {
		this.XmlElement = el;
		ValidateLearnElement(el);
	}

	internal static void ValidateLearnElement(XmlElement el) {
		var hasCategory = false;
		foreach (XmlNode childNode in el.ChildNodes) {
			if (childNode.NodeType is XmlNodeType.Text or XmlNodeType.CDATA)
				throw new AimlException($"Invalid node of type {childNode.NodeType} in <{el.Name}> element.");
			else if (childNode is XmlElement el2) {
				if (!el2.Name.Equals("category", StringComparison.OrdinalIgnoreCase))
					throw new AimlException($"Invalid element <{el2.Name}> in <{el.Name}> element.");
				hasCategory = true;
				bool hasPattern = false, hasThat = false, hasTopic = false, hasTemplate = false;
				foreach (XmlNode childNode2 in el2.ChildNodes) {
					if (childNode2.NodeType is XmlNodeType.Text or XmlNodeType.CDATA)
						throw new AimlException($"Invalid node of type {childNode.NodeType} in category of <{el.Name}> element.");
					else if (childNode2 is XmlElement el3) {
						switch (el3.Name.ToLowerInvariant()) {
							case "pattern":
								if (hasPattern) throw new AimlException($"Multiple <pattern> elements in category of <{el.Name}> element.");
								hasPattern = true;
								break;
							case "that":
								if (hasThat) throw new AimlException($"Multiple <that> elements in category of <{el.Name}> element.");
								hasThat = true;
								break;
							case "topic":
								if (hasTopic) throw new AimlException($"Multiple <topic> elements in category of <{el.Name}> element.");
								hasTopic = true;
								break;
							case "template":
								if (hasTemplate) throw new AimlException($"Multiple <template> elements in category of <{el.Name}> element.");
								hasTemplate = true;
								break;
							default:
								throw new AimlException($"Invalid element <{el3.Name}> in category of <{el.Name}> element.");
						}
					}
				}
				if (!hasPattern) throw new AimlException($"Missing <pattern> element in category of <{el.Name}> element.");
				if (!hasTemplate) throw new AimlException($"Missing <template> element in category of <{el.Name}> element.");
			}
		}
		if (!hasCategory) throw new AimlException($"Empty <{el.Name}> element.");
	}

	public override string Evaluate(RequestProcess process) {
		// Evaluate <eval> tags.
		var el = (XmlElement) this.XmlElement.Clone();
		ProcessXml(el, process);

		// Learn the result.
		process.Log(LogLevel.Diagnostic, $"In element <learn>: learning new category for {process.User.ID}: {el.OuterXml}");
		var loader = new AimlLoader(process.Bot);
		foreach (var el2 in el.ChildNodes.OfType<XmlElement>())
			loader.ProcessCategory(process.User.Graphmaster, el2, null);

		return string.Empty;
	}

	internal static void ProcessXml(XmlElement el, RequestProcess process) {
		for (var i = 0; i < el.ChildNodes.Count; ++i) {
			var childNode = el.ChildNodes[i];
			if (childNode is XmlElement childElement) {
				if (childNode.Name.Equals("eval", StringComparison.OrdinalIgnoreCase)) {
					var tags = TemplateElementCollection.FromXml(childElement, process.Bot.AimlLoader!);
					el.ReplaceChild(el.OwnerDocument.CreateTextNode(tags.Evaluate(process)), childElement);
				} else
					ProcessXml(childElement, process);
			}
		}
	}
}
