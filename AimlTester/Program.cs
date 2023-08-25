using Aiml;
using System;
using System.Collections.Generic;
using System.IO;

namespace AimlTester;
internal class Program {
	internal static int warnings;

	internal static int Main(string[] args) {
		var switches = true; string? botPath = null; string? testPath = null;
		var extensionPaths = new List<string>();
		var inputs = new List<string>();

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
						Console.WriteLine($"Usage: {nameof(AimlTester)} [switches] -t <test subpath> <bot path>");
						Console.WriteLine("Available switches:");
						Console.WriteLine("  -e [path], --extension [path]: Load AIML extensions from the specified assembly.");
						Console.WriteLine("  --: Stop processing switches.");
						return 0;
					case "-t":
					case "--test":
						testPath = args[++i];
						break;
					case "-e":
					case "--extension":
					case "--extensions":
					case "-S":
					case "--service":
					case "--services":
						extensionPaths.Add(args[++i]);
						break;
					default:
						Console.Error.WriteLine($"Unknown switch {s}");
						Console.Error.WriteLine($"Use `{nameof(AimlTester)} --help` for more information.");
						return 1;
				}
			} else {
				switches = false;
				botPath = s;
			}
		}
		if (botPath == null || testPath == null) {
			Console.Error.WriteLine($"Usage: {nameof(AimlTester)} [switches] -t <test subpath> <bot path>");
			Console.Error.WriteLine($"Use `{nameof(AimlTester)} --help` for more information.");
			return 1;
		}

		foreach (var path in extensionPaths) {
			Console.WriteLine($"Loading extensions from {path}...");
			AimlLoader.AddExtensions(path);
		}

		var bot = new Bot(botPath);
		bot.LogMessage += Bot_LogMessage;
		bot.LoadConfig();
		bot.LoadAIML();
		bot.AimlLoader!.LoadAimlFiles(Path.Combine(botPath, testPath!));

		var user = new User("User", bot);

		Console.WriteLine("Looking for tests...");

		var categories = new List<KeyValuePair<string, Template>>();
		var tests = new Dictionary<string, TestResult?>();
		foreach (var entry in bot.Graphmaster.GetTemplates()) {
			var tests2 = entry.Value.GetTests();
			foreach (var test in tests2)
				tests.Add(test.Name, null);
			if (tests2.Count > 0)
				categories.Add(entry);
		}

		if (tests.Count == 1)
			Console.WriteLine($"{tests.Count} test found.");
		else
			Console.WriteLine($"{tests.Count} tests found.");

		foreach (var (path, template) in categories) {
			Console.WriteLine($"Running test template in {template.Uri} line {template.LineNumber} with path '{path}'...");

			var pos = path.IndexOf(" <that> ");
			var input = path[..pos];

			var request = new Request(input, user, bot);
			var process = new RequestProcess(new RequestSentence(request, input), 0, true);
			var text = template.Content.Evaluate(process);
			user.Responses.Add(new Response(request, text));

			foreach (var (name, result) in process.TestResults!) {
				tests[name] = result;
			}
		}

		int passes = 0, failures = 0, j = 1;
		Console.WriteLine();
		Console.WriteLine("Test results:");
		Console.WriteLine();
		foreach (var (name, result) in tests) {
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(j++.ToString().PadLeft(4));
			Console.Write(": ");
			Console.Write(name);
			Console.Write(" ");
			if (result == null) {
				++failures;
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("was not reached");
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine(".");
			} else if (result.Passed) {
				++passes;
				Console.ForegroundColor = ConsoleColor.Green;
				Console.Write("passed");
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine($" in {result.Duration.TotalMilliseconds} ms.");
			} else {
				++failures;
				Console.ForegroundColor = ConsoleColor.Red;
				Console.Write("failed");
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine($" in {result.Duration.TotalMilliseconds} ms.");
				Console.ResetColor();
				Console.WriteLine(result.Message);
			}
		}
		Console.ResetColor();
		Console.WriteLine();

		if (passes > 0) Console.ForegroundColor = ConsoleColor.Green;
		Console.Write(passes);
		Console.ResetColor();
		if (passes == 1) Console.Write(" test passed; ");
		else Console.Write(" tests passed; ");

		if (failures > 0) Console.ForegroundColor = ConsoleColor.Red;
		Console.Write(failures);
		Console.ResetColor();
		if (failures == 1) Console.Write(" test failed; ");
		else Console.Write(" tests failed; ");

		if (warnings > 0) Console.ForegroundColor = ConsoleColor.Yellow;
		Console.Write(warnings);
		Console.ResetColor();
		if (warnings == 1) Console.Write(" warning.");
		else Console.Write(" warnings.");
		Console.WriteLine();

		return 0;
	}

	private static void Bot_LogMessage(object? sender, LogMessageEventArgs e) {
		switch (e.Level) {
			case LogLevel.Warning: Console.ForegroundColor = ConsoleColor.Yellow; ++warnings; break;
			case LogLevel.Gossip: Console.ForegroundColor = ConsoleColor.Blue; break;
			case LogLevel.Chat: Console.ForegroundColor = ConsoleColor.Blue; break;
			case LogLevel.Diagnostic: Console.ForegroundColor = ConsoleColor.DarkBlue; break;
		}
		Console.WriteLine($"[{e.Level}] {e.Message}");
		Console.ResetColor();
	}
}
