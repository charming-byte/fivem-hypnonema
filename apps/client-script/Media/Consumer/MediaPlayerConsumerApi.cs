using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Hypnonema.Client.Communications;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Media;
using Hypnonema.Shared.Media.Consumer;
using Hypnonema.Shared.Serialization;

namespace Hypnonema.Client.Media.Consumer;

public sealed class MediaPlayerConsumerApi
{
    private readonly IEventManager eventManager;
    private readonly Dictionary<int, List<KeyValuePair<string, Delegate>>> eventSubscriptionsByHandle = new();

    private readonly MediaPlayerManager mediaPlayerManager;

    private readonly Action<string, object[]> triggerEvent;

    public MediaPlayerConsumerApi(MediaPlayerManager mediaPlayerManager, IEventManager eventManager,
        Action<string, object[]> triggerEvent)
    {
        this.mediaPlayerManager = mediaPlayerManager ?? throw new ArgumentNullException(nameof(mediaPlayerManager));
        this.eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        this.triggerEvent = triggerEvent ?? throw new ArgumentNullException(nameof(triggerEvent));

        this.mediaPlayerManager.MediaPlayerAdded += OnMediaPlayerAdded;
        this.mediaPlayerManager.MediaPlayerRemoved += OnMediaPlayerRemoved;
    }

    public IReadOnlyList<ConsumerMediaPlayerDto> GetLocallyActiveMediaPlayers()
    {
        return mediaPlayerManager.GetMediaPlayers()
            .Where(mediaPlayer => mediaPlayer.CurrentState is ActiveVisibilityState)
            .Select(ConsumerMediaPlayerDto.From)
            .ToList();
    }

    public bool IsInRangeOf(int handle)
    {
        var mediaPlayer = mediaPlayerManager.Get(new Handle(handle));

        return mediaPlayer?.IsInRange() ?? false;
    }

    public void SetVolume(int handle, float volume)
    {
        var mediaPlayer = mediaPlayerManager.Get(new Handle(handle))
                          ?? throw new ConsumerMediaPlayerNotFoundException(handle);

        mediaPlayer.SetBaseVolume(volume);
    }

    public string GetLocallyActiveMediaPlayersJson()
    {
        return Serializer.Serialize(GetLocallyActiveMediaPlayers(), Serializer.Settings);
    }

    public void RegisterExports(ExportDictionary exports)
    {
        exports.Add(ConsumerExports.GetLocallyActiveMediaPlayers,
            new Func<string>(GetLocallyActiveMediaPlayersJson));

        exports.Add(ConsumerExports.IsInRangeOf, new Func<int, bool>(IsInRangeOf));

        exports.Add(ConsumerExports.SetVolume, new Action<int, float>(SetVolume));
    }

    private void OnMediaPlayerAdded(MediaPlayer mediaPlayer)
    {
        var subscriptions = new List<KeyValuePair<string, Delegate>>();

        void Subscribe<TDelegate>(string @event, TDelegate handler) where TDelegate : Delegate
        {
            eventManager.On(MediaPlayerEventScope.For(mediaPlayer.Handle, @event), handler);
            subscriptions.Add(new KeyValuePair<string, Delegate>(@event, handler));
        }

        Subscribe(MediaPlayerEvents.Played,
            (Action<CommunicationMessage, ITrack>)((_, track) => OnTrackChanged(mediaPlayer, track)));

        Subscribe(MediaPlayerEvents.TrackSkipped,
            (Action<CommunicationMessage, ITrack, ITrack>)((_, _, next) => OnTrackChanged(mediaPlayer, next)));

        Subscribe(MediaPlayerEvents.Paused,
            (Action<CommunicationMessage, ITrack>)((_, _) =>
                OnPlaybackStateChanged(mediaPlayer, ConsumerPlaybackState.Paused)));

        Subscribe(MediaPlayerEvents.Resumed,
            (Action<CommunicationMessage, ITrack>)((_, _) =>
                OnPlaybackStateChanged(mediaPlayer, ConsumerPlaybackState.Playing)));

        Subscribe(MediaPlayerEvents.QueueChanged,
            (Action<CommunicationMessage>)(_ => OnQueueChanged(mediaPlayer)));

        eventSubscriptionsByHandle[mediaPlayer.Handle.Value] = subscriptions;

        object? ownerResource = null;

        triggerEvent(ConsumerEvents.MediaPlayerCreated,
            [mediaPlayer.Handle.Value, mediaPlayer.Target.Type.ToString(), ownerResource]);
    }

    private void OnMediaPlayerRemoved(MediaPlayer mediaPlayer)
    {
        if (eventSubscriptionsByHandle.TryGetValue(mediaPlayer.Handle.Value, out var subscriptions))
        {
            eventSubscriptionsByHandle.Remove(mediaPlayer.Handle.Value);

            foreach (var subscription in subscriptions)
                eventManager.Off(MediaPlayerEventScope.For(mediaPlayer.Handle, subscription.Key),
                    subscription.Value);
        }

        triggerEvent(ConsumerEvents.MediaPlayerDestroyed, [mediaPlayer.Handle.Value]);
    }

    private void OnTrackChanged(MediaPlayer mediaPlayer, ITrack track)
    {
        triggerEvent(ConsumerEvents.TrackChanged,
            [mediaPlayer.Handle.Value, track.Url, track.Title, track.ThumbnailUrl]);
    }

    private void OnPlaybackStateChanged(MediaPlayer mediaPlayer, ConsumerPlaybackState state)
    {
        triggerEvent(ConsumerEvents.PlaybackStateChanged, [mediaPlayer.Handle.Value, state.ToString()]);
    }

    private void OnQueueChanged(MediaPlayer mediaPlayer)
    {
        triggerEvent(ConsumerEvents.QueueChanged, [mediaPlayer.Handle.Value]);
    }
}