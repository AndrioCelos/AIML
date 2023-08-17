using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Aiml.Media;

namespace Aiml;
public class Bot {
	public delegate string? OobHandler(XmlElement element);

	public static Version Version { get; } = typeof(Bot).Assembly.GetName().Version!;

	public string ConfigDirectory { get; set; }
	public Config Config { get; set; } = new();

	public DateTime StartedOn = DateTime.Now;
	public int Size { get; internal set; }
	public int Vocabulary { get; internal set; }
	public PatternNode Graphmaster { get; } = new(null, StringComparer.CurrentCultureIgnoreCase);
	internal readonly Random Random = new();

	public Dictionary<string, string> Properties => this.Config.BotProperties;
	public Dictionary<string, Set> Sets { get; } = new(StringComparer.CurrentCultureIgnoreCase);
	public Dictionary<string, Map> Maps { get; } = new(StringComparer.CurrentCultureIgnoreCase);
	public TripleCollection Triples = new();

	// TODO: private Dictionary<string, TagHandler> CustomTags = new(StringComparer.OrdinalIgnoreCase);
	public Dictionary<string, OobHandler> OobHandlers { get; } = new(StringComparer.OrdinalIgnoreCase);
	public Dictionary<string, ISraixService> SraixServices { get; } = new(StringComparer.OrdinalIgnoreCase);
	public Dictionary<string, (MediaElementType type, Func<Bot, XmlElement, IMediaElement> reviver)> MediaElements { get; } = new(StringComparer.OrdinalIgnoreCase) {
		{ "button", (MediaElementType.Block, Button.FromXml) },
		{ "br", (MediaElementType.Inline, LineBreak.FromXml) },
		{ "break", (MediaElementType.Inline, LineBreak.FromXml) },
		{ "card", (MediaElementType.Block, Card.FromXml) },
		{ "carousel", (MediaElementType.Block, Carousel.FromXml) },
		{ "delay", (MediaElementType.Separator, Delay.FromXml) },
		{ "image", (MediaElementType.Block, Image.FromXml) },
		{ "img", (MediaElementType.Block, Image.FromXml) },
		{ "hyperlink", (MediaElementType.Inline, Link.FromXml) },
		{ "link", (MediaElementType.Inline, Link.FromXml) },
		{ "list", (MediaElementType.Inline, List.FromXml) },
		{ "ul", (MediaElementType.Inline, List.FromXml) },
		{ "ol", (MediaElementType.Inline, OrderedList.FromXml) },
		{ "olist", (MediaElementType.Inline, OrderedList.FromXml) },
		{ "reply", (MediaElementType.Block, Reply.FromXml) },
		{ "split", (MediaElementType.Separator, Split.FromXml) },
		{ "video", (MediaElementType.Block, Video.FromXml) },
	};

	public AimlLoader? AimlLoader { get; internal set; }

	public event EventHandler<GossipEventArgs>? Gossip;
	public event EventHandler<LogMessageEventArgs>? LogMessage;

	protected void OnGossip(GossipEventArgs e) => this.Gossip?.Invoke(this, e);
	protected void OnLogMessage(LogMessageEventArgs e) => this.LogMessage?.Invoke(this, e);

	public Bot() : this("config") { }
	public Bot(string configDirectory) {
		this.ConfigDirectory = configDirectory;

		// Add predefined sets and maps.
		var inflector = new Inflector(StringComparer.CurrentCultureIgnoreCase);
		this.Sets.Add("number", new Sets.NumberSet());
		this.Maps.Add("successor", new Maps.ArithmeticMap(1));
		this.Maps.Add("predecessor", new Maps.ArithmeticMap(-1));
		this.Maps.Add("singular", new Maps.SingularMap(inflector));
		this.Maps.Add("plural", new Maps.PluralMap(inflector));
	}

	public void LoadAIML() {
		var aIMLLoader = new AimlLoader(this);
		aIMLLoader.LoadAimlFiles();
	}
	public void LoadAIML(XmlDocument newAIML, string filename) {
		var aIMLLoader = new AimlLoader(this);
		aIMLLoader.LoadAIML(newAIML, filename);
	}

