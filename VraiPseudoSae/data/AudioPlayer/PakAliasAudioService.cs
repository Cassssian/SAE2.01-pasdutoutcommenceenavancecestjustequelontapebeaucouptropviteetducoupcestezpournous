using System.IO;
using VraiPseudoSae.data.PakManager;

namespace VraiPseudoSae.data.AudioPlayer;

public sealed class PakAliasAudioService : IDisposable
{
    private readonly PakAudioCatalog _catalog;
    private readonly AudioPlaybackEngine _engine;
    private readonly Dictionary<string, CachedSound> _loadedByAlias = new(StringComparer.OrdinalIgnoreCase);

    public PakAliasAudioService(PakAudioCatalog catalog, int sampleRate = 44100, int channelCount = 2, int desiredLatency = 80)
    {
        _catalog = catalog;
        _engine = new AudioPlaybackEngine(sampleRate, channelCount, desiredLatency);
    }

    public IReadOnlyDictionary<string, CachedSound> LoadedSounds => _loadedByAlias;

    public void Preload(string pakEntryPath, string alias)
    {
        string normalizedAlias = NormalizeAlias(alias);
        string normalizedEntryPath = NormalizePath(pakEntryPath);

        if (string.IsNullOrWhiteSpace(normalizedAlias))
            throw new ArgumentException("L'alias ne peut pas être vide.", nameof(alias));

        if (string.IsNullOrWhiteSpace(normalizedEntryPath))
            throw new ArgumentException("Le chemin du son ne peut pas être vide.", nameof(pakEntryPath));

        if (_loadedByAlias.ContainsKey(normalizedAlias))
            return;

        var entry = FindEntryByPath(normalizedEntryPath);

        using var stream = _catalog.OpenEntryStream(entry);
        var sound = AudioDecodingHelper.FromStream(stream, entry.FileName);

        _loadedByAlias[normalizedAlias] = sound;
    }

    public void PreloadOrReplace(string pakEntryPath, string alias)
    {
        string normalizedAlias = NormalizeAlias(alias);
        string normalizedEntryPath = NormalizePath(pakEntryPath);

        if (string.IsNullOrWhiteSpace(normalizedAlias))
            throw new ArgumentException("L'alias ne peut pas être vide.", nameof(alias));

        if (string.IsNullOrWhiteSpace(normalizedEntryPath))
            throw new ArgumentException("Le chemin du son ne peut pas être vide.", nameof(pakEntryPath));

        var entry = FindEntryByPath(normalizedEntryPath);

        using var stream = _catalog.OpenEntryStream(entry);
        var sound = AudioDecodingHelper.FromStream(stream, entry.FileName);

        _loadedByAlias[normalizedAlias] = sound;
    }

    public void PreloadMany(IEnumerable<(string pakEntryPath, string alias)> items)
    {
        foreach (var item in items)
        {
            Preload(item.pakEntryPath, item.alias);
        }
    }

    public void Play(string alias, float volume = 1f)
    {
        string normalizedAlias = NormalizeAlias(alias);

        if (!_loadedByAlias.TryGetValue(normalizedAlias, out var sound))
            throw new KeyNotFoundException($"Alias audio introuvable : {alias}");

        _engine.PlaySound(sound, volume);
    }

    public bool TryPreload(string pakEntryPath, string alias)
    {
        try
        {
            Preload(pakEntryPath, alias);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryPlay(string alias, float volume = 1f)
    {
        try
        {
            Play(alias, volume);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsLoaded(string alias)
    {
        return _loadedByAlias.ContainsKey(NormalizeAlias(alias));
    }

    public bool Remove(string alias)
    {
        return _loadedByAlias.Remove(NormalizeAlias(alias));
    }

    public void Clear()
    {
        _loadedByAlias.Clear();
    }

    public IReadOnlyCollection<string> GetAliases()
    {
        return _loadedByAlias.Keys.ToList().AsReadOnly();
    }

    private PakAudioEntry FindEntryByPath(string normalizedEntryPath)
    {
        var entry = _catalog.Entries.FirstOrDefault(e =>
            NormalizePath(e.EntryPath).Equals(normalizedEntryPath, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
            throw new FileNotFoundException($"Entrée introuvable dans les .pak : {normalizedEntryPath}");

        return entry;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    private static string NormalizeAlias(string alias)
    {
        return alias.Trim();
    }

    public void Dispose()
    {
        _engine.Dispose();
    }
}