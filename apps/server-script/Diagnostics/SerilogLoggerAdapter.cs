using System;
using Serilog.Events;
using ILogger = Hypnonema.Shared.Diagnostics.ILogger;
using LogLevel = Hypnonema.Shared.Diagnostics.LogLevel;

namespace Hypnonema.Server.Diagnostics;

public sealed class SerilogLoggerAdapter(Serilog.ILogger logger) : ILogger
{
    public string Prefix { get; private set; } = string.Empty;

    public void Debug(string message)
    {
        logger.Debug(Format(message));
    }

    public void Error(Exception exception)
    {
        logger.Error(exception, Format("ERROR"));
    }

    public void Error(Exception exception, string message)
    {
        logger.Error(exception, Format(message));
    }

    public void Information(string message)
    {
        logger.Information(Format(message));
    }

    public void Log(string message, LogLevel level)
    {
        logger.Write(ToSerilogLevel(level), Format(message));
    }

    public void SetPrefix(string prefix)
    {
        Prefix = prefix;
    }

    public void Verbose(string message)
    {
        logger.Verbose(Format(message));
    }

    public void Warning(string message)
    {
        logger.Warning(Format(message));
    }

    private string Format(string message)
    {
        return string.IsNullOrEmpty(Prefix) ? message : $"[{Prefix}] {message}";
    }

    private static LogEventLevel ToSerilogLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Verbose => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            _ => LogEventLevel.Information
        };
    }
}
