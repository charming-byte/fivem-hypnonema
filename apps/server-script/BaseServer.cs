using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Hypnonema.Server.Communications;
using Hypnonema.Server.Configurations;
using Hypnonema.Server.Diagnostics;
using Hypnonema.Server.Media;
using Hypnonema.Server.Media.Consumer;
using Hypnonema.Server.Rpc;
using Hypnonema.Server.RpcControllers;
using Hypnonema.Server.Utilities;
using Hypnonema.Shared;
using Hypnonema.Shared.Communications;
using Serilog;
using Serilog.Events;
using Logger = Serilog.Core.Logger;

namespace Hypnonema.Server;

#pragma warning disable CS8618 // Disabling "Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable" because an instance of BaseServer becomes always created by the server.

public class BaseServer : BaseScript
{
    public static BaseServer Instance;

    public static string ResourceName;

    public static string ResourcePath;

    private readonly MediaPlayerBuilder mediaPlayerBuilder;

    private readonly MediaPlayerConsumerApi mediaPlayerConsumerApi;

    private readonly MediaPlayerController mediaPlayerController;

    private readonly MediaPlayerRegistry mediaPlayerRegistry;

    private readonly ScreenController screenController;

    private readonly ScreenRepository screenRepository;

    public BaseServer()
    {
        Instance = this;
        ResourceName = API.GetCurrentResourceName();
        ResourcePath = API.GetResourcePath(ResourceName);

        Configuration = Configuration.Load();

        Logger = new LoggerConfiguration()
            .MinimumLevel.Is((LogEventLevel)Configuration.LogLevel)
            .WriteTo.Console()
            .CreateLogger();

        EventManager = new EventManager(Logger);

        RpcManager.Init(Logger, Players, EventHandlers);
        RpcMethodBinder.Init(Configuration.PermissionsEnabled, Logger);

        TickManager = new TickManager(new SerilogLoggerAdapter(Logger), task => Tick += task, task => Tick -= task);

        screenRepository = new ScreenRepository(Configuration.Screens, Configuration.ConfigurationDirectory, Logger);

        mediaPlayerBuilder = new MediaPlayerBuilder(TickManager, Logger, Configuration.MaxQueueSize, EventManager,
            screenRepository, Configuration.YouTubeAdQuorum);

        mediaPlayerRegistry = new MediaPlayerRegistry(EventManager, Logger);

        mediaPlayerController = new MediaPlayerController(
            Logger,
            mediaPlayerBuilder,
            mediaPlayerRegistry,
            Configuration.Models,
            Configuration.Screens
        );

        mediaPlayerConsumerApi = new MediaPlayerConsumerApi(
            mediaPlayerBuilder,
            mediaPlayerRegistry,
            EventManager,
            Logger,
            Configuration.Screens,
            Configuration.Models,
            TriggerEvent
        );

        mediaPlayerConsumerApi.RegisterExports(Exports);

        screenController = new ScreenController(Logger, screenRepository);

        RpcManager.OnRequest(RpcEvents.GetConfiguration, null, m => m.Reply(Configuration));

        TickManager.RunAsync(UpdateHelper.CheckForUpdates);
    }

    public IEventManager EventManager { get; }
    public new ExportDictionary Exports => base.Exports;
    public ITickManager TickManager { get; }
    public Logger Logger { get; }
    public Configuration Configuration { get; }

    public bool IsClientConnected(IClient client)
    {
        return Players.FirstOrDefault(p => p.Handle == client.Handle) != null;
    }
}