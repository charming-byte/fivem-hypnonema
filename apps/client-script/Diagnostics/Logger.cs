using System;
using System.Linq;
using Hypnonema.Shared.Diagnostics;

namespace Hypnonema.Client.Diagnostics;

public sealed class Logger(string prefix = "") : ILogger
{
    private static LogLevel logLevel = LogLevel.Error;

    public string Prefix { get; private set; } = prefix;

    public void Debug(string message)
    {
        Log(message, LogLevel.Debug);
    }

    public void Error(Exception exception)
    {
        Error(exception, "ERROR");
    }

    public void Error(Exception exception, string message)
    {
        Log($"{message}: {exception.Message}", LogLevel.Error);
        Log($"{message}: {exception.StackTrace}", LogLevel.Error);
        // TODO: Output more details
    }

    public void Information(string message)
    {
        Log(message, LogLevel.Information);
    }

    public void Log(string message, LogLevel level)
    {
        if (logLevel > level) return;

        var output = $"{DateTime.Now:HH:mm:ss} [{level}]";

        if (!string.IsNullOrEmpty(Prefix)) output += $" [{Prefix}]";

        var lines = message.Split(new[]
        {
            '\r',
            '\n'
        }, StringSplitOptions.RemoveEmptyEntries);

        var formattedMessage = string.Join(Environment.NewLine, lines.Select(l => $"{output} {l}"));
        CitizenFX.Core.Debug.WriteLine($"{formattedMessage}{Environment.NewLine}");
    }

    public void SetPrefix(string prefix)
    {
        Prefix = prefix;
    }

    public void Verbose(string message)
    {
        Log(message, LogLevel.Verbose);
    }

    public void Warning(string message)
    {
        Log(message, LogLevel.Warning);
    }

    public static void Init(LogLevel logLevel)
    {
        Logger.logLevel = logLevel;
    }
}