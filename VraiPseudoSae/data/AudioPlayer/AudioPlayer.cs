using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace VraiPseudoSae.data.AudioPlayer;
public sealed class AudioReference
{
    public string Id { get; set; } = "";
    public string Pack { get; set; } = "";
    public string Path { get; set; } = "";
    public string? Number { get; set; }
    public string JsonKey { get; set; } = "";
}

public sealed class AudioRegistry
{
    private readonly Dictionary<string, AudioReference> _byKey = new(StringComparer.OrdinalIgnoreCase);

    public void Load(string jsonFilePath)
    {
        _byKey.Clear();

        string json = File.ReadAllText(jsonFilePath);
        using JsonDocument doc = JsonDocument.Parse(json);

        Walk(doc.RootElement, "");
    }

    public void Play(string key)
    {
        if (!_byKey.TryGetValue(Normalize(key), out var audio))
            throw new KeyNotFoundException($"Clé audio introuvable : {key}");

        string resolvedFile = ResolveAudioFile(audio);

        if (!File.Exists(resolvedFile))
            throw new FileNotFoundException($"Fichier audio introuvable : {resolvedFile}");

        Console.WriteLine($"PLAY => {audio.JsonKey}");
        Console.WriteLine($"PACK => {audio.Pack}");
        Console.WriteLine($"PATH => {audio.Path}");
        Console.WriteLine($"NUMBER => {audio.Number}");
        Console.WriteLine($"FILE => {resolvedFile}");

        // Branche ici ton moteur audio réel
        // _audioService.Play(resolvedFile);
    }

    public bool TryGet(string key, out AudioReference? audio)
    {
        return _byKey.TryGetValue(Normalize(key), out audio);
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
        if (element.TryGetProperty("number", out JsonElement numberProp) && numberProp.ValueKind == JsonValueKind.String)
            number = numberProp.GetString();

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

    private static string ResolveAudioFile(AudioReference audio)
    {
        string baseFolder = @"C:\Users\Asus\RiderProjects\VraiPseudoSae201\VraiPseudoSae\Assets\PacksExtracted";

        string folderPath = audio.Path.Replace('/', Path.DirectorySeparatorChar);

        string fileName = !string.IsNullOrWhiteSpace(audio.Number)
            ? $"{audio.Number}.wav"
            : $"{audio.Id}.wav";

        return Path.Combine(baseFolder, audio.Pack, folderPath, fileName);
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
}