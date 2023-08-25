using System.Diagnostics;
using System.Speech.Recognition;
using System.Speech.Recognition.SrgsGrammar;
using System.Speech.Synthesis;
using System.Text;
using System.Xml;
using Aiml;
using Aiml.Media;

namespace AimlVoice;
public class Program {
	internal static Bot? bot;
	internal static User? user;
	internal static SpeechSynthesizer? synthesizer;
	internal static Dictionary<string, Grammar> grammars = new(StringComparer.InvariantCultureIgnoreCase);
	internal static string progressMessage = "";
	internal static List<string> enabledGrammarPaths = new();
	internal static PartialInputMode partialInput;
	private static readonly Stopwatch partialInputTimeout = Stopwatch.StartNew();
	private static readonly List<Reply> replies = new();
	private static readonly Dictionary<string, Reply> repliesByText = new(StringComparer.CurrentCultureIgnoreCase);

	private static readonly Queue<SpeechQueueItem> speechQueue = new();

	static int Main(string[] args) {
		var switches = true; string? botPath = null; var defaultGrammarPath = new List<string>();
		string? voice = null; var extensionPaths = new List<string>();
		int rate = 0, volume = 100;
		var sr = true;

		for (var i = 0; i < args.Length; ++i) {
			var s = args[i];
			if (switches && s.StartsWith("-")) {
				switch (s) {
					case "--":
						switches = false;
						break;
					case "-h":
					case "--help":
					case "-?":
					case "/?":
						Console.WriteLine($"Usage: {nameof(AimlVoice)} [switches] <bot path>");
						Console.WriteLine("Available switches:");
						Console.WriteLine("  -g [name], --grammar [name]: Enable the specified grammar upon startup. Specify a file name in the `grammars` directory without the `.xml` extension. May be used multiple times.");
						Console.WriteLine("  -e [path], --extension [path]: Load AIML extensions from the specified assembly.");
						Console.WriteLine("  -V [name], --voice [name]: Use the specified voice.");
						Console.WriteLine("  --voices: Show a list of available voices and exit.");
						Console.WriteLine("  -r [number], --rate [number]: Modify the speech rate. -10 ~ +10; default is 0.");
						Console.WriteLine("  -v [number], --volume [number]: Modify the speech volume. -10 ~ +10; default is 0.");
						Console.WriteLine("  -n, --no-sr: Do not load the speech recogniser. Input will by typing only.");
						Console.WriteLine("  --: Stop processing switches.");
						return 0;
					case "--voices":
					case "--listvoices":
						Console.WriteLine("Available voices:");
						foreach (var voice2 in new SpeechSynthesizer().GetInstalledVoices().Where(v => v.Enabled))
							Console.WriteLine(voice2.VoiceInfo.Name);
						return 0;
					case "-g":
					case "--grammar":
						defaultGrammarPath.Add(args[++i]);
						break;
					case "-e":
					case "--extension":
					case "--extensions":
					case "-S":
					case "--service":
					case "--services":
						extensionPaths.Add(args[++i]);
						break;
					case "-V":
					case "--voice":
						voice = args[++i];
						break;
					case "-v":
					case "--volume":
						volume = int.Parse(args[++i]);
						break;
					case "-r":
					case "--rate":
						rate = int.Parse(args[++i]);
						break;
					case "-n":
					case "--no-sr":
						sr = false;
						break;
					default:
						Console.Error.WriteLine($"Unknown switch {s}");
						Console.Error.WriteLine($"Use `{nameof(AimlVoice)} --help` for more information.");
						return 1;
				}
			} else {
				switches = false;
				botPath = s;
			}
		}
		if (botPath == null) {
			Console.Error.WriteLine($"Usage: {nameof(AimlVoice)} [switches] <bot path>");
			Console.Error.WriteLine($"Use `{nameof(AimlVoice)} --help` for more information.");
			return 1;
		}

		if (Directory.Exists(Path.Combine(botPath, "grammars"))) {
			foreach (var file in Directory.GetFiles(Path.Combine(botPath, "grammars"), "*.xml", SearchOption.AllDirectories)) {
				var grammar = new Grammar(new SrgsDocument(file));  // Grammar..ctor(string) is not implemented.
				grammars[Path.GetFileNameWithoutExtension(file)] = grammar;
			}
		} else {
			Console.WriteLine($"Grammars directory {Path.Combine(botPath, "grammars")} does not exist. Skipping loading grammars.");
		}

		AimlLoader.AddExtension(new AimlVoiceExtension());
		foreach (var path in extensionPaths) {
			Console.WriteLine($"Loading extensions from {path}...");
			AimlLoader.AddExtensions(path);
		}

		bot = new Bot(botPath);
		bot.LogMessage += Bot_LogMessage;
		bot.LoadConfig();
		bot.LoadAIML();

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

		synthesizer.Rate = rate;
		synthesizer.Volume = volume;
		synthesizer.SpeakCompleted += Synthesizer_SpeakCompleted;

		using var recognizer = sr ? new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en")) {
			BabbleTimeout = TimeSpan.FromSeconds(1),
			EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(0.75)
		} : null;

