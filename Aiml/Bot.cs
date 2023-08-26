using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Aiml.Maps;
using Aiml.Sets;

namespace Aiml;
public class Bot {

	public static Version Version { get; } = typeof(Bot).Assembly.GetName().Version!;

	public string ConfigDirectory { get; set; }
	public Config Config { get; set; } = new();

	public int Size { get; internal set; }
	public int Vocabulary {
		get {
			if (this.vocabulary is not null) return this.vocabulary.Value;
			var vocabulary = this.CalculateVocabulary();
			this.vocabulary = vocabulary;
			return vocabulary;
		}
	}
	public PatternNode Graphmaster { get; } = new(null, StringComparer.CurrentCultureIgnoreCase);

	public Dictionary<string, string> Properties => this.Config.BotProperties;
	public Dictionary<string, Set> Sets { get; } = new(StringComparer.CurrentCultureIgnoreCase);
	public Dictionary<string, Map> Maps { get; } = new(StringComparer.CurrentCultureIgnoreCase);
	public TripleCollection Triples { get; } = new(StringComparer.CurrentCultureIgnoreCase);

	public AimlLoader AimlLoader { get; }

	public event EventHandler<GossipEventArgs>? Gossip;
	public event EventHandler<LogMessageEventArgs>? LogMessage;
	public event EventHandler<PostbackRequestEventArgs>? PostbackRequest;
	public event EventHandler<PostbackResponseEventArgs>? PostbackResponse;

	public void OnGossip(GossipEventArgs e) => this.Gossip?.Invoke(this, e);
	public void OnLogMessage(LogMessageEventArgs e) => this.LogMessage?.Invoke(this, e);

	internal readonly Random Random = new();

	public Bot() : this("config") { }
	public Bot(string configDirectory) {
		this.AimlLoader = new(this);
		this.ConfigDirectory = configDirectory;

		// Add predefined sets and maps.
		var inflector = new Inflector(StringComparer.CurrentCultureIgnoreCase);
		this.Sets.Add("number", new NumberSet());
		this.Maps.Add("successor", new ArithmeticMap(1));
		this.Maps.Add("predecessor", new ArithmeticMap(-1));
		this.Maps.Add("singular", new SingularMap(inflector));
		this.Maps.Add("plural", new PluralMap(inflector));
	}
	internal Bot(Random random) : this() => this.Random = random;

	public void LoadAiml() => this.AimlLoader.LoadAimlFiles();

	public void LoadConfig() {
		this.Config = Config.FromFile(Path.Combine(this.ConfigDirectory, "config.json"));
		this.LoadConfig2();
	}

	private void LoadConfig2() {
		this.CheckDefaultProperties();

		var inflector = new Inflector(this.Config.StringComparer);
		this.Maps["singular"] = new SingularMap(inflector);
		this.Maps["plural"] = new PluralMap(inflector);

		this.Config.GenderSubstitutions = new(this.Config.SubstitutionsPreserveCase);
		this.Config.PersonSubstitutions = new(this.Config.SubstitutionsPreserveCase);
		this.Config.Person2Substitutions = new(this.Config.SubstitutionsPreserveCase);
		this.Config.NormalSubstitutions = new(this.Config.SubstitutionsPreserveCase);
		this.Config.DenormalSubstitutions = new(this.Config.SubstitutionsPreserveCase);

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

			this.Sets[Path.GetFileNameWithoutExtension(file)] = set.Count == 1 && set[0].StartsWith("map:")
				? new MapSet(set[0][4..], this)
				: new StringSet(set, this.Config.StringComparer);
			this.InvalidateVocabulary();
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

			this.Maps[Path.GetFileNameWithoutExtension(file)] = new StringMap(map, this.Config.StringComparer);
			this.InvalidateVocabulary();
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
					this.Triples.Add(fields[0], fields[1], fields[2]);
			}
		}

		this.Log(LogLevel.Info, "Loaded " + this.Triples.Count + " triple(s).");
	}

	private bool logDirectoryCreated = false;
	private int? vocabulary;

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

	public Response Chat(Request request) => this.Chat(request, false);
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

	public Response Postback(Request request) {
		this.PostbackRequest?.Invoke(this, new(request));
		var response = this.Chat(request);
		this.PostbackResponse?.Invoke(this, new(response));
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
					process.Log(LogLevel.Diagnostic, $"Input matched user-specific category '{process.Path}'.");
				} else {
					template = this.Graphmaster.Search(sentence, process, that, trace);
					if (template != null) {
						process.template = template;
						process.Log(LogLevel.Diagnostic, $"Input matched category '{process.Path}' in {template.Uri} line {template.LineNumber}.");
					}
				}

				if (template != null) {
					output = template.Content.Evaluate(process);
				} else {
					process.Log(LogLevel.Warning, $"No match for input '{sentence.Text}'.");
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

	private int CalculateVocabulary() {
		static void TraversePatternNode(ICollection<string> words, PatternNode node) {
			foreach (var e in node.Children) {
				if (e.Key is not ("_" or "#" or "*" or "^" or "<that>" or "<topic>"))
					words.Add(e.Key.TrimStart('$'));
				TraversePatternNode(words, e.Value);
			}
		}

		var words = new HashSet<string>(this.Config.StringComparer);
		TraversePatternNode(words, this.Graphmaster);
		foreach (var set in this.Sets.Values) {
			switch (set) {
				case StringSet stringSet:
					foreach (var entry in stringSet) {
						words.UnionWith(entry.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries));
					}
					break;
				case MapSet mapSet:
					if (mapSet.Map is not StringMap stringMap) continue;
					foreach (var entry in stringMap.Keys) {
						words.UnionWith(entry.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries));
					}
					break;
			}
		}

		return words.Count;
	}

	internal void InvalidateVocabulary() => this.vocabulary = null;
}
