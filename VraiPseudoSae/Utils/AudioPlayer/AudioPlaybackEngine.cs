using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VraiPseudoSae.data.AudioPlayer;

public sealed class AudioPlaybackEngine : IDisposable
{
    private readonly IWavePlayer _outputDevice;
    private readonly MixingSampleProvider _mixer;

    public AudioPlaybackEngine(int sampleRate = 44100, int channelCount = 2, int desiredLatency = 100)
    {
        _outputDevice = new WaveOutEvent
        {
            DesiredLatency = desiredLatency
        };

        _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
        {
            ReadFully = true
        };

        _outputDevice.Init(_mixer);
        _outputDevice.Play();
    }

    public void PlaySound(CachedSound sound, float volume = 1f)
    {
        ISampleProvider input = new CachedSoundSampleProvider(sound);

        if (input.WaveFormat.SampleRate != _mixer.WaveFormat.SampleRate ||
            input.WaveFormat.Channels != _mixer.WaveFormat.Channels)
        {
            input = ConvertToMixerFormat(input);
        }

        if (Math.Abs(volume - 1f) > 0.001f)
        {
            var volumeProvider = new VolumeSampleProvider(input) { Volume = volume };
            input = volumeProvider;
        }

        _mixer.AddMixerInput(input);
    }

    private ISampleProvider ConvertToMixerFormat(ISampleProvider input)
    {
        ISampleProvider current = input;

        if (current.WaveFormat.Channels == 1 && _mixer.WaveFormat.Channels == 2)
            current = new MonoToStereoSampleProvider(current);

        if (current.WaveFormat.SampleRate != _mixer.WaveFormat.SampleRate)
            current = new WdlResamplingSampleProvider(current, _mixer.WaveFormat.SampleRate);

        return current;
    }

    public void Dispose()
    {
        _outputDevice.Dispose();
    }
}