	public void LoadConfig() {
		this.Config = Config.FromFile(Path.Combine(this.ConfigDirectory, "config.json"));
		this.LoadConfig2();
	}

	public void LoadConfig2() {
		this.CheckDefaultProperties();

		var inflector = new Inflector(this.Config.StringComparer);
		this.Maps["singular"] = new Maps.SingularMap(inflector);
		this.Maps["plural"] = new Maps.PluralMap(inflector);

		this.Config.LoadPredicates(Path.Combine(this.ConfigDirectory, "botpredicates.json"));
		this.Config.LoadGender(Path.Combine(this.ConfigDirectory, "gender.json"));
		this.Config.LoadPerson(Path.Combine(this.ConfigDirectory, "person.json"));
		this.Config.LoadPerson2(Path.Combine(this.ConfigDirectory, "person2.json"));
		this.Config.LoadNormal(Path.Combine(this.ConfigDirectory, "normal.json"));
		this.Config.LoadDenormal(Path.Combine(this.ConfigDirectory, "denormal.json"));
		this.Config.LoadDefaultPredicates(Path.Combine(this.ConfigDirectory, "predicates.json"));

		this.Config.GenderSubstitutions.CompileRegex();
		this.Config.PersonSubstitutions.CompileRegex();
		this.Config.Person2Substitutions.CompileRegex();
		this.Config.NormalSubstitutions.CompileRegex();
		this.Config.DenormalSubstitutions.CompileRegex();

		if (Directory.Exists(Path.Combine(this.ConfigDirectory, this.Config.SetsDirectory)))
			this.LoadSets(Path.Combine(this.ConfigDirectory, this.Config.SetsDirectory));

		if (Directory.Exists(Path.Combine(this.ConfigDirectory, this.Config.MapsDirectory)))
			this.LoadMaps(Path.Combine(this.ConfigDirectory, this.Config.MapsDirectory));

		this.LoadTriples(Path.Combine(this.ConfigDirectory, "triples.txt"));
	}

	private void CheckDefaultProperties() {
		if (!this.Config.BotProperties.ContainsKey("version"))
			this.Config.BotProperties.Add("version", Version.ToString(2));
	}

	private void LoadSets(string directory) {
		// TODO: Implement remote sets and maps
		this.Log(LogLevel.Info, "Loading sets from " + directory + ".");

		foreach (var file in Directory.EnumerateFiles(directory, "*.txt")) {
			if (this.Sets.ContainsKey(Path.GetFileNameWithoutExtension(file))) {
				this.Log(LogLevel.Warning, "Duplicate set name '" + Path.GetFileNameWithoutExtension(file) + "'.");
				continue;
			}

			var set = new List<string>();

			using var reader = new StreamReader(file);
			var phraseBuilder = new StringBuilder();
			while (!reader.EndOfStream) {
				phraseBuilder.Clear();
				bool trailingBackslash = false, whitespace = false;

					while (true) {
						var c = reader.Read();
						switch (c) {
							case < 0 or '\r' or '\n':
								// End of stream or newline
								goto endOfPhrase;
							case '\\':
								c = reader.Read();
								if (c is < 0 or '\r' or '\n') {
									// A backslash at the end of a line indicates that the empty string should be included the set.
									// Empty lines are ignored.
									trailingBackslash = true;
								} else {
									if (whitespace) {
										if (phraseBuilder.Length > 0) phraseBuilder.Append(' ');
										whitespace = false;
									}
									phraseBuilder.Append((char) c);
								}
								break;
							case '#':
								// Comment
								do { c = (char) reader.Read(); } while (c is >= 0 and not '\r' and not '\n');
								goto endOfPhrase;
							default:
								// Reduce consecutive whitespace into a single space.
								// Defer appending the space until a non-whitespace character is read, so as to ignore trailing whitespace.
								if (char.IsWhiteSpace((char) c)) {
									whitespace = true;
								} else {
									if (whitespace) {
										if (phraseBuilder.Length > 0) phraseBuilder.Append(' ');
										whitespace = false;
									}
									phraseBuilder.Append((char) c);
								}
								break;
						}
					}
					endOfPhrase:
					var phrase = phraseBuilder.ToString();
					if (!trailingBackslash && string.IsNullOrWhiteSpace(phrase)) continue;
					set.Add(phrase);
				}

				if (set.Count == 1 && set[0].StartsWith("map:")) {
					this.Sets[Path.GetFileNameWithoutExtension(file)] = new Sets.MapSet(set[0][4..], this);
				} else {
					this.Sets[Path.GetFileNameWithoutExtension(file)] = new Sets.StringSet(set, this.Config.StringComparer);
					this.Vocabulary += set.Count;
				}
			}

		this.Log(LogLevel.Info, "Loaded " + this.Sets.Count + " set(s).");
	}

