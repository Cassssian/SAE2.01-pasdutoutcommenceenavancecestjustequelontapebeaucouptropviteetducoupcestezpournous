using System.IO;
using VraiPseudoSae.data.PakManager;

namespace VraiPseudoSae.Utils.AudioPlayer;

/// <summary>
/// Fournit un service de lecture audio basé sur des alias, en s’appuyant sur des entrées
/// stockées dans des fichiers <c>.pak</c>.
/// </summary>
/// <remarks>
/// <para>
/// Ce service permet de :
/// </para>
/// <list type="bullet">
/// <item><description>
/// précharger des sons à partir d’un <see cref="PakAudioCatalog"/> en leur associant un alias ;
/// </description></item>
/// <item><description>
/// jouer ces sons par alias avec un moteur de lecture partagé de type <see cref="AudioPlaybackEngine"/> ;
/// </description></item>
/// <item><description>
/// gérer dynamiquement la liste des sons chargés (ajout, remplacement, suppression, vidage).
/// </description></item>
/// </list>
/// <para>
/// Les sons sont décodés depuis les entrées <c>.pak</c> via <see cref="AudioDecodingHelper"/> puis
/// stockés dans la mémoire de l’application sous forme de <see cref="CachedSound"/>.
/// Chaque son est identifié par un alias utilisateur, ce qui simplifie leur utilisation
/// dans le code gameplay.
/// </para>
/// <para>
/// Cette classe est particulièrement adaptée à la gestion d’effets sonores ponctuels, pour lesquels
/// on souhaite combiner un chargement à la demande par chemin logique et une lecture simple par alias.
/// </para>
/// </remarks>
public sealed class PakAliasAudioService : IDisposable
{
    /// <summary>
    /// Catalogue des entrées audio disponibles dans les fichiers <c>.pak</c>.
    /// </summary>
    /// <remarks>
    /// Ce catalogue fournit l’accès aux entrées audio brutes (flux) qui seront ensuite
    /// décodées en <see cref="CachedSound"/>.
    /// </remarks>
    private readonly PakAudioCatalog _catalog;

    /// <summary>
    /// Moteur de lecture audio partagé utilisé pour jouer les sons chargés.
    /// </summary>
    /// <remarks>
    /// Cette instance gère le mixage et la sortie audio pour l’ensemble des sons
    /// référencés par ce service.
    /// </remarks>
    private readonly AudioPlaybackEngine _engine;

    /// <summary>
    /// Cache des sons préchargés, indexés par alias utilisateur.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Les clés sont comparées sans tenir compte de la casse grâce au
    /// <see cref="StringComparer.OrdinalIgnoreCase"/>.
    /// </para>
    /// <para>
    /// Chaque alias pointe vers une instance de <see cref="CachedSound"/> représentant un son
    /// entièrement décodé et prêt à être joué.
    /// </para>
    /// </remarks>
    private readonly Dictionary<string, CachedSound> _loadedByAlias = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="PakAliasAudioService"/>.
    /// </summary>
    /// <param name="catalog">
    /// Le catalogue des entrées audio extraites des fichiers <c>.pak</c>.
    /// </param>
    /// <param name="sampleRate">
    /// La fréquence d’échantillonnage cible utilisée par le moteur de lecture.
    /// La valeur par défaut est <c>44100</c>.
    /// </param>
    /// <param name="channelCount">
    /// Le nombre de canaux audio géré par le moteur de lecture.
    /// La valeur par défaut est <c>2</c> (stéréo).
    /// </param>
    /// <param name="desiredLatency">
    /// La latence souhaitée pour le périphérique de sortie audio, en millisecondes.
    /// La valeur par défaut est <c>80</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Peut être levée indirectement si <paramref name="catalog"/> est <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Le constructeur retient le catalogue fourni et instancie un
    /// <see cref="AudioPlaybackEngine"/> avec les paramètres audio spécifiés.
    /// </para>
    /// <para>
    /// Les sons ne sont pas préchargés automatiquement à ce stade : ils le seront via les méthodes
    /// <see cref="Preload(string, string)"/> ou <see cref="PreloadOrReplace(string, string)"/>.
    /// </para>
    /// </remarks>
    public PakAliasAudioService(PakAudioCatalog catalog, int sampleRate = 44100, int channelCount = 2, int desiredLatency = 80)
    {
        _catalog = catalog;
        _engine = new AudioPlaybackEngine(sampleRate, channelCount, desiredLatency);
    }

