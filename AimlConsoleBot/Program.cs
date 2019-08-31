using Aiml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AimlConsoleBot {
	internal class Program {
		internal static int Main(string[] args) {
			bool switches = true; string? botPath = null;
			var inputs = new List<string>();
			var sraixServicePaths = new List<string>();

			for (int i = 0; i < args.Length; ++i) {
				var s = args[i];
				if (switches && s.StartsWith("-")) {
					if (s == "--")
						switches = false;
					else if (s == "-i" || s == "--input")
						inputs.Add(args[++i]);
					else if (s == "-s" || s == "--services")
						sraixServicePaths.Add(args[++i]);
				} else {
					switches = false;
					botPath = s;
				}
			}
			if (botPath == null) {
				Console.Error.WriteLine("Usage: AimlConsoleBot <bot path>");
				return 1;
			}

			var bot = new Bot(botPath);
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

			var user = new User("User", bot);

			foreach (var s in inputs) {
				Console.WriteLine("> " + s);
				bot.Chat(new Request(s, user, bot), false);
			}

			while (true) {
				Console.Write("> ");
				var message = Console.ReadLine();
				var trace = false;
				if (message.StartsWith(".trace ")) {
					trace = true;
					message = message.Substring(7);
				}
				var response = bot.Chat(new Request(message, user, bot), trace);
				Console.WriteLine($"{bot.Properties.GetValueOrDefault("name", "Robot")}: {response}");
			}
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
