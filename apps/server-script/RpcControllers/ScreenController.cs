using System;
using System.Collections.Generic;
using Hypnonema.Server.Communications;
using Hypnonema.Server.Media;
using Hypnonema.Server.Rpc;
using Hypnonema.Shared;
using Hypnonema.Shared.Communications;
using Hypnonema.Shared.Media;
using Serilog;

namespace Hypnonema.Server.RpcControllers;

public class ScreenController
{
    private readonly ILogger logger;

    /// <summary>Kept so the controller's handlers can be detached; it lives for the resource's lifetime today.</summary>
    private readonly List<EventSubscription> rpcHandlers;

    private readonly ScreenRepository screenRepository;

    public ScreenController(ILogger logger, ScreenRepository screenRepository)
    {
        this.logger = logger;
        this.screenRepository = screenRepository;

        rpcHandlers = RpcMethodBinder.Bind(this, logger);
    }

    [RpcMethod(RpcEvents.CreateScreen)]
    [PermissionRequired(Permissions.ManageScreens, DeniedEvent = RpcEvents.ScreenError)]
    private void OnCreateScreen(CommunicationMessage message, Screen screen)
    {
        logger.Verbose("Received '{event}' from client '{client}' for screen '{name}'.", RpcEvents.CreateScreen,
            message.Client?.Name, screen.Name);

        try
        {
            screenRepository.Create(screen);

            RpcManager.Emit(RpcEvents.ScreensUpdated, null, screenRepository.All);
        }
        catch (ScreenValidationException exception)
        {
            logger.Warning("Rejected screen create from client '{client}': {errors}", message.Client?.Name,
                string.Join("; ", exception.Errors));

            RpcManager.Emit(RpcEvents.ScreenError, message.Client, string.Join("; ", exception.Errors));
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Failed to create screen '{name}' requested by client '{client}'.", screen.Name,
                message.Client?.Name);

            RpcManager.Emit(RpcEvents.ScreenError, message.Client, "Failed to create screen on disk.");
        }
    }

    [RpcMethod(RpcEvents.UpdateScreen)]
    [PermissionRequired(Permissions.ManageScreens, DeniedEvent = RpcEvents.ScreenError)]
    private void OnUpdateScreen(CommunicationMessage message, Screen screen)
    {
        logger.Verbose("Received '{event}' from client '{client}' for screen '{name}'.", RpcEvents.UpdateScreen,
            message.Client?.Name, screen.Name);

        try
        {
            screenRepository.Update(screen);

            RpcManager.Emit(RpcEvents.ScreensUpdated, null, screenRepository.All);
        }
        catch (ScreenValidationException exception)
        {
            logger.Warning("Rejected screen update from client '{client}': {errors}", message.Client?.Name,
                string.Join("; ", exception.Errors));

            RpcManager.Emit(RpcEvents.ScreenError, message.Client, string.Join("; ", exception.Errors));
        }
        catch (ScreenNotFoundException exception)
        {
            logger.Warning("Rejected screen update from client '{client}': {message}", message.Client?.Name,
                exception.Message);

            RpcManager.Emit(RpcEvents.ScreenError, message.Client, exception.Message);
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Failed to update screen '{name}' requested by client '{client}'.", screen.Name,
                message.Client?.Name);

            RpcManager.Emit(RpcEvents.ScreenError, message.Client, "Failed to update screen on disk.");
        }
    }

    [RpcMethod(RpcEvents.DeleteScreen)]
    [PermissionRequired(Permissions.ManageScreens, DeniedEvent = RpcEvents.ScreenError)]
    private void OnDeleteScreen(CommunicationMessage message, string id)
    {
        logger.Verbose("Received '{event}' from client '{client}' for screen '{id}'.", RpcEvents.DeleteScreen,
            message.Client?.Name, id);

        try
        {
            screenRepository.Delete(id);

            RpcManager.Emit(RpcEvents.ScreensUpdated, null, screenRepository.All);
        }
        catch (ScreenNotFoundException exception)
        {
            logger.Warning("Rejected screen delete from client '{client}': {message}", message.Client?.Name,
                exception.Message);

            RpcManager.Emit(RpcEvents.ScreenError, message.Client, exception.Message);
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Failed to delete screen '{id}' requested by client '{client}'.", id,
                message.Client?.Name);

            RpcManager.Emit(RpcEvents.ScreenError, message.Client, "Failed to delete screen from disk.");
        }
    }
}