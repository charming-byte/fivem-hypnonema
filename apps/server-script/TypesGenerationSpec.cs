using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hypnonema.Shared;
using Hypnonema.Shared.Media;
using Hypnonema.Shared.Media.Audio;
using TypeGen.Core.SpecGeneration;

namespace Hypnonema.Server;

public sealed class TypesGenerationSpec : GenerationSpec
{
    public TypesGenerationSpec()
    {
        AddInterface<Model>();

        AddInterface<Size2>()
            .Member(nameof(Size2.Default))
            .Ignore();

        AddInterface<Vector3>()
            .Member(nameof(Vector3.Zero))
            .Ignore();

        AddInterface<Vector2>()
            .Member(nameof(Vector2.Zero))
            .Ignore();

        AddInterface<Target>();

        AddInterface<Screen>()
            .Member(nameof(Screen.Id))
            .Optional();

        AddInterface<ScreenTarget>()
            .IgnoreBase();
        AddInterface<ModelTarget>()
            .IgnoreBase();

        AddEnum<RenderMode>();

        AddInterface<ITrack>();

        AddEnum<AudioMode>();
        AddEnum<YoutubeAdStatus>("dui");

        AddInterface<AudioConfiguration>();

        AddInterface<DistanceAttenuation>()
            .Member(nameof(DistanceAttenuation.Default))
            .Ignore();

        AddInterface<DefaultAudioConfiguration>()
            .Member(nameof(DefaultAudioConfiguration.Default))
            .Ignore();

        AddInterface<ScaleformRenderConfiguration>()
            .Member(nameof(ScaleformRenderConfiguration.Default))
            .Ignore();

        AddInterface<UserInterfaceScreen>();

        AddInterface<ScreenEditorOverlayState>("nui");

        AddInterface<UserInterfaceMediaPlayer>()
            .IgnoreBase()
            .CustomHeader(
                "import { ScreenTarget } from './screen-target';\r\nimport { ModelTarget } from './model-target';\n\n")
            .Member(nameof(UserInterfaceMediaPlayer.CurrentTrack))
            .Optional()
            .Member(nameof(UserInterfaceMediaPlayer.Queue))
            .Optional()
            .Member(nameof(UserInterfaceMediaPlayer.Target))
            .TypeUnions(nameof(ScreenTarget), nameof(ModelTarget))
            .Member(nameof(UserInterfaceMediaPlayer.Error))
            .Optional()
            .Member(nameof(UserInterfaceMediaPlayer.Scaleform))
            .Optional();

        AddClass<Dui.DuiEvents>("dui");
        AddClass<Dui.DuiCallbacks>("dui");
        AddInterface<Dui.DuiClickRequest>("dui");

        AddClass<Nui.NuiEvents>("nui");
    }

    public override void OnBeforeBarrelGeneration(OnBeforeBarrelGenerationArgs args)
    {
        AddBarrel(".");

        var directories = GetAllDirectoriesRecursive(args.GeneratorOptions.BaseOutputDirectory)
            .Select(x => GetPathDiff(args.GeneratorOptions.BaseOutputDirectory, x));

        foreach (var directory in directories) AddBarrel(directory);
    }

    private string GetPathDiff(string pathFrom, string pathTo)
    {
        var pathFromUri = new Uri("file:///" + pathFrom?.Replace('\\', '/'));
        var pathToUri = new Uri("file:///" + pathTo?.Replace('\\', '/'));

        return pathFromUri.MakeRelativeUri(pathToUri).ToString();
    }

    private IEnumerable<string> GetAllDirectoriesRecursive(string directory)
    {
        var result = new List<string>();
        string[] subdirectories = Directory.GetDirectories(directory);

        if (!subdirectories.Any()) return result;

        result.AddRange(subdirectories);

        foreach (var subdirectory in subdirectories) result.AddRange(GetAllDirectoriesRecursive(subdirectory));

        return result;
    }
}