		if (recognizer is not null) {
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

			recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(Recognizer_SpeechRecognized);
			recognizer.SpeechRecognitionRejected += Recognizer_SpeechRecognitionRejected;
			recognizer.SpeechHypothesized += Recognizer_SpeechHypothesized;
			recognizer.RecognizerUpdateReached += Recognizer_RecognizerUpdateReached;

			recognizer.SetInputToDefaultAudioDevice();

			recognizer.RecognizeAsync(RecognizeMode.Multiple);
		}

		if (bot.Graphmaster.Children.TryGetValue("OOB", out var node) && node.Children.ContainsKey("START"))
			SendInput("OOB START");

		Console.Write("> ");
		while (true) {
			var message = Console.ReadLine();
			if (message == null) return 0;
			SendInput(message);
			Console.Write("> ");
		}
	}

	public static void SetPartialInput(PartialInputMode partialInputMode) {
		partialInput = partialInputMode;
		Console.WriteLine($"Partial input is {partialInput}.");
	}

	public static void TrySwitchGrammar(string name) {
		if (enabledGrammarPaths.Contains(name)) return;
		if (!grammars.TryGetValue(name, out var grammar)) {
			Console.WriteLine($"Could not find requested grammar '{name}'.");
			return;
		}
		Console.WriteLine($"Switching to grammar '{name}'");
		foreach (var path in enabledGrammarPaths)
			grammars[path].Enabled = false;
		enabledGrammarPaths.Clear();
		grammar.Enabled = true;
		enabledGrammarPaths.Add(name);
	}

	public static void TryDisableGrammar(string name) {
		if (!enabledGrammarPaths.Contains(name)) return;
		if (!grammars.TryGetValue(name, out var grammar)) {
			Console.WriteLine($"Could not find requested grammar '{name}'.");
			return;
		}
		if (enabledGrammarPaths.Count == 1) {
			Console.WriteLine($"Refusing to disable the last enabled grammar '{name}'");
			return;
		}
		Console.WriteLine($"Disabling grammar '{name}'");
		grammar.Enabled = false;
		enabledGrammarPaths.Remove(name);
	}

	public static void TryEnableGrammar(string name) {
		if (!enabledGrammarPaths.Contains(name)) return;
		if (!grammars.TryGetValue(name, out var grammar)) {
			Console.WriteLine($"Could not find requested grammar '{name}'.");
			return;
		}
		Console.WriteLine($"Enabling grammar '{name}'");
		grammar.Enabled = true;
		enabledGrammarPaths.Add(name);
	}

	private static void Synthesizer_SpeakCompleted(object? sender, SpeakCompletedEventArgs e) {
		try {
			if (speechQueue.Count > 0 && speechQueue.Peek().Prompt == e.Prompt)
				speechQueue.Dequeue();
		} catch (InvalidOperationException) { }
	}

	private static void Recognizer_RecognizerUpdateReached(object? sender, RecognizerUpdateReachedEventArgs e) => Console.WriteLine("OK");

	private static void ClearMessage() {
		Console.Write(new string(' ', progressMessage.Length));
		Console.CursorLeft = 2;
		progressMessage = "";
	}

	private static void WriteMessage(string message) {
		ClearMessage();
		Console.Write(message);
		progressMessage = message;
		Console.CursorLeft = 2;
	}

	private static void Recognizer_SpeechHypothesized(object? sender, SpeechHypothesizedEventArgs e) {
		Console.ForegroundColor = ConsoleColor.DarkMagenta;
		WriteMessage($"({e.Result.Text} ... {e.Result.Confidence})");
		Console.ResetColor();

		if (partialInput != 0 && (partialInput != PartialInputMode.On || partialInputTimeout.Elapsed >= TimeSpan.FromSeconds(3)) && e.Result.Confidence >= 0.25) {
			var response = bot!.Chat(new Request("PartialInput " + e.Result.Text, user!, bot), false);
			if (!response.IsEmpty) {
				partialInputTimeout.Restart();
				ProcessOutput(response);
			}
		}
	}

	private static void Recognizer_SpeechRecognitionRejected(object? sender, SpeechRecognitionRejectedEventArgs e) {
		Console.ForegroundColor = ConsoleColor.DarkMagenta;

		if (e.Result.Alternates.Count == 1 && e.Result.Alternates[0].Confidence >= 0.25) {
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine(e.Result.Alternates[0].Text + "    ");
			Console.ResetColor();
			if (partialInputTimeout.Elapsed >= TimeSpan.FromSeconds(3))
				SendInput(e.Result.Alternates[0].Text);
		} else {
			WriteMessage(string.Join(" ", e.Result.Alternates.Select(a => $"({a.Text} ...? {a.Confidence})")));
			Console.ResetColor();
		}
	}

	public static void SendInput(string input) {
		var trace = false;
		if (input.StartsWith(".trace ")) {
			trace = true;
			input = input[7..];
		}
		if (repliesByText.TryGetValue(input, out var reply)) input = reply.Postback;
		var response = bot!.Chat(new Request(input, user!, bot), trace);
		ProcessOutput(response);
	}

	private static void ProcessOutput(Response response) {
		replies.Clear();
		repliesByText.Clear();

		try {
			var messages = response.ToMessages();
			var replyIndex = 0;
			foreach (var message in messages) {
				var isPriority = false;
				var builder = new PromptBuilder(bot!.Config.Locale);
				var responseBuilder = new StringBuilder();

				foreach (var el in message.InlineElements) {
					switch (el) {
						case LineBreak:
							Console.WriteLine();
							break;
						case SpeakElement speak:
							builder.AppendSsml(speak.SSML.CreateReader());
							responseBuilder.Append(speak.AltText);
							break;
						default:
							var s = el.ToString();
							responseBuilder.Append(s);
							builder.AppendText(s);
							break;
					}
				}
				foreach (var el in message.BlockElements) {
					switch (el) {
						case Reply reply:
							replies.Add(reply);
							repliesByText[reply.Text] = reply;
							break;
						case PriorityElement:
							isPriority = true;
							break;
					}
				}

				if (responseBuilder.Length > 0) {
					var s = responseBuilder.ToString();
					if (!string.IsNullOrWhiteSpace(s)) {
						Console.ForegroundColor = ConsoleColor.Blue;
						Console.WriteLine(s);
					}
				}
				if (replies.Count > replyIndex) {
					Console.ForegroundColor = ConsoleColor.DarkMagenta;
					Console.WriteLine($"[{(replies.Count - replyIndex == 1 ? "Reply" : "Replies")}: {string.Join(", ", replies.Skip(replyIndex).Select(r => r.Text))}]");
					replyIndex = replies.Count;
				}
				Console.ResetColor();
				if (Enumerable.Range(0, responseBuilder.Length).Any(i => !char.IsWhiteSpace(responseBuilder[i]))) {
					try {
						while (speechQueue.Count > 0 && !speechQueue.Peek().Important) {
							synthesizer!.SpeakAsyncCancel(speechQueue.Peek().Prompt);
							speechQueue.Dequeue();
						}
					} catch (InvalidOperationException) { }

					var prompt = new Prompt(builder);
					speechQueue.Enqueue(new SpeechQueueItem(prompt, isPriority));
					synthesizer!.SpeakAsync(prompt);
				}

				Console.WriteLine();
				if (message.Separator is Delay delay) {
					Console.Write("...");
					Thread.Sleep(delay.Duration);
					Console.CursorLeft = 0;
				}
			}
		} catch (Exception ex) {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($"Failed to process response text: {ex.Message}");
			Console.WriteLine($"Response: {response}");
			Console.ResetColor();
		}
	}

	private static void Bot_LogMessage(object? sender, LogMessageEventArgs e) {
		switch (e.Level) {
			case LogLevel.Warning: Console.ForegroundColor = ConsoleColor.Yellow; break;
			case LogLevel.Gossip: Console.ForegroundColor = ConsoleColor.Blue; break;
			case LogLevel.Chat: Console.ForegroundColor = ConsoleColor.Blue; break;
			case LogLevel.Diagnostic: Console.ForegroundColor = ConsoleColor.DarkBlue; break;
		}
		Console.WriteLine($"[{e.Level}] {e.Message}");
		Console.ResetColor();
	}

	static void Recognizer_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e) {
		Console.ForegroundColor = ConsoleColor.Magenta;
		Console.WriteLine(e.Result.Text + "     ");
		Console.ResetColor();
		if (partialInput == PartialInputMode.Continuous || partialInputTimeout.Elapsed >= TimeSpan.FromSeconds(5))
			SendInput(e.Result.Text);
		Console.Write("> ");
	}
}

public class SpeechQueueItem(Prompt prompt, bool important) {
	public Prompt Prompt { get; } = prompt;
	public bool Important { get; } = important;
}

public enum PartialInputMode {
	/// <summary>Partial input will not be processed.</summary>
	Off = 0,
	/// <summary>Partial input will be processed, but if there is a response to partial input, further input will be ignored for 5 seconds.</summary>
	On = 1,
	/// <summary>Partial input will be processed with no cooldown.</summary>
	Continuous = 2
}
