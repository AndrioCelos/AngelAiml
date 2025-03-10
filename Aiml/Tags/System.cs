using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

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
public sealed partial class System(TemplateElementCollection children) : RecursiveTemplateTag(children) {
	public override string Evaluate(RequestProcess process) {
		if (!process.Bot.Config.EnableSystem) {
			LogDisabled(GetLogger(process, true));
			return process.Bot.Config.SystemFailedMessage;
		}

		var command = EvaluateChildren(process);

		try {
			var process2 = new Process();
			if (Environment.OSVersion.Platform < PlatformID.Unix) {
				// Windows
				process2.StartInfo = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"), "/Q /D /C \"" +
					WindowsEscapeRegex().Replace(command, "^$0") + "\"");
				//    /C string   Carries out the command specified by string and then terminates.
				//    /Q          Turns echo off.
				//    /D          Disable execution of AutoRun commands from registry (see 'CMD /?').
			} else if (Environment.OSVersion.Platform == PlatformID.Unix) {
				// UNIX
				process2.StartInfo = new ProcessStartInfo(Path.Combine(Path.GetPathRoot(Environment.SystemDirectory)!, "bin", "sh"),
					command.Replace(@"\", @"\\").Replace("\"", "\\\""));
			} else {
				LogPlatformNotSupported(GetLogger(process, true), Environment.OSVersion.Platform);
 				return process.Bot.Config.SystemFailedMessage;
			}

			process2.StartInfo.UseShellExecute = false;
			process2.StartInfo.RedirectStandardOutput = true;
			process2.StartInfo.RedirectStandardError = true;

			LogExecuting(GetLogger(process), process2.StartInfo.FileName, process2.StartInfo.Arguments);

			process2.Start();

			var output = process2.StandardOutput.ReadToEnd();
			process2.StandardError.ReadToEnd();
			process2.WaitForExit((int) process.Bot.Config.Timeout);

			if (!process2.HasExited)
				LogTimeout(GetLogger(process, true));
			else if (process2.ExitCode != 0)
				LogExit(GetLogger(process), process2.ExitCode);

			return output;
		} catch (Exception ex) {
			LogProcessException(GetLogger(process, true), ex);
			return process.Bot.Config.SystemFailedMessage;
		}
	}

#if NET8_0_OR_GREATER
	[GeneratedRegex(@"[/\\:*?""<>^]")]
	private static partial Regex WindowsEscapeRegex();
#else
	private static readonly Regex windowsEscapeRegex = new Regex(@"[/\\:*?""<>^]", RegexOptions.Compiled);
	private static Regex WindowsEscapeRegex() => windowsEscapeRegex;
#endif

	#region Log templates

	[LoggerMessage(LogLevel.Warning, "In element <system>: This element is disabled.")]
	private static partial void LogDisabled(ILogger logger);

	[LoggerMessage(LogLevel.Warning, "In element <system>: This element is not supported on {Platform}.")]
	private static partial void LogPlatformNotSupported(ILogger logger, PlatformID platform);

	[LoggerMessage(LogLevel.Trace, "In element <system>: executing {FileName} {Arguments}")]
	private static partial void LogExecuting(ILogger logger, string fileName, string arguments);

	[LoggerMessage(LogLevel.Warning, "In element <system>: the process timed out.")]
	private static partial void LogTimeout(ILogger logger);

	[LoggerMessage(LogLevel.Trace, "In element <system>: the process exited with code {ExitCode}.")]
	private static partial void LogExit(ILogger logger, int exitCode);

	[LoggerMessage(LogLevel.Warning, "In element <system>: exception running a process")]
	private static partial void LogProcessException(ILogger logger, Exception ex);

	#endregion
}
