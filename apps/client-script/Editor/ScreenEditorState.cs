using System;
using Hypnonema.Shared;
using Hypnonema.Shared.Media;
using Vector3 = Hypnonema.Shared.Vector3;

namespace Hypnonema.Client.Editor;

public enum EditorMode
{
    Translate,
    Rotate,
    Scale
}

public enum EditorAxis
{
    X,
    Y,
    Z
}

public enum EditorReferenceFrame
{
    /// <summary>Manipulation runs along the face's own axes (default).</summary>
    Local,

    /// <summary>Manipulation runs along the world axes.</summary>
    World
}

/// <summary>
///     A single manipulation the editor can apply to the working transform. The native shell (scaleform,
///     crosshair, raycast, per-frame tick) translates player input into one of these and feeds it to
///     <see cref="ScreenEditorState.Apply" /> — it never does transform math itself.
/// </summary>
public abstract record EditorInput
{
    private EditorInput()
    {
    }

    /// <summary>Switch between Translate (<c>1</c>) / Rotate (<c>2</c>) / Scale (<c>3</c>).</summary>
    public sealed record SetMode(EditorMode Mode) : EditorInput;

    /// <summary>Pick the axis (or, in scale, the corner driver) that subsequent nudges/drags act on.</summary>
    public sealed record SelectAxis(EditorAxis Axis) : EditorInput;

    /// <summary>
    ///     A keyboard step along the active axis. <paramref name="Direction" /> is normally ±1. <c>Shift</c>
    ///     multiplies the base step by 10, <c>Alt</c> by 0.1; in Rotate, <c>Alt</c> also releases the 5° grid.
    /// </summary>
    public sealed record Nudge(int Direction, bool Shift = false, bool Alt = false) : EditorInput;

    /// <summary>
    ///     One normalised pointer delta for the current frame while the mouse button is held.
    ///     <c>Alt</c> releases the rotation grid, same as <see cref="Nudge" />.
    /// </summary>
    public sealed record Drag(float Delta, bool Alt = false) : EditorInput;

    /// <summary>Flip the reference frame; shared by Translate and Rotate (<c>G</c>).</summary>
    public sealed record ToggleReferenceFrame : EditorInput;

    /// <summary>Lock/unlock the scale aspect ratio (<c>y</c> follows <c>x</c> when locked).</summary>
    public sealed record ToggleAspectLock : EditorInput;

    /// <summary>
    ///     One-shot: snap the face flush onto a surface hit (<c>E</c>). Position lands on
    ///     <paramref name="Point" /> plus a small offset along <paramref name="Normal" />; rotation faces the
    ///     normal. Not a mode — manual manipulation stays free afterwards.
    /// </summary>
    public sealed record SnapToSurface(Vector3 Point, Vector3 Normal) : EditorInput;
}

/// <summary>
///     The pure, FiveM-free manipulation logic of the screen editor. An immutable snapshot of the working
///     transform plus the editor's interaction state; <see cref="Apply" /> maps an <see cref="EditorInput" />
///     to the next snapshot. No rendering, no native calls — those live in the untested shell around it.
/// </summary>
public sealed class ScreenEditorState
{
    public const float TranslateStep = 0.05f;
    public const float RotateStep = 5f;
    public const float ScaleStep = 0.05f;

    public const float RotationGrid = 5f;

    public const float CoarseFactor = 10f;
    public const float FineFactor = 0.1f;

    public const float SnapOffset = 0.02f;

    public const float ScaleMin = 0.05f;

    // At WorldUnitsPerScaleUnit = 2 (ScaleformTarget) this is a ~50 m wide quad - covers cinema/stadium
    // screens. The server's ScreenValidator still rejects NaN/Infinity and absurd coordinates.
    public const float ScaleMax = 25f;

    // ponytail: raw guesses for "normalised pointer delta" → world units; the shell ticket tunes these
    // against real World3dToScreen2d deltas without touching the tested branch logic.
    public const float DragTranslateScale = 2f;
    public const float DragRotateScale = 90f;
    public const float DragScaleScale = 2f;

