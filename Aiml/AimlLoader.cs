using System.Reflection;
using System.Xml;
#if !NET6_0_OR_GREATER
using NullabilityInfoContext = Nullability.NullabilityInfoContextEx;
using NullabilityState = Nullability.NullabilityStateEx;
#endif

namespace Aiml;
public delegate TemplateNode ElementHandler(XmlElement element, AimlLoader loader);

public class AimlLoader {
	private readonly Bot bot;

	private static readonly Dictionary<string, ElementHandler> elementHandlers = new(StringComparer.InvariantCultureIgnoreCase) {
		// Elements that the reflection method can't handle
		{ "oob"     , (node, loader) => Tags.Oob.FromXml(node, loader) },
		{ "select"  , Tags.Select.FromXml },

		// AIML 2.1 draft rich media elements
		{ "button"  , (node, loader) => Tags.Oob.FromXml(node, loader, "text", "postback", "url") },
		{ "reply"   , (node, loader) => Tags.Oob.FromXml(node, loader, "text", "postback") },
		{ "link"    , (node, loader) => Tags.Oob.FromXml(node, loader, "text", "url") },
		{ "image"   , (node, loader) => Tags.Oob.FromXml(node, loader) },
		{ "video"   , (node, loader) => Tags.Oob.FromXml(node, loader) },
		{ "card"    , (node, loader) => Tags.Oob.FromXml(node, loader, "image", "title", "subtitle", "button") },
		{ "carousel", (node, loader) => Tags.Oob.FromXml(node, loader, "card") },
		{ "delay"   , (node, loader) => Tags.Oob.FromXml(node, loader) },
		{ "split"   , (node, loader) => Tags.Oob.FromXml(node, loader) },
		{ "list"    , (node, loader) => Tags.Oob.FromXml(node, loader, "item") },
		{ "olist"   , (node, loader) => Tags.Oob.FromXml(node, loader, "item") },

		// Invalid template-level elements
		{ "eval"    , SubtagHandler },
		{ "q"       , SubtagHandler },
		{ "notq"    , SubtagHandler },
		{ "vars"    , SubtagHandler },
		{ "subj"    , SubtagHandler },
		{ "pred"    , SubtagHandler },
		{ "obj"     , SubtagHandler },
		{ "text"    , SubtagHandler },
		{ "postback", SubtagHandler },
		{ "url"     , SubtagHandler },
		{ "title"   , SubtagHandler },
		{ "subtitle", SubtagHandler },
		{ "item"    , SubtagHandler }
	};
	private static readonly Dictionary<Type, TemplateElementBuilder> elementBuilders = new();
	public static Version AimlVersion => new(2, 1);
	/// <summary>Whether this loader is loading a newer version of AIML or an <see cref="Tags.Oob"/> element.</summary>
	public bool ForwardCompatible { get; internal set; }

	public AimlLoader(Bot bot) {
		this.bot = bot;
		this.bot.AimlLoader = this;
	}

	private static TemplateNode SubtagHandler(XmlElement el, AimlLoader loader) => loader.ForwardCompatible ? Tags.Oob.FromXml(el, loader) : throw new AimlException($"The <{el.Name}> tag is not valid here.");

	public void LoadAimlFiles() => this.LoadAimlFiles(Path.Combine(this.bot.ConfigDirectory, this.bot.Config.AimlDirectory));
	public void LoadAimlFiles(string path) {
		if (!Directory.Exists(path)) throw new FileNotFoundException("Path not found: " + path, path);

		this.bot.Log(LogLevel.Info, "Loading AIML files from " + path);
		var files = Directory.GetFiles(path, "*.aiml");

		foreach (var file in files)
			this.LoadAIML(file);

		GC.Collect();
		this.bot.Log(LogLevel.Info, "Finished loading the AIML files. " + Convert.ToString(this.bot.Size) + " categories in " + files.Length + (files.Length == 1 ? " file" : " files") + " processed.");
	}

	public void LoadAIML(string filename) {
		this.bot.Log(LogLevel.Info, "Processing AIML file: " + filename);
		var xmlDocument = new XmlDocument() { PreserveWhitespace = true };
		xmlDocument.Load(filename);
		this.LoadAIML(xmlDocument, filename);
	}

