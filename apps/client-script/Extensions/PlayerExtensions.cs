using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Hypnonema.Client.Configurations;
using Hypnonema.Client.Media;
using Hypnonema.Shared.Media;
using Model = Hypnonema.Shared.Model;

namespace Hypnonema.Client.Extensions;

public static class PlayerExtensions
{
    private static Dictionary<int, Model>? modelHashDictionary;

    public static List<UserInterfaceMediaPlayer> GetNearbyMediaPlayers(this Player player,
        List<MediaPlayer> activeMediaPlayers, Configuration configuration)
    {
        modelHashDictionary ??= configuration.Models.ToDictionary(
            m => API.GetHashKey(m.Prop),
            m => m);

        List<UserInterfaceMediaPlayer> nearbyMediaPlayers = [];

        GetEntityBasedMediaPlayers(nearbyMediaPlayers);
        GetScreenBasedMediaPlayers(nearbyMediaPlayers, configuration);
        MergeNearbyMediaPlayersWithActiveMediaPlayers(nearbyMediaPlayers, activeMediaPlayers);

        return nearbyMediaPlayers.OrderBy(m => m.Distance).ToList();
    }

    private static void GetEntityBasedMediaPlayers(List<UserInterfaceMediaPlayer> mediaPlayersDto)
    {
        foreach (var prop in World.GetAllProps())
        {
            if (!prop.Exists()) continue;

            if (!modelHashDictionary!.TryGetValue(prop.Model, out var model)) continue;

            var distance = World.GetDistance(Game.PlayerPed.Position, prop.Position);

            uint handle;
            var isNetworkedProp = API.NetworkGetEntityIsNetworked(prop.Handle);

            if (isNetworkedProp) handle = (uint)API.NetworkGetNetworkIdFromEntity(prop.Handle);
            else handle = prop.Model;

            var userInterfaceMediaPlayer = UserInterfaceMediaPlayer.Default(
                distance,
                model.Label,
                handle,
                new ModelTarget(model));

            mediaPlayersDto.Add(userInterfaceMediaPlayer);
        }
    }

    private static void GetScreenBasedMediaPlayers(List<UserInterfaceMediaPlayer> mediaPlayersDto,
        Configuration configuration)
    {
        foreach (var screen in configuration.Screens)
        {
            var distance = World.GetDistance(Game.PlayerPed.Position,
                screen.Scaleform.Transform.Position.ToFxVector3());

            if (distance > screen.RenderDistance) continue;

            var screenUiMediaPlayer = UserInterfaceMediaPlayer.From(screen, (int)distance,
                (uint)API.GetHashKey(screen.ResolveId()));

            mediaPlayersDto.Add(screenUiMediaPlayer);
        }
    }

    private static void MergeNearbyMediaPlayersWithActiveMediaPlayers(List<UserInterfaceMediaPlayer> mediaPlayersDto,
        List<MediaPlayer> activeMediaPlayers)
    {
        foreach (var mediaPlayer in activeMediaPlayers)
        {
            var existingIndex = mediaPlayersDto.FindIndex(p => Target.AreEqual(p.Target, mediaPlayer.Target));

            if (existingIndex == -1) continue;

            var existingMediaPlayer = mediaPlayersDto[existingIndex];

            mediaPlayersDto[existingIndex] =
                new UserInterfaceMediaPlayer(
                    existingMediaPlayer.Distance,
                    mediaPlayer.BaseVolume,
                    mediaPlayer.Error,
                    mediaPlayer.Label,
                    (uint)mediaPlayer.Handle.Value,
                    mediaPlayer.Target,
                    mediaPlayer.Track,
                    mediaPlayer.Queue,
                    mediaPlayer.Looped,
                    mediaPlayer.Muted,
                    mediaPlayer.Paused,
                    mediaPlayer.AdActive,
                    mediaPlayer.Video,
                    mediaPlayer.RenderMode,
                    mediaPlayer.Scaleform);
        }
    }
}