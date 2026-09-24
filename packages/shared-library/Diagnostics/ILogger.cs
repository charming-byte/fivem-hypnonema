using System;

namespace Hypnonema.Shared.Diagnostics;

public interface ILogger
{
    string Prefix { get; }

    void Debug(string message);

    void Error(Exception exception);

    void Error(Exception exception, string message);

    void Information(string message);

    void Log(string message, LogLevel level);

    void SetPrefix(string prefix);

    void Verbose(string message);

    void Warning(string message);
}