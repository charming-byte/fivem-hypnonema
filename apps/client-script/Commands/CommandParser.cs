using System;
using System.Collections.Generic;
using System.Linq;

namespace Hypnonema.Client.Commands;

public enum CommandDispatchOutcome
{
    NoSubcommand,

    Handled,

    UnknownSubcommand,

    InvalidArgument
}

public readonly struct CommandDispatchResult
{
    public static readonly CommandDispatchResult NoSubcommand = new(CommandDispatchOutcome.NoSubcommand, null!);
    public static readonly CommandDispatchResult Handled = new(CommandDispatchOutcome.Handled, null!);

    private CommandDispatchResult(CommandDispatchOutcome outcome, string errorMessage)
    {
        Outcome = outcome;
        ErrorMessage = errorMessage;
    }

    public static CommandDispatchResult UnknownSubcommand(string errorMessage)
    {
        return new CommandDispatchResult(CommandDispatchOutcome.UnknownSubcommand, errorMessage);
    }

    public static CommandDispatchResult InvalidArgument(string errorMessage)
    {
        return new CommandDispatchResult(CommandDispatchOutcome.InvalidArgument, errorMessage);
    }

    public CommandDispatchOutcome Outcome { get; }

    public string ErrorMessage { get; }
}

public sealed class CommandParameter
{
    public CommandParameter(string name, string help)
    {
        Name = name;
        Help = help;
    }

    public string Name { get; }

    public string Help { get; }
}

public sealed class CommandParser
{
    public delegate bool SubcommandHandler(IReadOnlyList<object> args, out string errorMessage);

    private readonly string commandName;
    private readonly Dictionary<string, SubcommandHandler> handlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Action<string, object[]> triggerEvent;

    public CommandParser(string commandName, Action<string, object[]> triggerEvent)
    {
        this.commandName = commandName ?? throw new ArgumentNullException(nameof(commandName));
        this.triggerEvent = triggerEvent ?? throw new ArgumentNullException(nameof(triggerEvent));
    }

    public void Register(string subcommand, SubcommandHandler handler, string helpText = "",
        IReadOnlyList<CommandParameter>? parameters = null)
    {
        handlers[subcommand] = handler ?? throw new ArgumentNullException(nameof(handler));

        var suggestionParameters = (parameters ?? Array.Empty<CommandParameter>())
            .Select(parameter => (object)new { name = parameter.Name, help = parameter.Help })
            .ToArray();

        triggerEvent("chat:addSuggestion",
            new object[] { $"/{commandName} {subcommand}", helpText, suggestionParameters });
    }

    public CommandDispatchResult Dispatch(IReadOnlyList<object> args)
    {
        if (args == null || args.Count == 0) return CommandDispatchResult.NoSubcommand;

        var subcommand = args[0]?.ToString() ?? string.Empty;

        if (!handlers.TryGetValue(subcommand, out var handler))
            return CommandDispatchResult.UnknownSubcommand(
                $"Unknown command \"{subcommand}\". Use /{commandName} for the menu.");

        var subcommandArgs = args.Skip(1).ToList();

        return handler(subcommandArgs, out var errorMessage)
            ? CommandDispatchResult.Handled
            : CommandDispatchResult.InvalidArgument(errorMessage);
    }
}