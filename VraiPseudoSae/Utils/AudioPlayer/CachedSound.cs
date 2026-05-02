using NAudio.Wave;

namespace VraiPseudoSae.Utils.AudioPlayer;

/// <summary>
/// Représente un son entièrement chargé en mémoire sous forme d’échantillons audio flottants.
/// </summary>
/// <remarks>
/// <para>
/// Cette classe constitue un conteneur de données audio déjà décodées, destiné à être utilisé
/// par le système de lecture de l’application sans avoir à relire ni redécoder la source d’origine
/// à chaque lecture.
/// </para>
/// <para>
/// Lors de la construction de l’objet, l’intégralité des échantillons produits par le
/// <see cref="ISampleProvider"/> fourni est lue puis copiée en mémoire dans un tableau de
/// <see cref="float"/>. Ce fonctionnement est particulièrement adapté aux effets sonores courts
/// ou fréquemment rejoués, comme des sons d’interface, d’impact, de saut ou d’explosion.
/// </para>
/// <para>
/// Le format audio associé aux données est également conservé via la propriété
/// <see cref="WaveFormat"/>, afin de garantir que les composants chargés de la lecture ou du mixage
/// puissent interpréter correctement les échantillons stockés dans <see cref="AudioData"/>.
/// </para>
/// <para>
/// Cette approche correspond à un mécanisme de mise en cache audio en mémoire :
/// la phase coûteuse de lecture et de décodage n’a lieu qu’une seule fois, puis les données
/// déjà prêtes peuvent être réutilisées autant de fois que nécessaire.
/// </para>
/// </remarks>
public sealed class CachedSound
{
    /// <summary>
    /// Obtient l’ensemble des échantillons audio décodés stockés en mémoire.
    /// </summary>
    /// <value>
    /// Un tableau de <see cref="float"/> contenant la totalité des échantillons audio lus depuis
    /// le <see cref="ISampleProvider"/> source.
    /// </value>
    /// <remarks>
    /// <para>
    /// Les données sont stockées de manière contiguë dans l’ordre de lecture fourni par le provider.
    /// </para>
    /// <para>
    /// Dans le cas d’un son multicanal, les échantillons sont intercalés selon le format audio
    /// associé, par exemple gauche / droite / gauche / droite pour une source stéréo.
    /// </para>
    /// <para>
    /// Ce tableau représente la version entièrement mise en cache du son, prête à être relue
    /// sans accéder à nouveau au flux d’origine.
    /// </para>
    /// </remarks>
    public float[] AudioData { get; }

    /// <summary>
    /// Obtient le format audio des données stockées dans <see cref="AudioData"/>.
    /// </summary>
    /// <value>
    /// Une instance de <see cref="NAudio.Wave.WaveFormat"/> décrivant les caractéristiques du son,
    /// notamment la fréquence d’échantillonnage, le nombre de canaux et le format des échantillons.
    /// </value>
    /// <remarks>
    /// <para>
    /// Cette propriété est récupérée directement depuis le <see cref="ISampleProvider"/> fourni au
    /// constructeur, avant la lecture complète des données.
    /// </para>
    /// <para>
    /// Elle est indispensable pour permettre aux composants consommateurs de savoir comment
    /// interpréter correctement les données du tableau <see cref="AudioData"/>.
    /// </para>
    /// </remarks>
    public WaveFormat WaveFormat { get; }

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="CachedSound"/>
    /// à partir d’un fournisseur d’échantillons audio.
    /// </summary>
    /// <param name="sampleProvider">
    /// Le fournisseur d’échantillons à lire intégralement afin de construire
    /// le son mis en cache.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Levée si <paramref name="sampleProvider"/> est <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Le constructeur lit l’intégralité des échantillons produits par
    /// <paramref name="sampleProvider"/> jusqu’à ce que celui-ci n’ait plus de données à fournir.
    /// </para>
    /// <para>
    /// Les échantillons sont d’abord lus dans un tampon temporaire, puis accumulés dans une liste
    /// dynamique avant d’être convertis en tableau final affecté à <see cref="AudioData"/>.
    /// </para>
    /// <para>
    /// La taille du tampon de lecture est calculée à partir de
    /// <see cref="WaveFormat.SampleRate"/> et <see cref="WaveFormat.Channels"/>, ce qui correspond
    /// approximativement à une seconde d’audio en mémoire flottante pour le format courant.
    /// </para>
    /// <para>
    /// Cette stratégie permet de lire un flux de taille inconnue sans devoir connaître à l’avance
    /// le nombre exact d’échantillons qu’il contient.
    /// </para>
    /// </remarks>
    public CachedSound(ISampleProvider sampleProvider)
    {
        ArgumentNullException.ThrowIfNull(sampleProvider);

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