	private void LoadMaps(string directory) {
		this.Log(LogLevel.Info, "Loading maps from " + directory + ".");

		foreach (var file in Directory.EnumerateFiles(directory, "*.txt")) {
			if (this.Maps.ContainsKey(Path.GetFileNameWithoutExtension(file))) {
				this.Log(LogLevel.Warning, "Duplicate map name '" + Path.GetFileNameWithoutExtension(file) + "'.");
				continue;
			}

			var map = new Dictionary<string, string>(this.Config.StringComparer);

			var reader = new StreamReader(file);
			while (true) {
				var line = reader.ReadLine();
				if (line is null) break;

				// Remove comments.
				line = Regex.Replace(line, @"\\([\\#])|#.*", "$1");
				if (string.IsNullOrWhiteSpace(line)) continue;

				var pos = line.IndexOf(':');
				if (pos < 0)
					this.Log(LogLevel.Warning, "Map '" + Path.GetFileNameWithoutExtension(file) + "' contains a badly formatted line: " + line);
				else {
					var key = line[..pos].Trim();
					var value = line[(pos + 1)..].Trim();
					if (map.ContainsKey(key))
						this.Log(LogLevel.Warning, "Map '" + Path.GetFileNameWithoutExtension(file) + "' contains duplicate key '" + key + "'.");
					else
						map.Add(key, value);
				}
			}

			this.Maps[Path.GetFileNameWithoutExtension(file)] = new Maps.StringMap(map, this.Config.StringComparer);
		}

		this.Log(LogLevel.Info, "Loaded " + this.Maps.Count + " map(s).");
	}

	private void LoadTriples(string filePath) {
		if (!File.Exists(filePath)) {
			this.Log(LogLevel.Info, "Triples file (" + filePath + ") was not found.");
			return;
		}

		this.Log(LogLevel.Info, "Loading maps from " + filePath + ".");
		using (var reader = new StreamReader(filePath)) {
			while (!reader.EndOfStream) {
				var line = reader.ReadLine();
				if (string.IsNullOrWhiteSpace(line)) continue;
				var fields = line.Split(new[] { ':' }, 3);
				if (fields.Length != 3)
					this.Log(LogLevel.Warning, "triples.txt contains a badly formatted line: " + line);
				else
					this.Triples.Add(fields[0], fields[1], fields[2], out _);
			}
		}

		this.Log(LogLevel.Info, "Loaded " + this.Triples.Count + " triple(s).");
	}

	private bool logDirectoryCreated = false;
	public void Log(LogLevel level, string message) {
		if (level < this.Config.LogLevel) return;

		var e = new LogMessageEventArgs(level, message);
		this.OnLogMessage(e);
		if (e.Handled) return;

		if (!this.logDirectoryCreated) {
			Directory.CreateDirectory(Path.Combine(this.ConfigDirectory, this.Config.LogDirectory));
			this.logDirectoryCreated = true;
		};

		try {
			var writer = new StreamWriter(Path.Combine(this.ConfigDirectory, this.Config.LogDirectory, DateTime.Now.ToString("yyyyMMdd") + ".log"), true);
			writer.WriteLine(DateTime.Now.ToString("[HH:mm:ss]") + "\t[" + level + "]\t" + message);
			writer.Close();
		} catch (IOException) { }
	}

	internal void WriteGossip(RequestProcess process, string message) {
		var e = new GossipEventArgs(message);
		this.OnGossip(e);
		if (e.Handled) return;
		process.Log(LogLevel.Gossip, "Gossip from " + process.User.ID + ": " + message);
	}