    private ScreenEditorState(Vector3 position, Vector3 rotation, Vector3 scale, EditorMode mode,
        EditorAxis activeAxis, EditorReferenceFrame frame, bool aspectLocked, Size2 textureDimension, bool isDirty)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
        Mode = mode;
        ActiveAxis = activeAxis;
        Frame = frame;
        AspectLocked = aspectLocked;
        TextureDimension = textureDimension;
        IsDirty = isDirty;
    }

    public Vector3 Position { get; }

    public Vector3 Rotation { get; }

    public Vector3 Scale { get; }

    public EditorMode Mode { get; }

    public EditorAxis ActiveAxis { get; }

    public EditorReferenceFrame Frame { get; }

    public bool AspectLocked { get; }

    /// <summary>Texture pixel size the aspect lock derives its ratio from (default 1280×720 → 16:9).</summary>
    public Size2 TextureDimension { get; }

    /// <summary>Whether any input has changed the transform since the session started.</summary>
    public bool IsDirty { get; }

    /// <summary><c>y / x</c> the aspect lock keeps, i.e. texture height / width.</summary>
    public float AspectRatio => TextureDimension.Width > 0
        ? (float)TextureDimension.Height / TextureDimension.Width
        : 9f / 16f;

    public static ScreenEditorState FromTransform(ScaleformTransform transform, Size2? textureDimension = null)
    {
        if (transform is null) throw new ArgumentNullException(nameof(transform));

        return new ScreenEditorState(Clone(transform.Position), Clone(transform.Rotation), Clone(transform.Scale),
            EditorMode.Translate, EditorAxis.X, EditorReferenceFrame.Local, aspectLocked: true,
            textureDimension ?? ScaleformTexture.StageDimension, isDirty: false);
    }

    public ScaleformTransform ToTransform()
    {
        return new ScaleformTransform
        {
            Position = Clone(Position),
            Rotation = Clone(Rotation),
            Scale = Clone(Scale)
        };
    }

    public ScreenEditorState Apply(EditorInput input)
    {
        switch (input)
        {
            case EditorInput.SetMode m:
                return With(mode: m.Mode);
            case EditorInput.SelectAxis a:
                return With(axis: a.Axis);
            case EditorInput.ToggleReferenceFrame:
                return With(frame: Frame == EditorReferenceFrame.Local
                    ? EditorReferenceFrame.World
                    : EditorReferenceFrame.Local);
            case EditorInput.ToggleAspectLock:
                return With(aspectLocked: !AspectLocked);
            case EditorInput.Nudge n:
                return ApplyDelta(NudgeAmount(n), n.Alt);
            case EditorInput.Drag d:
                return ApplyDelta(d.Delta * DragScale(), d.Alt);
            case EditorInput.SnapToSurface s:
                return Snap(s.Point, s.Normal);
            default:
                return this;
        }
    }

    private float NudgeAmount(EditorInput.Nudge n)
    {
        var modifier = n.Shift ? CoarseFactor : n.Alt ? FineFactor : 1f;
        var step = Mode switch
        {
            EditorMode.Translate => TranslateStep,
            EditorMode.Rotate => RotateStep,
            _ => ScaleStep
        };

        return n.Direction * step * modifier;
    }

    private float DragScale()
    {
        return Mode switch
        {
            EditorMode.Translate => DragTranslateScale,
            EditorMode.Rotate => DragRotateScale,
            _ => DragScaleScale
        };
    }

    private ScreenEditorState ApplyDelta(float amount, bool alt)
    {
        switch (Mode)
        {
            case EditorMode.Translate:
            {
                var local = AxisVector(ActiveAxis, amount);
                var world = Frame == EditorReferenceFrame.World ? local : VectorMath.Rotate(local, Rotation);
                return With(position: Add(Position, world), dirty: true);
            }
            case EditorMode.Rotate:
            {
                // Compose the delta with the current orientation so the reference frame actually matters:
                // world-axis rotation pre-multiplies, face-local rotation post-multiplies. Identical for a
                // yaw-only face, divergent once it is pitched/rolled. The Euler round-trip uses VectorMath's
                // own X→Y→Z order - the same convention the rest of the editor already assumes for the
                // native draw; worth a sanity check in-game on a steeply pitched face.
                var current = VectorMath.FromEuler(Rotation);
                var delta = VectorMath.AxisRotation(ActiveAxis, amount);
                var composed = Frame == EditorReferenceFrame.World
                    ? VectorMath.Mul(delta, current)
                    : VectorMath.Mul(current, delta);

                var rotation = VectorMath.ToEuler(composed);
                if (!alt)
                    // ponytail: snapping one Euler component is exact for axis-aligned turns and a hair off
                    // for a compound orientation - fine for a placement grid.
                    SetComponent(rotation, ActiveAxis, RoundToStep(Component(rotation, ActiveAxis), RotationGrid));
                return With(rotation: rotation, dirty: true);
            }
            default:
                return With(scale: ApplyScale(amount), dirty: true);
        }
    }

    private Vector3 ApplyScale(float amount)
    {
        if (AspectLocked)
        {
            // Locked: every scale delta drives x (the corner handle); y follows the texture ratio.
            // ponytail: if the y-clamp bites (very small x) the ratio breaks by a hair — accepted.
            var x = Clamp(Scale.X + amount);
            return new Vector3(x, Clamp(x * AspectRatio), Scale.Z);
        }

        var scale = Clone(Scale);
        if (ActiveAxis == EditorAxis.X) scale.X = Clamp(scale.X + amount);
        else if (ActiveAxis == EditorAxis.Y) scale.Y = Clamp(scale.Y + amount);
        return scale;
    }

    private ScreenEditorState Snap(Vector3 point, Vector3 normal)
    {
        var n = VectorMath.Normalize(normal);
        if (n is null) return this; // degenerate normal (no surface hit) → nothing happens

        var position = new Vector3(point.X + n.X * SnapOffset, point.Y + n.Y * SnapOffset, point.Z + n.Z * SnapOffset);

        // A wall (normal mostly horizontal) snaps plumb: take only the horizontal part of the normal for
        // the facing, so a collision normal that reads a few degrees off vertical - common on map and prop
        // meshes - can't tip the screen. Floor/ceiling/steeply-angled surfaces (normal mostly vertical)
        // still align to the raw normal. ponytail: 0.75 ≈ within ~40° of vertical counts as a wall - tune.
        var horizontal = (float)Math.Sqrt(n.X * n.X + n.Y * n.Y);
        var facing = horizontal > 0.75f
            ? VectorMath.Normalize(new Vector3(n.X, n.Y, 0f)) ?? n
            : n;

        return With(position: position, rotation: VectorMath.EulerFacing(facing), dirty: true);
    }

    private ScreenEditorState With(Vector3? position = null, Vector3? rotation = null, Vector3? scale = null,
        EditorMode? mode = null, EditorAxis? axis = null, EditorReferenceFrame? frame = null,
        bool? aspectLocked = null, bool dirty = false)
    {
        return new ScreenEditorState(position ?? Clone(Position), rotation ?? Clone(Rotation), scale ?? Clone(Scale),
            mode ?? Mode, axis ?? ActiveAxis, frame ?? Frame, aspectLocked ?? AspectLocked, TextureDimension,
            IsDirty || dirty);
    }

    private static float Clamp(float value)
    {
        return Math.Max(ScaleMin, Math.Min(ScaleMax, value));
    }

    /// <summary>
    ///     Round <paramref name="value" /> to the nearest multiple of <paramref name="step" />, half away
    ///     from zero. Hand-rolled: FiveM's Mono is missing the <see cref="Math" />.<c>Round</c> overloads
    ///     that take a digit count or a <see cref="MidpointRounding" />.
    /// </summary>
    internal static float RoundToStep(float value, float step)
    {
        if (step <= 0f) return value;

        var n = value / step;
        var rounded = n < 0f ? Math.Ceiling(n - 0.5) : Math.Floor(n + 0.5);
        return (float)(rounded * step);
    }

    private static Vector3 AxisVector(EditorAxis axis, float amount)
    {
        return axis switch
        {
            EditorAxis.X => new Vector3(amount, 0f, 0f),
            EditorAxis.Y => new Vector3(0f, amount, 0f),
            _ => new Vector3(0f, 0f, amount)
        };
    }

    private static Vector3 Add(Vector3 a, Vector3 b)
    {
        return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    private static Vector3 Clone(Vector3 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }

    private static float Component(Vector3 v, EditorAxis axis)
    {
        return axis switch
        {
            EditorAxis.X => v.X,
            EditorAxis.Y => v.Y,
            _ => v.Z
        };
    }

    private static void SetComponent(Vector3 v, EditorAxis axis, float value)
    {
        switch (axis)
        {
            case EditorAxis.X:
                v.X = value;
                break;
            case EditorAxis.Y:
                v.Y = value;
                break;
            default:
                v.Z = value;
                break;
        }
    }
}

