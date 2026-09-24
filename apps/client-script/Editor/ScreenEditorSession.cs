using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Hypnonema.Client.Communications;
using Hypnonema.Client.Extensions;
using Hypnonema.Client.Graphics;
using Hypnonema.Client.Rpc;
using Hypnonema.Shared;
using Hypnonema.Shared.Diagnostics;
using Hypnonema.Shared.Media;
using Vector3 = Hypnonema.Shared.Vector3;

namespace Hypnonema.Client.Editor;

/// <summary>
///     The untested native shell around <see cref="ScreenEditorState" /> (ticket 05). Owns the editor
///     session lifecycle: a client-local placeholder scaleform rendered ~3&#160;m in front of the camera, a
///     per-frame 2D tick that draws the crosshair / translate gizmo / value overlay, and the input plumbing
///     (mode keys, arrow-key nudge, hold-LMB-to-drag with a frozen camera). All transform math lives in
///     <see cref="ScreenEditorState" />; this class only turns native input into <see cref="EditorInput" />.
///     The crosshair, the translate/rotate axis gizmo and the scale corner handles are drawn natively
///     (world-space); the value read-out and the key legend live in the focusless NUI overlay, fed via
///     <c>emitNui</c>. <c>G</c> flips the reference frame, <c>L</c> the scale aspect lock, <c>E</c> snaps
///     the face onto the surface under the crosshair.
/// </summary>
public sealed class ScreenEditorSession
{
    // The editor's own movie - deliberately NOT named after the ScaleformPool scheme so it never collides
    // with a running media player. Ship assets/stream/hypnonema_screen_editor_placeholder.gfx + a files{}
    // entry in the resource manifest; until then the session runs without a visible surface.
    private const string PlaceholderMovie = "hypnonema_screen_editor_placeholder";
    private const double PlaceholderLoadTimeoutSeconds = 5d;

    private const float SpawnDistance = 3f;
    private const float EyeHeight = 0.65f;

    // "Copy as template": drop the new face 1 m along the source face's local right axis so it doesn't
    // overlap the original.
    private const float TemplateLateralOffset = 1f;

    private const float GizmoScreenFraction = 0.09f; // handle length as a fraction of camera distance
    private const float GizmoMinLength = 0.15f;
    private const float GizmoMaxLength = 0.75f;
    private const float HandlePickTolerance = 0.03f; // aspect-corrected normalised screen distance

    // ponytail: raw mouse-look normals are ~0.001-0.05 per frame; this lifts a drag into a usable range.
    // Tune together with ScreenEditorState.Drag*Scale against the real feel - no tested logic depends on it.
    private const float MouseSensitivity = 8f;

    // Control ids (keyboard/mouse). Best-effort defaults from the stock bindings - verify in-game and tune.
    private const int ControlLmb = 24; // INPUT_ATTACK
    private const int ControlAim = 25; // INPUT_AIM
    private const int ControlNextCamera = 0; // INPUT_NEXT_CAMERA
    private const int ControlLookLr = 1; // INPUT_LOOK_LR
    private const int ControlLookUd = 2; // INPUT_LOOK_UD
    // INPUT_SELECT_WEAPON_* (157-159, the number row): kept only to suppress weapon switching while
    // editing - the mode keys are read from the raw number row instead (see RawKey1..3), because
    // IsDisabledControlJustPressed doesn't fire reliably for these.
    private const int ControlMode1 = 157;
    private const int ControlMode2 = 158;
    private const int ControlMode3 = 159;
    private const int ControlFrame = 47; // INPUT_DETONATE (G)
    private const int ControlArrowUp = 172; // INPUT_CELLPHONE_UP
    private const int ControlArrowDown = 173; // INPUT_CELLPHONE_DOWN
    private const int ControlArrowLeft = 174; // INPUT_CELLPHONE_LEFT
    private const int ControlArrowRight = 175; // INPUT_CELLPHONE_RIGHT
    private const int ControlShift = 21; // INPUT_SPRINT (Shift)
    private const int ControlAlt = 19; // INPUT_CHARACTER_WHEEL (Left Alt)
    private const int ControlLegend = 170; // INPUT_REPLAY_START_STOP_RECORDING - kept only to suppress it
    private const int ControlExit = 322; // INPUT_FRONTEND_PAUSE (Escape)
    private const int ControlEnterVehicle = 23; // INPUT_ENTER = F / board a vehicle - suppressed so you don't drive off mid-edit
    private const int ControlPickup = 38; // INPUT_PICKUP - same, E on foot
    private const int ControlDuck = 36; // INPUT_DUCK - suppressed so the Z axis key doesn't also crouch the ped

    // Raw Windows VK codes for keys IsDisabledControlJustPressed handles unreliably: the number-row mode
    // keys (INPUT_SELECT_WEAPON_*) and F1 for the legend (INPUT_REPLAY_START_STOP_RECORDING).
    private const int RawKey1 = 0x31;
    private const int RawKey2 = 0x32;
    private const int RawKey3 = 0x33;
    private const int RawKeyF1 = 0x70;
    private const int RawKeyEnter = 0x0D; // VK_RETURN - open the finish/save dialog (control 23 is F, not Return)
    private const int RawKeyE = 0x45; // surface snap
    private const int RawKeyL = 0x4C; // aspect-lock toggle (scale mode)
    private const int RawKeyX = 0x58; // select the X axis
    private const int RawKeyY = 0x59; // select the Y axis
    private const int RawKeyZ = 0x5A; // select the Z axis

