using NAudio.Wave;

namespace VraiPseudoSae.Utils.AudioPlayer;

/// <summary>
/// Représente un fournisseur d’échantillons audio lisant les données d’un
/// <see cref="CachedSound"/> déjà chargé en mémoire.
/// </summary>
/// <remarks>
/// <para>
/// Cette classe implémente l’interface <see cref="ISampleProvider"/> afin d’exposer le contenu
/// d’un <see cref="CachedSound"/> sous la forme d’un flux d’échantillons lisible par les composants
/// audio de NAudio, comme un <see cref="NAudio.Wave.SampleProviders.MixingSampleProvider"/>.
/// </para>
/// <para>
/// Son rôle est de parcourir progressivement le tableau d’échantillons stocké dans
/// <see cref="CachedSound.AudioData"/> et de recopier ces données dans le tampon demandé
/// par l’appelant au fil des appels à <see cref="Read(float[], int, int)"/>.
/// </para>
/// <para>
/// Cette approche permet de transformer un son entièrement mis en cache en source de lecture
/// compatible avec le pipeline de mixage audio, sans relire de flux fichier ni redécoder
/// le contenu audio à chaque lecture. Ce pattern correspond au mécanisme classique de
/// <c>CachedSoundSampleProvider</c> utilisé avec NAudio.
/// </para>
/// </remarks>
public sealed class CachedSoundSampleProvider : ISampleProvider
{
    /// <summary>
    /// Représente le son mis en cache servant de source de données audio.
    /// </summary>
    /// <remarks>
    /// Cette instance contient les échantillons déjà décodés ainsi que leur format,
    /// qui seront exposés via l’implémentation de <see cref="ISampleProvider"/>.
    /// </remarks>
    private readonly CachedSound _cachedSound;

    /// <summary>
    /// Représente la position de lecture actuelle dans le tableau d’échantillons.
    /// </summary>
    /// <remarks>
    /// Cette valeur correspond à l’indice du prochain échantillon à recopier depuis
    /// <see cref="CachedSound.AudioData"/>. Elle progresse au fur et à mesure des appels
    /// à <see cref="Read(float[], int, int)"/>.
    /// </remarks>
    private long _position;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="CachedSoundSampleProvider"/>
    /// à partir d’un son déjà mis en cache.
    /// </summary>
    /// <param name="cachedSound">
    /// Le son mis en cache à exposer sous la forme d’un <see cref="ISampleProvider"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Peut être levée indirectement si <paramref name="cachedSound"/> est <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Le provider conserve simplement une référence vers l’objet <paramref name="cachedSound"/>
    /// et initialise implicitement la position de lecture au début du tableau d’échantillons.
    /// </para>
    /// <para>
    /// Aucune copie des données audio n’est effectuée ici : la classe lit directement
    /// les échantillons déjà présents dans <see cref="CachedSound.AudioData"/>.
    /// </para>
    /// </remarks>
    public CachedSoundSampleProvider(CachedSound cachedSound)
    {
        _cachedSound = cachedSound;
    }

    /// <summary>
    /// Obtient le format audio des échantillons fournis par cette source.
    /// </summary>
    /// <value>
    /// Le <see cref="WaveFormat"/> du <see cref="CachedSound"/> sous-jacent.
    /// </value>
    /// <remarks>
    /// <para>
    /// Cette propriété satisfait le contrat de l’interface <see cref="ISampleProvider"/>,
    /// qui impose qu’un fournisseur d’échantillons expose le format audio des données qu’il produit.
    /// </para>
    /// <para>
    /// Le format retourné est exactement celui du son mis en cache, sans transformation supplémentaire.
    /// </para>
    /// </remarks>
    public WaveFormat WaveFormat => _cachedSound.WaveFormat;

    /// <summary>
    /// Lit des échantillons audio depuis le son mis en cache et les copie dans le tampon fourni.
    /// </summary>
    /// <param name="buffer">
    /// Le tampon de destination dans lequel écrire les échantillons lus.
    /// </param>
    /// <param name="offset">
    /// L’index du premier emplacement du tampon à partir duquel écrire les données.
    /// </param>
    /// <param name="count">
    /// Le nombre maximal d’échantillons demandé.
    /// </param>
    /// <returns>
    /// Le nombre réel d’échantillons copiés dans <paramref name="buffer"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Cette méthode applique le contrat standard de <see cref="ISampleProvider"/> :
    /// le paramètre <paramref name="count"/> représente le nombre d’échantillons souhaité,
    /// et la valeur de retour indique combien d’échantillons ont effectivement été écrits
    /// dans le tampon.
    /// </para>
    /// <para>
    /// Le nombre d’échantillons réellement copiés est limité à la fois :
    /// </para>
    /// <list type="bullet">
    /// <item><description>par le nombre d’échantillons encore disponibles dans le son ;</description></item>
    /// <item><description>par le nombre d’échantillons demandés par l’appelant.</description></item>
    /// </list>
    /// <para>
    /// Lorsque la fin des données est atteinte, cette méthode retourne <c>0</c>,
    /// ce qui indique au consommateur qu’aucun autre échantillon n’est disponible.
    /// Ce comportement est conforme au fonctionnement attendu des providers NAudio.
    /// </para>
    /// <para>
    /// La position de lecture interne est avancée après chaque copie afin que les appels successifs
    /// reprennent exactement là où la lecture précédente s’est arrêtée.
    /// </para>
    /// </remarks>
    public int Read(float[] buffer, int offset, int count)
    {
        var availableSamples = _cachedSound.AudioData.Length - _position;
        var samplesToCopy = (int)Math.Min(availableSamples, count);

        Array.Copy(_cachedSound.AudioData, _position, buffer, offset, samplesToCopy);
        _position += samplesToCopy;

        return samplesToCopy;
    }
}