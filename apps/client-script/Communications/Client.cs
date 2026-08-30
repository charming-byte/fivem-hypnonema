using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Hypnonema.Shared.Communications;

namespace Hypnonema.Client.Communications;

public sealed class Client : IClient
{
    public Client()
    {
        Name = API.GetPlayerName(API.PlayerId());
        Handle = Game.Player.ServerId.ToString();
    }

    public static Client Local => new();
    public string Name { get; }

    public bool HasPermission(string permission)
    {
        throw new NotImplementedException();
    }

    public string Handle { get; }
}