using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace Aiml {
	public partial class TemplateNode {
		/// <summary>
		///     Sends the content as a command to a system command interpreter and returns the output of the command via standard output and standard error.
		/// </summary>
		/// <remarks>
		///     On Windows, the command interpreter used is cmd.exe:
		///         cmd.exe /Q /D /C "command"
		///     On UNIX, the command interpreter used is sh:
		///         /bin/sh command
		///     This element is defined by the AIML 1.1 specification.
		/// </remarks>
		public sealed class System : TemplateNode {
			public TemplateElementCollection Command { get; private set; }

			public System(TemplateElementCollection command) {
				this.Command = command;
			}

			public override string Evaluate(RequestProcess process) {
				string command = this.Command.Evaluate(process);

				Process process2 = new Process();
				if (Environment.OSVersion.Platform < PlatformID.Unix) {
					// Windows
					process2.StartInfo = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"), "/Q /D /C \"" +
						Regex.Replace(command, @"[/\\:*?""<>^]", "^$0") + "\"");
					//    /C string   Carries out the command specified by string and then terminates.
					//    /Q          Turns echo off.
					//    /D          Disable execution of AutoRun commands from registry (see 'CMD /?').
				} else if (Environment.OSVersion.Platform == PlatformID.Unix) {
					// UNIX
					process2.StartInfo = new ProcessStartInfo(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), "bin", "sh"),
						command.Replace(@"\", @"\\").Replace("\"", "\\\""));
				}
				process2.StartInfo.UseShellExecute = false;
				process2.StartInfo.RedirectStandardOutput = true;
				process2.StartInfo.RedirectStandardError = true;

				process.Log(LogLevel.Diagnostic, $"In element <system>: executing {process2.StartInfo.FileName} {process2.StartInfo.Arguments}");

				process2.Start();

				string output = process2.StandardOutput.ReadToEnd();
				string output2 = process2.StandardError.ReadToEnd();
				process2.WaitForExit((int) process.Bot.Config.Timeout);

				if (!process2.HasExited)
					process.Log(LogLevel.Diagnostic, $"In element <system>: the process timed out.");
				else if (process2.ExitCode != 0)
					process.Log(LogLevel.Diagnostic, $"In element <system>: the process exited with code {process2.ExitCode}.");

				return output;
			}

			public static System FromXml(XmlNode node, AimlLoader loader) {
				return new System(TemplateElementCollection.FromXml(node, loader));
			}

		}
	}
}
