using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Hypnonema.Client.Communications;
using Hypnonema.Client.Configurations;
using Hypnonema.Client.Extensions;
using Hypnonema.Client.Media;
using Hypnonema.Client.Rpc;
using Hypnonema.Shared;
using Hypnonema.Shared.Diagnostics;
using Hypnonema.Shared.Media;
using Hypnonema.Shared.Serialization;

namespace Hypnonema.Client.UI;

public sealed class UserInterface
{
    private readonly Configuration configuration;
    private readonly ILogger logger;
    private readonly MediaPlayerManager mediaPlayerManager;
    private readonly ITickManager tickManager;
    private readonly Action<string, object[]> triggerEvent;

    public UserInterface(MediaPlayerManager mediaPlayerManager, ITickManager tickManager, Configuration configuration,
        Action<string, object[]> triggerEvent, ILogger logger)
    {
        this.tickManager = tickManager;
        this.configuration = configuration;
        this.mediaPlayerManager = mediaPlayerManager;
        this.triggerEvent = triggerEvent;
        this.logger = logger;

        NuiManager.RegisterNuiCallback(Nui.NuiEvents.CloseUi, Hide);
        NuiManager.RegisterNuiCallback<Nui.PlayEventArgs>(Nui.NuiEvents.Play,
            args => RpcManager.Emit(RpcEvents.CreateMediaPlayer, args.Track, args.Target));
        NuiManager.RegisterNuiCallback(Nui.NuiEvents.Reset, () => _ = ResetAsync());
        NuiManager.RegisterNuiCallback<Nui.DeleteScreenEventArgs>(Nui.NuiEvents.DeleteScreen,
            args => RpcManager.Emit(RpcEvents.DeleteScreen, args.Id));
        NuiManager.RegisterNuiCallback<Screen>(Nui.NuiEvents.UpdateScreen,
            screen => RpcManager.Emit(RpcEvents.UpdateScreen, screen));

        RpcManager.On<List<Screen>>(RpcEvents.ScreensUpdated, OnScreensUpdated);
        RpcManager.On<string>(RpcEvents.ScreenError, OnScreenError);
    }

    private void OnScreensUpdated(CommunicationMessage message, List<Screen> screens)
    {
        configuration.Screens.Clear();
        configuration.Screens.AddRange(screens);

        if (IsVisible) PushScreens();
    }

    private void OnScreenError(CommunicationMessage message, string error)
    {
        Emit(Nui.NuiEvents.ScreenError, error);
    }

    private async Task ResetAsync()
    {
        var succeeded = await mediaPlayerManager.ResetAsync();

        NotifyPlayer(succeeded
            ? "Client reset complete."
            : "Client reset failed. Check the F8 console for details.");
    }

    private void NotifyPlayer(string message)
    {
        triggerEvent("chat:addMessage", new object[] { new { args = new[] { $"[Hypnonema] {message}" } } });
    }

    public bool IsVisible { get; private set; }

    public void Show()
    {
        if (IsVisible) return;
        IsVisible = true;

        Emit(Nui.NuiEvents.ShowUi);
        API.SetNuiFocus(true, true);

        tickManager.Add(OnTick);
    }

    public void Hide()
    {
        if (!IsVisible) return;
        IsVisible = false;

        Emit(Nui.NuiEvents.CloseUi);
        API.SetNuiFocus(false, false);

        tickManager.Remove(OnTick);
    }

    private void UpdateUi()
    {
        var existingMediaPlayers = mediaPlayerManager.GetMediaPlayers();
        var nearbyMediaPlayers = Game.Player.GetNearbyMediaPlayers(existingMediaPlayers, configuration);
        var serializedNearbyMediaPlayers = Serializer.Serialize(nearbyMediaPlayers, Serializer.NuiSerializerSettings);

        Emit(Nui.NuiEvents.UpdateUi, serializedNearbyMediaPlayers);

        PushScreens();
    }

    private void PushScreens()
    {
        var playerPosition = Game.PlayerPed.Position;
        var activeScreenIds = new HashSet<string>(mediaPlayerManager.GetMediaPlayers()
            .Select(mediaPlayer => mediaPlayer.Target)
            .OfType<ScreenTarget>()
            .Select(target => target.Screen.ResolveId()));

        // Every defined screen, not just the ones in render range - the settings list is a full inventory.
        var screens = configuration.Screens.Select(screen => new UserInterfaceScreen(
            screen,
            World.GetDistance(playerPosition, screen.Scaleform.Transform.Position.ToFxVector3()),
            activeScreenIds.Contains(screen.ResolveId()))).ToList();

        Emit(Nui.NuiEvents.UpdateScreens, Serializer.Serialize(screens, Serializer.NuiSerializerSettings));
    }

    private async Task OnTick()
    {
        try
        {
            UpdateUi();
        }
        catch (Exception exception)
        {
            // A transient native failure (e.g. a prop vanishing mid-iteration) must not permanently
            // detach this tick - skip the frame and retry on the next one.
            logger.Error(exception, "Failed to push a UI update; skipping this frame.");
        }

        if (API.IsPauseMenuActive()) Hide();

        await BaseScript.Delay(250);
    }

    public static void Emit(string type, object? data = null)
    {
        var message = new
        {
            type,
            data
        };

        var serializedMessage = Serializer.Serialize(message, Serializer.NuiSerializerSettings);

        API.SendNuiMessage(serializedMessage);
    }
}