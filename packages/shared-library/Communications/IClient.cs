namespace Hypnonema.Shared.Communications;

/// <summary>
///     Represents a connected client/player in the system.
///     Provides access to the player's handle, name, and permission checks.
/// </summary>
public interface IClient
{
    /// <summary>
    ///     Gets the unique identifier/handle of the client.
    /// </summary>
    public string Handle { get; }

    /// <summary>
    ///     Gets the display name of the client.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Determines whether the client has a specific permission.
    /// </summary>
    /// <param name="permission">The permission to check.</param>
    /// <returns><c>true</c> if the client has the permission; otherwise, <c>false</c>.</returns>
    public bool HasPermission(string permission);
}