    // Surface snap (E): one ray from the camera through the crosshair, started with StartShapeTestRay and
    // read back over the next few ticks (the async pattern from Media/Audio/OcclusionProbe).
    private const float SnapRayDistance = 1000f;
    private const int SnapShapeTestFlags = 1 | 2 | 16; // world geometry + vehicles + objects, not peds
    private const int SnapHintDurationMs = 5000; // how long the hit-normal marker stays on screen

    // Scale-handle half-extents = |scale| * ScaleformTarget.WorldUnitsPerScaleUnit / 2.
    // ponytail: "up" is taken as the face's local Z (correct for the yaw-only screens in screens.yaml);
    // verify handle placement on a pitched face in-game.

    private static readonly int[] SuppressedControls =
    {
        ControlLmb, ControlAim, ControlNextCamera,
        ControlMode1, ControlMode2, ControlMode3, ControlFrame,
        ControlArrowUp, ControlArrowDown, ControlArrowLeft, ControlArrowRight,
        ControlLegend, ControlEnterVehicle, ControlPickup, ControlDuck
    };

    private static readonly EditorAxis[] Axes = { EditorAxis.X, EditorAxis.Y, EditorAxis.Z };

    private readonly Action<string, object?> emitNui;
    private readonly Action closeUi;
    private readonly Action openUi;
    private readonly Func<IReadOnlyList<Screen>> allScreens;
    private readonly ILogger logger;
    private readonly Func<Task> tick;
    private readonly ITickManager tickManager;
    private readonly Action<string, object[]> triggerEvent;

    private bool dragging;

    // Finish flow (ticket 07): the name/save dialog is a focused NUI overlay. While it is up the
    // per-frame manipulation input is suspended; awaitingCreate is the "sent CreateScreen, waiting for
    // ScreensUpdated / ScreenError" latch, pendingScreenName only feeds the confirmation message.
    private bool finishing;
    private bool awaitingCreate;
    private string pendingScreenName = string.Empty;
    private string pendingScreenId = string.Empty;

    // True when the session was opened from the NUI menu ("Screen erstellen" / "Bearbeiten" / template
    // copy). A successful save then closes the editor and re-opens the menu, so the confirmation toast is
    // visible and the new screen shows in the list — a command-started session (Toggle) just closes.
    private bool reopenUiOnClose;

    // Ticket 08: non-null while this session edits an existing screen - "Speichern" then sends UpdateScreen
    // (carrying the frozen id + the screen's non-geometry fields) instead of CreateScreen. A template copy
    // leaves this null (it saves as a brand-new screen) but still pre-fills the name.
    private Screen? editOriginal;
    private string prefillName = string.Empty;

    // While a scale handle is being dragged, its world position - the drag delta is projected onto the
    // screen-space direction from the face centre to this point (outward = grow). Null for translate/rotate,
    // where ScreenAxisDirection derives the direction from the active axis gizmo instead.
    private Vector3? dragReference;

    // Handle of an in-flight surface-snap ray (0 = none). Polled in the tick, applied when the trace lands.
    private int pendingSnapHandle;

    // Last surface-snap hit, drawn as a short normal for a few seconds so it's visible which surface (and
    // which way its normal points) the ray actually grabbed.
    private Vector3? lastSnapHit;
    private Vector3 lastSnapNormal = new(0f, 0f, 1f);
    private int lastSnapHintUntilMs;

    // Raw keys held as of the previous tick, for our own just-pressed edge detection (no IsRawKeyJustPressed
    // native in this CitizenFX version).
    private readonly HashSet<int> rawKeysDown = new();

    // Bumped on every Start/Stop so a placeholder still loading from a previous session is discarded.
    private int generation;
    private Scaleform? placeholder;

    public ScreenEditorSession(ITickManager tickManager, ILogger logger, Action<string, object[]> triggerEvent,
        Action<string, object?> emitNui, Action closeUi, Action openUi, Func<IReadOnlyList<Screen>> allScreens)
    {
        this.tickManager = tickManager ?? throw new ArgumentNullException(nameof(tickManager));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.triggerEvent = triggerEvent ?? throw new ArgumentNullException(nameof(triggerEvent));
        this.emitNui = emitNui ?? throw new ArgumentNullException(nameof(emitNui));
        this.closeUi = closeUi ?? throw new ArgumentNullException(nameof(closeUi));
        this.openUi = openUi ?? throw new ArgumentNullException(nameof(openUi));
        this.allScreens = allScreens ?? throw new ArgumentNullException(nameof(allScreens));

        tick = OnTick;

        NuiManager.RegisterNuiCallback<Nui.EditorStartEventArgs>(Nui.NuiEvents.EditorStart, OnEditorStart);
        NuiManager.RegisterNuiCallback(Nui.NuiEvents.EditorFinishCancel, OnFinishCancel);
        NuiManager.RegisterNuiCallback<Screen>(Nui.NuiEvents.EditorFinishSave, OnFinishSave);
        RpcManager.On<List<Screen>>(RpcEvents.ScreensUpdated, OnScreensUpdated);
        RpcManager.On<string>(RpcEvents.ScreenError, OnScreenError);
    }

