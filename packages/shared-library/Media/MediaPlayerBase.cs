using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Media.Audio;

namespace Hypnonema.Shared.Media;

public abstract class MediaPlayerBase : IMediaPlayer
{
    protected readonly IEventManager EventManager;

    protected readonly object QueueLock = new();

    protected readonly ITickManager TickManager;
    private bool disposed;

    protected MediaPlayerBase(MediaPlayerOptions options)
    {
        Handle = options.Handle;
        Label = options.Label;
        Track = options.Track;
        Target = options.Target;
        Video = options.VideoEnabled;
        Looped = options.Looped;
        Muted = options.Muted;
        MaxQueuedTracks = options.MaxQueuedTracks;
        RenderMode = options.RenderMode;
        Paused = options.Paused;
        Scaleform = options.ScaleformRenderConfiguration;
        Queue = options.Queue;
        EventManager = options.EventManager;
        TickManager = options.TickManager;
        Audio = options.Audio;

        Track.Ended += OnTrackEnd;
    }

    public virtual void Dispose()
    {
        if (disposed) return;
        disposed = true;

        Track.Ended -= OnTrackEnd;
    }

    public Handle Handle { get; }
    public string Label { get; }
    public Target Target { get; }

    public ITrack Track { get; }

    public AudioConfiguration Audio { get; private set; }

    public Queue<ITrack> Queue { get; }
    public ScaleformRenderConfiguration Scaleform { get; private set; }
    public bool Looped { get; private set; }
    public bool Muted { get; private set; }
    public bool Paused { get; private set; }
    public bool AdActive { get; private set; }

    public int MaxQueuedTracks { get; }
    public bool Video { get; private set; }
    public RenderMode RenderMode { get; private set; }

    public void SetAudioConfiguration(AudioConfiguration audio)
    {
        if (audio == null) throw new ArgumentNullException(nameof(audio));
        if (Audio == audio) return;

        Audio = audio;

        Emit(MediaPlayerEvents.AudioConfigurationChanged, Audio);
    }

    public virtual void SetPaused(bool paused)
    {
        if (Paused == paused) return;

        Paused = paused;

        if (Paused)
        {
            Track.Pause();
            Emit(MediaPlayerEvents.Paused, Track);
        }

        else
        {
            Track.Resume();
            Emit(MediaPlayerEvents.Resumed, Track);
        }
    }

    public void SetAdActive(bool adActive)
    {
        if (AdActive == adActive) return;

        AdActive = adActive;

        Emit(MediaPlayerEvents.AdActiveChanged, adActive);
    }

    public virtual void SetLooped(bool looped)
    {
        if (Looped == looped) return;

        Looped = looped;
        Emit(MediaPlayerEvents.LoopChanged, looped);
    }

    public virtual void SetMuted(bool muted)
    {
        if (Muted == muted) return;

        Muted = muted;

        Emit(MediaPlayerEvents.MutedChanged, muted);
    }

    public virtual void SetRenderMode(RenderMode renderMode)
    {
        if (renderMode == RenderMode) return;

        var isScreenBased = Target is ScreenTarget;
        var isRenderModeScaleform = renderMode == RenderMode.Scaleform;

        if (isScreenBased && !isRenderModeScaleform)
            throw new InvalidOperationException(
                "Cannot set render mode to 'renderTarget': screen targets support only 'scaleform' render mode.");

        RenderMode = renderMode;

        Emit(MediaPlayerEvents.RenderModeChanged, renderMode);
    }

    public virtual void SetVideoEnabled(bool isVideoEnabled)
    {
        if (Video == isVideoEnabled) return;

        Video = isVideoEnabled;

        Emit(Video ? MediaPlayerEvents.VideoEnabled : MediaPlayerEvents.VideoDisabled);
    }

    public virtual void Enqueue(ITrack track)
    {
        if (track == null) throw new ArgumentNullException(nameof(track));

        if (!track.IsValid()) throw new ArgumentException("Track is invalid", nameof(track));

        lock (QueueLock)
        {
            if (Queue.Count >= MaxQueuedTracks) throw new QueueLimitExceededException(MaxQueuedTracks);

            Queue.Enqueue(track);
        }

        Emit(MediaPlayerEvents.Enqueued, track);
        Emit(MediaPlayerEvents.QueueChanged);
    }

