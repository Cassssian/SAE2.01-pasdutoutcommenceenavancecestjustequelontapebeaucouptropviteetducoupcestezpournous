using System.IO;
using System.IO.Compression;

namespace VraiPseudoSae.Utils.PakManager;

public static class PakBuildUtility
{
    public static void BuildAllDefaultPaks(string rootAudioFolder, string outputPaksFolder)
    {
        Directory.CreateDirectory(outputPaksFolder);

        BuildPak(
            outputPakPath: Path.Combine(outputPaksFolder, "ambiance.pak"),
            sources: new[]
            {
                new PakSource(Path.Combine(rootAudioFolder, "Ambiance"), "Ambiance"),
                new PakSource(Path.Combine(rootAudioFolder, "Crowds"), "Crowds"),
            });

        BuildPak(
            outputPakPath: Path.Combine(outputPaksFolder, "balls.pak"),
            sources: new[]
            {
                new PakSource(Path.Combine(rootAudioFolder, "Balls"), "Balls"),
            });

        BuildPak(
            outputPakPath: Path.Combine(outputPaksFolder, "boosts.pak"),
            sources: new[]
            {
                new PakSource(Path.Combine(rootAudioFolder, "Boosts"), "Boosts"),
            });

        BuildPak(
            outputPakPath: Path.Combine(outputPaksFolder, "goal-explosions.pak"),
            sources: new[]
            {
                new PakSource(Path.Combine(rootAudioFolder, "GoalExplosions"), "GoalExplosions"),
            });

        BuildPak(
            outputPakPath: Path.Combine(outputPaksFolder, "uncategorized.pak"),
            sources: new[]
            {
                new PakSource(Path.Combine(rootAudioFolder, "Uncategorized"), "Uncategorized"),
            });    
        
        BuildPak(
            outputPakPath: Path.Combine(outputPaksFolder, "ui-voice.pak"),
            sources: new[]
            {
                new PakSource(Path.Combine(rootAudioFolder, "UserInterface"), "UserInterface"),
                new PakSource(Path.Combine(rootAudioFolder, "VoiceOvers"), "VoiceOvers"),
            });
    }

    public static void BuildPak(string outputPakPath, IEnumerable<PakSource> sources)
    {
        var outputDir = Path.GetDirectoryName(outputPakPath);
        if (!string.IsNullOrWhiteSpace(outputDir))
            Directory.CreateDirectory(outputDir);

        if (File.Exists(outputPakPath))
            File.Delete(outputPakPath);

        using var fs = new FileStream(outputPakPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using var archive = new ZipArchive(fs, ZipArchiveMode.Create);

        foreach (var source in sources)
        {
            if (!Directory.Exists(source.SourceDirectory))
                continue;

            AddDirectoryToArchive(archive, source.SourceDirectory, source.RootNameInPak);
        }
    }

    private static void AddDirectoryToArchive(ZipArchive archive, string sourceDirectory, string rootNameInPak)
    {
        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var extension = Path.GetExtension(file);

            if (!IsSupportedAudio(extension))
                continue;

            var relativePath = Path.GetRelativePath(sourceDirectory, file).Replace('\\', '/');
            var entryPath = $"{rootNameInPak}/{relativePath}";

            archive.CreateEntryFromFile(file, entryPath, CompressionLevel.Optimal);
        }
    }

    private static bool IsSupportedAudio(string extension)
    {
        return extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".wav", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record PakSource(string SourceDirectory, string RootNameInPak);