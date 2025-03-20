using AngelAiml;
using AngelAiml.Media;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;

namespace AngelAimlConsole;
internal class Program {
	private static readonly Argument<string> botPathArgument = new("botPath", "Path to the bot directory.");
	private static readonly Option<ICollection<string>> extensionOption = new(["-e", "--extension"], "Load AIML extensions from the specified assembly.") { ArgumentHelpName = "path" };
	private static readonly Option<LogLevel> verbosityOption = new(["-v", "--verbosity"], ParseVerbosity, true, "Set the logging verbosity level.") { Arity = ArgumentArity.ZeroOrOne };

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

	internal static void Main(string[] args) {
		var rootCommand = new RootCommand("Runs an AIML bot on the console.") {
			botPathArgument, extensionOption, verbosityOption
		};
		rootCommand.SetHandler(Run, botPathArgument, extensionOption, verbosityOption);
		rootCommand.Invoke(args);
	}

	private static void Run(string botPath, ICollection<string> extensionPaths, LogLevel verbosity) {
		var inputs = new List<string>();
		List<Reply>? replies = null;

		foreach (var path in extensionPaths) {
			Console.WriteLine($"Loading extensions from {path}...");
			AimlLoader.AddExtensions(path);
		}

		var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().AddFile("logs/{Date}.log").SetMinimumLevel(verbosity));
		var bot = new Bot(botPath, loggerFactory);
		bot.LoadConfig();
		bot.LoadAiml();
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
			if (input.StartsWith('/')) {
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
			Console.WriteLine($"{botName}: {response}");
			var messages = response.ToMessages();
			replies = null;
			foreach (var message in messages) {
				if (message.BlockElements.OfType<Reply>().Any()) {
					replies ??= [];
					Console.ForegroundColor = ConsoleColor.DarkMagenta;
					Console.WriteLine($"[Replies (type /number to reply): {string.Join(", ", message.BlockElements.OfType<Reply>().Select(r => {
						var s = $"(/{replies.Count}) {r.Text}";
						replies.Add(r);
						return s;
					}))}]");
					Console.ResetColor();
				}
			}
		}
	}
}