    public bool IsActive { get; private set; }

    public ScreenEditorState? State { get; private set; }

    /// <summary>Command entry point: opens a session, or closes the one that is running.</summary>
    public void Toggle()
    {
        if (IsActive) Stop();
        else Start();
    }

    /// <summary>
    ///     NUI "Bearbeiten" / "als Vorlage kopieren" (ticket 08): close the menu and open the spatial editor
    ///     on the picked screen. Editing starts on its saved transform and saves back via UpdateScreen; a
    ///     template copy starts 1&#160;m to the side and saves as a new screen.
    /// </summary>
    private void OnEditorStart(Nui.EditorStartEventArgs args)
    {
        if (args is null || string.IsNullOrWhiteSpace(args.Id)) return;

        var screen = allScreens().FirstOrDefault(s => s.ResolveId() == args.Id);
        if (screen is null)
        {
            Notify($"Screen '{args.Id}' is no longer defined.");
            return;
        }

        closeUi();
        Start(new ScreenEditorStartOptions(screen, args.AsTemplate));
        reopenUiOnClose = true; // Start() cleared it; this session came from the menu.
    }

    public void Start(ScreenEditorStartOptions? options = null)
    {
        if (IsActive)
        {
            // Can't happen via Toggle(); guards a direct call (e.g. the NUI edit button).
            Notify("Screen editor is already running - press Escape to close it.");
            return;
        }

        editOriginal = options is { AsTemplate: false } ? options.Screen : null;
        prefillName = options?.Screen.Name ?? string.Empty;

        State = ScreenEditorState.FromTransform(BuildStartTransform(options), options?.Screen.Scaleform.Texture.Dimension);
        generation++;
        dragging = false;
        dragReference = null;
        pendingSnapHandle = 0;
        lastSnapHit = null;
        finishing = false;
        awaitingCreate = false;
        reopenUiOnClose = false; // OnEditorStart sets this back on for menu-started sessions.
        rawKeysDown.Clear();
        IsActive = true;

        _ = tickManager.RunAsync(() => LoadPlaceholderAsync(generation));
        tickManager.Add(tick);

        emitNui(Nui.NuiEvents.EditorOpen, null);

        Notify("Screen editor started. 1/2/3 mode, X/Y/Z axis, arrows nudge, hold LMB on a handle to drag, G frame, L aspect, E snap, Enter names & saves, F1 legend, Escape closes.");
    }

    public void Stop()
    {
        Stop("Screen editor closed.");
    }

    private void Stop(string message)
    {
        if (!IsActive) return;

        IsActive = false;
        generation++; // invalidates a placeholder that is still loading
        tickManager.Remove(tick);
        ReleasePlaceholder();
        dragging = false;
        dragReference = null;
        pendingSnapHandle = 0;
        lastSnapHit = null;
        editOriginal = null;
        prefillName = string.Empty;
        reopenUiOnClose = false;
        State = null;

        if (finishing) CloseFinishDialog();

        emitNui(Nui.NuiEvents.EditorClose, null);

        Notify(message);
    }

    // Drop the focused finish dialog and hand input back to the game. Shared by "back to editing",
    // a successful save, and every teardown path (Escape / death / resource stop).
    private void CloseFinishDialog()
    {
        finishing = false;
        awaitingCreate = false;
        API.SetNuiFocus(false, false);
    }

    /// <summary>
    ///     Enter / "Fertigstellen": grab NUI focus and show the name/save dialog. The dialog carries the
    ///     current scaleform config, a pre-filled name, and — for a new screen — the ids this client already
    ///     knows (its own duplicate pre-check). Editing an existing screen skips the duplicate check (the id
    ///     is frozen). The placeholder keeps rendering behind it.
    /// </summary>
    private void OpenFinishDialog()
    {
        if (State is null) return;

        finishing = true;
        awaitingCreate = false;
        dragging = false;
        dragReference = null;

        API.SetNuiFocus(true, true);

        var scaleform = new ScaleformRenderConfiguration
        {
            Transform = State.ToTransform(),
            Texture = new ScaleformTexture { Dimension = State.TextureDimension }
        };

        var isEdit = editOriginal is not null;
        var existingIds = isEdit
            ? new List<string>()
            : allScreens().Select(s => s.ResolveId()).ToList();

        emitNui(Nui.NuiEvents.EditorFinishOpen, new { scaleform, existingIds, name = prefillName, isEdit });
    }

    private void OnFinishCancel()
    {
        if (!finishing) return;
        CloseFinishDialog();
    }

