namespace Hypnonema.Shared.Media;

/// <summary>
///     The live transform and interaction state that the screen editor's focusless NUI overlay renders
///     (ticket 05). The native shell builds one of these every frame from its client-only
///     <c>ScreenEditorState</c> and pushes it as the <see cref="Nui.NuiEvents.EditorState" /> message;
///     the NUI page never has focus and only reads it. <see cref="Mode" /> / <see cref="Frame" /> /
///     <see cref="Axis" /> are plain strings so the reducer's enums can stay client-side.
/// </summary>
public sealed class ScreenEditorOverlayState
{
    public ScreenEditorOverlayState(Vector3 position, Vector3 rotation, Vector2 scale, string mode,
        string frame, string axis, bool aspectLocked, bool dirty)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
        Mode = mode;
        Frame = frame;
        Axis = axis;
        AspectLocked = aspectLocked;
        Dirty = dirty;
    }

    public Vector3 Position { get; }

    public Vector3 Rotation { get; }

    public Vector2 Scale { get; }

    public string Mode { get; }

    public string Frame { get; }

    public string Axis { get; }

    public bool AspectLocked { get; }

    public bool Dirty { get; }
}
