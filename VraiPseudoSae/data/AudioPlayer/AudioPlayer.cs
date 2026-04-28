using System.IO;
using System.Text.Json;
using VraiPseudoSae.data.PakManager;

namespace VraiPseudoSae.data.AudioPlayer;

public sealed class AudioReference
{
    public string Id { get; set; } = "";
    public string Pack { get; set; } = "";
    public string Path { get; set; } = "";
    public string? Number { get; set; }
    public string JsonKey { get; set; } = "";
}

public sealed class JsonPakAudioService : IDisposable
{
    private readonly PakAudioCatalog _catalog;
    private readonly AudioPlaybackEngine _engine;

    private readonly Dictionary<string, AudioReference> _byKey = new(StringComparer.OrdinalIgnoreCase);

    // Cache technique par clé JSON
    private readonly Dictionary<string, CachedSound> _cacheByKey = new(StringComparer.OrdinalIgnoreCase);

    // Cache fonctionnel par alias utilisateur
    private readonly Dictionary<string, CachedSound> _cacheByAlias = new(StringComparer.OrdinalIgnoreCase);

    public JsonPakAudioService(PakAudioCatalog catalog)
    {
        _catalog = catalog;
        _engine = new AudioPlaybackEngine(44100, 2, 80);
    }

    public void Load(string jsonFilePath)
    {
        _byKey.Clear();
        _cacheByKey.Clear();
        _cacheByAlias.Clear();

        string json = File.ReadAllText(jsonFilePath);
        using JsonDocument doc = JsonDocument.Parse(json);

        Walk(doc.RootElement, "");
    }

    // Précharge par clé JSON, sans alias
    public void PreloadByKey(string key)
    {
        string normalizedKey = Normalize(key);

        if (_cacheByKey.ContainsKey(normalizedKey))
            return;

        if (!_byKey.TryGetValue(normalizedKey, out var audio))
            throw new KeyNotFoundException($"Clé audio introuvable : {key}");

        _cacheByKey[normalizedKey] = LoadSound(audio);
    }

    // Précharge par clé JSON + alias personnalisé
    public void Preload(string key, string alias)
    {
        string normalizedKey = Normalize(key);
        string normalizedAlias = NormalizeAlias(alias);

        if (string.IsNullOrWhiteSpace(normalizedAlias))
            throw new ArgumentException("L'alias ne peut pas être vide.", nameof(alias));

        if (!_cacheByKey.TryGetValue(normalizedKey, out var sound))
        {
            if (!_byKey.TryGetValue(normalizedKey, out var audio))
                throw new KeyNotFoundException($"Clé audio introuvable : {key}");

            sound = LoadSound(audio);
            _cacheByKey[normalizedKey] = sound;
        }

        _cacheByAlias[normalizedAlias] = sound;
    }

    // Joue par alias
    public void Play(string alias, float volume = 1f)
    {
        string normalizedAlias = NormalizeAlias(alias);

        if (!_cacheByAlias.TryGetValue(normalizedAlias, out var sound))
            throw new KeyNotFoundException($"Alias audio introuvable : {alias}");

        _engine.PlaySound(sound, volume);
    }

    // Ancien comportement : jouer directement via la clé JSON
    public void PlayByKey(string key, float volume = 1f)
    {
        string normalizedKey = Normalize(key);

        if (!_cacheByKey.TryGetValue(normalizedKey, out var sound))
        {
            if (!_byKey.TryGetValue(normalizedKey, out var audio))
                throw new KeyNotFoundException($"Clé audio introuvable : {key}");

            sound = LoadSound(audio);
            _cacheByKey[normalizedKey] = sound;
        }

        _engine.PlaySound(sound, volume);
    }