    public virtual void SetQueue(IEnumerable<ITrack> tracks)
    {
        if (tracks == null) throw new ArgumentNullException(nameof(tracks));

        var trackList = new List<ITrack>(tracks);

        if (trackList.Any(t => t == null || !t.IsValid()))
            throw new ArgumentException("Queue contains an invalid track.", nameof(tracks));

        if (trackList.Count > MaxQueuedTracks) throw new QueueLimitExceededException(MaxQueuedTracks);

        lock (QueueLock)
        {
            Queue.Clear();

            foreach (var track in trackList) Queue.Enqueue(track);
        }

        Emit(MediaPlayerEvents.QueueChanged);
    }

    public virtual void SkipCurrentTrack()
    {
        ITrack nextTrack;

        lock (QueueLock)
        {
            if (Queue.Count < 1) throw new ArgumentException("There is no item in queue to skip to");

            nextTrack = Queue.Peek();

            if (!nextTrack.IsValid()) throw new ArgumentException("Track is not valid.", nameof(nextTrack));

            Queue.Dequeue();
        }

        var previousTrack = Track;

        Track.Play(nextTrack);
        OnTrackChanged();

        Emit(MediaPlayerEvents.TrackSkipped, previousTrack, nextTrack);
        Emit(MediaPlayerEvents.QueueChanged);
    }

    public virtual void Seek(int position)
    {
        if (position < 0) throw new ArgumentOutOfRangeException(nameof(position), "position cannot be negative");

        Track.Seek(position);

        Emit(MediaPlayerEvents.TrackPositionChanged, Track);
    }

    public virtual void SetScaleformSettings(ScaleformRenderConfiguration renderConfiguration)
    {
        if (renderConfiguration == null) throw new ArgumentNullException(nameof(renderConfiguration));

        if (Scaleform == renderConfiguration) return;

        Scaleform = renderConfiguration;

        Emit(MediaPlayerEvents.ScaleformSettingsChanged, Scaleform);
    }

    /// <summary>
    ///     Hot-apply a screen edit (ticket 08). Unlike <see cref="SetScaleformSettings" /> this does not ask
    ///     for a drawable rebuild: the client nudges the running <c>ScaleformTarget</c> in place, and a stale
    ///     transform is only picked up on the next rebuild. Store the new config so that rebuild is correct.
    /// </summary>
    public void ApplyScreenGeometry(ScaleformRenderConfiguration renderConfiguration)
    {
        if (renderConfiguration == null) throw new ArgumentNullException(nameof(renderConfiguration));

        if (Scaleform == renderConfiguration) return;

        Scaleform = renderConfiguration;

        Emit(MediaPlayerEvents.ScreenGeometryChanged, Scaleform);
    }

    public virtual void Stop()
    {
        Emit(MediaPlayerEvents.Stopped, this);
    }

    public void SetDuration(int duration)
    {
        if (Track.Duration == duration) return;

        Track.SetDuration(duration);

        Emit(MediaPlayerEvents.TrackDurationChanged, Track);
    }

    public void Play(ITrack track)
    {
        if (track == null) throw new ArgumentNullException(nameof(track));

        if (!track.IsValid()) throw new ArgumentException("Track is not valid.", nameof(track));

        Track.Play(track);
        OnTrackChanged();

        Emit(MediaPlayerEvents.Played, Track);
    }

    protected abstract Task OnTick();
    protected abstract void HandleTrackEnd();

    protected virtual void OnTrackChanged()
    {
    }

    private void OnTrackEnd(object sender, EventArgs e)
    {
        HandleTrackEnd();
    }

    public string GetScopedEvent(string eventName)
    {
        return MediaPlayerEventScope.For(Handle, eventName);
    }

    protected void Emit(string @event, params object[] args)
    {
        EventManager.Emit(GetScopedEvent(@event), args);
    }

    protected void On(string @event, Delegate action)
    {
        EventManager.On(GetScopedEvent(@event), action);
    }

    public override string ToString()
    {
        return $"{Label} ({Handle})";
    }
}