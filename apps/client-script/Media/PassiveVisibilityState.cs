using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using Hypnonema.Client.Configurations;
using Hypnonema.Shared;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Diagnostics;

namespace Hypnonema.Client.Media;

public sealed class PassiveVisibilityState : MediaPlayerVisibilityState
{
    private const int ActivationRetryDelayMs = 10000;

    private const int RangeCheckIntervalMs = 500;


    public PassiveVisibilityState(MediaPlayer mediaPlayer, ILogger logger, ITickManager tickManager,
        IEventManager eventManager, Configuration configuration)
        : base(
            mediaPlayer,
            logger,
            tickManager,
            eventManager,
            configuration)
    {
    }

    public override async Task OnTick()
    {
        var hasPendingTracks = MediaPlayer.Queue.Count > 0;
        var isCurrentTrackPlaying = !MediaPlayer.Track.HasEnded();

        if (!MediaPlayer.IsInRange() || (!hasPendingTracks && !isCurrentTrackPlaying))
        {
            await BaseScript.Delay(RangeCheckIntervalMs);

            return;
        }

        try
        {
            var activeState =
                await ActiveVisibilityState.CreateAsync(MediaPlayer, Logger, TickManager, EventManager, Configuration);

            MediaPlayer.TransitionTo(activeState);
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"Failed to activate {MediaPlayer}. Retrying in {ActivationRetryDelayMs}ms.");

            await BaseScript.Delay(ActivationRetryDelayMs);
        }
    }
}