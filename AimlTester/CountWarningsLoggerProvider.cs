using System;
using Microsoft.Extensions.Logging;

namespace AimlTester;
internal sealed class CountWarningsLoggerProvider() : ILoggerProvider {
	public static CountWarningsLoggerProvider Instance { get; } = new();
	public ILogger CreateLogger(string categoryName) => new CountWarningsLogger();
	public void Dispose() { }
}

internal sealed class CountWarningsLogger() : ILogger {
	public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
	public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
		if (logLevel < LogLevel.Warning) return;
		Program.warnings++;
	}
}
