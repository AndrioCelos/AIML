using System.Reflection;
using System.Xml;
#if !NET6_0_OR_GREATER
using NullabilityInfoContext = Nullability.NullabilityInfoContextEx;
using NullabilityState = Nullability.NullabilityStateEx;
#endif

namespace Aiml;
internal class TemplateElementBuilder {
	private readonly ConstructorInfo constructor;
	private readonly AimlParameterData[] parameterData;
	private readonly int? contentParamIndex;

	public TemplateElementBuilder(Type type) {
		// If there is a constructor with the appropriate attribute, use that; otherwise use the first constructor, which will be the primary constructor if the type has one.
		var constructors = type.GetConstructors();
		var constructor = constructors.FirstOrDefault(c => c.GetCustomAttribute<AimlLoaderContructorAttribute>() is not null) ?? constructors[0];
		this.constructor = constructor;

		var nullabilityInfoContext = new NullabilityInfoContext();

		// Analyze the constructor parameters.
		var parameters = constructor.GetParameters();
		this.parameterData = new AimlParameterData[parameters.Length];
		for (var i = 0; i < parameters.Length; i++) {
			var param = parameters[i];
			if (param.ParameterType == typeof(TemplateElementCollection)) {
				// Either an attribute parameter or the children parameter.
				if (param.Name == "children") {
					this.parameterData[i] = new(ParameterType.Children, null, false, null);
					this.contentParamIndex = i;
				} else
					this.parameterData[i] = new(ParameterType.Attribute, param.Name, nullabilityInfoContext.Create(param).WriteState == NullabilityState.Nullable, null);
			} else if (param.ParameterType == typeof(XmlAttributeCollection)) {
				this.parameterData[i] = new(ParameterType.XmlAttributeCollection, null, false, null);
			} else if (param.ParameterType == typeof(XmlElement)) {
				this.parameterData[i] = new(ParameterType.XmlElement, null, false, null);
			} else if (param.ParameterType.IsArray && param.ParameterType.GetArrayRank() == 1 && param.ParameterType.GetElementType() is Type elementType && typeof(TemplateNode).IsAssignableFrom(elementType)) {
				// A special element parameter (for <li> elements).
				this.parameterData[i] = new(ParameterType.SpecialElement, elementType.Name, false, elementType);
			} else
				throw new ArgumentException($"Invalid parameter type: {param.ParameterType}");
		}
	}

	public object Parse(XmlElement el, AimlLoader loader) {
		var values = new object?[this.parameterData.Length];
		var children = new List<object>[this.parameterData.Length];
		for (var i = 0; i < children.Length; i++) children[i] = new();
		var content = this.contentParamIndex is not null ? children[this.contentParamIndex.Value] : null;

		// Populate attribute parameters from XML attributes.
		foreach (XmlAttribute attr in el.Attributes) {
			var i = Array.FindIndex(this.parameterData, p => p.Type == ParameterType.Attribute && p.Name!.Equals(attr.Name, StringComparison.OrdinalIgnoreCase));
			if (i >= 0)
				values[i] = new TemplateElementCollection(attr.Value);
			else if (!parameterData.Any(p => p.Type == ParameterType.XmlAttributeCollection))
				throw new AimlException($"Unknown attribute '{attr.Name}' in <{el.Name}> element");
		}
		// Populate parameters from XML child nodes.
		foreach (XmlNode childNode in el.ChildNodes) {
			switch (childNode.NodeType) {
				case XmlNodeType.Whitespace:
					content?.Add(TemplateText.Space);
					break;
				case XmlNodeType.SignificantWhitespace:
					content?.Add(new TemplateText(childNode.InnerText));
					break;
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
					if (content is null) throw new AimlException($"<{el.Name}> element cannot have content.");
					content.Add(new TemplateText(childNode.InnerText));
					break;
				default:
					if (childNode is XmlElement childElement) {
						var i = Array.FindIndex(this.parameterData, p => p.Name is not null && p.Name.Equals(childElement.Name, StringComparison.OrdinalIgnoreCase));
						if (i >= 0) {
							if (this.parameterData[i].Type == ParameterType.SpecialElement)
								children[i].Add(loader.ParseChildElementInternal(childElement, this.parameterData[i].ChildType!));
							else
								values[i] = values[i] is null
									? TemplateElementCollection.FromXml(childElement, loader)
									: throw new AimlException($"<{el.Name}> element '{this.parameterData[i].Name}' attribute provided multiple times.");
						}
						else if (content is null)
							throw new AimlException($"<{el.Name}> element cannot have content.");
						else
							content.Add(loader.ParseElement(childElement));
					}
					break;
			}
		}

		for (var i = 0; i < values.Length; i++) {
			var param = this.parameterData[i];
			switch (param.Type) {
				case ParameterType.Children:
					values[i] = new TemplateElementCollection(children[i].Cast<TemplateNode>());
					break;
				case ParameterType.Attribute:
					if (values[i] is null && !param.IsOptional)
						throw new AimlException($"Missing required attribute '{param.Name}' in <{el.Name}> element");
					break;
				case ParameterType.SpecialElement:
					var array = Array.CreateInstance(param.ChildType!, children[i].Count);
					for (var j = 0; j < array.Length; j++)
						array.SetValue(children[i][j], j);
					values[i] = array;
					break;
				case ParameterType.XmlElement:
					values[i] = el;
					break;
				case ParameterType.XmlAttributeCollection:
					values[i] = el.Attributes;
					break;
			}
		}
		return constructor.Invoke(values);
	}

	private record struct AimlParameterData(ParameterType Type, string? Name, bool IsOptional, Type? ChildType);
	private enum ParameterType {
		Children,
		Attribute,
		SpecialElement,
		XmlElement,
		XmlAttributeCollection
	}
}
