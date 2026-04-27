namespace VraiPseudoSae.data.PakManager;

public sealed class PakAudioEntry
{
    public string PackPath { get; init; } = string.Empty;
    public string EntryPath { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;

    public override string ToString() => $"{PackPath} :: {EntryPath}";
}