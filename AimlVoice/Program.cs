using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Speech.Recognition;
using System.Speech.Recognition.SrgsGrammar;
using System.Speech.Synthesis;
using System.Text;
using Aiml;
using Aiml.Media;
using Microsoft.Extensions.Logging;

namespace AimlVoice;
internal partial class Program {
	private static readonly Argument<string> botPathArgument = new("bot path", "Path to the bot directory.");

	private static readonly Option<ICollection<string>> grammarOption = new(["-g", "--grammar"], "Enable the specified grammar upon startup.") { ArgumentHelpName = "name" };
	private static readonly Option<ICollection<string>> extensionOption = new(["-e", "--extension"], "Load AIML extensions from the specified assembly.") { ArgumentHelpName = "path"};
	private static readonly Option<string> voiceOption = new(["-V", "--voice"], "Voice to use for speech synthesis.") { ArgumentHelpName = "voice" };
	private static readonly Option<int> rateOption = new(["-r", "--rate"], () => 0, "Modify the speech rate. -10 ~ +10") { ArgumentHelpName = "rate" };
	private static readonly Option<int> volumeOption = new(["-a", "--volume"], () => 100, "Modify the speech volume. 0 ~ 100") { ArgumentHelpName = "volume" };
	private static readonly Option<bool> noSrOption = new(["-n", "--no-sr"], "Do not load the speech recogniser. Input will by typing only.");
	private static readonly Option<LogLevel> verbosityOption = new(["-v", "--verbosity"], ParseVerbosity, true, "Set the logging verbosity level.") { Arity = ArgumentArity.ZeroOrOne };

	private static readonly Command voicesSubcommand = new("--voices", "Show a list of available voices and exit.");

	private static int exitCode;
	private static ILogger? logger;
	internal static Bot? bot;
	internal static User? user;
	internal static SpeechSynthesizer? synthesizer;
	internal static Dictionary<string, Grammar> grammars = new(StringComparer.InvariantCultureIgnoreCase);
	internal static string progressMessage = "";
	internal static List<string> enabledGrammarPaths = [];
	internal static PartialInputMode partialInput;
	private static Stopwatch? partialInputTimeout;
	private static readonly List<Reply> replies = [];

