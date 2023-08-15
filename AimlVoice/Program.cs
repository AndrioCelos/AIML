using System.Diagnostics;
using System.Reflection;
using System.Speech.Recognition;
using System.Speech.Recognition.SrgsGrammar;
using System.Speech.Synthesis;
using System.Text;
using System.Xml;
using Aiml;

namespace AimlVoice;
public class Program {
	internal static Bot? bot;
	internal static User? user;
	internal static SpeechRecognitionEngine? recognizer;
	internal static SpeechSynthesizer? synthesizer;
	internal static Dictionary<string, Grammar> grammars = new(StringComparer.InvariantCultureIgnoreCase);
	internal static string progressMessage = "";
	internal static List<string> enabledGrammarPaths = new();
	internal static PartialInputMode partialInput;
	private static readonly Stopwatch partialInputTimeout = Stopwatch.StartNew();
	private static readonly List<Reply> replies = new();
	private static readonly Dictionary<string, Reply> repliesByText = new(StringComparer.CurrentCultureIgnoreCase);

	private static readonly Queue<SpeechQueueItem> speechQueue = new();

	public static Dictionary<string, Action<XmlElement>> OobHandlers { get; } = new(StringComparer.CurrentCultureIgnoreCase) {
		{ "SetGrammar", OobSetGrammar },
		{ "EnableGrammar", OobEnableGrammar },
		{ "DisableGrammar", OobDisableGrammar },
		{ "SetPartialInput", OobPartialInput }
	};
	public static Dictionary<string, Action<string>> OldOobHandlers { get; } = new(StringComparer.CurrentCultureIgnoreCase) {
		{ "SetGrammar", OobSetGrammar },
		{ "EnableGrammar", OobEnableGrammar },
		{ "DisableGrammar", OobDisableGrammar },
		{ "SetPartialInput", OobPartialInput }
	};

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
					case "/?":
						Console.WriteLine("Usage: AimlVoice [switches] <bot path>");
						Console.WriteLine("Available switches:");
						Console.WriteLine("  -g [name], --grammar [name]: Enable the specified grammar upon startup. Specify a file name in the `grammars` directory without the `.xml` extension.");
						Console.WriteLine("  -e [path], --extension [path]: Load AIML extensions from the specified library.");
						Console.WriteLine("  -V [name], --voice [name]: Use the specified voice. If invalid, a list of available voices will be shown.");
						Console.WriteLine("  -r [number], --rate [number]: Modify the speech rate. -10 ~ +10; default is 0.");
						Console.WriteLine("  -v [number], --volume [number]: Modify the speech volume. -10 ~ +10; default is 0.");
						Console.WriteLine("  -n, --no-sr: Do not load the speech recogniser. Input will by typing only.");
						Console.WriteLine("  --: Stop processing switches.");
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
						Console.Error.WriteLine("Unknown switch " + s);
						Console.Error.WriteLine("Use `AimlVoice --help` for more information.");
						return 1;
				}
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

		if (Directory.Exists(Path.Combine(botPath, "grammars"))) {
			foreach (var file in Directory.GetFiles(Path.Combine(botPath, "grammars"), "*.xml", SearchOption.AllDirectories)) {
				var grammar = new Grammar(new SrgsDocument(file));  // Grammar..ctor(string) is not implemented.
				grammars[Path.GetFileNameWithoutExtension(file)] = grammar;
			}
		} else {
			Console.WriteLine($"Grammars directory {Path.Combine(botPath, "grammars")} does not exist. Skipping loading grammars.");
		}

		bot = new Bot(botPath);
		bot.LogMessage += Bot_LogMessage;

		foreach (var path in extensionPaths) {
			Console.WriteLine($"Loading extensions from {path}...");
			var loadContext = new PluginLoadContext(Path.GetFullPath(path));
			var assemblyName = AssemblyName.GetAssemblyName(path);
			var assembly = loadContext.LoadFromAssemblyName(assemblyName);
			var found = false;
			foreach (var type in assembly.GetExportedTypes()) {
				if (!type.IsAbstract && typeof(ISraixService).IsAssignableFrom(type)) {
					Console.WriteLine($"Initialising type {type.FullName}...");
					found = true;
					var service = (ISraixService) Activator.CreateInstance(type)!;
					bot.SraixServices.Add(type.Name, service);
					bot.SraixServices.Add(type.FullName!, service);
				} else if (!type.IsAbstract && typeof(IAimlExtension).IsAssignableFrom(type)) {
					Console.WriteLine($"Initialising type {type.FullName}...");
					found = true;
					var extension = (IAimlExtension) Activator.CreateInstance(type)!;
					extension.Initialise();
				}
			}
			if (!found) {
				Console.Error.WriteLine($"No extensions found in {path}.");
				return 1;
			}
		}

		bot.LoadConfig();
		bot.LoadAIML();
		//bot.Config.LogLevel = LogLevel.Info;

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

		if (!sr) {
			if (bot.Graphmaster.Children.ContainsKey("OOB") && bot.Graphmaster.Children["OOB"].Children.ContainsKey("START")) {
				Console.ForegroundColor = ConsoleColor.DarkGreen;
				Console.WriteLine("* OOB START");
				Console.ResetColor();
				SendInput("OOB START");
			}

			Console.Write("> ");
			while (true) {
				var message = Console.ReadLine();
				if (message == null) return 0;
				SendInput(message);
				Console.Write("> ");
			}
		} else {
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

				recognizer.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(Recognizer_SpeechRecognized);
				recognizer.SpeechRecognitionRejected += Recognizer_SpeechRecognitionRejected;
				recognizer.SpeechHypothesized += Recognizer_SpeechHypothesized;
				recognizer.RecognizerUpdateReached += Recognizer_RecognizerUpdateReached;

				recognizer.SetInputToDefaultAudioDevice();

				recognizer.RecognizeAsync(RecognizeMode.Multiple);

				if (bot.Graphmaster.Children.ContainsKey("OOB") && bot.Graphmaster.Children["OOB"].Children.ContainsKey("START")) {
					Console.ForegroundColor = ConsoleColor.DarkGreen;
					Console.WriteLine("* OOB START");
					Console.ResetColor();
					SendInput("OOB START");
				}

				Console.Write("> ");
				while (true) {
					var message = Console.ReadLine();
					if (message == null) return 0;
					SendInput(message);
					Console.Write("> ");
				}
			}
		}
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
			var text = response.ToString();
			if (!string.IsNullOrWhiteSpace(text)) {
				partialInputTimeout.Restart();
				ProcessOutput(text);
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
		ProcessOutput(response.ToString());
	}

	private static void ProcessOutput(string responseString) {
		var queue = false;
		var builder = new PromptBuilder(bot!.Config.Locale);
		var ssmlOverride = false;
		var responseBuilder = new StringBuilder();
		var mediaBuilder = new StringBuilder();

		replies.Clear();
		repliesByText.Clear();

		var xmlDocument = new XmlDocument();
		try {
			xmlDocument.LoadXml("<response>" + responseString + "</response>");

			foreach (XmlNode node in xmlDocument.DocumentElement!.ChildNodes) {
				switch (node.NodeType) {
					case XmlNodeType.Text:
					case XmlNodeType.SignificantWhitespace:
					case XmlNodeType.CDATA:
						responseBuilder.Append(node.InnerText);
						if (!ssmlOverride) builder.AppendText(node.InnerText);
						break;
					case XmlNodeType.Whitespace:
						responseBuilder.Append(' ');
						if (!ssmlOverride) builder.AppendText(" ");
						break;
					case XmlNodeType.Element:
						// Handle oob and rich media elements.
						switch (node.Name.ToLowerInvariant()) {
							case "oob":
								if (node.HasChildNodes) {
									if (!node.ChildNodes.Cast<XmlNode>().Any(n => n.NodeType == XmlNodeType.Element)) {
										var fields = node.InnerText.Trim().Split((char[]?) null, 2, StringSplitOptions.RemoveEmptyEntries);
										if (OldOobHandlers.TryGetValue(fields[0], out var action)) {
											action.Invoke(fields.Length == 1 ? "" : fields[1].TrimEnd());
										} else if (fields[0].Equals("queue", StringComparison.CurrentCultureIgnoreCase))
											queue = true;
									} else {
										foreach (var childElement in node.ChildNodes.OfType<XmlElement>()) {
											if (OobHandlers.TryGetValue(childElement.Name, out var action))
												action.Invoke(childElement);
											else if (childElement.Name.Equals("queue", StringComparison.CurrentCultureIgnoreCase))
												queue = true;
											else if (childElement.Name.Equals("speak", StringComparison.CurrentCultureIgnoreCase)) {
												if (childElement.Attributes["version"] == null) {
													var attribute = xmlDocument.CreateAttribute("version");
													attribute.Value = "1.0";
													childElement.Attributes.Append(attribute);
												}
												if (childElement.Attributes["xml:lang"] == null) {
													var attribute = xmlDocument.CreateAttribute("xml:lang");
													attribute.Value = bot.Config.Locale.Name.ToLowerInvariant();
													childElement.Attributes.Append(attribute);
												}
												// If an <alt> element is included, this is treated as a segment which needs pronunciation specified.
												// Its content will be displayed in place of the SSML.
												// Otherwise, it is treated as specifying pronunciation for the entire response.
												// The SSML overrides the entire response (except other <speak> OOB tags).
												if (!ssmlOverride && !node.ChildNodes.Cast<XmlNode>().Any(n => n.Name.Equals("alt", StringComparison.CurrentCultureIgnoreCase))) {
													ssmlOverride = true;
													builder.ClearContent();
												}
												builder.AppendSsml(new XmlNodeReader(childElement));
											} else if (childElement.Name.Equals("alt", StringComparison.CurrentCultureIgnoreCase)) {
												responseBuilder.Append(childElement.InnerText);
											}
										}
									}
								}
								break;
							case "reply":
								// Replies are displayed in the console. You can enter the displayed text to trigger the postback.
								var textBuilder = new StringBuilder();
								string? text = null;
								string? postback = null;
								foreach (XmlNode node2 in node.ChildNodes) {
									switch (node2.NodeType) {
										case XmlNodeType.Text:
										case XmlNodeType.SignificantWhitespace:
										case XmlNodeType.CDATA:
											textBuilder.Append(node2.InnerText);
											break;
										case XmlNodeType.Whitespace:
											textBuilder.Append(' ');
											break;
										case XmlNodeType.Element:
											switch (node2.Name.ToLowerInvariant()) {
												case "postback":
													postback = node2.InnerText.Trim();
													break;
												case "text":
													text = node2.InnerText;
													break;
												default:
													throw new XmlException("Bad child element in reply: " + node2.Name);
											}
											break;
									}
								}
								text ??= textBuilder.ToString();
								text = text.Trim();
								if (postback == null || postback.Length == 0) postback = text;
								var reply = new Reply(text, postback);
								replies.Add(reply);
								repliesByText[text] = reply;
								break;
							default:
								throw new XmlException("Unknown XML element: " + node.Name);
						}
						break;
				}
			}
		} catch (XmlException) {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("Badly formed XML in output: " + responseString);
			Console.ResetColor();
			return;
		}

		if (responseBuilder.Length > 0) {
			var s = responseBuilder.ToString();
			if (!string.IsNullOrWhiteSpace(s)) {
				Console.ForegroundColor = ConsoleColor.Blue;
				Console.WriteLine(s);
			}
		}
		if (replies.Count > 0) {
			Console.ForegroundColor = ConsoleColor.DarkMagenta;
			Console.WriteLine($"[{(replies.Count == 1 ? "Reply" : "Replies")}: {string.Join(", ", replies.Select(r => r.Text))}]");
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
			speechQueue.Enqueue(new SpeechQueueItem(prompt, queue));
			synthesizer!.SpeakAsync(prompt);
		}
	}

	private static void OobPartialInput(XmlElement element)
		=> OobPartialInput(element.InnerText);
	private static void OobPartialInput(string text) {
		switch (text.Trim().ToLowerInvariant()) {
			case "off": case "false": case "0": partialInput = PartialInputMode.Off; break;
			case "on": case "true": case "1": partialInput = PartialInputMode.On; break;
			case "continuous": case "2": partialInput = PartialInputMode.Continuous; break;
			default: Console.WriteLine($"Invalid partial input setting '{text}'."); return;
		}
		Console.WriteLine($"Partial input is {partialInput}.");
	}

	private static void OobSetGrammar(XmlElement element)
		=> OobSetGrammar(element.InnerText);
	private static void OobSetGrammar(string text) {
		if (!enabledGrammarPaths.Contains(text)) {
			if (grammars.TryGetValue(text, out var grammar)) {
				Console.WriteLine($"Switching to grammar '{text}'");
				foreach (var path in enabledGrammarPaths)
					grammars[path].Enabled = false;
				enabledGrammarPaths.Clear();
				grammar.Enabled = true;
				enabledGrammarPaths.Add(text);
			} else
				Console.WriteLine($"Could not find requested grammar '{text}'.");
		}
	}

	private static void OobDisableGrammar(XmlElement element)
		=> OobDisableGrammar(element.InnerText);
	private static void OobDisableGrammar(string text) {
		if (enabledGrammarPaths.Contains(text)) {
			if (enabledGrammarPaths.Count == 1) {
				Console.WriteLine($"Refusing to disable the last enabled grammar '{text}'");
			} else if (grammars.TryGetValue(text, out var grammar)) {
				Console.WriteLine($"Disabling grammar '{text}'");
				grammar.Enabled = false;
				enabledGrammarPaths.Remove(text);
			} else
				Console.WriteLine($"Could not find requested grammar '{text}'.");
		}
	}
	private static void OobEnableGrammar(XmlElement element)
		=> OobEnableGrammar(element.InnerText);
	private static void OobEnableGrammar(string text) {
		if (!enabledGrammarPaths.Contains(text)) {
			if (grammars.TryGetValue(text, out var grammar)) {
				Console.WriteLine($"Enabling grammar '{text}'");
				grammar.Enabled = true;
				enabledGrammarPaths.Add(text);
			} else
				Console.WriteLine($"Could not find requested grammar '{text}'.");
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
		if (partialInput == PartialInputMode.Continuous || partialInputTimeout.Elapsed >= TimeSpan.FromSeconds(3))
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
	/// <summary>Partial input will be processed, but if there is a response to partial input, further input will be ignored for 3 seconds.</summary>
	On = 1,
	/// <summary>Partial input will be processed with no cooldown.</summary>
	Continuous = 2
}

public class Reply {
	public string Text;
	public string Postback;

	public Reply(string text, string postback) {
		if (string.IsNullOrEmpty(text)) throw new ArgumentException($"'{nameof(text)}' cannot be null or empty.", nameof(text));
		if (string.IsNullOrEmpty(postback)) throw new ArgumentException($"'{nameof(postback)}' cannot be null or empty.", nameof(postback));
		this.Text = text;
		this.Postback = postback;
	}
}
