using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Aiml;
using Microsoft.Extensions.Logging;

namespace AimlTester;
internal partial class Program {
	private static readonly Argument<string> botPathArgument = new("botPath", "Path to the bot directory.");
	private static readonly Option<string> testPathOption = new(["-t", "--tests"], "Specify the path, relative to the bot directory, to look for AIML tests.") { ArgumentHelpName = "path", IsRequired = true };
	private static readonly Option<ICollection<string>> extensionOption = new(["-e", "--extension"], "Load AIML extensions from the specified assembly.") { ArgumentHelpName = "path" };
	private static readonly Option<LogLevel> verbosityOption = new(["-v", "--verbosity"], ParseVerbosity, true, "Set the logging verbosity level.") { Arity = ArgumentArity.ZeroOrOne };

	internal static int warnings;
	internal static int exitCode;
	private static ILogger? logger;

	private static LogLevel ParseVerbosity(ArgumentResult result) {
		return result.Parent is null or OptionResult { IsImplicit: true } ? LogLevel.Information
			: !result.Tokens.Any() ? LogLevel.Trace
			: result.Tokens.Single().Value.ToLowerInvariant() switch {
				"q" or "quiet" or "s" or "silent" => LogLevel.None,
				"m" or "minimal" or "w" or "warning" => LogLevel.Warning,
				"n" or "normal" or "i" or "info" or "information" => LogLevel.Information,
				"d" or "detailed" or "debug" => LogLevel.Debug,
				"diag" or "diagnostic" or "t" or "trace" => LogLevel.Trace,
				_ => throw new ArgumentException("Unknown verbosity")
			};
	}

	internal static int Main(string[] args) {
		var rootCommand = new RootCommand("Runs AIML tests for an AIML bot. Returns exit code 1 if any tests failed.") {
			botPathArgument, testPathOption, extensionOption, verbosityOption
		};
		rootCommand.SetHandler(Run, botPathArgument, testPathOption, extensionOption, verbosityOption);
		rootCommand.Invoke(args);
		return exitCode;
	}

	private static void Run(string botPath, string testPath, ICollection<string> extensionPaths, LogLevel logLevel) {
		foreach (var path in extensionPaths) {
			LogLoadingExtensions(logger!, path);
			AimlLoader.AddExtensions(path);
		}

		var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().AddProvider(CountWarningsLoggerProvider.Instance).SetMinimumLevel(logLevel));
		logger = loggerFactory.CreateLogger(nameof(AimlTester));
		var bot = new Bot(botPath);
		bot.LoadConfig();
		bot.LoadAiml();
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
			LogRunningTest(logger, template.Uri, template.LineNumber, path);

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

		if (failures > 0) exitCode = 1;
	}

	#region Log templates

	[LoggerMessage(LogLevel.Information, "Loading extensions from {Path}.")]
	private static partial void LogLoadingExtensions(ILogger logger, string path);

	[LoggerMessage(LogLevel.Information, "Running test template in {Uri} line {LineNumber} with path '{Path}'.")]
	private static partial void LogRunningTest(ILogger logger, string? uri, int lineNumber, string path);

	#endregion
}