    public bool TryPreload(string key, string alias)
    {
        try
        {
            Preload(key, alias);
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

    public bool TryPlayByKey(string key, float volume = 1f)
    {
        try
        {
            PlayByKey(key, volume);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool ContainsKey(string key)
    {
        return _byKey.ContainsKey(Normalize(key));
    }

    public bool ContainsAlias(string alias)
    {
        return _cacheByAlias.ContainsKey(NormalizeAlias(alias));
    }

    public void RemoveAlias(string alias)
    {
        _cacheByAlias.Remove(NormalizeAlias(alias));
    }

    public void ClearAliases()
    {
        _cacheByAlias.Clear();
    }

    public void ClearAllCache()
    {
        _cacheByKey.Clear();
        _cacheByAlias.Clear();
    }

    public IReadOnlyCollection<string> GetAliases()
    {
        return _cacheByAlias.Keys.ToList().AsReadOnly();
    }

    private CachedSound LoadSound(AudioReference audio)
    {
        // Nom du .pak (ex : "uncategorized.pak")
        string expectedPakFileName = Path.GetFileName(audio.Pack);

        // Nom du dossier final (ex : "SFX_Car_Mouvements")
        string folderName = audio.Path.Split('/', '\\').Last();

        // Nom du fichier : NomDossier_number.mp3
        string expectedFileName = $"{folderName}_{audio.Number}.mp3";

        // Chemin interne dans le pak, ex : "Uncategorized/SFX_Car_Mouvements/SFX_Car_Mouvements_0001.mp3"
        string expectedEntryPath = $"{audio.Path}/{expectedFileName}"
            .Replace('\\', '/')
            .Trim('/');

        var entry = _catalog.Entries.FirstOrDefault(e =>
            Path.GetFileName(e.PackPath).Equals(expectedPakFileName, StringComparison.OrdinalIgnoreCase) &&
            e.EntryPath.Equals(expectedEntryPath, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
            throw new FileNotFoundException(
                $"Entrée introuvable dans les .pak : pack={audio.Pack}, path={audio.Path}, file={expectedFileName}");
        }

        using var stream = _catalog.OpenEntryStream(entry);
        return AudioDecodingHelper.FromStream(stream, entry.FileName);
    }

    private void Walk(JsonElement element, string currentPath)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                if (TryCreateAudioReference(element, currentPath, out var audioRef))
                {
                    _byKey[Normalize(audioRef.JsonKey)] = audioRef;
                }

                foreach (JsonProperty property in element.EnumerateObject())
                {
                    string nextPath = AppendPath(currentPath, property.Name);
                    Walk(property.Value, nextPath);
                }

                break;
            }

            case JsonValueKind.Array:
            {
                foreach (JsonElement item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object &&
                        item.TryGetProperty("id", out JsonElement idProp) &&
                        idProp.ValueKind == JsonValueKind.String)
                    {
                        string id = idProp.GetString() ?? "";
                        string nextPath = AppendPath(currentPath, id);
                        Walk(item, nextPath);
                    }
                    else
                    {
                        Walk(item, currentPath);
                    }
                }

                break;
            }
        }
    }

    private static bool TryCreateAudioReference(JsonElement element, string currentPath, out AudioReference audioRef)
    {
        audioRef = null!;

        if (!element.TryGetProperty("id", out JsonElement idProp) || idProp.ValueKind != JsonValueKind.String)
            return false;

        if (!element.TryGetProperty("pack", out JsonElement packProp) || packProp.ValueKind != JsonValueKind.String)
            return false;

        if (!element.TryGetProperty("path", out JsonElement pathProp) || pathProp.ValueKind != JsonValueKind.String)
            return false;

        string id = idProp.GetString() ?? "";
        string pack = packProp.GetString() ?? "";
        string path = pathProp.GetString() ?? "";

        string jsonKey = currentPath;
        if (string.IsNullOrWhiteSpace(jsonKey) || !jsonKey.EndsWith(id, StringComparison.OrdinalIgnoreCase))
            jsonKey = AppendPath(currentPath, id);

        string? number = null;
        if (element.TryGetProperty("number", out JsonElement numberProp) &&
            numberProp.ValueKind == JsonValueKind.String)
        {
            number = numberProp.GetString();
        }

        audioRef = new AudioReference
        {
            Id = id,
            Pack = pack,
            Path = path,
            Number = number,
            JsonKey = jsonKey
        };

        return true;
    }

    private static string AppendPath(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left))
            return right;

        if (string.IsNullOrWhiteSpace(right))
            return left;

        return $"{left}/{right}";
    }

    private static string Normalize(string path)
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