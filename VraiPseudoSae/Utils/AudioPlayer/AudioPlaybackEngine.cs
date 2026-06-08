using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VraiPseudoSae.Utils.AudioPlayer;

/// <summary>
/// Fournit un moteur de lecture audio temps réel capable de mixer plusieurs sons en mémoire
/// et de les envoyer vers le périphérique de sortie audio.
/// </summary>
/// <remarks>
/// <para>
/// Cette classe encapsule un périphérique de sortie <see cref="IWavePlayer"/> ainsi qu’un
/// <see cref="MixingSampleProvider"/> servant de mixeur central pour agréger plusieurs sources audio.
/// Elle permet ainsi de jouer plusieurs sons simultanément, sans interrompre les lectures déjà en cours.
/// </para>
/// <para>
/// Le moteur est conçu pour fonctionner avec des instances de <see cref="CachedSound"/>,
/// généralement utilisées pour les effets sonores courts ou fréquemment rejoués. Chaque son ajouté
/// est transformé en <see cref="ISampleProvider"/> puis injecté dans le mixeur.
/// </para>
/// <para>
/// Si le format d’un son ne correspond pas au format du mixeur, une conversion est appliquée
/// automatiquement. La conversion actuellement prise en charge concerne :
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// le passage d’un signal mono vers stéréo via <see cref="MonoToStereoSampleProvider"/> ;
/// </description>
/// </item>
/// <item>
/// <description>
/// le rééchantillonnage vers la fréquence cible du mixeur via
/// <see cref="WdlResamplingSampleProvider"/>.
/// </description>
/// </item>
/// </list>
/// <para>
/// Le mixeur est configuré avec <see cref="MixingSampleProvider.ReadFully"/> à <see langword="true"/>,
/// ce qui signifie qu’il continue de produire du silence lorsqu’aucune source n’est active,
/// permettant au périphérique de sortie de rester initialisé et en lecture continue. Cette approche
/// est cohérente avec les usages de NAudio pour les moteurs de lecture “fire-and-forget”.
/// </para>
/// </remarks>
public sealed class AudioPlaybackEngine : IDisposable
{
    /// <summary>
    /// Représente le périphérique de sortie audio utilisé pour la lecture effective.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cette instance est responsable de l’envoi du flux audio mixé vers le système audio
    /// de la machine.
    /// </para>
    /// <para>
    /// L’implémentation concrète utilisée ici est <see cref="WaveOutEvent"/>, souvent recommandée
    /// par défaut dans NAudio pour la lecture audio générale.
    /// </para>
    /// </remarks>
    private readonly IWavePlayer _outputDevice;

    /// <summary>
    /// Représente le mixeur central chargé d’agréger plusieurs sources audio en un seul flux.
    /// </summary>
    /// <remarks>
    /// Toutes les entrées audio jouées par ce moteur sont ajoutées à cette instance.
    /// Chaque source ajoutée est lue puis sommée avec les autres sources actives.
    /// </remarks>
    private readonly MixingSampleProvider _mixer;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="AudioPlaybackEngine"/>.
    /// </summary>
    /// <param name="sampleRate">
    /// La fréquence d’échantillonnage cible du moteur audio, exprimée en hertz.
    /// La valeur par défaut est <c>44100</c>.
    /// </param>
    /// <param name="channelCount">
    /// Le nombre de canaux audio géré par le moteur.
    /// La valeur par défaut est <c>2</c>, correspondant à un signal stéréo.
    /// </param>
    /// <param name="desiredLatency">
    /// La latence souhaitée du périphérique de sortie, exprimée en millisecondes.
    /// La valeur par défaut est <c>100</c>.
    /// </param>
    /// <remarks>
    /// <para>
    /// Le constructeur crée un périphérique de sortie de type <see cref="WaveOutEvent"/> et
    /// un <see cref="MixingSampleProvider"/> configuré au format IEEE float correspondant à
    /// <paramref name="sampleRate"/> et <paramref name="channelCount"/>.
    /// </para>
    /// <para>
    /// Le périphérique de sortie est ensuite initialisé avec le mixeur, puis immédiatement démarré.
    /// Ainsi, le moteur est prêt à recevoir de nouvelles entrées audio dès la fin de sa construction.
    /// </para>
    /// <para>
    /// Le paramètre <paramref name="desiredLatency"/> est transmis à <see cref="WaveOutEvent.DesiredLatency"/>,
    /// propriété exprimée en millisecondes dans NAudio.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Ajoute un son au moteur de lecture afin qu’il soit joué immédiatement.
    /// </summary>
    /// <param name="sound">
    /// Le son mis en cache à jouer.
    /// </param>
    /// <param name="volume">
    /// Le volume de lecture à appliquer à ce son.
    /// La valeur par défaut est <c>1f</c>, correspondant au volume nominal.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Peut être levée indirectement si <paramref name="sound"/> est <see langword="null"/>
    /// lors de la création du provider associé.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Cette méthode crée d’abord un <see cref="CachedSoundSampleProvider"/> à partir du son fourni,
    /// puis vérifie si son format correspond à celui du mixeur.
    /// </para>
    /// <para>
    /// Si le nombre de canaux ou la fréquence d’échantillonnage diffèrent, une conversion est appliquée
    /// via <see cref="ConvertToMixerFormat(ISampleProvider)"/> avant ajout au mixeur.
    /// </para>
    /// <para>
    /// Si le volume demandé diffère significativement de <c>1f</c>, un
    /// <see cref="VolumeSampleProvider"/> est intercalé afin d’ajuster le gain de cette lecture
    /// uniquement, sans modifier les autres sons en cours.
    /// </para>
    /// <para>
    /// Le son est ensuite ajouté au mixeur via <see cref="MixingSampleProvider.AddMixerInput(ISampleProvider)"/>,
    /// ce qui permet une lecture simultanée avec les autres sons actifs.
    /// </para>
    /// </remarks>
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