/// <summary>
///     Euler-degree vector math for the editor. Rotation is <c>(pitch X, roll Y, yaw Z)</c> in degrees, matching
///     <see cref="ScaleformTransform.Rotation" /> and the game's heading-on-Z convention. The face looks along
///     its local <c>+Y</c>.
///     ponytail: everything here composes X→Y→Z. That is the editor's own convention end to end (gizmo axes,
///     translate, snap, rotate) - a steeply pitched face is still worth an in-game sanity check against the
///     native draw, but nothing downstream mixes orders.
/// </summary>
internal static class VectorMath
{
    private const double Deg2Rad = Math.PI / 180d;
    private const double Rad2Deg = 180d / Math.PI;

    /// <summary>A 3×3 rotation held as its three column vectors (the images of the basis axes).</summary>
    internal readonly struct Mat3
    {
        public Mat3(Vector3 c0, Vector3 c1, Vector3 c2)
        {
            C0 = c0;
            C1 = c1;
            C2 = c2;
        }

        public Vector3 C0 { get; }

        public Vector3 C1 { get; }

        public Vector3 C2 { get; }

        public Vector3 Apply(Vector3 v)
        {
            return new Vector3(
                C0.X * v.X + C1.X * v.Y + C2.X * v.Z,
                C0.Y * v.X + C1.Y * v.Y + C2.Y * v.Z,
                C0.Z * v.X + C1.Z * v.Y + C2.Z * v.Z);
        }
    }