	public Response Chat(Request request, bool trace) {
		this.Log(LogLevel.Chat, request.User.ID + ": " + request.Text);
		request.User.AddRequest(request);

		var response = this.ProcessRequest(request, trace, false, 0, out _);

		if (!this.Config.BotProperties.TryGetValue("name", out var botName)) botName = "Robot";
		this.Log(LogLevel.Chat, botName + ": " + response.ToString());

		response.ProcessOobElements();
		request.User.AddResponse(response);
		return response;
	}

	internal Response ProcessRequest(Request request, bool trace, bool useTests, int recursionDepth, out TimeSpan duration) {
		var stopwatch = Stopwatch.StartNew();
		var that = this.Normalize(request.User.GetThat());
		var topic = this.Normalize(request.User.Topic);

		// Respond to each sentence separately.
		var builder = new StringBuilder();
		foreach (var sentence in request.Sentences) {
			var process = new RequestProcess(sentence, recursionDepth, useTests);

			process.Log(LogLevel.Diagnostic, $"Normalized path: {sentence.Text} <THAT> {that} <TOPIC> {topic}");

			string output;
			try {
				var template = request.User.Graphmaster.Search(sentence, process, that, trace);
				if (template != null) {
					process.template = template;
					process.Log(LogLevel.Diagnostic, "Input matched user-specific category '" + process.Path + "'.");
				} else {
					template = this.Graphmaster.Search(sentence, process, that, trace);
					if (template != null) {
						process.template = template;
						process.Log(LogLevel.Diagnostic, "Input matched category '" + process.Path + "' in file '" + Path.GetFileName(template.FileName) + "'.");
					}
				}

				if (template != null) {
					output = template.Content.Evaluate(process);
				} else {
					process.Log(LogLevel.Warning, "No match for input '" + sentence.Text + "'.");
					output = this.Config.DefaultResponse;
				}
			} catch (TimeoutException) {
				output = this.Config.TimeoutMessage;
			} catch (RecursionLimitException) {
				output = this.Config.RecursionLimitMessage;
			} catch (LoopLimitException) {
				output = this.Config.LoopLimitMessage;
			}

			output = output.Trim();

			if (output.Length > 0) {
				if (builder.Length != 0) builder.Append(' ');
				builder.Append(output);
			}

			process.Finish();
		}

		duration = stopwatch.Elapsed;

		var response = new Response(request, builder.ToString());
		request.Response = response;
		return response;
	}

	public string[] SentenceSplit(string text, bool preserveMarks) {
		if (this.Config.Splitters.Length == 0) {
			var sentence = text.Trim();
			return sentence != "" ? new[] { text.Trim() } : Array.Empty<string>();
		}

		int sentenceStart = 0, searchFrom = 0;
		var sentences = new List<string>();

		while (true) {
			string sentence;
			var pos2 = text.IndexOfAny(this.Config.Splitters, searchFrom);
			if (pos2 < 0) {
				sentence = text[sentenceStart..].Trim();
				if (sentence != "") sentences.Add(sentence);
				break;
			}
			if (pos2 < text.Length - 1 && !char.IsWhiteSpace(text[pos2 + 1]) && text[pos2 + 1] != '<') {
				// The sentence splitter must not be immediately followed by anything other than whitespace or an XML tag.
				searchFrom = pos2 + 1;
				continue;
			}
			sentence = text.Substring(sentenceStart, pos2 - sentenceStart + (preserveMarks ? 1 : 0)).Trim();
			if (sentence != "") sentences.Add(sentence);
			sentenceStart = pos2 + 1;
			searchFrom = sentenceStart;
		}

		return sentences.ToArray();
	}

	public string GetProperty(string predicate) => this.Config.BotProperties.GetValueOrDefault(predicate, this.Config.DefaultPredicate);

	public string Normalize(string text) {
		text = this.Config.NormalSubstitutions.Apply(text);
		// Strip sentence delimiters from the end when normalising (from Pandorabots).
		for (var i = text.Length - 1; i >= 0; --i) {
			if (!this.Config.Splitters.Contains(text[i])) return text[..(i + 1)];
		}
		return text;
	}

	public string Denormalize(string text) => this.Config.DenormalSubstitutions.Apply(text);
}