    private void OnFinishSave(Screen screen)
    {
        if (!finishing || screen is null) return;

        pendingScreenName = screen.Name;
        awaitingCreate = true;

        if (editOriginal is not null)
        {
            // Full Screen object, but the id and every non-geometry field come from the stored original -
            // the editor only ever touched the transform. The id is frozen, so ScreensUpdated still
            // correlates on it even when the name changed.
            var updated = new Screen
            {
                // ResolveId(), not .Id: a hand-authored screens.yaml entry has no explicit id, and this
                // write is what freezes its slug identity going forward (consistent with ticket 01).
                Id = editOriginal.ResolveId(),
                Name = screen.Name,
                Scaleform = screen.Scaleform,
                RenderDistance = editOriginal.RenderDistance,
                Audio = editOriginal.Audio
            };

            pendingScreenId = updated.ResolveId();
            RpcManager.Emit(RpcEvents.UpdateScreen, updated);
            return;
        }

        pendingScreenId = screen.ResolveId();
        RpcManager.Emit(RpcEvents.CreateScreen, screen);
    }

    /// <summary>
    ///     Server accepted the create: our screen shows up in the broadcast list. Correlated on the id so
    ///     an unrelated <c>ScreensUpdated</c> (another admin editing) can't false-trigger the exit.
    /// </summary>
    private void OnScreensUpdated(CommunicationMessage message, List<Screen> screens)
    {
        if (!finishing || !awaitingCreate) return;
        if (screens is null || !screens.Any(s => s.ResolveId() == pendingScreenId)) return;

        awaitingCreate = false;
        emitNui(Nui.NuiEvents.EditorFinishSaved, pendingScreenName);

        var reopenUi = reopenUiOnClose;
        Stop("Screen editor closed.");
        // Hand focus back to the menu (on its Screens route) so the toast is visible and the freshly
        // saved screen shows in the list. Stop() already emitted editorClose + dropped NUI focus.
        if (reopenUi) openUi();
    }

    /// <summary>
    ///     Server rejected the create (id clash, missing permission, unwritable directory). The dialog
    ///     shows the text itself — <c>UserInterface</c> forwards <c>ScreenError</c> to NUI — so here we
    ///     only clear the latch and keep the dialog open with its values.
    /// </summary>
    private void OnScreenError(CommunicationMessage message, string error)
    {
        if (finishing) awaitingCreate = false;
    }

    private async Task OnTick()
    {
        // Per-frame by design: DrawScaleformMovie3D / DrawLine / DrawRect only persist for the frame that
        // issues them. This is `async Task` with no `await` (same pattern as Graphics/DrawableSlot.OnDraw):
        // it completes synchronously, so CitizenFX re-invokes it every frame. A real `await BaseScript.Delay`
        // here skips frames and the scaleform flickers; `Task.CompletedTask` fails FiveM's Mono verifier.
        // Independent of the 4 Hz UserInterface tick.
        if (!IsActive || State is null) return;

        if (API.IsPlayerDead(API.PlayerId()))
        {
            Stop("Screen editor closed (you died).");
            return;
        }

        API.DisableIdleCamera(true);
        SuppressConflictingControls();
        HandleInput();

        if (IsActive && State is not null) Render(); // input may have closed the session
    }

    private void SuppressConflictingControls()
    {
        foreach (var control in SuppressedControls) API.DisableControlAction(0, control, true);

        // Freeze the camera while the mouse is driving an axis.
        if (dragging || API.IsDisabledControlPressed(0, ControlLmb))
        {
            API.DisableControlAction(0, ControlLookLr, true);
            API.DisableControlAction(0, ControlLookUd, true);
        }
    }

    private void HandleInput()
    {
        var state = State!;

        // The focused finish dialog owns input; the game controls below are dead while NUI has focus
        // anyway, and its own Escape/backdrop maps to "back to editing" (OnFinishCancel).
        if (finishing) return;

        if (API.IsDisabledControlJustPressed(0, ControlExit) || API.IsControlJustPressed(0, ControlExit))
        {
            Stop();
            return;
        }

        if (RawKeyJustPressed(RawKeyEnter))
        {
            OpenFinishDialog();
            return;
        }

        if (RawKeyJustPressed(RawKeyF1)) emitNui(Nui.NuiEvents.EditorToggleLegend, null);

        if (RawKeyJustPressed(RawKey1))
            state = state.Apply(new EditorInput.SetMode(EditorMode.Translate));
        if (RawKeyJustPressed(RawKey2))
        {
            var enteringRotate = state.Mode != EditorMode.Rotate;
            state = state.Apply(new EditorInput.SetMode(EditorMode.Rotate));
            if (enteringRotate) state = state.Apply(new EditorInput.SelectAxis(EditorAxis.Z)); // yaw pre-active
        }
        if (RawKeyJustPressed(RawKey3))
        {
            state = state.Apply(new EditorInput.SetMode(EditorMode.Scale));
            // Scale has no Z; keep the arrow keys live.
            if (state.ActiveAxis == EditorAxis.Z) state = state.Apply(new EditorInput.SelectAxis(EditorAxis.X));
        }

        if (RawKeyJustPressed(RawKeyX)) state = state.Apply(new EditorInput.SelectAxis(EditorAxis.X));
        if (RawKeyJustPressed(RawKeyY)) state = state.Apply(new EditorInput.SelectAxis(EditorAxis.Y));
        if (RawKeyJustPressed(RawKeyZ) && state.Mode != EditorMode.Scale)
            state = state.Apply(new EditorInput.SelectAxis(EditorAxis.Z));

        if (API.IsDisabledControlJustPressed(0, ControlFrame))
            state = state.Apply(new EditorInput.ToggleReferenceFrame());
        if (RawKeyJustPressed(RawKeyL))
            state = state.Apply(new EditorInput.ToggleAspectLock());
        if (RawKeyJustPressed(RawKeyE) && pendingSnapHandle == 0)
            StartSnapProbe();
        state = CollectSnapProbe(state);

        var shift = API.IsControlPressed(0, ControlShift);
        var alt = API.IsControlPressed(0, ControlAlt);

        if (API.IsDisabledControlJustPressed(0, ControlArrowRight) || API.IsDisabledControlJustPressed(0, ControlArrowUp))
            state = state.Apply(new EditorInput.Nudge(1, shift, alt));
        if (API.IsDisabledControlJustPressed(0, ControlArrowLeft) || API.IsDisabledControlJustPressed(0, ControlArrowDown))
            state = state.Apply(new EditorInput.Nudge(-1, shift, alt));

        state = HandleMouse(state, alt);

        State = state;
    }

