using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using AngelAiml.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AngelAiml;
public delegate TemplateNode TemplateTagParser(XElement element, AimlLoader loader);
public delegate string? OobReplacementHandler(XElement element, Response response);
public delegate void OobHandler(XElement element, Response response);
public delegate IMediaElement MediaElementParser(XElement element, Response response);

public partial class AimlLoader(Bot bot) {
	private readonly Bot bot = bot;
	private readonly ILogger logger = bot.LoggerFactory?.CreateLogger<AimlLoader>() ?? NullLogger<AimlLoader>.Instance;

	internal static readonly Dictionary<string, TemplateTagParser> tags = new(StringComparer.InvariantCultureIgnoreCase) {
		// Elements that the reflection method can't handle
		{ "learn"    , (el, loader) => new Tags.Learn(el) },
		{ "learnf"   , (el, loader) => new Tags.LearnF(el) },
		{ "oob"      , Tags.Oob.FromXml },
		{ "select"   , Tags.Select.FromXml },

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
	internal static readonly Dictionary<string, (MediaElementType type, MediaElementParser parser, string[] childElements)> mediaElements = new(StringComparer.OrdinalIgnoreCase) {
		{ "a"        , (MediaElementType.Inline, Link.FromXml, Array.Empty<string>()) },
		{ "button"   , (MediaElementType.Block, Button.FromXml, new[] { "text", "postback", "url" }) },
		{ "br"       , (MediaElementType.Inline, LineBreak.FromXml, Array.Empty<string>()) },
		{ "break"    , (MediaElementType.Inline, LineBreak.FromXml, Array.Empty<string>()) },
		{ "card"     , (MediaElementType.Block, Card.FromXml, new[] { "image", "title", "subtitle", "button" }) },
		{ "carousel" , (MediaElementType.Block, Carousel.FromXml, new[] { "card" }) },
		{ "delay"    , (MediaElementType.Separator, Delay.FromXml, Array.Empty<string>()) },
		{ "image"    , (MediaElementType.Block, Image.FromXml, Array.Empty<string>()) },
		{ "img"      , (MediaElementType.Block, Image.FromXml, Array.Empty<string>()) },
		{ "hidden"   , (MediaElementType.Inline, Hidden.FromXml, Array.Empty<string>()) },
		{ "hyperlink", (MediaElementType.Inline, Link.FromXml, new[] { "text", "url" }) },
		{ "link"     , (MediaElementType.Inline, Link.FromXml, new[] { "text", "url" }) },
		{ "list"     , (MediaElementType.Inline, List.FromXml, new[] { "item", "li" }) },
		{ "ul"       , (MediaElementType.Inline, List.FromXml, new[] { "item", "li" }) },
		{ "ol"       , (MediaElementType.Inline, OrderedList.FromXml, new[] { "item", "li" }) },
		{ "olist"    , (MediaElementType.Inline, OrderedList.FromXml, new[] { "item", "li" }) },
		{ "reply"    , (MediaElementType.Block, Reply.FromXml, new[] { "text", "postback" }) },
		{ "split"    , (MediaElementType.Separator, Split.FromXml, Array.Empty<string>()) },
		{ "video"    , (MediaElementType.Block, Video.FromXml, Array.Empty<string>()) },
	};
	internal static readonly Dictionary<string, OobReplacementHandler> oobHandlers = new(StringComparer.OrdinalIgnoreCase);
	internal static readonly Dictionary<string, ISraixService> sraixServices = new(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<Type, TemplateElementBuilder> childElementBuilders = [];

	public static Version AimlVersion => new(2, 1);
	/// <summary>Whether this loader is loading a newer version of AIML or an <see cref="Tags.Oob"/> or rich media element.</summary>
	public bool ForwardCompatible { get; internal set; }

	static AimlLoader() {
		foreach (var type in typeof(TemplateNode).Assembly.GetTypes().Where(t => !t.IsAbstract && t != typeof(TemplateText) && typeof(TemplateNode).IsAssignableFrom(t))) {
			var elementName = type.Name.ToLowerInvariant();
			if (tags.ContainsKey(elementName)) continue;

			var builder = new TemplateElementBuilder(type);
			tags[elementName] = (el, loader) => (TemplateNode) builder.Parse(el, loader);
		}
		foreach (var e in mediaElements)
			tags.Add(e.Key, (node, loader) => Tags.Oob.FromXml(node, loader, e.Value.childElements));
	}

	private static TemplateNode SubtagHandler(XElement el, AimlLoader loader) => loader.ForwardCompatible ? Tags.Oob.FromXml(el, loader) : throw new AimlException("Element is not valid here.", el);

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
		if (!tags.TryAdd(elementName, (el, loader) => (TemplateNode) builder.Parse(el, loader)))
			throw new ArgumentException($"An AIML template element named <{elementName}> already exists.");
	}
	public static void AddCustomTag(string elementName, TemplateTagParser parser) => tags.Add(elementName, parser);

	public static void AddCustomMediaElement(string elementName, MediaElementType mediaElementType, MediaElementParser parser, params string[] childElementNames) {
		if (!tags.TryAdd(elementName, (node, loader) => Tags.Oob.FromXml(node, loader, childElementNames)))
			throw new ArgumentException("Cannot add a custom rich media element with the same name as an existing AIML template element.");
		if (!mediaElements.TryAdd(elementName, (mediaElementType, parser, childElementNames)))
			throw new ArgumentException($"A rich media element named <{elementName}> already exists.");
	}

	public static void AddCustomOobHandler(string elementName, OobHandler handler) => oobHandlers.Add(elementName, (el, r) => { handler(el, r); return null; });
	public static void AddCustomOobHandler(string elementName, OobReplacementHandler handler) => oobHandlers.Add(elementName, handler);

	public static void AddCustomSraixService(ISraixService service) {
		sraixServices.Add(service.GetType().Name, service);
		sraixServices.Add(service.GetType().FullName!, service);
	}

	public void LoadAimlFiles() => LoadAimlFiles(Path.Combine(bot.ConfigDirectory, bot.Config.AimlDirectory));
	public void LoadAimlFiles(string path) {
		if (!Directory.Exists(path)) throw new FileNotFoundException($"AIML directory not found: {path}", path);

		LogLoadingDirectory(logger, path);
		var files = Directory.GetFiles(path, "*.aiml", SearchOption.AllDirectories);

		foreach (var file in files)
			LoadAiml(file);

		GC.Collect();
		LogFinishedLoading(logger, bot.Size, files.Length);
	}

	public void LoadAiml(string path) {
		LogLoadingFile(logger, path);
		var document = XDocument.Load(path, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
		LoadAiml(document);
	}
	public void LoadAiml(XDocument document)
		=> LoadAiml(document.Root ?? throw new ArgumentException("The specified XML document is not a valid AIML document.", nameof(document)));
	public void LoadAiml(XElement aimlElement) {
		if (!aimlElement.Name.LocalName.Equals("aiml", StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException("The specified XML document is not a valid AIML document.", nameof(aimlElement));
		var versionString = aimlElement.Attribute("version")?.Value;
		ForwardCompatible = !Version.TryParse(versionString, out var version) || version > AimlVersion;
		LoadAimlInto(bot.Graphmaster, aimlElement);
	}
	public void LoadAimlInto(PatternNode target, XElement aimlElement) {
		foreach (var el in aimlElement.Nodes().OfType<XElement>()) {
			if (el.Name == "topic") {
				ProcessTopic(target, el);
			} else if (el.Name == "category") {
				ProcessCategory(target, el);
			}
		}

		bot.InvalidateVocabulary();
	}

	private void ProcessTopic(PatternNode target, XElement el) {
		if (el.Attribute("name") is not XAttribute attr)
			throw new AimlException("Missing 'name' attribute", el);
		var topicName = attr.Value;
		foreach (var el2 in el.Elements()) {
			if (el2.Name == "category")
				ProcessCategory(target, el2, topicName);
			else
				throw new AimlException($"Invalid child element <{el2.Name}>", el);
		}
	}

	public void ProcessCategory(PatternNode target, XElement el) => ProcessCategory(target, el, "*");
	public void ProcessCategory(PatternNode target, XElement el, string topicName) {
		XElement? patternNode = null, thatNode = null, topicNode = null, templateNode = null;

		foreach (var el2 in el.Elements()) {
			switch (el2.Name.LocalName.ToLowerInvariant()) {
				case "pattern": patternNode = el2; break;
				case "that": thatNode = el2; break;
				case "topic": topicNode = el2; break;
				case "template": templateNode = el2; break;
				default: throw new AimlException($"Invalid child element <{el2.Name}>", el);
			}
		}
		if (patternNode == null) throw new AimlException("Missing <pattern>", el);
		if (templateNode == null) throw new AimlException("Missing <template>", el);

		var templateContent = TemplateElementCollection.FromXml(templateNode, this);

		var path = GeneratePath(patternNode, thatNode, topicNode, topicName);
		target.AddChild(path, new(templateContent, templateNode.BaseUri, ((IXmlLineInfo) templateNode).LineNumber), out var existingTemplate);
		if (existingTemplate is null)
			bot.Size++;
		else
#if NET8_0_OR_GREATER
			LogDuplicateCategory(logger, string.Join(' ', path), existingTemplate.Uri, existingTemplate.LineNumber, templateNode.BaseUri, ((IXmlLineInfo) templateNode).LineNumber);
#else
			LogDuplicateCategory(logger, string.Join(" ", path), existingTemplate.Uri, existingTemplate.LineNumber, templateNode.BaseUri, ((IXmlLineInfo) templateNode).LineNumber);
#endif
	}

	public TemplateNode ParseElement(XElement el) {
		if (tags.TryGetValue(el.Name.LocalName, out var handler)) {
			try {
				return handler(el, this);
			} catch (ArgumentException ex) {
				throw new AimlException(ex.Message, el, ex);
			}
		}
		return ForwardCompatible || mediaElements.ContainsKey(el.Name.LocalName) ? Tags.Oob.FromXml(el, this)
			: throw new AimlException($"Not a valid AIML {AimlVersion} tag", el);
	}

	public T ParseChildElement<T>(XElement el) => (T) ParseChildElementInternal(el, typeof(T));
	internal object ParseChildElementInternal(XElement el, Type type) {
		if (!childElementBuilders.TryGetValue(type, out var builder))
			builder = childElementBuilders[type] = new(type);
		return builder.Parse(el, this);
	}

	private IEnumerable<PathToken> GeneratePath(XElement patternNode, XElement? thatNode, XElement? topicNode, string topic) {
		var patternTokens = ParsePattern(patternNode);
		var thatTokens = ParsePattern(thatNode);
		var topicTokens = topicNode != null ? ParsePattern(topicNode) :
			topic.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries).Select(s => new PathToken(s, false));

		foreach (var token in patternTokens) yield return token;
		yield return PathToken.ThatSeparator;
		foreach (var token in thatTokens) yield return token;
		yield return PathToken.TopicSeparator;
		foreach (var token in topicTokens) yield return token;
	}

	private IEnumerable<PathToken> ParsePattern(XElement? el) {
		if (el is null) {
			yield return PathToken.Star;
			yield break;
		}

		foreach (var node in el.Nodes()) {
			switch (node) {
				case XText textNode:
					foreach (var s in textNode.Value.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries))
						yield return new(s);
					break;
				case XElement el2:
					switch (el2.Name.LocalName.ToLowerInvariant()) {
						case "bot":
							var attr = el2.Attribute("name") ?? throw new AimlException("Missing 'name' attribute in <bot> tag", el);
							yield return new(bot.GetProperty(attr.Value));  // Bot properties don't change during the bot's uptime, so we process them here.
							break;
						case "set":
							yield return new(el2.Value, true);
							break;
						default:
							throw new AimlException($"Invalid child element <{el2.Name}>", el);
					}
					break;
			}
		}
	}

	#region Log templates

	[LoggerMessage(LogLevel.Information, "Loading AIML files from {Path}")]
	private static partial void LogLoadingDirectory(ILogger logger, string path);

	[LoggerMessage(LogLevel.Information, "Loading AIML file: {Path}")]
	private static partial void LogLoadingFile(ILogger logger, string path);

	[LoggerMessage(LogLevel.Information, "Finished loading the AIML files. {Size} categories in {FileCount} file(s) loaded.")]
	private static partial void LogFinishedLoading(ILogger logger, int size, int fileCount);

	[LoggerMessage(LogLevel.Warning, "Duplicate category: '{Path}', first defined in {FirstUri} line {FirstLine}, duplicated in {SecondUri} line {SecondLine}.")]
	private static partial void LogDuplicateCategory(ILogger logger, string path, string? firstUri, int? firstLine, string? secondUri, int? secondLine);

	#endregion
}