	public void LoadAIML(XmlDocument document, string filename) {
		if (document.DocumentElement is null || !document.DocumentElement.Name.Equals("aiml", StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException("The specified XML document is not a valid AIML document.", nameof(document));
		this.LoadAIML(this.bot.Graphmaster, document.DocumentElement, filename);
	}
	public void LoadAIML(PatternNode target, XmlElement document, string filename) {
		var versionString = document.GetAttribute("version");
		this.ForwardCompatible = !Version.TryParse(versionString, out var version) || version > AimlVersion;

		foreach (var el in document.ChildNodes.OfType<XmlElement>()) {
			if (el.Name == "topic") {
				this.ProcessTopic(target, el, filename);
			} else if (el.Name == "category") {
				this.ProcessCategory(target, el, filename);
			}
		}
	}

	private void ProcessTopic(PatternNode target, XmlElement el, string filename) {
		var topicName = "*";
		if (el.Attributes.Count == 1 & el.Attributes[0].Name == "name") {
			topicName = el.GetAttribute("name");
		}
		foreach (var el2 in el.ChildNodes.OfType<XmlElement>()) {
			if (el2.Name == "category")
				this.ProcessCategory(target, el2, topicName, filename);
		}
	}

	public void ProcessCategory(PatternNode target, XmlElement el, string? filename) => this.ProcessCategory(target, el, "*", filename);
	public void ProcessCategory(PatternNode target, XmlElement el, string topicName, string? filename) {
		XmlElement? patternNode = null, templateNode = null, thatNode = null, topicNode = null;

		foreach (var el2 in el.ChildNodes.OfType<XmlElement>()) {
			if (el2.Name.Equals("pattern", StringComparison.InvariantCultureIgnoreCase)) patternNode = el2;
			else if (el2.Name.Equals("template", StringComparison.InvariantCultureIgnoreCase)) templateNode = el2;
			else if (el2.Name.Equals("that", StringComparison.InvariantCultureIgnoreCase)) thatNode = el2;
			else if (el2.Name.Equals("topic", StringComparison.InvariantCultureIgnoreCase)) topicNode = el2;
		}
		if (patternNode == null) throw new AimlException($"Missing pattern element in a node found in {filename}.");
		if (templateNode == null) throw new AimlException($"Node missing a template, with pattern '{patternNode.InnerXml}' in file {filename}.");
		if (string.IsNullOrWhiteSpace(patternNode.InnerXml)) this.bot.Log(LogLevel.Warning, $"Attempted to load a new category with an empty pattern, in file {filename}.");

		// Parse the template.
		var templateContent = TemplateElementCollection.FromXml(templateNode, this);

		target.AddChild(this.GeneratePath(patternNode, thatNode, topicNode, topicName), new Template(this.bot, templateNode, templateContent, filename));
		this.bot.Size++;
	}

	public TemplateNode ParseElement(XmlElement el) {
		if (elementHandlers.TryGetValue(el.Name, out var handler))
			return handler(el, this);
		var type = typeof(TemplateNode).Assembly.GetType($"{nameof(Aiml)}.{nameof(Tags)}.{el.Name}", false, true);
		return type is not null ? (TemplateNode) this.ParseElementInternal(el, type)
			: this.bot.MediaElements.ContainsKey(el.Name) || this.ForwardCompatible ? Tags.Oob.FromXml(el, this)
			: throw new AimlException($"'{el.Name}' is not a valid AIML {AimlVersion} tag.");
	}
	public object ParseElement(XmlElement el, Type type) => type.Name.Equals(el.Name, StringComparison.OrdinalIgnoreCase)
		? this.ParseElementInternal(el, type)
		: throw new ArgumentException($"Element name <{el.Name}> does not match expected <{type.Name.ToLowerInvariant()}>.");
	internal T ParseElementInternal<T>(XmlElement el) => (T) this.ParseElementInternal(el, typeof(T));
	internal object ParseElementInternal(XmlElement el, Type type) {
		if (!elementBuilders.TryGetValue(type, out var builder))
			builder = elementBuilders[type] = new(type);
		return builder.Parse(el, this);
	}

	private IEnumerable<PathToken> GeneratePath(XmlElement patternNode, XmlElement? thatNode, XmlElement? topicNode, string topic) {
		var patternTokens = this.ParsePattern(patternNode);
		var thatTokens = this.ParsePattern(thatNode);
		var topicTokens = topicNode != null ? this.ParsePattern(topicNode) :
			topic.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries).Select(s => new PathToken(s, false));

		foreach (var token in patternTokens) yield return token;
		yield return PathToken.ThatSeparator;
		foreach (var token in thatTokens) yield return token;
		yield return PathToken.TopicSeparator;
		foreach (var token in topicTokens) yield return token;
	}

	private IList<PathToken> ParsePattern(XmlElement? el) {
		if (el is null) return new[] { new PathToken("*") };

		var tokens = new List<PathToken>();
		foreach (XmlNode node in el.ChildNodes) {
			if (node.NodeType == XmlNodeType.Text)
				tokens.AddRange(node.InnerText.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries).Select(s => new PathToken(s, false)));
			else if (node is XmlElement el2) {
				if (el2.Name.Equals("bot", StringComparison.InvariantCultureIgnoreCase))
					tokens.Add(new PathToken(this.bot.GetProperty(el2.GetAttribute("name")), false));  // Bot properties don't change during the bot's uptime, so we process them here.
				else if (el2.Name.Equals("set", StringComparison.InvariantCultureIgnoreCase))
					tokens.Add(new PathToken(el2.InnerText, true));
				else
					throw new AimlException("Unknown pattern tag: " + el2.Name);
			}
		}
		return tokens;
	}

