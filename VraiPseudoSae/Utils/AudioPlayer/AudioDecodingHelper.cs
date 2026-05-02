using System.IO;
using NAudio.Wave;

namespace VraiPseudoSae.Utils.AudioPlayer;

/// <summary>
/// Fournit des méthodes utilitaires pour décoder des flux audio
/// en instances de <see cref="CachedSound"/> prêtes à être jouées.
/// </summary>
/// <remarks>
/// <para>
/// Cette classe centralise la logique de décodage des flux audio bruts
/// en objets exploitables par le système de lecture audio de l’application.
/// </para>
/// <para>
/// Les formats actuellement pris en charge sont :
/// </para>
/// <list type="bullet">
/// <item><description>MP3 via <see cref="Mp3FileReader"/> ;</description></item>
/// <item><description>WAV via <see cref="WaveFileReader"/>.</description></item>
/// </list>
/// <para>
/// La méthode <see cref="FromStream(Stream, string)"/> permet de choisir automatiquement
/// la stratégie de décodage en fonction de l’extension du fichier fourni.
/// </para>
/// </remarks>
public static class AudioDecodingHelper
{
    /// <summary>
    /// Décode un flux MP3 et retourne un son mis en cache sous forme de <see cref="CachedSound"/>.
    /// </summary>
    /// <param name="stream">
    /// Le flux contenant les données audio MP3 à lire.
    /// </param>
    /// <returns>
    /// Une instance de <see cref="CachedSound"/> contenant les échantillons audio décodés.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Levée si <paramref name="stream"/> est <see langword="null"/>..
    /// </exception>
    /// <exception cref="EndOfStreamException">
    /// Peut être levée si le flux ne contient pas suffisamment de données pour un fichier MP3 valide.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// Peut être levée si le contenu du flux ne correspond pas à un flux MP3 valide.
    /// </exception>
    /// <remarks>
    /// <para>
    /// La position du flux est réinitialisée à <c>0</c> avant lecture.
    /// </para>
    /// <para>
    /// Le flux n’est pas directement renvoyé ; son contenu est lu, converti en
    /// <see cref="ISampleProvider"/>, puis encapsulé dans un <see cref="CachedSound"/>.
    /// </para>
    /// </remarks>
    public static CachedSound FromMp3Stream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        
        stream.Position = 0;
        using var reader = new Mp3FileReader(stream);
        ISampleProvider sampleProvider = reader.ToSampleProvider();
        return new CachedSound(sampleProvider);
    }

    /// <summary>
    /// Décode un flux WAV et retourne un son mis en cache sous forme de <see cref="CachedSound"/>.
    /// </summary>
    /// <param name="stream">
    /// Le flux contenant les données audio WAV à lire.
    /// </param>
    /// <returns>
    /// Une instance de <see cref="CachedSound"/> contenant les échantillons audio décodés.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Levée si <paramref name="stream"/> est <see langword="null"/>..
    /// </exception>
    /// <exception cref="EndOfStreamException">
    /// Peut être levée si le flux est incomplet ou tronqué.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// Peut être levée si le contenu du flux ne correspond pas à un fichier WAV valide.
    /// </exception>
    /// <remarks>
    /// <para>
    /// La position du flux est réinitialisée à <c>0</c> avant lecture.
    /// </para>
    /// <para>
    /// Le flux est lu par <see cref="WaveFileReader"/>, converti en
    /// <see cref="ISampleProvider"/>, puis encapsulé dans un <see cref="CachedSound"/>.
    /// </para>
    /// </remarks>
    public static CachedSound FromWavStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        
        stream.Position = 0;
        using var reader = new WaveFileReader(stream);
        ISampleProvider sampleProvider = reader.ToSampleProvider();
        return new CachedSound(sampleProvider);
    }

    /// <summary>
    /// Décode un flux audio en sélectionnant automatiquement le lecteur approprié
    /// à partir de l’extension du fichier fourni.
    /// </summary>
    /// <param name="stream">
    /// Le flux contenant les données audio à décoder.
    /// </param>
    /// <param name="fileNameOrPath">
    /// Le nom de fichier ou le chemin servant à déterminer l’extension du format audio.
    /// </param>
    /// <returns>
    /// Une instance de <see cref="CachedSound"/> contenant les données audio décodées.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Levée si <paramref name="stream"/> est <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Levée si <paramref name="fileNameOrPath"/> est vide ou ne contient pas d’extension.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Levée si l’extension du fichier ne correspond à aucun format audio pris en charge.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Cette méthode inspecte l’extension obtenue avec <see cref="Path.GetExtension(string)"/>
    /// puis délègue le décodage à :
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="FromMp3Stream(Stream)"/> pour les fichiers <c>.mp3</c> ;</description></item>
    /// <item><description><see cref="FromWavStream(Stream)"/> pour les fichiers <c>.wav</c>.</description></item>
    /// </list>
    /// <para>
    /// La comparaison d’extension est effectuée en minuscules invariantes afin d’éviter
    /// les problèmes liés à la casse.
    /// </para>
    /// </remarks>
    /// <example>
    /// Exemple d’utilisation :
    /// <code>
    /// using var stream = File.OpenRead("Assets/jump.wav");
    /// CachedSound sound = AudioDecodingHelper.FromStream(stream, "jump.wav");
    /// </code>
    /// </example>
    public static CachedSound FromStream(Stream stream, string fileNameOrPath)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileNameOrPath);
        
        var ext = Path.GetExtension(fileNameOrPath);

        return ext.ToLowerInvariant() switch
        {
            ".mp3" => FromMp3Stream(stream),
            ".wav" => FromWavStream(stream),
            _ => throw new NotSupportedException($"Format non supporté : {ext}")
        };
    }
}