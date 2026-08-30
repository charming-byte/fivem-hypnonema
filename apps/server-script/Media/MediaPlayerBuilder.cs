using System;
using Hypnonema.Shared;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Configurations;
using Hypnonema.Shared.Media;
using Hypnonema.Shared.Media.Audio;
using Serilog;

namespace Hypnonema.Server.Media;

public sealed class MediaPlayerBuilder : IMediaPlayerBuilder
{
    private readonly IEventManager eventManager;
    private readonly ILogger logger;
    private readonly int maxQueueSize;
    private readonly ScreenRepository screenRepository;
    private readonly ITickManager tickManager;
    private readonly YouTubeAdQuorumConfiguration youtubeAdQuorum;

    private int nextHandleValue;

    public MediaPlayerBuilder(ITickManager tickManager, ILogger logger, int maxQueueSize, IEventManager eventManager,
        ScreenRepository screenRepository, YouTubeAdQuorumConfiguration youtubeAdQuorum)
    {
        this.logger = logger;
        this.tickManager = tickManager;
        this.maxQueueSize = maxQueueSize;
        this.eventManager = eventManager;
        this.screenRepository = screenRepository;
        this.youtubeAdQuorum = youtubeAdQuorum;
    }

    public IMediaPlayer Create(ITrack track, Target target)
    {
        return target switch
        {
            ScreenTarget screenTarget => CreateScreenBasedMediaPlayer(track, screenTarget),
            ModelTarget modelTarget => CreateModelBasedMediaPlayer(track, modelTarget),
            _ => throw new ArgumentException("Invalid target type for media player.", nameof(target))
        };
    }

    private MediaPlayer CreateScreenBasedMediaPlayer(ITrack track, ScreenTarget screenTarget)
    {
        var handle = new Handle(++nextHandleValue);
        var label = screenTarget.Screen.Name;
        var scaleformSettings = screenTarget.Screen.Scaleform;
        var options = MediaPlayerOptions.Default(handle, label, track, screenTarget, maxQueueSize, scaleformSettings,
            tickManager, eventManager, screenTarget.Screen.Audio);

        return new MediaPlayer(logger, options, youtubeAdQuorum, screenRepository);
    }

    private MediaPlayer CreateModelBasedMediaPlayer(ITrack track, ModelTarget modelTarget)
    {
        var handle = new Handle(++nextHandleValue);
        var label = modelTarget.Model.Label;
        var options = MediaPlayerOptions.Default(handle, label, track, modelTarget, maxQueueSize,
            ScaleformRenderConfiguration.Default, tickManager, eventManager, DefaultAudioConfiguration.Default);

        return new MediaPlayer(logger, options, youtubeAdQuorum);
    }
}