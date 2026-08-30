using System;
using System.Collections.Generic;
using System.Linq;
using Hypnonema.Server.Communications;
using Hypnonema.Server.Media;
using Hypnonema.Server.Rpc;
using Hypnonema.Shared;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Media;
using Serilog;
using static Hypnonema.Shared.Media.MediaPlayerRpcEvents;

namespace Hypnonema.Server.RpcControllers;

public class MediaPlayerController
{
    private readonly ILogger logger;

    private readonly MediaPlayerBuilder mediaPlayerBuilder;

    private readonly MediaPlayerRegistry mediaPlayerRegistry;

    private readonly List<Model> models;

    private readonly List<Screen> screens;

    public MediaPlayerController(ILogger logger,
        MediaPlayerBuilder mediaPlayerBuilder, MediaPlayerRegistry mediaPlayerRegistry,
        List<Model> models, List<Screen> screens)
    {
        this.logger = logger;
        this.mediaPlayerBuilder = mediaPlayerBuilder;
        this.mediaPlayerRegistry = mediaPlayerRegistry;
        this.models = models;
        this.screens = screens;

        RpcMethodBinder.Bind(this, logger);

        RpcManager.OnRequest(RpcEvents.GetMediaPlayers, null,
            (msg) => msg.Reply(mediaPlayerRegistry.All.Select(MediaPlayerDTO.From)));
    }

    [RpcMethod(RpcEvents.CreateMediaPlayer)]
    [PermissionRequired(Permissions.Use)]
    private void OnCreate(CommunicationMessage message, Track track, Target target)
    {
        logger.Verbose(
            "Received '{event}' request from client '{client}' (track: {track}, target: {target})",
            Play, message.Client?.Name, track, target);

        try
        {
            if (!track.IsValid())
            {
                logger.Warning("Rejected '{event}' from client '{client}': track '{track}' is not valid.",
                    Play, message.Client?.Name, track);

                RpcManager.Emit(Error, message.Client, "Invalid track");

                return;
            }

            //TODO: Allow new screenTargets
            if (!TryResolveTarget(target, out var resolvedTarget))
            {
                logger.Warning("Rejected '{event}' from client '{client}': target '{target}' does not exist.",
                    Play, message.Client?.Name, target);

                RpcManager.Emit(Error, message.Client, "Invalid target");

                return;
            }

            track.SetRequestingClient(message.Client!);

            //TODO: Check if target is modeltarget and if rendertarget is already in use by another media player, if so reject the request

            var mediaPlayer = mediaPlayerRegistry.FindByTarget(resolvedTarget!);

            if (mediaPlayer != null)
            {
                mediaPlayer.Enqueue(track);
                return;
            }

            mediaPlayer = mediaPlayerBuilder.Create(track, resolvedTarget!);

            mediaPlayerRegistry.Add(mediaPlayer);

            RpcManager.Emit(RpcEvents.CreateMediaPlayer, null, MediaPlayerDTO.From(mediaPlayer));
        }
        catch (Exception exception)
        {
            logger.Error(exception,
                "Error while attempting to play track '{track}' on target '{target}' for client '{client}'",
                track, target, message.Client?.Name);

            RpcManager.Emit(Error, message.Client, "Error on playing track");
        }
    }

    private bool TryResolveTarget(Target target, out Target? resolvedTarget)
    {
        switch (target)
        {
            case ScreenTarget screenTarget:
                var screen = screens.FirstOrDefault(s => s.Id == screenTarget.Screen.Id);
                resolvedTarget = screen is null ? null : new ScreenTarget(screen);
                return screen is not null;
            case ModelTarget modelTarget:
                var model = models.FirstOrDefault(m => m.Prop == modelTarget.Model.Prop);
                resolvedTarget = model is null ? null : new ModelTarget(model);
                return model is not null;
            default:
                resolvedTarget = null;
                return false;
        }
    }
}