    /// <summary>True on the frame <paramref name="virtualKey" /> goes down. Edge-detected against the last tick.</summary>
    private bool RawKeyJustPressed(int virtualKey)
    {
        var isDown = API.IsRawKeyPressed(virtualKey);
        var wasDown = rawKeysDown.Contains(virtualKey);

        if (isDown) rawKeysDown.Add(virtualKey);
        else rawKeysDown.Remove(virtualKey);

        return isDown && !wasDown;
    }

    private ScreenEditorState HandleMouse(ScreenEditorState state, bool alt)
    {
        if (!API.IsDisabledControlPressed(0, ControlLmb))
        {
            dragging = false;
            dragReference = null;
            return state;
        }

        if (!dragging)
        {
            if (state.Mode == EditorMode.Scale)
            {
                if (!TryPickScaleHandle(state, out var handleAxis, out var handleWorld)) return state;
                dragging = true;
                dragReference = handleWorld;
                state = state.Apply(new EditorInput.SelectAxis(handleAxis));
            }
            else
            {
                if (!TryPickAxis(state, out var axis)) return state; // clicked empty space
                dragging = true;
                dragReference = null;
                state = state.Apply(new EditorInput.SelectAxis(axis));
            }
        }

        var mouseX = API.GetDisabledControlNormal(0, ControlLookLr) * MouseSensitivity;
        var mouseY = API.GetDisabledControlNormal(0, ControlLookUd) * MouseSensitivity;

        ScreenAxisDirection(state, out var dirX, out var dirY);
        var delta = mouseX * dirX + mouseY * dirY;

        return Math.Abs(delta) < 1e-5f ? state : state.Apply(new EditorInput.Drag(delta, alt));
    }

    private bool TryPickAxis(ScreenEditorState state, out EditorAxis picked)
    {
        var length = GizmoLength(state.Position);
        var tips = new List<Vector3>(3);
        foreach (var axis in Axes) tips.Add(Add(state.Position, Scale(AxisWorldDirection(state, axis), length)));

        var index = NearestToCrosshair(tips);
        picked = index < 0 ? state.ActiveAxis : Axes[index];
        return index >= 0;
    }

    /// <summary>Index of the world point whose screen projection sits nearest the crosshair, or -1 if none is in reach.</summary>
    private static int NearestToCrosshair(IReadOnlyList<Vector3> worldPoints)
    {
        var aspect = API.GetAspectRatio(false);
        var best = HandlePickTolerance;
        var found = -1;

        for (var i = 0; i < worldPoints.Count; i++)
        {
            if (!TryProject(worldPoints[i], out var screenX, out var screenY)) continue;

            var dx = (screenX - 0.5f) * aspect;
            var dy = screenY - 0.5f;
            var distance = (float)Math.Sqrt(dx * dx + dy * dy);

            if (distance >= best) continue;

            best = distance;
            found = i;
        }

        return found;
    }

    /// <summary>
    ///     Screen-space unit direction of the active axis, so "mouse toward the arrow tip" reads as positive.
    /// </summary>
    private void ScreenAxisDirection(ScreenEditorState state, out float dirX, out float dirY)
    {
        dirX = 1f;
        dirY = 0f;

        // Scale drag: direction is centre → the grabbed handle (outward grows the face).
        var reference = dragReference ?? Add(state.Position,
            Scale(AxisWorldDirection(state, state.ActiveAxis), GizmoLength(state.Position)));

        if (!TryProject(state.Position, out var centerX, out var centerY) ||
            !TryProject(reference, out var tipX, out var tipY))
            return;

        var dx = tipX - centerX;
        var dy = tipY - centerY;
        var magnitude = (float)Math.Sqrt(dx * dx + dy * dy);

        if (magnitude < 1e-6f) return;

        dirX = dx / magnitude;
        dirY = dy / magnitude;
    }

