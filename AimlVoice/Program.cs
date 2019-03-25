#nullable enable

using Aiml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
		internal static string currentGrammar = "";
		internal static bool partialInput;
		private static Stopwatch partialInputTimeout = Stopwatch.StartNew();

		static int Main(string[] args) {
			bool switches = true; string? botPath = null; string? defaultGrammarPath = null;

			for (int i = 0; i < args.Length; ++i) {
				var s = args[i];
				if (switches && s.StartsWith("-")) {
					if (s == "--")
						switches = false;
					else if (s == "-g" || s == "--grammar")
						defaultGrammarPath = args[++i];
				} else {
					switches = false;
					botPath = s;
				}
			}
			if (botPath == null) {
				Console.Error.WriteLine("Usage: AimlVoice [--grammar <path>] <bot path>");
				return 1;
			}

			grammars[""] = new DictationGrammar();
			foreach (var file in Directory.GetFiles(Path.Combine(botPath, "grammars"), "*.xml", SearchOption.AllDirectories)) {
				var grammar = new Grammar(file);
				grammars[Path.GetFileNameWithoutExtension(file)] = grammar;
			}

			bot = new Bot(botPath);
			bot.LogMessage += Bot_LogMessage;

			bot.LoadConfig();
			bot.LoadAIML();
			bot.Config.LogLevel = LogLevel.Info;

			user = new User("User", bot);
			synthesizer = new SpeechSynthesizer();
			synthesizer.SelectVoice("Microsoft Zira Desktop");
			synthesizer.Rate = 1;

			using (recognizer = new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en-AU")) {
				BabbleTimeout = TimeSpan.FromSeconds(1),
				EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(0.75)
			}) {
				currentGrammar = defaultGrammarPath ?? "";
				recognizer.LoadGrammar(grammars[defaultGrammarPath ?? ""]);

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

			return 0;
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
						if (name != currentGrammar) {
							Console.WriteLine($"Loading grammar '{name}...'");
							recognizer.RequestRecognizerUpdate();
							recognizer.UnloadAllGrammars();
							recognizer.LoadGrammar(grammars[name]);
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