    public LoopingSoundHandle PlayLoopingSound(CachedSound sound, float volume = 1f)
    {
        var loopProvider = new LoopingCachedSoundSampleProvider(sound)
        {
            Volume = volume
        };

        ISampleProvider input = loopProvider;

        if (input.WaveFormat.SampleRate != _mixer.WaveFormat.SampleRate ||
            input.WaveFormat.Channels != _mixer.WaveFormat.Channels)
        {
            input = ConvertToMixerFormat(input);
        }

        _mixer.AddMixerInput(input);
        return new LoopingSoundHandle(loopProvider);
    }

    /// <summary>
    /// Convertit un fournisseur d’échantillons vers un format compatible avec celui du mixeur.
    /// </summary>
    /// <param name="input">
    /// Le fournisseur d’échantillons à convertir.
    /// </param>
    /// <returns>
    /// Un <see cref="ISampleProvider"/> compatible avec le format du mixeur.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Cette méthode applique successivement les conversions nécessaires afin de rendre
    /// <paramref name="input"/> compatible avec le format audio attendu par <see cref="_mixer"/>.
    /// </para>
    /// <para>
    /// Si l’entrée est mono et que le mixeur fonctionne en stéréo, elle est convertie en stéréo
    /// via <see cref="MonoToStereoSampleProvider"/>.
    /// </para>
    /// <para>
    /// Si la fréquence d’échantillonnage de l’entrée diffère de celle du mixeur,
    /// un rééchantillonnage est appliqué via <see cref="WdlResamplingSampleProvider"/>,
    /// qui opère sur des échantillons flottants.
    /// </para>
    /// <para>
    /// Cette méthode ne gère explicitement que les cas présents dans le code actuel.
    /// Les autres conversions plus complexes, comme certains changements de nombre de canaux,
    /// ne sont pas traitées ici.
    /// </para>
    /// </remarks>
    private ISampleProvider ConvertToMixerFormat(ISampleProvider input)
    {
        ISampleProvider current = input;

        if (current.WaveFormat.Channels == 1 && _mixer.WaveFormat.Channels == 2)
            current = new MonoToStereoSampleProvider(current);

        if (current.WaveFormat.SampleRate != _mixer.WaveFormat.SampleRate)
            current = new WdlResamplingSampleProvider(current, _mixer.WaveFormat.SampleRate);

        return current;
    }

    /// <summary>
    /// Libère les ressources utilisées par le moteur de lecture audio.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cette méthode libère le périphérique de sortie audio sous-jacent.
    /// Une fois l’instance supprimée, elle ne doit plus être utilisée pour lire de nouveaux sons.
    /// </para>
    /// <para>
    /// La responsabilité de la libération du mixeur est indirectement couverte par la libération
    /// du périphérique de sortie et par le cycle de vie de l’instance elle-même.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        _outputDevice.Dispose();
    }
}

public sealed class LoopingSoundHandle : IDisposable
{
    private readonly LoopingCachedSoundSampleProvider _provider;

    internal LoopingSoundHandle(LoopingCachedSoundSampleProvider provider)
    {
        _provider = provider;
    }

    public float Volume
    {
        get => _provider.Volume;
        set => _provider.Volume = value;
    }

    public void Stop()
    {
        _provider.Stop();
    }

    public void Dispose()
    {
        Stop();
    }
}

internal sealed class LoopingCachedSoundSampleProvider : ISampleProvider
{
    private readonly CachedSound _cachedSound;
    private long _position;
    private bool _stopped;
    private float _volume = 1f;

    public LoopingCachedSoundSampleProvider(CachedSound cachedSound)
    {
        _cachedSound = cachedSound;
    }

    public WaveFormat WaveFormat => _cachedSound.WaveFormat;

    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0f, 1f);
    }

    public void Stop()
    {
        _stopped = true;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        if (_stopped || _cachedSound.AudioData.Length == 0)
            return 0;

        int samplesWritten = 0;

        while (samplesWritten < count)
        {
            long availableSamples = _cachedSound.AudioData.Length - _position;
            if (availableSamples <= 0)
            {
                _position = 0;
                availableSamples = _cachedSound.AudioData.Length;
            }

            int samplesToCopy = (int)Math.Min(availableSamples, count - samplesWritten);
            int destinationOffset = offset + samplesWritten;

            if (Math.Abs(_volume - 1f) <= 0.001f)
            {
                Array.Copy(_cachedSound.AudioData, _position, buffer, destinationOffset, samplesToCopy);
            }
            else
            {
                for (int i = 0; i < samplesToCopy; i++)
                {
                    buffer[destinationOffset + i] = _cachedSound.AudioData[_position + i] * _volume;
                }
            }

            _position += samplesToCopy;
            samplesWritten += samplesToCopy;
        }

        return samplesWritten;
    }
}
