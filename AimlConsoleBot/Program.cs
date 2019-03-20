using Aiml;
using System;
using System.Collections.Generic;
using System.IO;

namespace AimlConsoleBot {
	internal class Program {
		internal static int Main(string[] args) {
			bool switches = true; bool useTests = false; string? botPath = null; string? testPath = null;

			for (int i = 0; i < args.Length; ++i) {
				var s = args[i];
				if (switches && s.StartsWith("-")) {
					if (s == "--")
						switches = false;
					else if (s == "-t" || s == "--test") {
						useTests = true;
						testPath = args[++i];
					}
				} else {
					switches = false;
					botPath = s;
				}
			}
			if (botPath == null) {
				Console.Error.WriteLine("Usage: AimlConsoleBot [--test] <bot path>");
				return 1;
			}

			var bot = new Bot(botPath);
			bot.LogMessage += Bot_LogMessage;

			bot.LoadConfig();
			bot.LoadAIML();
			if (useTests) bot.AimlLoader.LoadAimlFiles(Path.Combine(botPath, testPath!));

			var user = new User("User", bot);

			if (useTests) {
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
					Console.WriteLine($"Running test template in file '{template.FileName}' with path '{path}'...");

					var pos = path.IndexOf(" <that> ");
					var input = path.Substring(0, pos);

					var request = new Request(input, user, bot);
					var process = new RequestProcess(new RequestSentence(request, input), 0, true);
					var text = template.Content.Evaluate(process);
					user.Responses.Add(new Response(request, text));

					foreach (var (name, result) in process.TestResults) {
						tests[name] = result;
					}
				}

				int passes = 0, failures = 0, i = 1;
				Console.WriteLine();
				Console.WriteLine("Test results:");
				Console.WriteLine();
				foreach (var (name, result) in tests) {
					Console.ForegroundColor = ConsoleColor.White;
					Console.Write((i++).ToString().PadLeft(4));
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
				if (failures == 1) Console.Write(" test failed.");
				else Console.Write(" tests failed.");
				Console.WriteLine();
			} else {
				while (true) {
					Console.Write("> ");
					var message = Console.ReadLine();
					var trace = false;
					if (message.StartsWith(".trace ")) {
						trace = true;
						message = message.Substring(7);
					}
					var response = bot.Chat(new Request(message, user, bot), trace);
				}
			}

			return 0;
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
	}
}
