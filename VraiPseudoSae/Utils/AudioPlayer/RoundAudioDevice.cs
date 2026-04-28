using VraiPseudoSae.data.PakManager;

namespace VraiPseudoSae.data.AudioPlayer;

public sealed class RoundAudioService : IDisposable
{
    private readonly PakAudioCatalog _catalog;
    private readonly AudioPlaybackEngine _engine;
    private readonly Dictionary<string, CachedSound> _loadedRoundSounds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Random _rng = new();

    public RoundAudioService(PakAudioCatalog catalog)
    {
        _catalog = catalog;
        _engine = new AudioPlaybackEngine(44100, 2, 80);
    }

    public IReadOnlyDictionary<string, CachedSound> LoadedRoundSounds => _loadedRoundSounds;

    public void PrepareRound(Dictionary<string, (string group, int count)> roundRules)
    {
        _loadedRoundSounds.Clear();

        foreach (var rule in roundRules)
        {
            var logicalKey = rule.Key;
            var group = rule.Value.group;
            var count = rule.Value.count;

            var picked = _catalog.PickRandomFromGroup(group, count, _rng);

            int i = 0;
            foreach (var item in picked)
            {
                using var stream = _catalog.OpenEntryStream(item);
                var sound = AudioDecodingHelper.FromStream(stream, item.FileName);

                var finalKey = count == 1 ? logicalKey : $"{logicalKey}_{i}";
                _loadedRoundSounds[finalKey] = sound;
                i++;
            }
        }
    }

    public void Play(string key, float volume = 1f)
    {
        if (_loadedRoundSounds.TryGetValue(key, out var sound))
            _engine.PlaySound(sound, volume);
    }

    public void PlayRandomVariant(string keyPrefix, float volume = 1f)
    {
        var variants = _loadedRoundSounds
            .Where(kv => kv.Key.Equals(keyPrefix, StringComparison.OrdinalIgnoreCase)
                      || kv.Key.StartsWith(keyPrefix + "_", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (variants.Count == 0)
            return;

        var chosen = variants[_rng.Next(variants.Count)];
        _engine.PlaySound(chosen.Value, volume);
    }

    public void Dispose()
    {
        _engine.Dispose();
    }
}