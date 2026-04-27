using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VraiPseudoSae.data.AudioPlayer;

public sealed class CachedSound
{
    public float[] AudioData { get; }
    public WaveFormat WaveFormat { get; }

    public CachedSound(ISampleProvider sampleProvider)
    {
        WaveFormat = sampleProvider.WaveFormat;

        var wholeFile = new List<float>();
        var readBuffer = new float[WaveFormat.SampleRate * WaveFormat.Channels];
        int samplesRead;

        while ((samplesRead = sampleProvider.Read(readBuffer, 0, readBuffer.Length)) > 0)
        {
            wholeFile.AddRange(readBuffer.Take(samplesRead));
        }

        AudioData = wholeFile.ToArray();
    }
}