    private void Render()
    {
        var state = State!;

        PushState(state);
        DrawPlaceholder(state);
        DrawGizmo(state);
        DrawSnapHint();
        DrawCrosshair();
    }

    /// <summary>
    ///     Push the live values to the focusless NUI overlay every frame. It's a tiny payload and the
    ///     overlay mounts lazily on <see cref="Nui.NuiEvents.EditorOpen" />, so a per-frame resend is the
    ///     simplest way to guarantee it has data the moment its listener is up - no ready handshake.
    /// </summary>
    private void PushState(ScreenEditorState state)
    {
        emitNui(Nui.NuiEvents.EditorState, new ScreenEditorOverlayState(
            Round(state.Position, 0.01f),
            Round(state.Rotation, 0.01f),
            new Hypnonema.Shared.Vector2(
                ScreenEditorState.RoundToStep(state.Scale.X, 0.001f),
                ScreenEditorState.RoundToStep(state.Scale.Y, 0.001f)),
            state.Mode.ToString(),
            state.Frame.ToString(),
            state.ActiveAxis.ToString(),
            state.AspectLocked,
            state.IsDirty));
    }

    // Trim the wire values to a readable precision. ScreenEditorState.RoundToStep, not Math.Round(x, n) -
    // FiveM's Mono is missing that overload.
    private static Vector3 Round(Vector3 v, float step)
    {
        return new Vector3(
            ScreenEditorState.RoundToStep(v.X, step),
            ScreenEditorState.RoundToStep(v.Y, step),
            ScreenEditorState.RoundToStep(v.Z, step));
    }

    private void DrawPlaceholder(ScreenEditorState state)
    {
        if (placeholder is not { IsValid: true, IsLoaded: true }) return;

        placeholder.Render3D(state.Position.ToFxVector3(), state.Rotation.ToFxVector3(), state.Scale.ToFxVector3());
    }

    private static void DrawGizmo(ScreenEditorState state)
    {
        if (state.Mode == EditorMode.Scale)
        {
            DrawScaleHandles(state);
            return;
        }

        var length = GizmoLength(state.Position);
        var aspect = API.GetAspectRatio(false);

        foreach (var axis in Axes)
        {
            var tip = Add(state.Position, Scale(AxisWorldDirection(state, axis), length));
            AxisColor(axis, out var r, out var g, out var b);
            var active = axis == state.ActiveAxis;
            var alpha = active ? 255 : 130;

            API.DrawLine(state.Position.X, state.Position.Y, state.Position.Z, tip.X, tip.Y, tip.Z, r, g, b, alpha);

            // Guard the projection: World3dToScreen2d returns garbage behind the camera (no ghost handles).
            if (!TryProject(tip, out var screenX, out var screenY)) continue;

            var size = active ? 0.013f : 0.009f;
            API.DrawRect(screenX, screenY, size / aspect, size, r, g, b, alpha); // width / aspect keeps it square
        }
    }

    // Scale mode: handles at the face corners (drag = proportional resize; the aspect lock keeps y in step
    // with x). When the aspect lock is off, two extra edge handles drive x and y on their own.
    private readonly struct ScaleHandle
    {
        public ScaleHandle(Vector3 world, EditorAxis axis)
        {
            World = world;
            Axis = axis;
        }

        public Vector3 World { get; }

        public EditorAxis Axis { get; }
    }

    private static readonly float[] CornerSigns = { -1f, 1f };

    private static List<ScaleHandle> ScaleHandles(ScreenEditorState state)
    {
        var right = VectorMath.Rotate(new Vector3(1f, 0f, 0f), state.Rotation);
        var up = VectorMath.Rotate(new Vector3(0f, 0f, 1f), state.Rotation);
        var halfWidth = Math.Abs(state.Scale.X) * Graphics.ScaleformTarget.WorldUnitsPerScaleUnit / 2f;
        var halfHeight = Math.Abs(state.Scale.Y) * Graphics.ScaleformTarget.WorldUnitsPerScaleUnit / 2f;

        var handles = new List<ScaleHandle>(6);
        foreach (var sx in CornerSigns)
        foreach (var sy in CornerSigns)
            handles.Add(new ScaleHandle(
                Add(state.Position, Add(Scale(right, sx * halfWidth), Scale(up, sy * halfHeight))),
                EditorAxis.X)); // corner → the aspect-lock driver (x; y follows the texture ratio)

        if (!state.AspectLocked)
        {
            handles.Add(new ScaleHandle(Add(state.Position, Scale(right, halfWidth)), EditorAxis.X));
            handles.Add(new ScaleHandle(Add(state.Position, Scale(up, halfHeight)), EditorAxis.Y));
        }

        return handles;
    }

    private static void DrawScaleHandles(ScreenEditorState state)
    {
        var aspect = API.GetAspectRatio(false);

        foreach (var handle in ScaleHandles(state))
        {
            AxisColor(handle.Axis, out var r, out var g, out var b);

            API.DrawLine(state.Position.X, state.Position.Y, state.Position.Z,
                handle.World.X, handle.World.Y, handle.World.Z, r, g, b, 150);

            if (!TryProject(handle.World, out var screenX, out var screenY)) continue;

            API.DrawRect(screenX, screenY, 0.014f / aspect, 0.014f, r, g, b, 230);
        }
    }

