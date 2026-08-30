using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Hypnonema.Server.Communications;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Media;
using Serilog;

namespace Hypnonema.Server.Media;

public sealed class MediaPlayerRegistry
{
    private readonly IEventManager eventManager;

    private readonly ILogger logger;

    private readonly ConcurrentDictionary<int, IMediaPlayer> mediaPlayers = new();

    public MediaPlayerRegistry(IEventManager eventManager, ILogger logger)
    {
        this.eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyCollection<IMediaPlayer> All => mediaPlayers.Values.ToList();

    public event Action<IMediaPlayer>? MediaPlayerAdded;

    public event Action<IMediaPlayer>? MediaPlayerRemoved;

    public IMediaPlayer? Get(Handle handle)
    {
        return mediaPlayers.TryGetValue(handle.Value, out var mediaPlayer) ? mediaPlayer : null;
    }

    public IEnumerable<IMediaPlayer> GetByName(string name)
    {
        return mediaPlayers.Values.Where(mp => string.Equals(mp.Label, name, StringComparison.OrdinalIgnoreCase));
    }

    public IMediaPlayer? FindByTarget(Target target)
    {
        return mediaPlayers.Values.FirstOrDefault(mp => Target.AreEqual(mp.Target, target));
    }

    public void Add(IMediaPlayer mediaPlayer)
    {
        if (!mediaPlayers.TryAdd(mediaPlayer.Handle.Value, mediaPlayer))
        {
            logger.Warning("MediaPlayer with handle '{handle}' is already registered, ignoring.",
                mediaPlayer.Handle);

            return;
        }

        eventManager.On(MediaPlayerEventScope.For(mediaPlayer.Handle, MediaPlayerEvents.Stopped),
            OnMediaPlayerStopped);

        MediaPlayerAdded?.Invoke(mediaPlayer);
    }

    private void OnMediaPlayerStopped(CommunicationMessage message, IMediaPlayer mediaPlayer)
    {
        eventManager.Off(MediaPlayerEventScope.For(mediaPlayer.Handle, MediaPlayerEvents.Stopped),
            OnMediaPlayerStopped);

        if (!mediaPlayers.TryRemove(mediaPlayer.Handle.Value, out var removed))
        {
            logger.Warning("MediaPlayer '{label}' ({handle}) stopped but was not found in the registry.",
                mediaPlayer.Label, mediaPlayer.Handle);

            return;
        }

        MediaPlayerRemoved?.Invoke(removed);

        removed.Dispose();
    }
}