    /// <summary>Matrix product <c>a · b</c>.</summary>
    public static Mat3 Mul(Mat3 a, Mat3 b)
    {
        return new Mat3(a.Apply(b.C0), a.Apply(b.C1), a.Apply(b.C2));
    }

    /// <summary>The rotation matrix for the Euler degrees in <paramref name="eulerDeg" /> (X, then Y, then Z).</summary>
    public static Mat3 FromEuler(Vector3 eulerDeg)
    {
        return new Mat3(
            Rotate(new Vector3(1f, 0f, 0f), eulerDeg),
            Rotate(new Vector3(0f, 1f, 0f), eulerDeg),
            Rotate(new Vector3(0f, 0f, 1f), eulerDeg));
    }

    /// <summary>The rotation matrix for <paramref name="deg" /> about a single basis axis.</summary>
    public static Mat3 AxisRotation(EditorAxis axis, float deg)
    {
        var euler = axis switch
        {
            EditorAxis.X => new Vector3(deg, 0f, 0f),
            EditorAxis.Y => new Vector3(0f, deg, 0f),
            _ => new Vector3(0f, 0f, deg)
        };
        return FromEuler(euler);
    }

    /// <summary>Inverse of <see cref="FromEuler" />: recover <c>(pitch X, roll Y, yaw Z)</c> degrees from a matrix.</summary>
    public static Vector3 ToEuler(Mat3 m)
    {
        // M = Rz·Ry·Rx, so C0 = (cy·cz, cy·sz, -sy); C1.Z = sx·cy; C2.Z = cx·cy.
        var sinPitch = Math.Max(-1d, Math.Min(1d, -m.C0.Z));
        var y = Math.Asin(sinPitch) * Rad2Deg;

        double x, z;
        if (Math.Abs(m.C0.Z) < 0.99999f)
        {
            x = Math.Atan2(m.C1.Z, m.C2.Z) * Rad2Deg;
            z = Math.Atan2(m.C0.Y, m.C0.X) * Rad2Deg;
        }
        else
        {
            // Gimbal lock (pitch ≈ ±90°): yaw is indeterminate, fold it into roll.
            x = Math.Atan2((sinPitch >= 0d ? 1d : -1d) * m.C1.X, m.C1.Y) * Rad2Deg;
            z = 0d;
        }

        return new Vector3((float)x, (float)y, (float)z);
    }

    /// <summary>Rotate <paramref name="v" /> by the Euler degrees in <paramref name="eulerDeg" /> (X, then Y, then Z).</summary>
    public static Vector3 Rotate(Vector3 v, Vector3 eulerDeg)
    {
        var p = RotateX(v, eulerDeg.X);
        p = RotateY(p, eulerDeg.Y);
        p = RotateZ(p, eulerDeg.Z);
        return p;
    }

    public static Vector3? Normalize(Vector3 v)
    {
        var length = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        if (length < 1e-6) return null;

        return new Vector3((float)(v.X / length), (float)(v.Y / length), (float)(v.Z / length));
    }

    /// <summary>The Euler rotation whose local <c>+Y</c> points along <paramref name="forward" /> (assumed unit), roll 0.</summary>
    public static Vector3 EulerFacing(Vector3 forward)
    {
        var pitch = Math.Asin(Math.Max(-1d, Math.Min(1d, forward.Z))) * Rad2Deg;
        var yaw = Math.Atan2(-forward.X, forward.Y) * Rad2Deg;
        return new Vector3((float)pitch, 0f, (float)yaw);
    }

    private static Vector3 RotateX(Vector3 v, float deg)
    {
        var a = deg * Deg2Rad;
        double cos = Math.Cos(a), sin = Math.Sin(a);
        return new Vector3(v.X, (float)(v.Y * cos - v.Z * sin), (float)(v.Y * sin + v.Z * cos));
    }

    private static Vector3 RotateY(Vector3 v, float deg)
    {
        var a = deg * Deg2Rad;
        double cos = Math.Cos(a), sin = Math.Sin(a);
        return new Vector3((float)(v.X * cos + v.Z * sin), v.Y, (float)(-v.X * sin + v.Z * cos));
    }

    private static Vector3 RotateZ(Vector3 v, float deg)
    {
        var a = deg * Deg2Rad;
        double cos = Math.Cos(a), sin = Math.Sin(a);
        return new Vector3((float)(v.X * cos - v.Y * sin), (float)(v.X * sin + v.Y * cos), v.Z);
    }
}
