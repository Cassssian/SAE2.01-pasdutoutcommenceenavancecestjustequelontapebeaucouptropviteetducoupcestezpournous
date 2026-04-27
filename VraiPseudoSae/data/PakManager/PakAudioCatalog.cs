using System.IO;
using System.IO.Compression;

namespace VraiPseudoSae.data.PakManager;

public sealed class PakAudioCatalog
{
    private readonly List<PakAudioEntry> _entries = new();
    private readonly Dictionary<string, List<PakAudioEntry>> _entriesByGroup = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<PakAudioEntry> Entries => _entries;

    public void LoadFromPaks(string packsDirectory)
    {
        _entries.Clear();
        _entriesByGroup.Clear();

        if (!Directory.Exists(packsDirectory))
            throw new DirectoryNotFoundException(packsDirectory);

        foreach (var pakFile in Directory.EnumerateFiles(packsDirectory, "*.pak", SearchOption.TopDirectoryOnly))
        {
            IndexPak(pakFile);
        }
    }

    private void IndexPak(string pakPath)
    {
        using var archive = ZipFile.OpenRead(pakPath);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
                continue;

            var ext = Path.GetExtension(entry.FullName);
            if (!ext.Equals(".mp3", StringComparison.OrdinalIgnoreCase) &&
                !ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
                continue;

            var normalized = entry.FullName.Replace('\\', '/');
            var group = ExtractGroup(normalized);

            var item = new PakAudioEntry
            {
                PackPath = pakPath,
                EntryPath = normalized,
                Group = group,
                FileName = entry.Name
            };

            _entries.Add(item);

            if (!_entriesByGroup.TryGetValue(group, out var list))
            {
                list = new List<PakAudioEntry>();
                _entriesByGroup[group] = list;
            }

            list.Add(item);
        }
    }

    private static string ExtractGroup(string entryPath)
    {
        var parts = entryPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[0] : "ROOT";
    }

    public IReadOnlyList<string> GetGroups()
    {
        return _entriesByGroup.Keys.OrderBy(x => x).ToList();
    }

    public IReadOnlyList<PakAudioEntry> GetEntriesByGroup(string group)
    {
        if (_entriesByGroup.TryGetValue(group, out var list))
            return list;

        return Array.Empty<PakAudioEntry>();
    }

    public IReadOnlyList<PakAudioEntry> PickRandomFromGroup(string group, int count, Random rng)
    {
        var source = GetEntriesByGroup(group).ToList();

        if (source.Count == 0)
            return Array.Empty<PakAudioEntry>();

        return source
            .OrderBy(_ => rng.Next())
            .Take(Math.Min(count, source.Count))
            .ToList();
    }

    public Stream OpenEntryStream(PakAudioEntry item)
    {
        var archive = ZipFile.OpenRead(item.PackPath);
        var entry = archive.GetEntry(item.EntryPath);

        if (entry == null)
        {
            archive.Dispose();
            throw new FileNotFoundException($"Entrée introuvable dans le pak : {item.EntryPath}");
        }

        var entryStream = entry.Open();
        var memory = new MemoryStream();
        entryStream.CopyTo(memory);
        memory.Position = 0;

        entryStream.Dispose();
        archive.Dispose();

        return memory;
    }
}