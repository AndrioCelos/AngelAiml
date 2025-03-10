using Microsoft.Extensions.Logging;

namespace Aiml.Tests;
internal sealed class FailOnWarningLoggerProvider(AimlTest test) : ILoggerProvider {
	public ILogger CreateLogger(string categoryName) => new FailOnWarningLogger(test);
	public void Dispose() { }
}

internal sealed class FailOnWarningLogger(AimlTest test) : ILogger {
	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
	public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
		if (logLevel < LogLevel.Warning) return;
		if (test.expectingWarning)
			test.expectingWarning = false;
		else
			Assert.Fail($"AIML request raised a warning: {formatter(state, exception)}");
	}
}