    /// <summary>
    /// Obtient la collection en lecture seule des sons actuellement chargés, indexés par alias.
    /// </summary>
    /// <value>
    /// Un dictionnaire en lecture seule associant chaque alias utilisateur à son
    /// <see cref="CachedSound"/> correspondant.
    /// </value>
    public IReadOnlyDictionary<string, CachedSound> LoadedSounds => _loadedByAlias;

    /// <summary>
    /// Précharge un son à partir d’un chemin interne de <c>.pak</c> et l’associe à un alias,
    /// sans remplacer un son déjà chargé sous cet alias.
    /// </summary>
    /// <param name="pakEntryPath">
    /// Le chemin logique de l’entrée audio dans le <see cref="PakAudioCatalog"/>.
    /// </param>
    /// <param name="alias">
    /// L’alias utilisateur à associer au son chargé.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Levée si <paramref name="alias"/> est vide ou si <paramref name="pakEntryPath"/> est vide.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Levée si aucune entrée correspondant au chemin fourni n’est trouvée dans le catalogue.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Si un son est déjà chargé pour l’alias normalisé, cette méthode ne fait rien et ne remplace
    /// pas la valeur existante.
    /// </para>
    /// <para>
    /// Le chemin interne est normalisé (remplacement des antislashs et suppression des séparateurs
    /// superflus) avant la recherche dans le catalogue. Le flux correspondant est ensuite décodé
    /// via <see cref="AudioDecodingHelper.FromStream(System.IO.Stream, string)"/>.
    /// </para>
    /// </remarks>
    public void Preload(string pakEntryPath, string alias)
    {
        string normalizedAlias = NormalizeAlias(alias);
        string normalizedEntryPath = NormalizePath(pakEntryPath);

        if (string.IsNullOrWhiteSpace(normalizedAlias))
            throw new ArgumentException("L'alias ne peut pas être vide.", nameof(alias));

        if (string.IsNullOrWhiteSpace(normalizedEntryPath))
            throw new ArgumentException("Le chemin du son ne peut pas être vide.", nameof(pakEntryPath));

        if (_loadedByAlias.ContainsKey(normalizedAlias))
            return;

        var entry = FindEntryByPath(normalizedEntryPath);

        using var stream = _catalog.OpenEntryStream(entry);
        var sound = AudioDecodingHelper.FromStream(stream, entry.FileName);

        _loadedByAlias[normalizedAlias] = sound;
    }

    /// <summary>
    /// Précharge un son à partir d’un chemin interne de <c>.pak</c> et l’associe à un alias,
    /// en remplaçant systématiquement un son éventuellement déjà chargé sous cet alias.
    /// </summary>
    /// <param name="pakEntryPath">
    /// Le chemin logique de l’entrée audio dans le <see cref="PakAudioCatalog"/>.
    /// </param>
    /// <param name="alias">
    /// L’alias utilisateur à associer au son chargé.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Levée si <paramref name="alias"/> est vide ou si <paramref name="pakEntryPath"/> est vide.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Levée si aucune entrée correspondant au chemin fourni n’est trouvée dans le catalogue.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Contrairement à <see cref="Preload(string, string)"/>, cette méthode remplace toujours
    /// le son associé à l’alias si celui-ci existe déjà.
    /// </para>
    /// <para>
    /// Elle est utile dans les cas où l’on souhaite mettre à jour un son sans avoir à gérer
    /// manuellement la suppression d’un alias existant.
    /// </para>
    /// </remarks>
    public void PreloadOrReplace(string pakEntryPath, string alias)
    {
        string normalizedAlias = NormalizeAlias(alias);
        string normalizedEntryPath = NormalizePath(pakEntryPath);

        if (string.IsNullOrWhiteSpace(normalizedAlias))
            throw new ArgumentException("L'alias ne peut pas être vide.", nameof(alias));

        if (string.IsNullOrWhiteSpace(normalizedEntryPath))
            throw new ArgumentException("Le chemin du son ne peut pas être vide.", nameof(pakEntryPath));

        var entry = FindEntryByPath(normalizedEntryPath);

        using var stream = _catalog.OpenEntryStream(entry);
        var sound = AudioDecodingHelper.FromStream(stream, entry.FileName);

        _loadedByAlias[normalizedAlias] = sound;
    }

