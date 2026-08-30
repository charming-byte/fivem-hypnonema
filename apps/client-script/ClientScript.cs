using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Hypnonema.Client.Commands;
using Hypnonema.Client.Communications;
using Hypnonema.Client.Configurations;
using Hypnonema.Client.Diagnostics;
using Hypnonema.Client.Editor;
using Hypnonema.Client.Media;
using Hypnonema.Client.Media.Consumer;
using Hypnonema.Client.ObjectPools;
using Hypnonema.Client.Rpc;
using Hypnonema.Client.Serialization;
using Hypnonema.Client.UI;
using Hypnonema.Shared;
using Hypnonema.Shared.Diagnostics;
using Hypnonema.Shared.Extensions;
using Hypnonema.Shared.Media;
using Hypnonema.Shared.Serialization;

public class ClientScript : BaseScript
{
    private bool isInitialized;

    private Configuration configuration;
    private ClientRuntime runtime;
    private MediaPlayerManager mediaPlayerManager;

    private CommandParser commandParser;
    private UserInterface userInterface;
    private ScreenEditorSession screenEditor;

    public ClientScript()
    {
        RpcManager.Init(EventHandlers);

        AddSerializationConverters();
        Tick += OnFirstTick;
    }

    private async Task OnFirstTick()
    {
        Tick -= OnFirstTick;
        await InitializeClient();
    }

