using Aiml;
using Aiml.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AimlConsoleBot;
internal class Program {
	internal static int Main(string[] args) {
		var switches = true; string? botPath = null;
		var extensionPaths = new List<string>();
		var inputs = new List<string>();
		List<Reply>? replies = null;

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
						Console.WriteLine($"Usage: {nameof(AimlConsoleBot)} [switches] <bot path>");
						Console.WriteLine("Available switches:");
						Console.WriteLine("  -e [path], --extension [path]: Load AIML extensions from the specified assembly.");
						Console.WriteLine("  --: Stop processing switches.");
						return 0;
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
						Console.Error.WriteLine($"Use `{nameof(AimlConsoleBot)} --help` for more information.");
						return 1;
				}
			} else {
				switches = false;
				botPath = s;
			}
		}
		if (botPath == null) {
			Console.Error.WriteLine($"Usage: {nameof(AimlConsoleBot)} [switches] <bot path>");
			Console.Error.WriteLine($"Use `{nameof(AimlConsoleBot)} --help` for more information.");
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
		var botName = bot.Properties.GetValueOrDefault("name", "Robot");
		var user = new User("User", bot);

		foreach (var s in inputs) {
			Console.WriteLine("> " + s);
			bot.Chat(new Request(s, user, bot), false);
		}

		while (true) {
			Console.Write("> ");
			var input = Console.ReadLine();
			if (input is null) break;

			var trace = false;
			if (input.StartsWith("/")) {
				if (input.StartsWith("/trace ")) {
					trace = true;
					input = input[7..];
				} else if (int.TryParse(input[1..], out var n)) {
					if (replies is not null && n >= 0 && n < replies.Count) {
						input = replies[n].Postback;
					} else {
						Console.WriteLine("No such reply.");
						continue;
					}
				}
			}

			var response = bot.Chat(new Request(input, user, bot), trace);
			var messages = response.ToMessages();
			replies = null;
			foreach (var message in messages) {
				Console.WriteLine($"{botName}: {message}");
				if (message.BlockElements.OfType<Reply>().Any()) {
					replies ??= new();
					Console.ForegroundColor = ConsoleColor.DarkMagenta;
					Console.WriteLine($"[Replies (type /number to reply): {string.Join(", ", message.BlockElements.OfType<Reply>().Select(r => {
						var s = $"({replies.Count}) {r.Text}";
						replies.Add(r);
						return s;
					}))}]");
					Console.ResetColor();
				}
			}
		}

		return 0;
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
}
