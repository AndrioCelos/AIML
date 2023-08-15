using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Aiml.Tags;
/// <summary>Sends the content as a command to a system command interpreter and returns the output of the command via standard output.</summary>
/// <remarks>
///		<para>The command interpreter used depends on the platform. The currently supported platforms are as follows:</para>
///		<list type="table">
///			<listheader>
///				<term>Platform</term>
///				<description>Command interpreter</description>
///			</listheader>
///			<item>
///				<term>Windows</term>
///				<description><c>cmd.exe /Q /D /C "command"</c></description>
///			</item>
///			<item>
///				<term>UNIX</term>
///				<description><c>/bin/sh command</c></description>
///			</item>
///		</list>
///		<para>This element is defined by the AIML 1.1 specification.</para>
/// </remarks>
/// <seealso cref="SraiX"/>
public sealed class System(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) {
		var command = this.EvaluateChildren(process);

		var process2 = new Process();
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
		} else
			throw new PlatformNotSupportedException($"The system element is not supported on {Environment.OSVersion.Platform}.");

		process2.StartInfo.UseShellExecute = false;
		process2.StartInfo.RedirectStandardOutput = true;
		process2.StartInfo.RedirectStandardError = true;

		process.Log(LogLevel.Diagnostic, $"In element <system>: executing {process2.StartInfo.FileName} {process2.StartInfo.Arguments}");

		process2.Start();

		var output = process2.StandardOutput.ReadToEnd();
		process2.StandardError.ReadToEnd();
		process2.WaitForExit((int) process.Bot.Config.Timeout);

		if (!process2.HasExited)
			process.Log(LogLevel.Diagnostic, $"In element <system>: the process timed out.");
		else if (process2.ExitCode != 0)
			process.Log(LogLevel.Diagnostic, $"In element <system>: the process exited with code {process2.ExitCode}.");

		return output;
	}
}
