using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hypnonema.Server.Configurations;
using Hypnonema.Shared.Configurations;
using Hypnonema.Shared.Media;
using Serilog;

namespace Hypnonema.Server.Media;

public sealed class ScreenRepository
{
    private const string FileName = "screens.yaml";

    private readonly string configurationDirectory;
    private readonly ILogger logger;
    private readonly List<Screen> screens;
    private readonly object writeLock = new();

    public ScreenRepository(List<Screen> screens, string configurationDirectory, ILogger logger)
    {
        this.screens = screens ?? throw new ArgumentNullException(nameof(screens));
        this.configurationDirectory =
            configurationDirectory ?? throw new ArgumentNullException(nameof(configurationDirectory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IReadOnlyList<Screen> All => screens;

    public Screen Create(Screen screenDto)
    {
        if (screenDto == null) throw new ArgumentNullException(nameof(screenDto));

        lock (writeLock)
        {
            var errors = ScreenValidator.Validate(screenDto);
            if (errors.Count > 0) throw new ScreenValidationException(errors);

            var id = screenDto.ResolveId();

            if (screens.Any(s => s.ResolveId() == id))
                throw new ScreenValidationException(new[]
                {
                    $"A screen with id '{id}' already exists."
                });
            
            var screen = new Screen()
            {
                Id = id,
                Name = screenDto.Name,      
                Audio = screenDto.Audio,
                RenderDistance = screenDto.RenderDistance,
                Scaleform = screenDto.Scaleform
            };

            return Save(screen);
        }
    }

    public Screen Update(Screen screen)
    {
        if (screen == null) throw new ArgumentNullException(nameof(screen));

        lock (writeLock)
        {
            var errors = ScreenValidator.Validate(screen);
            if (errors.Count > 0) throw new ScreenValidationException(errors);

            // Match on ResolveId(), not raw Id: screens loaded from a hand-authored screens.yaml have no
            // explicit id, and the editor addresses them by their slug identity (ticket 01 / 08).
            var id = screen.ResolveId();
            if (string.IsNullOrWhiteSpace(id) || screens.All(s => s.ResolveId() != id))
                throw new ScreenNotFoundException(id ?? string.Empty);

            return Save(screen);
        }
    }

    private Screen Save(Screen screen)
    {
        var workingCopy = new List<Screen>(screens);
        var existingIndex = workingCopy.FindIndex(s => s.ResolveId() == screen.ResolveId());

        if (existingIndex >= 0)
            workingCopy[existingIndex] = screen;
        else
            workingCopy.Add(screen);

        Persist(workingCopy);

        ApplyWorkingCopy(workingCopy);

        logger.Information("Saved screen '{name}' ({id}) to {file}.", screen.Name, screen.Id, FileName);

        return screen;
    }

    public void Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id must not be empty.", nameof(id));

        lock (writeLock)
        {
            var workingCopy = new List<Screen>(screens);
            var removed = workingCopy.RemoveAll(s => s.ResolveId() == id);

            if (removed == 0) throw new ScreenNotFoundException(id);

            Persist(workingCopy);

            ApplyWorkingCopy(workingCopy);

            logger.Information("Deleted screen '{id}' from {file}.", id, FileName);
        }
    }

    private void ApplyWorkingCopy(List<Screen> workingCopy)
    {
        screens.Clear();
        screens.AddRange(workingCopy);
    }

    private void Persist(List<Screen> workingCopy)
    {
        var yaml = Yaml.Serialize(new ScreensConfiguration { Screens = workingCopy });

        // Not optional: Configuration.Load() catches every exception and falls back to the default configuration,
        // so a corrupt write would silently delete all screens on the next server start.
        var reparsed = Yaml.Deserialize<ScreensConfiguration>(yaml).Screens;

        if (!workingCopy.SequenceEqual(reparsed))
            throw new InvalidOperationException(
                "Round-trip validation of screens.yaml failed: re-parsing the serialized content did not match the in-memory data. Refusing to write.");

        var path = Path.Combine(configurationDirectory, FileName);
        var tempPath = path + ".tmp";

        File.WriteAllText(tempPath, yaml);

        try
        {
            if (File.Exists(path))
                File.Replace(tempPath, path, null);
            else
                File.Move(tempPath, path);
        }
        catch
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);

            throw;
        }
    }
}

public sealed class ScreenValidationException(IReadOnlyList<string> errors)
    : Exception($"Screen validation failed: {string.Join("; ", errors)}")
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

public sealed class ScreenNotFoundException(string id) : Exception($"No screen with id '{id}' was found.")
{
    public string Id { get; } = id;
}