#nullable enable

using Aiml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AimlVoice {
	class Program {
		internal static SpeechRecognitionEngine recognizer;
		internal static Bot bot;
		internal static User user;
		internal static SpeechSynthesizer synthesizer;
		internal static Dictionary<string, Grammar> grammars = new Dictionary<string, Grammar>(StringComparer.InvariantCultureIgnoreCase);
		internal static string progressMessage = "";
		internal static List<string> enabledGrammarPaths = new List<string>();
		internal static bool partialInput;
		private static Stopwatch partialInputTimeout = Stopwatch.StartNew();
		
		static int Main(string[] args) {
			bool switches = true; string? botPath = null; var defaultGrammarPath = new List<string>();
			string? voice = null; var sraixServicePaths = new List<string>();

			for (int i = 0; i < args.Length; ++i) {
				var s = args[i];
				if (switches && s.StartsWith("-")) {
					if (s == "--")
						switches = false;
					else if (s == "-h" || s == "--help" || s == "/?") {
						Console.WriteLine("Usage: AimlVoice [switches] <bot path>");
						Console.WriteLine("Available switches:");
						Console.WriteLine("  -g [name], --grammar [name]: Enable the specified grammar upon startup. Specify a file name in the `grammars` directory without the `.xml` extension.");
						Console.WriteLine("  -s [path], --services [path]: Load AIML services from the specified library.");
						Console.WriteLine("  -v [name], --voice [name]: Use the specified voice. If invalid, a list of available voices will be shown.");
						Console.WriteLine("  --: Stop processing switches.");
						return 0;
					} else if (s == "-g" || s == "--grammar")
						defaultGrammarPath.Add(args[++i]);
					else if (s == "-s" || s == "--services")
						sraixServicePaths.Add(args[++i]);
					else if (s == "-v" || s == "--voice")
						voice = args[++i];
				} else {
					switches = false;
					botPath = s;
				}
			}
			if (botPath == null) {
				Console.Error.WriteLine("Usage: AimlVoice [switches] <bot path>");
				Console.Error.WriteLine("Use `AimlVoice --help` for more information.");
				return 1;
			}

			foreach (var file in Directory.GetFiles(Path.Combine(botPath, "grammars"), "*.xml", SearchOption.AllDirectories)) {
				var grammar = new Grammar(file);
				grammars[Path.GetFileNameWithoutExtension(file)] = grammar;
			}

			bot = new Bot(botPath);
			bot.LogMessage += Bot_LogMessage;

			foreach (var path in sraixServicePaths) {
				var assembly = Assembly.LoadFrom(path);
				var found = false;
				foreach (var type in assembly.GetExportedTypes()) {
					if (!type.IsAbstract && typeof(ISraixService).IsAssignableFrom(type)) {
						Console.WriteLine($"Initialising service {type.FullName} from {path}...");
                        found = true;
						bot.SraixServices.Add(type.FullName, (ISraixService) Activator.CreateInstance(type));
					}
				}
				if (!found) {
					Console.Error.WriteLine($"No services found in {path}.");
					return 1;
				}
			}

			bot.LoadConfig();
			bot.LoadAIML();
			bot.Config.LogLevel = LogLevel.Info;

			user = new User("User", bot);
			synthesizer = new SpeechSynthesizer();
			if (voice != null) {
				try {
					synthesizer.SelectVoice(voice);
				} catch (ArgumentException) {
					Console.Error.WriteLine($"Couldn't load the voice {voice}.");
					Console.Error.WriteLine($"Available voices:");
					foreach (var voice2 in synthesizer.GetInstalledVoices().Where(v => v.Enabled))
						Console.Error.WriteLine(voice2.VoiceInfo.Name);
					return 1;
				}
			}
			synthesizer.Rate = 1;

			using (recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en")) {
				BabbleTimeout = TimeSpan.FromSeconds(1),
				EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(0.75)
			}) {

				foreach (var entry in grammars) {
					Console.WriteLine($"Loading grammar '{entry.Key}'...");
					entry.Value.Enabled = false;
					recognizer.LoadGrammar(entry.Value);
				}

				if (defaultGrammarPath.Count == 0) {
					Console.WriteLine($"Loading dictation grammar...");
					enabledGrammarPaths.Add("");
					grammars[""] = new DictationGrammar();
					recognizer.LoadGrammar(grammars[""]);
				} else {
					foreach (var name in defaultGrammarPath) {
						enabledGrammarPaths.Add(name);
						grammars[name].Enabled = true;
					}
				}

				recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);
				recognizer.SpeechRecognitionRejected += Recognizer_SpeechRecognitionRejected;
				recognizer.SpeechHypothesized += Recognizer_SpeechHypothesized;
				recognizer.RecognizerUpdateReached += Recognizer_RecognizerUpdateReached;

				recognizer.SetInputToDefaultAudioDevice();

				recognizer.RecognizeAsync(RecognizeMode.Multiple);

				Console.Write("> ");
				while (true) {
					var message = Console.ReadLine();
					sendInput(message);
					Console.Write("> ");
				}
			}
		}

		private static void Recognizer_RecognizerUpdateReached(object sender, RecognizerUpdateReachedEventArgs e) {
			Console.WriteLine("OK");
		}

		private static void clearMessage() {
			Console.Write(new string(' ', progressMessage.Length));
			Console.CursorLeft = 2;
			progressMessage = "";
		}

		private static void writeMessage(string message) {
			clearMessage();
			Console.Write(message);
			progressMessage = message;
			Console.CursorLeft = 2;
		}

		private static void Recognizer_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e) {
			Console.ForegroundColor = ConsoleColor.DarkMagenta;
			writeMessage($"({e.Result.Text} ... {e.Result.Confidence})");
			Console.ResetColor();

			if (partialInput && partialInputTimeout.Elapsed >= TimeSpan.FromSeconds(3) && e.Result.Confidence
>= 0.25) {
					var response = bot.Chat(new Request("PartialInput " + e.Result.Text, user, bot), false);
					var text = Regex.Replace(response.ToString(), "<oob>([^<]*)</oob>", "");
					if (!string.IsNullOrWhiteSpace(text)) {
							partialInputTimeout.Restart();
							Console.ForegroundColor = ConsoleColor.Blue;
							Console.WriteLine();
							Console.WriteLine(text);
							Console.ResetColor();
							Console.Write("> ");
							synthesizer.SpeakAsyncCancelAll();
							synthesizer.SpeakAsync(text);
					}
			}
		}

		private static void Recognizer_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e) {
			Console.ForegroundColor = ConsoleColor.DarkMagenta;

			if (e.Result.Alternates.Count == 1 && e.Result.Alternates[0].Confidence >= 0.25) {
				Console.ForegroundColor = ConsoleColor.Magenta;
				Console.WriteLine(e.Result.Alternates[0].Text + "    ");
				Console.ResetColor();
				if (partialInputTimeout.Elapsed >= TimeSpan.FromSeconds(3))
					sendInput(e.Result.Alternates[0].Text);
			} else {
				writeMessage(string.Join(" ", e.Result.Alternates.Select(a => $"({a.Text} ...? {a.Confidence})")));
				Console.ResetColor();
			}
		}

		private static void sendInput(string input) {
			var trace = false;
			if (input.StartsWith(".trace ")) {
				trace = true;
				input = input.Substring(7);
			}
			var response = bot.Chat(new Request(input, user, bot), trace);
			var text = Regex.Replace(response.ToString(), "<oob>([^<]*)</oob>", m => {
				var fields = m.Groups[1].Value.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries);
				switch (fields[0]) {
					case "SetGrammar":
						var name = fields.Length > 1 ? fields[1] : "";
						if (!enabledGrammarPaths.Contains(name)) {
							if (grammars.TryGetValue(name, out var grammar)) {
								Console.WriteLine($"Switching to grammar '{name}'");
								foreach (var path in enabledGrammarPaths)
									grammars[path].Enabled = false;
								enabledGrammarPaths.Clear();
								grammar.Enabled = true;
								enabledGrammarPaths.Add(name);
							} else
								Console.WriteLine($"Could not find requested grammar '{name}'.");
						}
						break;
					case "SetPartialInput":
						partialInput = bool.Parse(fields[1]);
						Console.WriteLine($"Partial input is {(partialInput ? "enabled" : "disabled")}.");
						break;
				}
				return "";
			});
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.WriteLine(text);
			Console.ResetColor();
			synthesizer.SpeakAsyncCancelAll();
			synthesizer.SpeakAsync(text);
		}

		private static void Bot_LogMessage(object sender, LogMessageEventArgs e) {
			switch (e.Level) {
				case LogLevel.Warning: Console.ForegroundColor = ConsoleColor.Yellow; break;
				case LogLevel.Gossip: Console.ForegroundColor = ConsoleColor.Blue; break;
				case LogLevel.Chat: Console.ForegroundColor = ConsoleColor.Blue; break;
				case LogLevel.Diagnostic: Console.ForegroundColor = ConsoleColor.DarkBlue; break;
			}
			Console.WriteLine($"[{e.Level}] {e.Message}");
			Console.ResetColor();
		}

		static void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e) {
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine(e.Result.Text + "     ");
			Console.ResetColor();
			if (partialInputTimeout.Elapsed >= TimeSpan.FromSeconds(3))
				sendInput(e.Result.Text);
			Console.Write("> ");
		}
	}
}
