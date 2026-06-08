using VraiPseudoSae.Utils.PakManager;

namespace VraiPseudoSae.Utils.AudioPlayer;

/// <summary>
/// Fournit un service audio orienté “manche” ou “round”, permettant de préparer à l’avance
/// un ensemble de sons sélectionnés aléatoirement depuis des groupes définis dans un
/// <see cref="PakAudioCatalog"/>, puis de les rejouer pendant la durée de cette manche.
/// </summary>
/// <remarks>
/// <para>
/// Cette classe a pour objectif de constituer un cache audio temporaire représentant
/// l’environnement sonore d’une manche donnée. Au lieu de charger individuellement chaque son
/// au moment où il doit être joué, elle prépare à l’avance un ensemble de variantes audio
/// choisies dans des groupes logiques du catalogue, puis les stocke en mémoire sous forme
/// de <see cref="CachedSound"/>.
/// </para>
/// <para>
/// Le service s’appuie sur un moteur de lecture partagé de type <see cref="AudioPlaybackEngine"/>
/// pour jouer les sons préparés, et sur un générateur pseudo-aléatoire pour sélectionner
/// des variantes au sein des groupes fournis. Cette approche permet d’éviter la monotonie
/// sonore en introduisant de la variation d’une manche à l’autre ou d’un déclenchement
/// à l’autre, ce qui est un objectif courant dans les systèmes audio de jeu.
/// </para>
/// <para>
/// Les sons chargés sont indexés par des clés logiques définies par l’appelant. Lorsqu’une règle
/// demande plusieurs éléments pour une même clé logique, le service génère des variantes suffixées
/// automatiquement sous la forme <c>clé_0</c>, <c>clé_1</c>, etc.
/// </para>
/// </remarks>
public sealed class RoundAudioService : IDisposable
{
    /// <summary>
    /// Représente le catalogue des entrées audio disponibles dans les fichiers <c>.pak</c>.
    /// </summary>
    /// <remarks>
    /// Ce catalogue est utilisé pour sélectionner aléatoirement des sons depuis des groupes,
    /// puis pour ouvrir les flux correspondants avant décodage.
    /// </remarks>
    private readonly PakAudioCatalog _catalog;

    /// <summary>
    /// Représente le moteur de lecture audio utilisé pour jouer les sons préparés pour la manche.
    /// </summary>
    /// <remarks>
    /// Cette instance est partagée par l’ensemble des sons de la manche et assure leur mixage
    /// ainsi que leur diffusion vers la sortie audio.
    /// </remarks>
    private readonly AudioPlaybackEngine _engine;

    /// <summary>
    /// Représente le cache des sons actuellement préparés pour la manche en cours.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Les clés de ce dictionnaire correspondent aux identifiants logiques définis par l’appelant,
    /// ou à leurs variantes suffixées lorsque plusieurs sons sont associés à une même règle.
    /// </para>
    /// <para>
    /// Les valeurs sont des instances de <see cref="CachedSound"/> déjà décodées et prêtes à être jouées.
    /// </para>
    /// </remarks>
    private readonly Dictionary<string, CachedSound> _loadedRoundSounds = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Représente le générateur pseudo-aléatoire utilisé pour sélectionner les variantes audio.
    /// </summary>
    /// <remarks>
    /// Ce générateur est utilisé à la fois lors de la préparation initiale des sons d’une manche
    /// et lors de la sélection d’une variante à jouer via <see cref="PlayRandomVariant(string, float)"/>.
    /// </remarks>
    private readonly Random _rng = new();

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="RoundAudioService"/>.
    /// </summary>
    /// <param name="catalog">
    /// Le catalogue audio utilisé pour sélectionner et ouvrir les entrées stockées dans les <c>.pak</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Peut être levée indirectement si <paramref name="catalog"/> est <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Le service crée en interne un <see cref="AudioPlaybackEngine"/> configuré en 44 100 Hz,
    /// stéréo et avec une latence souhaitée de 80 ms.
    /// </para>
    /// <para>
    /// Aucun son n’est chargé dans le constructeur ; le cache de manche est rempli
    /// lors de l’appel à <see cref="PrepareRound(Dictionary{string, (string group, int count)})"/>.
    /// </para>
    /// </remarks>
    public RoundAudioService(PakAudioCatalog catalog)
    {
        _catalog = catalog;
        _engine = new AudioPlaybackEngine(44100, 2, 80);
    }