    /// <summary>
    /// Précharge plusieurs sons à partir d’une collection de couples (chemin, alias).
    /// </summary>
    /// <param name="items">
    /// La collection des éléments à précharger, chaque élément contenant un chemin d’entrée
    /// dans le <see cref="PakAudioCatalog"/> et l’alias associé.
    /// </param>
    /// <remarks>
    /// Cette méthode appelle <see cref="Preload(string, string)"/> pour chaque élément de
    /// <paramref name="items"/>. Les alias déjà présents ne sont pas remplacés.
    /// </remarks>
    public void PreloadMany(IEnumerable<(string pakEntryPath, string alias)> items)
    {
        foreach (var item in items)
        {
            Preload(item.pakEntryPath, item.alias);
        }
    }

    /// <summary>
    /// Joue un son préalablement chargé, en le référant par son alias.
    /// </summary>
    /// <param name="alias">
    /// L’alias du son à jouer.
    /// </param>
    /// <param name="volume">
    /// Le volume de lecture souhaité pour cette lecture.
    /// La valeur par défaut est <c>1f</c>.
    /// </param>
    /// <exception cref="KeyNotFoundException">
    /// Levée si aucun son n’est associé à l’alias fourni.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Le son doit avoir été préalablement chargé via <see cref="Preload(string, string)"/>,
    /// <see cref="PreloadOrReplace(string, string)"/> ou <see cref="PreloadMany(System.Collections.Generic.IEnumerable{(string pakEntryPath, string alias)})"/>.
    /// </para>
    /// <para>
    /// Cette méthode délègue la lecture effective à <see cref="AudioPlaybackEngine.PlaySound(CachedSound, float)"/>.
    /// </para>
    /// </remarks>
    public void Play(string alias, float volume = 1f)
    {
        string normalizedAlias = NormalizeAlias(alias);

        if (!_loadedByAlias.TryGetValue(normalizedAlias, out var sound))
            throw new KeyNotFoundException($"Alias audio introuvable : {alias}");

        _engine.PlaySound(sound, volume);
    }

