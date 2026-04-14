using System;
using Godot;
using Microsoft.Extensions.Logging;

namespace BattleTank.Godot.CrashReport;

public sealed class GodotLoggerFactory : ILoggerFactory
{
    private readonly CrashReporter _crashReporter;

    public GodotLoggerFactory(CrashReporter crashReporter)
    {
        _crashReporter = crashReporter;
    }

    public ILogger CreateLogger(string categoryName) => new GodotLogger(categoryName, _crashReporter);

    public void AddProvider(ILoggerProvider provider) { }

    public void Dispose() { }
}

internal sealed class GodotLogger : ILogger
{
    private readonly string _category;
    private readonly CrashReporter _crashReporter;

    public GodotLogger(string category, CrashReporter crashReporter)
    {
        _category = category;
        _crashReporter = crashReporter;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var line = $"[{logLevel}] [{_category}] {message}";
        if (exception is not null)
            line += $"\n{exception}";

        if (logLevel >= LogLevel.Warning)
            GD.PrintErr(line);
        else
            GD.Print(line);

        _crashReporter.LogLine(line);
    }
}