    /// <summary>
    /// Obtient la collection en lecture seule des sons actuellement préparés pour la manche.
    /// </summary>
    /// <value>
    /// Un dictionnaire en lecture seule associant chaque clé logique de manche
    /// au <see cref="CachedSound"/> correspondant.
    /// </value>
    /// <remarks>
    /// Cette propriété expose l’état courant du cache audio de manche tel qu’il a été
    /// constitué par <see cref="PrepareRound(Dictionary{string, (string group, int count)})"/>.
    /// </remarks>
    public IReadOnlyDictionary<string, CachedSound> LoadedRoundSounds => _loadedRoundSounds;

    /// <summary>
    /// Prépare l’ensemble des sons d’une manche en sélectionnant aléatoirement des entrées
    /// dans des groupes audio du catalogue selon les règles fournies.
    /// </summary>
    /// <param name="roundRules">
    /// Le dictionnaire des règles de préparation, où chaque clé représente une clé logique
    /// de manche, et chaque valeur indique :
    /// <list type="bullet">
    /// <item><description>le nom du groupe source dans le catalogue ;</description></item>
    /// <item><description>le nombre d’éléments à sélectionner dans ce groupe.</description></item>
    /// </list>
    /// </param>
    /// <remarks>
    /// <para>
    /// Cette méthode commence par vider complètement le cache audio de manche courant.
    /// Toute préparation précédente est donc abandonnée et remplacée par la nouvelle sélection.
    /// </para>
    /// <para>
    /// Pour chaque règle, le service appelle une méthode du catalogue afin de sélectionner
    /// aléatoirement <c>count</c> entrées dans le groupe demandé, puis ouvre chacune de ces entrées,
    /// les décode via <see cref="AudioDecodingHelper"/>, et les stocke dans le dictionnaire interne
    /// sous la clé logique correspondante.
    /// </para>
    /// <para>
    /// Si une règle demande un seul son, celui-ci est stocké directement sous la clé logique.
    /// Si elle en demande plusieurs, les sons sont stockés sous des variantes suffixées :
    /// <c>clé_0</c>, <c>clé_1</c>, etc.
    /// </para>
    /// <para>
    /// Ce mode de préparation permet de constituer à l’avance un ensemble fini de variantes,
    /// afin d’accélérer la lecture pendant la manche et de favoriser la diversité sonore.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Joue un son préparé pour la manche en le référant par sa clé exacte.
    /// </summary>
    /// <param name="key">
    /// La clé logique du son à jouer.
    /// </param>
    /// <param name="volume">
    /// Le volume de lecture souhaité pour ce son.
    /// La valeur par défaut est <c>1f</c>.
    /// </param>
    /// <remarks>
    /// <para>
    /// Si la clé est présente dans <see cref="LoadedRoundSounds"/>, le son associé est transmis
    /// au moteur de lecture <see cref="AudioPlaybackEngine"/>.
    /// </para>
    /// <para>
    /// Si la clé n’existe pas, la méthode ne lève pas d’exception et n’effectue aucune lecture.
    /// </para>
    /// </remarks>
    public void Play(string key, float volume = 1f)
    {
        if (_loadedRoundSounds.TryGetValue(key, out var sound))
            _engine.PlaySound(sound, volume);
    }

    /// <summary>
    /// Joue aléatoirement une variante sonore parmi toutes celles correspondant
    /// à un préfixe logique donné.
    /// </summary>
    /// <param name="keyPrefix">
    /// Le préfixe logique permettant d’identifier un ensemble de variantes.
    /// </param>
    /// <param name="volume">
    /// Le volume de lecture souhaité pour le son choisi.
    /// La valeur par défaut est <c>1f</c>.
    /// </param>
    /// <remarks>
    /// <para>
    /// Cette méthode recherche tous les sons dont la clé :
    /// </para>
    /// <list type="bullet">
    /// <item><description>est exactement égale à <paramref name="keyPrefix"/> ;</description></item>
    /// <item><description>ou commence par <c>keyPrefix_</c>.</description></item>
    /// </list>
    /// <para>
    /// Cela permet de prendre en charge à la fois :
    /// </para>
    /// <list type="bullet">
    /// <item><description>un son unique chargé sous une clé simple ;</description></item>
    /// <item><description>plusieurs variantes chargées sous des clés suffixées.</description></item>
    /// </list>
    /// <para>
    /// Si aucune variante n’est trouvée, la méthode se termine silencieusement.
    /// Sinon, une variante est choisie aléatoirement et jouée via <see cref="AudioPlaybackEngine"/>.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Libère les ressources utilisées par le service audio de manche.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cette méthode libère le moteur de lecture interne utilisé pour la diffusion audio.
    /// </para>
    /// <para>
    /// Une fois l’instance supprimée, elle ne doit plus être utilisée pour préparer
    /// de nouvelles manches ni jouer de nouveaux sons.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        _engine.Dispose();
    }
}