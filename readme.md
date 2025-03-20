# Angel AIML

Angel AIML is a .NET library and accompanying software for running chat robots using [Artificial Intelligence Markup Language](http://www.aiml.foundation/).

The following projects are included in this repository:

* **Angel AIML**: the core library.
* **Angel AIML Console**: a basic command-line AIML interpreter front-end.
* **Angel AIML Tester**: a console application that runs AIML unit tests (a non-standard AIML extension).
* **Angel AIML Voice Console**: an AIML interpreter using Windows Speech Recognition and text-to-speech features.
* **ExampleBot**: a sample set of AIML and configuration files for use with the AIML library.

## Example usage

```Csharp
using AngelAiml;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var bot = new Bot(botPath, loggerFactory);
bot.LoadConfig();
bot.LoadAIML();

var user = new User("User", bot);
while (true) {
	Console.Write("> ");
	var message = Console.ReadLine();
	if (message == null) break;
	var response = bot.Chat(new Request(message, user, bot), trace);
	Console.WriteLine($"{bot.Properties.GetValueOrDefault("name", "Robot")}: {response}");
}
```

## Logging

This library uses the Microsoft.Extensions.Logging framework for logging. Logging can be configured by passing an `ILoggerFactory` to the `Bot` constructor. The [Angel Aiml Console](AngelAimlConsole) project demonstrates how to log to the console and to a file using Serilog.
