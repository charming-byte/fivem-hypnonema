using System;
using System.Threading.Tasks;
using Hypnonema.Client.Configurations;
using Hypnonema.Client.Rpc;
using Hypnonema.Shared;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Diagnostics;

namespace Hypnonema.Client.Media;

public abstract class MediaPlayerVisibilityState
{
    protected Configuration Configuration;
    protected IEventManager EventManager;
    protected ILogger Logger;

    protected MediaPlayer MediaPlayer;

    protected ITickManager TickManager;

    protected MediaPlayerVisibilityState(MediaPlayer mediaPlayer, ILogger logger, ITickManager tickManager,
        IEventManager eventManager, Configuration configuration)
    {
        Logger = logger;
        MediaPlayer = mediaPlayer;
        TickManager = tickManager;
        EventManager = eventManager;
        Configuration = configuration;
    }

    protected void On(string @event, Delegate handler)
    {
        EventManager.On(MediaPlayer.GetScopedEvent(@event), handler);
    }

    protected void Off(string @event, Delegate handler)
    {
        EventManager.Off(MediaPlayer.GetScopedEvent(@event), handler);
    }

    protected void EmitRpc(string @event, params object[] args)
    {
        RpcManager.Emit(MediaPlayer.GetScopedEvent(@event), args);
    }

    public abstract Task OnTick();

    public virtual void OnEnterState()
    {
        Logger.Debug($"Entering {GetType().Name}");

        TickManager.Add(OnTick);
    }

    public virtual void OnExitState()
    {
        Logger.Debug($"Exiting {GetType().Name}");

        TickManager.Remove(OnTick);
    }
}