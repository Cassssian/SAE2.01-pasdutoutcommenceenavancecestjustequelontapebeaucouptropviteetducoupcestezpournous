using System.IO;
using System.IO.Compression;

namespace VraiPseudoSae.Utils.PakManager;

public static class PakBuilder
{
    public static void CreatePakFromFolder(string sourceDir, string outputPakPath)
    {
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException(sourceDir);

        var outputDir = Path.GetDirectoryName(outputPakPath);
        if (!string.IsNullOrWhiteSpace(outputDir))
            Directory.CreateDirectory(outputDir);

        if (File.Exists(outputPakPath))
            File.Delete(outputPakPath);

        using var fs = new FileStream(outputPakPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using var archive = new ZipArchive(fs, ZipArchiveMode.Create);

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file).Replace('\\', '/');
            archive.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
        }
    }

    public static void CreateMultiplePaks(Dictionary<string, string> jobs)
    {
        foreach (var job in jobs)
            CreatePakFromFolder(job.Key, job.Value);
    }
}