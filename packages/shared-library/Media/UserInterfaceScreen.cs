namespace Hypnonema.Shared.Media;

/// <summary>
///     A screen as shown in the NUI "Screens" settings list: the screen definition plus the two
///     values only the client can compute — the local player's distance to it and whether a
///     MediaPlayer is currently running on it (delete is blocked while one is).
/// </summary>
public sealed class UserInterfaceScreen
{
    public UserInterfaceScreen(Screen screen, float distance, bool isPlaying)
    {
        Screen = screen;
        Distance = distance;
        IsPlaying = isPlaying;
    }

    public Screen Screen { get; }

    public float Distance { get; }

    public bool IsPlaying { get; }
}