    private async Task InitializeClient()
    {
        if (isInitialized)
            return;

        try
        {
            var configuration = await GetConfiguration();

            Logger.Init(configuration.LogLevel);
            ScaleformPool.Init(configuration.ScaleformLimit);

            var mediaPlayerDtos = await GetExistingMediaPlayers();

            var runtime = CreateRuntime();

            var mediaPlayers = CreateMediaPlayers(
                mediaPlayerDtos,
                configuration,
                runtime);

            var mediaPlayerManager = new MediaPlayerManager(
                runtime.Logger,
                mediaPlayers,
                runtime.TickManager,
                configuration,
                runtime.EventManager);

            RegisterConsumerApi(
                mediaPlayerManager,
                runtime.EventManager,
                Exports);

            var userInterface = new UserInterface(
                mediaPlayerManager,
                runtime.TickManager,
                configuration,
                TriggerEvent,
                runtime.Logger);

            var commandParser = new CommandParser(
                configuration.CommandName,
                TriggerEvent);

            var screenEditor = new ScreenEditorSession(
                runtime.TickManager,
                runtime.Logger,
                TriggerEvent,
                UserInterface.Emit,
                userInterface.Hide,
                userInterface.Show,
                () => configuration.Screens);

            this.runtime = runtime;
            this.configuration = configuration;
            this.userInterface = userInterface;
            this.commandParser = commandParser;
            this.screenEditor = screenEditor;
            this.mediaPlayerManager = mediaPlayerManager;

            RegisterCommands();

            EventHandlers["onResourceStop"] += new Action<string>(resourceName =>
            {
                if (resourceName == API.GetCurrentResourceName()) screenEditor.Stop();
            });

            isInitialized = true;

            Debug.WriteLine("[Hypnonema] Client initialization finished.");
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"[Hypnonema] Client initialization failed: {exception}");

            throw;
        }
    }

    private ClientRuntime CreateRuntime()
    {
        var logger = new Logger();

        var tickManager = new TickManager(
            logger,
            task => Tick += task,
            task => Tick -= task);

        var eventManager = new EventManager(new Logger("Events"));

        return new ClientRuntime(
            logger,
            tickManager,
            eventManager);
    }

    private static async Task<Configuration> GetConfiguration()
    {
        Debug.WriteLine("[Hypnonema] Requesting configuration from server...");

        var configuration =
            await RpcManager.Request<Configuration>(
                RpcEvents.GetConfiguration);

        Debug.WriteLine("[Hypnonema] Configuration received.");

        return configuration;
    }

    private static async Task<List<MediaPlayerDTO>> GetExistingMediaPlayers()
    {
        Debug.WriteLine("[Hypnonema] Requesting existing media players from server...");

        var mediaPlayerDtos =
            await RpcManager.Request<List<MediaPlayerDTO>>(
                RpcEvents.GetMediaPlayers);

        Debug.WriteLine($"[Hypnonema] Received {mediaPlayerDtos.Count} media player(s).");

        return mediaPlayerDtos;
    }

    private static List<MediaPlayer> CreateMediaPlayers(
        IEnumerable<MediaPlayerDTO> mediaPlayerDtos,
        Configuration configuration,
        ClientRuntime runtime)
    {
        return mediaPlayerDtos
            .Select(dto =>
            {
                var options = MediaPlayerOptions.FromDTO(
                    dto,
                    configuration.MaxQueueSize,
                    runtime.TickManager,
                    runtime.EventManager);

                return new MediaPlayer(
                    configuration,
                    options);
            })
            .ToList();
    }

    private static void RegisterConsumerApi(
        MediaPlayerManager mediaPlayerManager,
        EventManager eventManager,
        ExportDictionary exports)
    {
        var consumerApi = new MediaPlayerConsumerApi(
            mediaPlayerManager,
            eventManager,
            TriggerEvent);

        consumerApi.RegisterExports(exports);
    }

    private void RegisterCommands()
    {
        API.RegisterCommand(
            configuration.CommandName,
            new Action<int, List<dynamic>, string>(OnCommand),
            configuration.CommandRestricted);
        TriggerEvent("chat:addSuggestion", $"/{configuration.CommandName}", "Shows the Hypnonema interface.", new object[] { });

        RegisterSubcommands();
    }

    private void RegisterSubcommands()
    {
        commandParser.Register(
            "setVolume",
            HandleSetVolume,
            "Sets the volume of the nearest media player in range.",
            [
                new CommandParameter(
                    "volume",
                    "Volume in percent (0-100)")
            ]);

        commandParser.Register(
            "screen-editor",
            HandleScreenEditor,
            "Opens the client-only spatial screen editor (run again to close it).");

        commandParser.Register(
            "stop",
            HandleStop,
            "Stops the nearest media player in range.");

        commandParser.Register(
            "pause",
            HandlePause,
            "Pauses the nearest media player in range.");

        commandParser.Register(
            "play",
            HandlePlay,
            "Plays the nearest media player in range.",
            [
                new CommandParameter(
                    "url",
                    "URL of the media to play")
            ]);
    }

    private bool HandlePause(
        IReadOnlyList<object> args,
        out string? errorMessage)
    {
        var mediaPlayer = GetNearestMediaPlayerInRange();
        if (mediaPlayer == null)
        {
            errorMessage = "No media player in range.";
            
            return false;
        }

        mediaPlayer.EmitPause(true);

        errorMessage = null;
        return true;
    }

    private bool HandleStop(
        IReadOnlyList<object> args,
        out string? errorMessage)
    {
        var mediaPlayer = GetNearestMediaPlayerInRange();
        if (mediaPlayer == null)
        {
            errorMessage = "No media player in range.";

            return false;
        }

        mediaPlayer.EmitStop();

        errorMessage = null;
        return true;
    }

    private bool HandlePlay(
        IReadOnlyList<object> args, 
        out string? errorMessage)
    {
        if (args.Count != 1)
        {
            errorMessage =
                $"Usage: /{configuration.CommandName} play <url>";

            return false;
        }

        if (!args[0].ToString().IsValidUrl())
        {
            errorMessage = "Invalid URL.";

            return false;
        }

        var mediaPlayer = GetNearestMediaPlayerInRange();
        if (mediaPlayer == null)
        {
            errorMessage = "No mediaPlayer in range.";
            
            return false;
        }

        RpcManager.Emit(RpcEvents.CreateMediaPlayer, mediaPlayer.Track, mediaPlayer.Target);
        
        errorMessage = null;
        return true;
    }

    private bool HandleScreenEditor(
        IReadOnlyList<object> args,
        out string? errorMessage)
    {
        // Running the command again while a session is active closes it (ticket 05); the toggle guarantees
        // two sessions can never stack.
        screenEditor.Toggle();

        errorMessage = null;
        return true;
    }

    private void OnCommand(
        int source,
        List<dynamic> args,
        string raw)
    {
        if (args.Count == 0)
        {
            userInterface.Show();
            return;
        }

        var result = commandParser.Dispatch(args);

        if (result.Outcome is
            CommandDispatchOutcome.UnknownSubcommand or
            CommandDispatchOutcome.InvalidArgument)
            NotifyCommandError(result.ErrorMessage);
    }

    private bool HandleSetVolume(
        IReadOnlyList<object> args,
        out string? errorMessage)
    {
        if (args.Count != 1)
        {
            errorMessage =
                $"Usage: /{configuration.CommandName} setVolume <0-100>";

            return false;
        }

        if (!int.TryParse(
                args[0]?.ToString(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var volumePercent) ||
            volumePercent is < 0 or > 100)
        {
            errorMessage =
                "Volume must be a whole number between 0 and 100.";

            return false;
        }

        var mediaPlayer = GetNearestMediaPlayerInRange();

        if (mediaPlayer is null)
        {
            errorMessage = "No media player in range.";
            return false;
        }

        mediaPlayer.SetBaseVolume(volumePercent / 100f);

        errorMessage = null;
        return true;
    }

    private MediaPlayer? GetNearestMediaPlayerInRange()
    {
        return mediaPlayerManager
            .GetMediaPlayers()
            .Where(mediaPlayer => mediaPlayer.IsInRange())
            .OrderBy(mediaPlayer => mediaPlayer.GetDistance())
            .FirstOrDefault();
    }

    private static void NotifyCommandError(string errorMessage)
    {
        TriggerEvent(
            "chat:addMessage",
            new
            {
                args = new[]
                {
                    $"[Hypnonema] {errorMessage}"
                }
            });
    }

    private static void AddSerializationConverters()
    {
        Serializer.Settings.Converters.Add(
            new TrackConverter());

        Serializer.NuiSerializerSettings.Converters.Add(
            new TrackConverter());

        Serializer.Settings.Converters.Add(
            new ClientConverter());
    }

    private sealed record ClientRuntime(
        ILogger Logger,
        TickManager TickManager,
        EventManager EventManager);
}