	private static readonly Queue<SpeechQueueItem> speechQueue = new();

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
		var command = new RootCommand("Runs an AIML bot with speech recognition and synthesis.") {
			botPathArgument, grammarOption, extensionOption, voiceOption, rateOption, volumeOption, noSrOption, verbosityOption, voicesSubcommand
		};
		voicesSubcommand.SetHandler(RunVoices);
		command.SetHandler(Run);
		command.Invoke(args);
		return exitCode;
	}

	private static void RunVoices(InvocationContext context) {
		Console.WriteLine("Available voices:");
		foreach (var voice2 in new SpeechSynthesizer().GetInstalledVoices().Where(v => v.Enabled))
			Console.WriteLine(voice2.VoiceInfo.Name);
	}

	private static void Run(InvocationContext context) {
		var botPath = context.ParseResult.GetValueForArgument(botPathArgument);
		var defaultGrammarPath = context.ParseResult.GetValueForOption(grammarOption);
		var voice = context.ParseResult.GetValueForOption(voiceOption);
		var extensionPaths = context.ParseResult.GetValueForOption(extensionOption);
		var rate = context.ParseResult.GetValueForOption(rateOption);
		var volume = context.ParseResult.GetValueForOption(volumeOption);
		var noSr = context.ParseResult.GetValueForOption(noSrOption);

		var level = context.ParseResult.GetValueForOption(verbosityOption);
		var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(level));
		logger = loggerFactory.CreateLogger(nameof(AimlVoice));

		if (Directory.Exists(Path.Combine(botPath, "grammars"))) {
			foreach (var file in Directory.GetFiles(Path.Combine(botPath, "grammars"), "*.xml", SearchOption.AllDirectories)) {
				var grammar = new Grammar(new SrgsDocument(file));  // Grammar..ctor(string) is not implemented.
				grammars[Path.GetFileNameWithoutExtension(file)] = grammar;
			}
		} else {
			LogNoGrammars(logger, Path.Combine(botPath, "grammars"));
		}

		AimlLoader.AddExtension(new AimlVoiceExtension());
		if (extensionPaths is not null) {
			foreach (var path in extensionPaths) {
				LogLoadingExtensions(logger, path);
				AimlLoader.AddExtensions(path);
			}
		}

		bot = new Bot(botPath);
		bot.PostbackResponse += (s, e) => ProcessOutput(e.Response);
		bot.LoadConfig();
		bot.LoadAiml();

		user = new User("User", bot);
		synthesizer = new SpeechSynthesizer();
		if (voice is not null) {
			try {
				synthesizer.SelectVoice(voice);
			} catch (ArgumentException) {
				Console.Error.WriteLine($"Couldn't load the voice {voice}.");
				Console.Error.WriteLine($"Available voices:");
				foreach (var voice2 in synthesizer.GetInstalledVoices().Where(v => v.Enabled))
					Console.Error.WriteLine(voice2.VoiceInfo.Name);
				exitCode = 1;
				return;
			}
		}

		synthesizer.Rate = rate;
		synthesizer.Volume = volume;
		synthesizer.SpeakCompleted += Synthesizer_SpeakCompleted;

		using var recognizer = noSr ? null : new SpeechRecognitionEngine(new System.Globalization.CultureInfo("en")) {
			BabbleTimeout = TimeSpan.FromSeconds(1),
			EndSilenceTimeoutAmbiguous = TimeSpan.FromSeconds(0.75)
		};

		if (recognizer is not null) {
			foreach (var entry in grammars) {
				LogLoadingGrammar(logger, entry.Key);
				entry.Value.Enabled = false;
				recognizer.LoadGrammar(entry.Value);
			}

			if (defaultGrammarPath is null || defaultGrammarPath.Count == 0) {
				LogLoadingDefaultGrammar(logger);
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
		}

		if (bot.Graphmaster.Children.TryGetValue("OOB", out var node) && node.Children.ContainsKey("START"))
			SendInput("OOB START");

		Console.Write("> ");
		while (true) {
			var message = Console.ReadLine();
			if (message is null) return;
			SendInput(message);
			Console.Write("> ");
		}
	}

	public static void SetPartialInput(PartialInputMode partialInputMode) {
		partialInput = partialInputMode;
		if (partialInputMode != PartialInputMode.On) partialInputTimeout = null;
		LogPartialInput(logger!, partialInput);
	}

	public static void TrySwitchGrammar(string name) {
		if (enabledGrammarPaths.Contains(name)) return;
		if (!grammars.TryGetValue(name, out var grammar)) {
			LogGrammarNotFound(logger!, name);
			return;
		}
		LogSwitchingGrammar(logger!, name);
		foreach (var path in enabledGrammarPaths)
			grammars[path].Enabled = false;
		enabledGrammarPaths.Clear();
		grammar.Enabled = true;
		enabledGrammarPaths.Add(name);
	}

	public static void TryDisableGrammar(string name) {
		if (!enabledGrammarPaths.Contains(name)) return;
		if (!grammars.TryGetValue(name, out var grammar)) {
			LogGrammarNotFound(logger!, name);
			return;
		}
		if (enabledGrammarPaths.Count == 1) {
			LogCannotDisableLastGrammar(logger!, name);
			return;
		}
		LogDisablingGrammar(logger!, name);
		grammar.Enabled = false;
		enabledGrammarPaths.Remove(name);
	}

	public static void TryEnableGrammar(string name) {
		if (enabledGrammarPaths.Contains(name)) return;
		if (!grammars.TryGetValue(name, out var grammar)) {
			LogGrammarNotFound(logger!, name);
			return;
		}
		LogEnablingGrammar(logger!, name);
		grammar.Enabled = true;
		enabledGrammarPaths.Add(name);
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

		if (partialInput != PartialInputMode.Off && (partialInput == PartialInputMode.Continuous || partialInputTimeout == null || partialInputTimeout.Elapsed >= TimeSpan.FromSeconds(3)) && e.Result.Confidence >= 0.25) {
			var response = bot!.Chat(new Request("PartialInput " + e.Result.Text, user!, bot), false);
			if (!response.IsEmpty) {
				partialInputTimeout = Stopwatch.StartNew();
				ProcessOutput(response);
			}
		}
	}

	private static void Recognizer_SpeechRecognitionRejected(object? sender, SpeechRecognitionRejectedEventArgs e) {
		Console.ForegroundColor = ConsoleColor.DarkMagenta;

		if (e.Result.Alternates.Count == 1 && e.Result.Alternates[0].Confidence >= 0.25) {
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine(e.Result.Alternates[0].Text + "    ");
			Console.ResetColor();
			if (partialInputTimeout is null || partialInputTimeout.Elapsed >= TimeSpan.FromSeconds(5))
				SendInput(e.Result.Alternates[0].Text);
		} else {
			WriteMessage(string.Join(" ", e.Result.Alternates.Select(a => $"({a.Text} ...? {a.Confidence})")));
			Console.ResetColor();
		}
	}

	private static void SendInput(string input) {
		var trace = false;
		if (input.StartsWith(".trace ")) {
			trace = true;
			input = input[7..];
		}
		if (replies.FirstOrDefault(r => bot!.Config.StringComparer.Equals(r.Text, input.Trim())) is Reply reply)
			input = reply.Postback;
		var response = bot!.Chat(new Request(input, user!, bot), trace);
		ProcessOutput(response);
	}

	private static void ProcessOutput(Response response) {
		if (string.IsNullOrWhiteSpace(response.Text))
			return;
		replies.Clear();

		try {
			var messages = response.ToMessages();
			foreach (var message in messages) {
				var isPriority = false;
				var builder = new PromptBuilder(bot!.Config.Locale);
				var responseBuilder = new StringBuilder();

				foreach (var el in message.InlineElements) {
					switch (el) {
						case LineBreak:
							Console.WriteLine();
							break;
						case SpeakElement speak:
							builder.AppendSsml(speak.SSML.CreateReader());
							responseBuilder.Append(speak.AltText);
							break;
						default:
							var s = el.ToString();
							responseBuilder.Append(s);
							builder.AppendText(s);
							break;
					}
				}
				foreach (var el in message.BlockElements) {
					switch (el) {
						case Reply reply:
							replies.Add(reply);
							break;
						case PriorityElement:
							isPriority = true;
							break;
					}
				}

				if (responseBuilder.Length > 0) {
					var s = responseBuilder.ToString();
					if (!string.IsNullOrWhiteSpace(s)) {
						Console.ForegroundColor = ConsoleColor.Blue;
						Console.WriteLine(s);
					}
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
					speechQueue.Enqueue(new SpeechQueueItem(prompt, isPriority));
					synthesizer!.SpeakAsync(prompt);
				}

				if (message.Separator is Delay delay) {
					Console.Write("...");
					Thread.Sleep(delay.Duration);
					Console.CursorLeft = 0;
				}
			}
		} catch (Exception ex) {
			LogExceptionProcessingResponse(logger!, ex, response);
		}
	}

	static void Recognizer_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e) {
		Console.ForegroundColor = ConsoleColor.Magenta;
		Console.WriteLine(e.Result.Text + "     ");
		Console.ResetColor();
		if (partialInput == PartialInputMode.Continuous || partialInputTimeout is null || partialInputTimeout.Elapsed >= TimeSpan.FromSeconds(5))
			SendInput(e.Result.Text);
		else
			partialInputTimeout = null;
	}

	#region Log templates

	[LoggerMessage(LogLevel.Information, "Grammars directory {Path} does not exist. Skipping loading grammars.")]
	private static partial void LogNoGrammars(ILogger logger, string path);

	[LoggerMessage(LogLevel.Information, "Loading extensions from {Path}.")]
	private static partial void LogLoadingExtensions(ILogger logger, string path);

	[LoggerMessage(LogLevel.Information, "Loading grammar '{Name}'.")]
	private static partial void LogLoadingGrammar(ILogger logger, string name);

	[LoggerMessage(LogLevel.Information, "Loading a dictation grammar.")]
	private static partial void LogLoadingDefaultGrammar(ILogger logger);

	[LoggerMessage(LogLevel.Information, "Partial input is {NewState}.")]
	private static partial void LogPartialInput(ILogger logger, PartialInputMode newState);

	[LoggerMessage(LogLevel.Warning, "Could not find requested grammar '{Name}'.")]
	private static partial void LogGrammarNotFound(ILogger logger, string name);

	[LoggerMessage(LogLevel.Information, "Switching to grammar '{Name}'.")]
	private static partial void LogSwitchingGrammar(ILogger logger, string name);

	[LoggerMessage(LogLevel.Warning, "Refusing to disable the last enabled grammar '{Name}'.")]
	private static partial void LogCannotDisableLastGrammar(ILogger logger, string name);

	[LoggerMessage(LogLevel.Information, "Disabling grammar '{Name}'.")]
	private static partial void LogDisablingGrammar(ILogger logger, string name);

	[LoggerMessage(LogLevel.Information, "Enabling grammar '{Name}'.")]
	private static partial void LogEnablingGrammar(ILogger logger, string name);

	[LoggerMessage(LogLevel.Error, "Failed to process response text: {Response}")]
	private static partial void LogExceptionProcessingResponse(ILogger logger, Exception ex, Response response);

	#endregion
}

public class SpeechQueueItem(Prompt prompt, bool important) {
	public Prompt Prompt { get; } = prompt;
	public bool Important { get; } = important;
}

public enum PartialInputMode {
	/// <summary>Partial input will not be processed.</summary>
	Off = 0,
	/// <summary>Partial input will be processed, but if there is a response to partial input, further input will be ignored for 5 seconds.</summary>
	On = 1,
	/// <summary>Partial input will be processed with no cooldown.</summary>
	Continuous = 2
}
