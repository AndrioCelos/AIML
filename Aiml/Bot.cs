using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Aiml {
	public class Bot {
		public string ConfigDirectory { get; set; }
		public Config Config { get; set; }

		// TODO: private Dictionary<string, TagHandler> CustomTags;

		public DateTime StartedOn = DateTime.Now;
		public int Size;
		public int Vocabulary;
		public PatternNode Graphmaster;

		public Dictionary<string, string> Properties => this.Config.BotProperties;
		public Dictionary<string, Set> Sets;
		public Dictionary<string, Map> Maps;
		public TripleCollection Triples;

		public AimlLoader? AimlLoader { get; internal set; }

		public Dictionary<string, ISraixService> SraixServices = new Dictionary<string, ISraixService>(StringComparer.InvariantCultureIgnoreCase);

		public event EventHandler<GossipEventArgs> Gossip;
		public event EventHandler<LogMessageEventArgs> LogMessage;
		protected void OnGossip(GossipEventArgs e) {
			this.Gossip?.Invoke(this, e);
		}
		protected void OnLogMessage(LogMessageEventArgs e) {
			this.LogMessage?.Invoke(this, e);
		}

		public Bot() : this("config") { }
		public Bot(string configDirectory) {
			this.Config = new Config();
			this.ConfigDirectory = configDirectory;
			this.Graphmaster = new PatternNode(null, StringComparer.CurrentCultureIgnoreCase);
			this.Sets = new Dictionary<string, Set>(StringComparer.CurrentCultureIgnoreCase);
			this.Maps = new Dictionary<string, Map>(StringComparer.CurrentCultureIgnoreCase);
			this.Triples = new TripleCollection();
			//this.CustomTags = new Dictionary<string, TagHandler>(StringComparer.InvariantCultureIgnoreCase);

			// Add predefined sets and maps.
			var inflector = new Inflector(StringComparer.CurrentCultureIgnoreCase);
			this.Sets.Add("number", new Sets.NumberSet());
			this.Maps.Add("successor", new Maps.ArithmeticMap(1));
			this.Maps.Add("predecessor", new Maps.ArithmeticMap(-1));
			this.Maps.Add("singular", new Maps.SingularMap(inflector));
			this.Maps.Add("plural", new Maps.PluralMap(inflector));
		}

		public void LoadAIML() {
			AimlLoader aIMLLoader = new AimlLoader(this);
			aIMLLoader.LoadAimlFiles();
		}
		public void LoadAIML(XmlDocument newAIML, string filename) {
			AimlLoader aIMLLoader = new AimlLoader(this);
			aIMLLoader.LoadAIML(newAIML, filename);
		}

		public void LoadConfig() {
			if (this.Config == null)
				this.Config = Config.FromFile(Path.Combine(this.ConfigDirectory, "config.json"));
			else
				this.Config.Load(Path.Combine(this.ConfigDirectory, "config.json"));

			this.LoadConfig2();
		}

		public void LoadConfig2() {
			//if (this.Graphmaster.children.Count == 0 && this.Graphmaster.setChildren.Count == 0)
			//	this.Graphmaster = new PatternNode(this, null, null, Config.StringComparer);
			this.checkDefaultSettings();

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

		private void checkDefaultSettings() {
			if (!this.Config.BotProperties.ContainsKey("version"))
				this.Config.BotProperties.Add("version", Assembly.GetExecutingAssembly().GetName().Version.ToString());
		}

		private void LoadSets(string directory) {
			// TODO: Implement remote sets and maps
			this.Log(LogLevel.Info, "Loading sets from " + directory + ".");

			foreach (string file in Directory.EnumerateFiles(directory, "*.txt")) {
				if (this.Maps.ContainsKey(Path.GetFileNameWithoutExtension(file))) {
					this.Log(LogLevel.Warning, "Duplicate set name '" + Path.GetFileNameWithoutExtension(file) + "'.");
					continue;
				}

				var set = new List<string>();

				StreamReader reader = new StreamReader(file);
				while (!reader.EndOfStream) {
					var phrase = reader.ReadLine();
					// Remove comments.
					var pos = phrase.IndexOf('#');
					if (pos >= 0) phrase = phrase.Substring(0, pos);
					phrase = phrase.Trim();
					if (phrase == "") continue;

					set.Add(phrase);
				}

				this.Sets[Path.GetFileNameWithoutExtension(file)] = new Sets.StringSet(set, this.Config.StringComparer);
				this.Vocabulary += set.Count;
			}

			this.Log(LogLevel.Info, "Loaded " + this.Sets.Count + " set(s).");
		}

		private void LoadMaps(string directory) {
			this.Log(LogLevel.Info, "Loading maps from " + directory + ".");

			foreach (string file in Directory.EnumerateFiles(directory, "*.txt")) {
				if (this.Maps.ContainsKey(Path.GetFileNameWithoutExtension(file))) {
					this.Log(LogLevel.Warning, "Duplicate map name '" + Path.GetFileNameWithoutExtension(file) + "'.");
					continue;
				}

				Dictionary<string, string> map = new Dictionary<string, string>(this.Config.StringComparer);

				StreamReader reader = new StreamReader(file);
				while (!reader.EndOfStream) {
					var line = reader.ReadLine();

					// Remove comments.
					var pos = line.IndexOf('#');
					if (pos >= 0) line = line.Substring(0, pos);
					if (string.IsNullOrWhiteSpace(line)) continue;

					pos = line.IndexOf(':');
					if (pos < 0)
						this.Log(LogLevel.Warning, "Map '" + Path.GetFileNameWithoutExtension(file) + "' contains a badly formatted line: " + line);
					else {
						var key = line.Substring(0, pos).Trim();
						var value = line.Substring(pos + 1).Trim();
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
					string[] fields = line.Split(new[] { ':' }, 3);
					if (fields.Length != 3)
						this.Log(LogLevel.Warning, "triples.txt contains a badly formatted line: " + line);
					else
						this.Triples.Add(fields[0], fields[1], fields[2]);
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

			if (!logDirectoryCreated) {
				Directory.CreateDirectory(Path.Combine(this.ConfigDirectory, this.Config.LogDirectory));
				logDirectoryCreated = true;
			};

			try {
				var writer = new StreamWriter(Path.Combine(this.ConfigDirectory, this.Config.LogDirectory, DateTime.Now.ToString("yyyyMMdd") + ".log"), true);
				writer.WriteLine(DateTime.Now.ToString("[HH:mm:ss]") + "\t[" + level + "]\t" + message);
				writer.Close();
			} catch (IOException ex) {

			}
		}

		internal void WriteGossip(RequestProcess process, string message) {
			var e = new GossipEventArgs(message);
			this.OnGossip(e);
			if (e.Handled) return;
			process.Log(LogLevel.Gossip, "Gossip from " + process.User.ID + ": " + message);
		}

		[Obsolete]
		public string GeneratePath(string message, string that, string topic) {
			//if (pattern.Length == 0) return string.Empty;
			if (string.IsNullOrWhiteSpace(that)) that = "*";
			if (string.IsNullOrWhiteSpace(topic)) topic = "*";

			return message + " <that> " + that + " <topic> " + topic;
		}

		public Response Chat(string message, string userID, bool trace) {
			Request request = new Request(message, new User(userID, this), this);
			return this.ProcessRequest(request, trace, false, 0, out _);
		}
		public Response Chat(Request request, bool trace) {
			this.Log(LogLevel.Chat, request.User.ID + ": " + request.Text);
			request.User.AddRequest(request);

			var response = this.ProcessRequest(request, trace, false, 0, out _);

			if (!this.Config.BotProperties.TryGetValue("name", out string botName)) botName = "Robot";
			this.Log(LogLevel.Chat, botName + ": " + response.ToString());

			request.User.AddResponse(response);
			return response;
		}

		internal Response ProcessRequest(Request request, bool trace, bool useTests, int recursionDepth, out TimeSpan duration) {
			var stopwatch = Stopwatch.StartNew();
			var that = this.Normalize(request.User.GetThat());

			// Respond to each sentence separately.
			var builder = new StringBuilder();
			foreach (var sentence in request.Sentences) {
				var process = new RequestProcess(sentence, recursionDepth, useTests);

				process.Log(LogLevel.Diagnostic, "Normalized text: " + sentence.Text);

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

			var response = new Response(request, builder.ToString());
			request.Response = response;

			stopwatch.Stop();
			duration = stopwatch.Elapsed;
			return response;
		}

		public string[] SentenceSplit(string text, bool preserveMarks) {
			if (this.Config.Splitters.Length == 0) {
				var sentence = text.Trim();
				if (sentence == "") return new string[0];
				return new[] { text.Trim() };
			}

			int sentenceStart = 0, searchFrom = 0;
			var sentences = new List<string>();

			while (true) {
				string sentence;
				var pos2 = text.IndexOfAny(this.Config.Splitters, searchFrom);
				if (pos2 < 0) {
					sentence = text.Substring(sentenceStart).Trim();
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

		public string GetProperty(string predicate) {
			string value;
			if (this.Config.BotProperties.TryGetValue(predicate, out value)) return value;
			return this.Config.DefaultPredicate;
		}

		public string Normalize(string text) {
			return this.Config.NormalSubstitutions.Apply(text);
		}

		public string Denormalize(string text) {
			return this.Config.DenormalSubstitutions.Apply(text);
		}
	}
}