    private bool TryPickScaleHandle(ScreenEditorState state, out EditorAxis axis, out Vector3 world)
    {
        var handles = ScaleHandles(state);
        var points = new List<Vector3>(handles.Count);
        foreach (var handle in handles) points.Add(handle.World);

        var index = NearestToCrosshair(points);
        if (index < 0)
        {
            axis = state.ActiveAxis;
            world = state.Position;
            return false;
        }

        axis = handles[index].Axis;
        world = handles[index].World;
        return true;
    }

    /// <summary>
    ///     Fire the surface-snap ray (<c>E</c>) from the camera through the crosshair; <see cref="CollectSnapProbe" />
    ///     reads it back once the trace finishes. Same async shape-test flow as
    ///     <see cref="Media.Audio.OcclusionProbe" />.
    /// </summary>
    private void StartSnapProbe()
    {
        var camera = API.GetGameplayCamCoord();
        var direction = DirectionFromRotation(API.GetGameplayCamRot(2));
        var end = camera + direction * SnapRayDistance;

        pendingSnapHandle = API.StartShapeTestRay(
            camera.X, camera.Y, camera.Z, end.X, end.Y, end.Z,
            SnapShapeTestFlags, API.GetPlayerPed(API.PlayerId()), 4);
    }

    /// <summary>
    ///     Poll the pending snap ray. On a hit the face lands flush on the surface (offset + facing come from
    ///     <see cref="ScreenEditorState" />); on a miss (sky, out of range) the face is left untouched.
    /// </summary>
    private ScreenEditorState CollectSnapProbe(ScreenEditorState state)
    {
        if (pendingSnapHandle == 0) return state;

        var hit = false;
        var hitCoords = CitizenFX.Core.Vector3.Zero;
        var surfaceNormal = CitizenFX.Core.Vector3.Zero;
        var entityHit = 0;

        // 1 means the trace is still running; anything else releases the handle.
        if (API.GetShapeTestResult(pendingSnapHandle, ref hit, ref hitCoords, ref surfaceNormal, ref entityHit) == 1)
            return state;

        pendingSnapHandle = 0;
        if (!hit) return state;

        lastSnapHit = new Vector3(hitCoords.X, hitCoords.Y, hitCoords.Z);
        lastSnapNormal = new Vector3(surfaceNormal.X, surfaceNormal.Y, surfaceNormal.Z);
        lastSnapHintUntilMs = API.GetGameTimer() + SnapHintDurationMs;

        return state.Apply(new EditorInput.SnapToSurface(lastSnapHit, lastSnapNormal));
    }

    /// <summary>Draw the last snap hit as a stub normal so it's clear which surface the ray grabbed.</summary>
    private void DrawSnapHint()
    {
        if (lastSnapHit is not { } hit || API.GetGameTimer() > lastSnapHintUntilMs) return;

        var tip = Add(hit, Scale(lastSnapNormal, 0.5f));
        API.DrawLine(hit.X, hit.Y, hit.Z, tip.X, tip.Y, tip.Z, 255, 214, 84, 255);
    }

    private static void DrawCrosshair()
    {
        var aspect = API.GetAspectRatio(false);

        API.DrawRect(0.5f, 0.5f, 0.012f / aspect, 0.0018f, 255, 255, 255, 200);
        API.DrawRect(0.5f, 0.5f, 0.0018f / aspect, 0.012f, 255, 255, 255, 200);
    }

    private async Task LoadPlaceholderAsync(int forGeneration)
    {
        // Fire-and-forget from Start(); a native hiccup here must never surface as an unobserved task fault.
        try
        {
            var movie = new Scaleform(PlaceholderMovie);
            var deadline = DateTime.UtcNow.AddSeconds(PlaceholderLoadTimeoutSeconds);

            while (!movie.IsLoaded)
            {
                if (forGeneration != generation)
                {
                    movie.Dispose();
                    return;
                }

                if (DateTime.UtcNow >= deadline)
                {
                    logger.Warning(
                        $"Editor placeholder movie '{PlaceholderMovie}' did not load within {PlaceholderLoadTimeoutSeconds}s; " +
                        "the session runs without a visible surface. Ship assets/stream/hypnonema_screen_editor_placeholder.gfx.");
                    movie.Dispose();
                    return;
                }

                await BaseScript.Delay(50);
            }

            if (forGeneration != generation)
            {
                movie.Dispose();
                return;
            }

            placeholder = movie;
            logger.Debug($"Editor placeholder movie loaded ({movie.Handle}).");
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Loading the editor placeholder movie failed");
        }
    }

    private void ReleasePlaceholder()
    {
        placeholder?.Dispose();
        placeholder = null;
    }

