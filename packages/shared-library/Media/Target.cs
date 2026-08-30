namespace Hypnonema.Shared.Media;

public enum TargetType
{
    Model,
    Screen
}

public abstract class Target
{
    public abstract TargetType Type { get; }

    public static bool AreEqual(Target? a, Target? b)
    {
        return (a, b) switch
        {
            (ScreenTarget x, ScreenTarget y) => x.Screen.Id == y.Screen.Id,
            (ModelTarget x, ModelTarget y) => x.Model.Prop == y.Model.Prop,
            _ => false
        };
    }
}

public sealed class ModelTarget(Model model) : Target
{
    public override TargetType Type => TargetType.Model;

    public Model Model { get; set; } = model;
}

public sealed class ScreenTarget(Screen screen) : Target
{
    public override TargetType Type => TargetType.Screen;

    public Screen Screen { get; set; } = screen;
}