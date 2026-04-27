using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VraiPseudoSae.data.AudioPlayer;

public static class AudioDecodingHelper
{
    public static CachedSound FromMp3Stream(Stream stream)
    {
        stream.Position = 0;
        using var reader = new Mp3FileReader(stream);
        ISampleProvider sampleProvider = reader.ToSampleProvider();
        return new CachedSound(sampleProvider);
    }

    public static CachedSound FromWavStream(Stream stream)
    {
        stream.Position = 0;
        using var reader = new WaveFileReader(stream);
        ISampleProvider sampleProvider = reader.ToSampleProvider();
        return new CachedSound(sampleProvider);
    }

    public static CachedSound FromStream(Stream stream, string fileNameOrPath)
    {
        var ext = Path.GetExtension(fileNameOrPath);

        return ext.ToLowerInvariant() switch
        {
            ".mp3" => FromMp3Stream(stream),
            ".wav" => FromWavStream(stream),
            _ => throw new NotSupportedException($"Format non supporté : {ext}")
        };
    }
}