    /// <summary>
    /// Tente de précharger un son sans propager d’exception.
    /// </summary>
    /// <param name="pakEntryPath">
    /// Le chemin interne de l’entrée audio dans le catalogue.
    /// </param>
    /// <param name="alias">
    /// L’alias utilisateur à associer au son.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si le préchargement a réussi ;
    /// sinon <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Cette méthode encapsule <see cref="Preload(string, string)"/> dans un bloc <c>try/catch</c>
    /// et renvoie simplement un booléen indiquant le succès ou l’échec de l’opération.
    /// </remarks>
    public bool TryPreload(string pakEntryPath, string alias)
    {
        try
        {
            Preload(pakEntryPath, alias);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tente de jouer un son par alias sans propager d’exception.
    /// </summary>
    /// <param name="alias">
    /// L’alias du son à jouer.
    /// </param>
    /// <param name="volume">
    /// Le volume de lecture souhaité.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si la lecture a pu être lancée ;
    /// sinon <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Cette méthode encapsule <see cref="Play(string, float)"/> dans un bloc <c>try/catch</c>
    /// et renvoie un booléen indiquant simplement si la lecture a réussi.
    /// </remarks>
    public bool TryPlay(string alias, float volume = 1f)
    {
        try
        {
            Play(alias, volume);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Indique si un alias est actuellement associé à un son chargé.
    /// </summary>
    /// <param name="alias">
    /// L’alias à tester.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si un son est chargé pour cet alias ;
    /// sinon <see langword="false"/>.
    /// </returns>
    public bool IsLoaded(string alias)
    {
        return _loadedByAlias.ContainsKey(NormalizeAlias(alias));
    }

    /// <summary>
    /// Supprime un son du cache à partir de son alias.
    /// </summary>
    /// <param name="alias">
    /// L’alias du son à supprimer.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si un élément a été trouvé et supprimé ;
    /// sinon <see langword="false"/>.
    /// </returns>
    public bool Remove(string alias)
    {
        return _loadedByAlias.Remove(NormalizeAlias(alias));
    }

    /// <summary>
    /// Supprime tous les sons actuellement chargés dans le cache.
    /// </summary>
    /// <remarks>
    /// Après l’appel à cette méthode, aucun alias ne sera plus associé à un son
    /// tant que de nouveaux préchargements n’auront pas été effectués.
    /// </remarks>
    public void Clear()
    {
        _loadedByAlias.Clear();
    }

    /// <summary>
    /// Retourne la liste des alias pour lesquels un son est actuellement chargé.
    /// </summary>
    /// <returns>
    /// Une collection en lecture seule de toutes les clés alias présentes dans le cache.
    /// </returns>
    public IReadOnlyCollection<string> GetAliases()
    {
        return _loadedByAlias.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Recherche une entrée audio dans le catalogue à partir d’un chemin interne normalisé.
    /// </summary>
    /// <param name="normalizedEntryPath">
    /// Le chemin interne normalisé de l’entrée à rechercher.
    /// </param>
    /// <returns>
    /// L’entrée <see cref="PakAudioEntry"/> correspondante.
    /// </returns>
    /// <exception cref="FileNotFoundException">
    /// Levée si aucune entrée ne correspond au chemin fourni.
    /// </exception>
    /// <remarks>
    /// Cette méthode effectue une comparaison insensible à la casse
    /// sur les chemins d’entrée normalisés du catalogue.
    /// </remarks>
    private PakAudioEntry FindEntryByPath(string normalizedEntryPath)
    {
        var entry = _catalog.Entries.FirstOrDefault(e =>
            NormalizePath(e.EntryPath).Equals(normalizedEntryPath, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
            throw new FileNotFoundException($"Entrée introuvable dans les .pak : {normalizedEntryPath}");

        return entry;
    }

    /// <summary>
    /// Normalise un chemin interne de <c>.pak</c> en harmonisant les séparateurs
    /// et en supprimant les séparateurs superflus en bordure.
    /// </summary>
    /// <param name="path">
    /// Le chemin à normaliser.
    /// </param>
    /// <returns>
    /// Le chemin normalisé utilisant <c>/</c> comme séparateur.
    /// </returns>
    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    /// <summary>
    /// Normalise un alias utilisateur en supprimant les espaces superflus
    /// en début et en fin de chaîne.
    /// </summary>
    /// <param name="alias">
    /// L’alias à normaliser.
    /// </param>
    /// <returns>
    /// L’alias normalisé.
    /// </returns>
    private static string NormalizeAlias(string alias)
    {
        return alias.Trim();
    }

    /// <summary>
    /// Libère les ressources utilisées par le service audio.
    /// </summary>
    /// <remarks>
    /// Cette méthode libère le moteur de lecture interne <see cref="_engine"/>.
    /// Une fois le service supprimé, il ne doit plus être utilisé pour précharger
    /// ou jouer de nouveaux sons.
    /// </remarks>
    public void Dispose()
    {
        _engine.Dispose();
    }
}