    private static ScaleformTransform BuildStartTransform(ScreenEditorStartOptions? options)
    {
        if (options is null) return BuildSpawnTransform();

        var source = options.Screen.Scaleform.Transform;
        var position = new Vector3(source.Position.X, source.Position.Y, source.Position.Z);
        var rotation = new Vector3(source.Rotation.X, source.Rotation.Y, source.Rotation.Z);
        var scale = new Vector3(source.Scale.X, source.Scale.Y, source.Scale.Z);

        if (options.AsTemplate)
        {
            var right = VectorMath.Rotate(new Vector3(1f, 0f, 0f), rotation);
            position = new Vector3(
                position.X + right.X * TemplateLateralOffset,
                position.Y + right.Y * TemplateLateralOffset,
                position.Z + right.Z * TemplateLateralOffset);
        }

        return new ScaleformTransform { Position = position, Rotation = rotation, Scale = scale };
    }

    private static ScaleformTransform BuildSpawnTransform()
    {
        // Natives, not the CitizenFX.Core wrappers: Game.PlayerPed goes through Entity.FromHandle, which
        // returns null (→ NRE on .Position) whenever the ped handle is briefly invalid, and the rest of
        // this resource already reads the player/camera through API.* for the same reason.
        var camera = API.GetGameplayCamCoord();
        var forward = DirectionFromRotation(API.GetGameplayCamRot(2));

        var flat = new CitizenFX.Core.Vector3(forward.X, forward.Y, 0f);
        flat = flat.Length() < 1e-3f
            ? new CitizenFX.Core.Vector3(0f, 1f, 0f)
            : CitizenFX.Core.Vector3.Normalize(flat);

        var center = camera + flat * SpawnDistance;
        var playerZ = API.GetEntityCoords(API.GetPlayerPed(API.PlayerId()), false).Z;
        var eyeLevel = playerZ + EyeHeight;

        // The face looks along its local +Y; turn it back toward the player.
        var rotation = VectorMath.EulerFacing(new Vector3(-flat.X, -flat.Y, 0f));

        return new ScaleformTransform
        {
            Position = new Vector3(center.X, center.Y, eyeLevel),
            Rotation = rotation,
            Scale = new Vector3(1f, 0.5625f, -0.1f) // ~16:9, ~2 m wide via ScaleformTarget's 2 world-units-per-scale-unit
        };
    }

    private static Vector3 AxisWorldDirection(ScreenEditorState state, EditorAxis axis)
    {
        var unit = axis switch
        {
            EditorAxis.X => new Vector3(1f, 0f, 0f),
            EditorAxis.Y => new Vector3(0f, 1f, 0f),
            _ => new Vector3(0f, 0f, 1f)
        };

        return state.Frame == EditorReferenceFrame.World ? unit : VectorMath.Rotate(unit, state.Rotation);
    }

    private static float GizmoLength(Vector3 center)
    {
        var distance = Distance(API.GetGameplayCamCoord(), center);
        return Clamp(distance * GizmoScreenFraction, GizmoMinLength, GizmoMaxLength);
    }

    /// <summary>GTA rotation (degrees, pitch X / yaw Z) → a unit forward vector, matching the engine's own.</summary>
    private static CitizenFX.Core.Vector3 DirectionFromRotation(CitizenFX.Core.Vector3 rotationDeg)
    {
        const double deg2Rad = Math.PI / 180d;
        var z = rotationDeg.Z * deg2Rad;
        var x = rotationDeg.X * deg2Rad;
        var flatLength = Math.Abs(Math.Cos(x));

        return new CitizenFX.Core.Vector3(
            (float)(-Math.Sin(z) * flatLength),
            (float)(Math.Cos(z) * flatLength),
            (float)Math.Sin(x));
    }

    private static bool TryProject(Vector3 world, out float screenX, out float screenY)
    {
        screenX = 0f;
        screenY = 0f;
        return API.World3dToScreen2d(world.X, world.Y, world.Z, ref screenX, ref screenY);
    }

    // Out params, not a tuple - FiveM's Mono has no System.ValueTuple. Matches the native gizmo colours
    // mirrored in the NUI overlay's AXIS_COLOR.
    private static void AxisColor(EditorAxis axis, out int r, out int g, out int b)
    {
        switch (axis)
        {
            case EditorAxis.X:
                r = 235; g = 64; b = 52;
                break;
            case EditorAxis.Y:
                r = 66; g = 214; b = 92;
                break;
            default:
                r = 74; g = 136; b = 255;
                break;
        }
    }

    private static Vector3 Add(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    private static Vector3 Scale(Vector3 v, float s)
    {
        return new Vector3(v.X * s, v.Y * s, v.Z * s);
    }

    private static float Distance(CitizenFX.Core.Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X, dy = a.Y - b.Y, dz = a.Z - b.Z;
        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static float Clamp(float value, float min, float max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    private void Notify(string message)
    {
        triggerEvent("chat:addMessage", new object[] { new { args = new[] { $"[Hypnonema] {message}" } } });
    }
}

/// <summary>
///     How a session was opened from the Screens list (ticket 08). <see cref="AsTemplate" /> starts the
///     face offset to the side and saves it as a brand-new screen; otherwise the session edits
///     <see cref="Screen" /> in place.
/// </summary>
public sealed record ScreenEditorStartOptions(Screen Screen, bool AsTemplate);
