using System;
using Hypnonema.Shared.Media;

namespace Hypnonema.Shared;

public static class Nui
{
    public class NuiEvents
    {
        public const string Play = "play";

        public const string Seek = "seek";

        public const string Stop = "stop";

        public const string PlayNext = "playNext";

        public const string SetVolume = "setVolume";

        public const string CloseUi = "closeUi";

        public const string UpdateUi = "updateUi";

        public const string ShowUi = "showUi";

        public const string SetScaleformOptions = "setScaleformOptions";

        public const string SaveScaleformSettings = "saveScaleformSettings";

        public const string SetRenderMode = "setRenderMode";

        public const string SetLooped = "setLooped";

        public const string SetMuted = "setMuted";

        public const string SetVideoEnabled = "setVideoEnabled";

        public const string SetPaused = "setPaused";

        public const string SetAudioMode = "setAudioMode";

        public const string Reset = "reset";

        public const string UpdateScreens = "updateScreens";

        public const string UpdateScreen = "updateScreen";

        public const string DeleteScreen = "deleteScreen";

        public const string ScreenError = "screenError";

        // Screen editor (ticket 05): the focusless value overlay. The native shell owns the world-space
        // gizmo/crosshair and pushes these; the NUI page never gets focus and only reads them.
        public const string EditorOpen = "editorOpen";

        public const string EditorClose = "editorClose";

        public const string EditorState = "editorState";

        public const string EditorToggleLegend = "editorToggleLegend";

        // NUI -> C#: open the spatial editor on an existing screen (ticket 08). Payload is an
        // EditorStartEventArgs; the native session closes the menu and starts with that screen's transform
        // (asTemplate = offset copy that saves as a new screen).
        public const string EditorStart = "editorStart";

        // Screen editor finish flow (ticket 07/08): the name/save dialog briefly takes NUI focus.
        // EditorFinishOpen carries { scaleform, existingIds, name, isEdit }; EditorFinishSave carries a Screen.
        public const string EditorFinishOpen = "editorFinishOpen";

        public const string EditorFinishCancel = "editorFinishCancel";

        public const string EditorFinishSave = "editorFinishSave";

        // C# -> NUI, payload = the saved screen name, so the dialog can toast before it closes.
        public const string EditorFinishSaved = "editorFinishSaved";
    }

    public class DeleteScreenEventArgs
    {
        public string Id { get; set; } = string.Empty;
    }

    public class EditorStartEventArgs
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>Open the screen as a template: same transform, shifted 1&#160;m sideways, saved as a new screen.</summary>
        public bool AsTemplate { get; set; }
    }

    public class MediaPlayerEventArgs(Handle handle) : EventArgs
    {
        public Handle Handle { get; } = handle;
    }

    public class PlayEventArgs
    {
        public PlayEventArgs(ITrack track, Target target)
        {
            // we receive this event from the user-interface therefore we to set the duration to -1 because the actual duration is yet unknown
            track.SetDuration(-1);

            Track = track;
            Target = target;
        }

        public Target Target { get; }

        public ITrack Track { get; }
    }

    public class SetScaleformOptionsEventArgs(Handle handle, ScaleformRenderConfiguration scaleform)
        : MediaPlayerEventArgs(handle)
    {
        public ScaleformRenderConfiguration Scaleform { get; } = scaleform;
    }

    public class SeekEventArgs(Handle handle, int position) : MediaPlayerEventArgs(handle)
    {
        public int Position { get; } = position;
    }

    public class SetVolumeEventArgs : MediaPlayerEventArgs
    {
        public SetVolumeEventArgs(Handle handle, float volume) : base(handle)
        {
            // The NUI wire format is a percentage in [0, 100]. This is the only place it gets normalized — consumers
            // receive a plain [0, 1] gain and must not rescale it again.
            if (volume < 0f) volume = 0f;
            if (volume > 100f) volume = 100f;

            Volume = volume / 100f;
        }

        public float Volume { get; }
    }

    public class SetVideoEnabledEventArgs(Handle handle, bool videoEnabled) : MediaPlayerEventArgs(handle)
    {
        public bool VideoEnabled { get; } = videoEnabled;
    }

    public class SetPausedEventArgs(Handle handle, bool paused) : MediaPlayerEventArgs(handle)
    {
        public bool Paused { get; } = paused;
    }

    public class SetLoopedEventArgs(Handle handle, bool looped) : MediaPlayerEventArgs(handle)
    {
        public bool Looped { get; } = looped;
    }

    public class SetMutedEventArgs(Handle handle, bool muted) : MediaPlayerEventArgs(handle)
    {
        public bool Muted { get; } = muted;
    }

    public class SetRenderModeEventArgs(Handle handle, RenderMode renderMode) : MediaPlayerEventArgs(handle)
    {
        public RenderMode RenderMode { get; } = renderMode;
    }
}