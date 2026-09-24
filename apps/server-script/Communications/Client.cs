using CitizenFX.Core.Native;
using Hypnonema.Shared.Communications;

namespace Hypnonema.Server.Communications;

using static API;

public sealed class Client : IClient
{
    public Client(string handle)
    {
        Handle = handle;
        Name = GetPlayerName(handle);
    }

    public string Handle { get; }

    public string Name { get; }

    public bool HasPermission(string permission)
    {
        return IsPlayerAceAllowed(Handle, permission);
    }

    public override string ToString()
    {
        return $"{Handle}: {Name}";
    }
}