	private class TemplateElementBuilder {
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
						this.parameterData[i] = new(ParameterType.Children, null, false, null, new());
						this.contentParamIndex = i;
					} else
						this.parameterData[i] = new(ParameterType.Attribute, param.Name, nullabilityInfoContext.Create(param).WriteState == NullabilityState.Nullable, null, new());
				} else if (param.ParameterType == typeof(XmlAttributeCollection)) {
					this.parameterData[i] = new(ParameterType.XmlAttributeCollection, null, false, null, null);
				} else if (param.ParameterType == typeof(XmlElement)) {
					this.parameterData[i] = new(ParameterType.XmlElement, null, false, null, null);
				} else if (param.ParameterType.IsArray && param.ParameterType.GetArrayRank() == 1 && param.ParameterType.GetElementType() is Type elementType && typeof(TemplateNode).IsAssignableFrom(elementType)) {
					// A special element parameter (for <li> elements).
					this.parameterData[i] = new(ParameterType.SpecialElement, elementType.Name, false, elementType, new());
				} else
					throw new ArgumentException($"Invalid parameter type: {param.ParameterType}");
			}
		}

		public object Parse(XmlElement el, AimlLoader loader) {
			var values = new object?[this.parameterData.Length];

			// Populate attribute parameters from XML attributes.
			foreach (XmlAttribute attr in el.Attributes) {
				var i = Array.FindIndex(this.parameterData, p => p.Type == ParameterType.Attribute && p.Name!.Equals(attr.Name, StringComparison.OrdinalIgnoreCase));
				if (i < 0)
					throw new AimlException($"Unknown attribute {attr.Name} in <{el.Name}> element");
				values[i] = new TemplateElementCollection(attr.Value);
			}
			// Populate parameters from XML child nodes.
			foreach (XmlNode childNode in el.ChildNodes) {
				switch (childNode.NodeType) {
					case XmlNodeType.Whitespace:
						if (this.contentParamIndex is null) break;
						this.parameterData[this.contentParamIndex.Value].Children!.Add(new TemplateText(" "));
						break;
					case XmlNodeType.SignificantWhitespace:
						if (this.contentParamIndex is null) break;
						this.parameterData[this.contentParamIndex.Value].Children!.Add(new TemplateText(childNode.InnerText));
						break;
					case XmlNodeType.Text:
					case XmlNodeType.CDATA:
						if (this.contentParamIndex is null) throw new AimlException($"<{el.Name}> element cannot have content.");
						this.parameterData[this.contentParamIndex.Value].Children!.Add(new TemplateText(childNode.InnerText));
						break;
					default:
						if (childNode is XmlElement childElement) {
							var i = Array.FindIndex(this.parameterData, p => p.Name is not null && p.Name.Equals(childElement.Name, StringComparison.OrdinalIgnoreCase));
							if (i >= 0) {
								if (this.parameterData[i].Type == ParameterType.SpecialElement)
									this.parameterData[i].Children!.Add(loader.ParseElementInternal(childElement, this.parameterData[i].ChildType!));
								else
									values[i] = values[i] is null
										? TemplateElementCollection.FromXml(childElement, loader)
										: throw new AimlException($"<{el.Name}> element {this.parameterData[i].Name} attribute provided multiple times.");
							}
							else if (this.contentParamIndex is null)
								throw new AimlException($"<{el.Name}> element cannot have content.");
							else
								this.parameterData[this.contentParamIndex.Value].Children!.Add(loader.ParseElement(childElement));
						}
						break;
				}
			}

			for (var i = 0; i < values.Length; i++) {
				var param = this.parameterData[i];
				switch (param.Type) {
					case ParameterType.Children:
						values[i] = new TemplateElementCollection(param.Children!.Cast<TemplateNode>());
						break;
					case ParameterType.Attribute:
						if (!param.IsOptional)
							throw new AimlException($"Missing required attribute {param.Name} in <{el.Name}> element");
						break;
					case ParameterType.SpecialElement:
						var array = Array.CreateInstance(param.ChildType!, param.Children!.Count);
						for (var j = 0; j < array.Length; j++)
							array.SetValue(param.Children[j], j);
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

		private record struct AimlParameterData(ParameterType Type, string? Name, bool IsOptional, Type? ChildType, List<object>? Children);
		private enum ParameterType {
			Children,
			Attribute,
			SpecialElement,
			XmlElement,
			XmlAttributeCollection
		}
	}
}
