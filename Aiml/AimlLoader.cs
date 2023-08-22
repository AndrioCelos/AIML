using Aiml.Media;
using System.Reflection;
using System.Xml;

namespace Aiml;
public delegate TemplateNode TemplateTagParser(XmlElement element, AimlLoader loader);
public delegate string? OobReplacementHandler(XmlElement element);
public delegate void OobHandler(XmlElement element);
public delegate IMediaElement MediaElementParser(XmlElement element);

public class AimlLoader(Bot bot) {
	private readonly Bot bot = bot;

	internal static readonly Dictionary<string, TemplateTagParser> tags = new(StringComparer.InvariantCultureIgnoreCase) {
		// Elements that the reflection method can't handle
		{ "oob"      , (node, loader) => Tags.Oob.FromXml(node, loader) },
		{ "select"   , Tags.Select.FromXml },

		// AIML 2.1 draft rich media elements
		{ "button"   , (node, loader) => Tags.Oob.FromXml(node, loader, "text", "postback", "url") },
		{ "br"       , (node, loader) => Tags.Oob.FromXml(node, loader) },
		{ "break"    , (node, loader) => Tags.Oob.FromXml(node, loader) },
		{ "card"     , (node, loader) => Tags.Oob.FromXml(node, loader, "image", "title", "subtitle", "button") },
		{ "carousel" , (node, loader) => Tags.Oob.FromXml(node, loader, "card") },
		{ "delay"    , (node, loader) => Tags.Oob.FromXml(node, loader) },
		{ "image"    , (node, loader) => Tags.Oob.FromXml(node, loader) },
		{ "img"      , (node, loader) => Tags.Oob.FromXml(node, loader) },
		{ "hyperlink", (node, loader) => Tags.Oob.FromXml(node, loader, "text", "url") },
		{ "link"     , (node, loader) => Tags.Oob.FromXml(node, loader, "text", "url") },
		{ "list"     , (node, loader) => Tags.Oob.FromXml(node, loader, "item") },
		{ "ul"       , (node, loader) => Tags.Oob.FromXml(node, loader, "item") },
		{ "ol"       , (node, loader) => Tags.Oob.FromXml(node, loader, "item") },
		{ "olist"    , (node, loader) => Tags.Oob.FromXml(node, loader, "item") },
		{ "reply"    , (node, loader) => Tags.Oob.FromXml(node, loader, "text", "postback") },
		{ "split"    , (node, loader) => Tags.Oob.FromXml(node, loader) },
		{ "video"    , (node, loader) => Tags.Oob.FromXml(node, loader) },

		// Invalid template-level elements
		{ "eval"     , SubtagHandler },
		{ "q"        , SubtagHandler },
		{ "notq"     , SubtagHandler },
		{ "vars"     , SubtagHandler },
		{ "subj"     , SubtagHandler },
		{ "pred"     , SubtagHandler },
		{ "obj"      , SubtagHandler },
		{ "text"     , SubtagHandler },
		{ "postback" , SubtagHandler },
		{ "url"      , SubtagHandler },
		{ "title"    , SubtagHandler },
		{ "subtitle" , SubtagHandler },
		{ "item"     , SubtagHandler }
	};
	internal static readonly Dictionary<string, (MediaElementType type, MediaElementParser parser)> mediaElements = new(StringComparer.OrdinalIgnoreCase) {
		{ "button"   , (MediaElementType.Block, Button.FromXml) },
		{ "br"       , (MediaElementType.Inline, LineBreak.FromXml) },
		{ "break"    , (MediaElementType.Inline, LineBreak.FromXml) },
		{ "card"     , (MediaElementType.Block, Card.FromXml) },
		{ "carousel" , (MediaElementType.Block, Carousel.FromXml) },
		{ "delay"    , (MediaElementType.Separator, Delay.FromXml) },
		{ "image"    , (MediaElementType.Block, Image.FromXml) },
		{ "img"      , (MediaElementType.Block, Image.FromXml) },
		{ "hyperlink", (MediaElementType.Inline, Link.FromXml) },
		{ "link"     , (MediaElementType.Inline, Link.FromXml) },
		{ "list"     , (MediaElementType.Inline, List.FromXml) },
		{ "ul"       , (MediaElementType.Inline, List.FromXml) },
		{ "ol"       , (MediaElementType.Inline, OrderedList.FromXml) },
		{ "olist"    , (MediaElementType.Inline, OrderedList.FromXml) },
		{ "reply"    , (MediaElementType.Block, Reply.FromXml) },
		{ "split"    , (MediaElementType.Separator, Split.FromXml) },
		{ "video"    , (MediaElementType.Block, Video.FromXml) },
	};
	internal static readonly Dictionary<string, OobReplacementHandler> oobHandlers = new(StringComparer.OrdinalIgnoreCase);
	internal static readonly Dictionary<string, ISraixService> sraixServices = new(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<Type, TemplateElementBuilder> childElementBuilders = new();

	public static Version AimlVersion => new(2, 1);
	/// <summary>Whether this loader is loading a newer version of AIML or an <see cref="Tags.Oob"/> element.</summary>
	public bool ForwardCompatible { get; internal set; }

	static AimlLoader() {
		foreach (var type in typeof(TemplateNode).Assembly.GetTypes().Where(t => !t.IsAbstract && t != typeof(TemplateText) && typeof(TemplateNode).IsAssignableFrom(t))) {
			var elementName = type.Name.ToLowerInvariant();
			if (tags.ContainsKey(elementName)) continue;

			var builder = new TemplateElementBuilder(type);
			tags[elementName] = (el, loader) => (TemplateNode) builder.Parse(el, loader);
		}
	}

	private static TemplateNode SubtagHandler(XmlElement el, AimlLoader loader) => loader.ForwardCompatible ? Tags.Oob.FromXml(el, loader) : throw new AimlException($"The <{el.Name}> tag is not valid here.");

	public static void AddExtension(IAimlExtension extension) => extension.Initialise();
#if NET5_0_OR_GREATER
	public static void AddExtensions(string path) {
		var assemblyName = AssemblyName.GetAssemblyName(path);
		var loadContext = new PluginLoadContext(Path.GetFullPath(path));
		var assembly = loadContext.LoadFromAssemblyName(assemblyName);
		var found = false;
		foreach (var type in assembly.GetExportedTypes()) {
			if (!type.IsAbstract && typeof(IAimlExtension).IsAssignableFrom(type)) {
				found = true;
				AddExtension((IAimlExtension) Activator.CreateInstance(type)!);
			}
		}
		if (!found) throw new ArgumentException($"No {nameof(IAimlExtension)} types found in the specified assembly: {path}");
	}
#endif

	public static void AddCustomTag(Type type) => AddCustomTag(type.Name.ToLowerInvariant(), type);
	public static void AddCustomTag(string elementName, Type type) {
		var builder = new TemplateElementBuilder(type);
		tags.Add(elementName, (el, loader) => (TemplateNode) builder.Parse(el, loader));
	}
	public static void AddCustomTag(string elementName, TemplateTagParser parser) => tags.Add(elementName, parser);

	public static void AddCustomMediaElement(string elementName, MediaElementType mediaElementType, MediaElementParser parser, params string[] childElementNames) {
		tags.Add(elementName, (node, loader) => Tags.Oob.FromXml(node, loader, childElementNames));
		mediaElements.Add(elementName, (mediaElementType, parser));
	}

	public static void AddCustomOobHandler(string elementName, OobHandler handler) => oobHandlers.Add(elementName, el => { handler(el); return null; });
	public static void AddCustomOobHandler(string elementName, OobReplacementHandler handler) => oobHandlers.Add(elementName, handler);

	public static void AddCustomSraixService(ISraixService service) {
		sraixServices.Add(service.GetType().Name, service);
		sraixServices.Add(service.GetType().FullName!, service);
	}

	public void LoadAimlFiles() => this.LoadAimlFiles(Path.Combine(this.bot.ConfigDirectory, this.bot.Config.AimlDirectory));
	public void LoadAimlFiles(string path) {
		if (!Directory.Exists(path)) throw new FileNotFoundException("Path not found: " + path, path);

		this.bot.Log(LogLevel.Info, "Loading AIML files from " + path);
		var files = Directory.GetFiles(path, "*.aiml", SearchOption.AllDirectories);

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
	public void LoadAIML(XmlDocument document) => this.LoadAIML(document, "*");
	public void LoadAIML(XmlDocument document, string filename) {
		if (document.DocumentElement is null || !document.DocumentElement.Name.Equals("aiml", StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException("The specified XML document is not a valid AIML document.", nameof(document));
		this.LoadAIML(this.bot.Graphmaster, document.DocumentElement, filename);
	}
	public void LoadAIML(PatternNode target, XmlElement document) => this.LoadAIML(target, document, "*");
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
			else
				throw new AimlException($"Invalid element in topic element: {el2.Name}");
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

	public TemplateNode ParseElement(XmlElement el)
		=> tags.TryGetValue(el.Name, out var handler) ? handler(el, this)
			: this.ForwardCompatible || mediaElements.ContainsKey(el.Name) ? Tags.Oob.FromXml(el, this)
			: throw new AimlException($"<{el.Name}> is not a valid AIML {AimlVersion} tag.");

	internal T ParseChildElementInternal<T>(XmlElement el) => (T) this.ParseChildElementInternal(el, typeof(T));
	internal object ParseChildElementInternal(XmlElement el, Type type) {
		if (!childElementBuilders.TryGetValue(type, out var builder))
			builder = childElementBuilders[type] = new(type);
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
}
