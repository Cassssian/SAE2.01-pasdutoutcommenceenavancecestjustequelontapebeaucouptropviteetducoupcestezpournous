using System.IO;
using System.Text.Json;
using VraiPseudoSae.Utils.PakManager;

namespace VraiPseudoSae.Utils.AudioPlayer;

/// <summary>
/// Représente une référence audio logique extraite du fichier JSON de structure audio.
/// </summary>
/// <remarks>
/// Cette classe ne contient pas de données audio décodées.
/// Elle stocke uniquement les métadonnées nécessaires pour retrouver
/// un son dans un fichier <c>.pak</c>, reconstruire son nom de fichier réel
/// et l’associer à une clé JSON exploitable par <see cref="JsonPakAudioService"/>.
/// </remarks>
public sealed class AudioReference
{
    /// <summary>
    /// Obtient ou définit l’identifiant logique de l’entrée audio.
    /// </summary>
    /// <value>
    /// L’identifiant unique de l’entrée, généralement issu du champ <c>id</c> dans le JSON.
    /// </value>
    public string Id { get; set; } = "";
    
    /// <summary>
    /// Obtient ou définit le nom du fichier <c>.pak</c> contenant la ressource audio.
    /// </summary>
    /// <value>
    /// Le nom logique ou le chemin vers le paquet source, par exemple <c>uncategorized.pak</c>.
    /// </value>
    public string Pack { get; set; } = "";
    
    /// <summary>
    /// Obtient ou définit le chemin interne du dossier contenant le son dans le <c>.pak</c>.
    /// </summary>
    /// <value>
    /// Le chemin relatif à l’intérieur du paquet, par exemple
    /// <c>Uncategorized/SFX_Car_Mouvements</c>.
    /// </value>
    public string Path { get; set; } = "";
    
    /// <summary>
    /// Obtient ou définit la liste des suffixes numériques disponibles pour cette entrée audio.
    /// </summary>
    /// <value>
    /// Une liste de numéros de variantes, par exemple <c>0001</c>, <c>0002</c>, etc.
    /// Si plusieurs valeurs sont présentes, une variante peut être sélectionnée aléatoirement
    /// lors du chargement effectif via <see cref="JsonPakAudioService"/>.
    /// </value>
    public List<string> Numbers { get; set; } = new();
    
    /// <summary>
    /// Obtient ou définit la clé JSON complète normalisée associée à cette entrée audio.
    /// </summary>
    /// <value>
    /// La clé logique complète permettant de retrouver l’entrée depuis le service audio,
    /// par exemple <c>car_sound/category/first_jump/jump0001</c>.
    /// </value>
    public string JsonKey { get; set; } = "";
}

/// <summary>
/// Fournit un service de chargement, de mise en cache et de lecture de sons
/// définis dans un fichier JSON et stockés à l’intérieur de fichiers <c>.pak</c>.
/// </summary>
/// <remarks>
/// <para>
/// Ce service construit un index logique à partir d’un fichier JSON décrivant les ressources audio,
/// puis charge les sons à la demande depuis un <see cref="PakAudioCatalog"/>.
/// </para>
/// <para>
/// Deux niveaux de cache sont utilisés :
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// un cache technique par clé JSON, afin d’éviter de redécoder plusieurs fois le même son ;
/// </description>
/// </item>
/// <item>
/// <description>
/// un cache fonctionnel par alias utilisateur, afin de manipuler les sons avec des noms plus simples
/// dans le code gameplay.
/// </description>
/// </item>
/// </list>
/// <para>
/// La lecture effective est déléguée à <see cref="AudioPlaybackEngine"/>, qui reçoit des objets
/// <c>CachedSound</c> déjà décodés.
/// </para>
/// </remarks>
/// <seealso cref="AudioReference"/>
/// <seealso cref="PakAudioCatalog"/>
public sealed class JsonPakAudioService : IDisposable
{
    /// <summary>
    /// Générateur pseudo-aléatoire utilisé pour choisir une variante audio
    /// lorsque plusieurs numéros sont disponibles pour une même entrée.
    /// </summary>
    private Random _rng = new();
    
    /// <summary>
    /// Catalogue des ressources audio disponibles dans les fichiers <c>.pak</c>.
    /// </summary>
    private readonly PakAudioCatalog _catalog;
    
    /// <summary>
    /// Moteur de lecture audio chargé de jouer les sons décodés.
    /// </summary>
    private readonly AudioPlaybackEngine _engine;

    /// <summary>
    /// Index principal des références audio, accessible par clé JSON normalisée.
    /// </summary>
    /// <remarks>
    /// Ce dictionnaire est rempli lors de l’appel à <see cref="Load(string)"/>.
    /// </remarks>
    private readonly Dictionary<string, AudioReference> _byKey = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Cache technique des sons décodés, indexé par clé JSON normalisée.
    /// </summary>
    /// <remarks>
    /// Ce cache évite de relire et redécoder plusieurs fois le même son
    /// lorsqu’il est demandé par clé.
    /// </remarks>
    private readonly Dictionary<string, CachedSound> _cacheByKey = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Cache fonctionnel des sons décodés, indexé par alias utilisateur normalisé.
    /// </summary>
    /// <remarks>
    /// Ce cache permet de faire correspondre un alias simple, par exemple <c>goal_anime</c>,
    /// à un son déjà chargé et prêt à être joué.
    /// </remarks>
    private readonly Dictionary<string, CachedSound> _cacheByAlias = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initialise une nouvelle instance du service audio JSON/.pak.
    /// </summary>
    /// <remarks>
    /// Ce constructeur crée un catalogue vide. Il est utile pour les cas où le service
    /// est seulement utilisé avec des fichiers audio locaux via <see cref="LoadFromPath(string, string)"/>.
    /// </remarks>
    public JsonPakAudioService()
        : this(new PakAudioCatalog())
    {
    }

    /// <summary>
    /// Initialise une nouvelle instance du service audio JSON/.pak.
    /// </summary>
    /// <param name="catalog">
    /// Le catalogue des entrées audio disponibles dans les fichiers <c>.pak</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Peut être levée indirectement si <paramref name="catalog"/> est <see langword="null"/>
    /// et qu’il est utilisé ensuite dans le service.
    /// </exception>
    /// <remarks>
    /// Le moteur audio interne est créé avec une fréquence de 44 100 Hz,
    /// deux canaux et une capacité configurée à 80.
    /// </remarks>
    public JsonPakAudioService(PakAudioCatalog catalog)
    {
        _catalog = catalog;
        _engine = new AudioPlaybackEngine(44100, 2, 80);
    }

    /// <summary>
    /// Charge le fichier JSON de structure audio, reconstruit l’index des clés audio
    /// et vide tous les caches existants.
    /// </summary>
    /// <param name="jsonFilePath">
    /// Le chemin du fichier JSON décrivant l’arborescence logique des sons.
    /// </param>
    /// <exception cref="FileNotFoundException">
    /// Levée si le fichier JSON spécifié n’existe pas.
    /// </exception>
    /// <exception cref="JsonException">
    /// Levée si le contenu du fichier n’est pas un JSON valide.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Cette méthode efface l’index précédent ainsi que les caches techniques et fonctionnels.
    /// </para>
    /// <para>
    /// Elle ne précharge pas automatiquement tous les sons. Elle construit uniquement
    /// la structure logique nécessaire pour les retrouver plus tard.
    /// </para>
    /// </remarks>
    public void Load(string jsonFilePath)
    {
        _byKey.Clear();
        _cacheByKey.Clear();
        _cacheByAlias.Clear();

        string json = File.ReadAllText(jsonFilePath);
        using JsonDocument doc = JsonDocument.Parse(json);

        Walk(doc.RootElement, "");
    }

    /// <summary>
    /// Précharge un son à partir de sa clé JSON complète, sans lui associer d’alias.
    /// </summary>
    /// <param name="key">
    /// La clé JSON logique du son à précharger.
    /// </param>
    /// <exception cref="KeyNotFoundException">
    /// Levée si la clé demandée n’existe pas dans l’index audio chargé.
    /// </exception>
    /// <remarks>
    /// Si le son est déjà présent dans le cache technique, cette méthode ne fait rien.
    /// </remarks>
    public void PreloadByKey(string key)
    {
        string normalizedKey = Normalize(key);

        if (_cacheByKey.ContainsKey(normalizedKey))
            return;

        if (!_byKey.TryGetValue(normalizedKey, out var audio))
            throw new KeyNotFoundException($"Clé audio introuvable : {key}");

        _cacheByKey[normalizedKey] = LoadSound(audio);
    }

    /// <summary>
    /// Précharge un son à partir de sa clé JSON et l’associe à un alias utilisateur.
    /// </summary>
    /// <param name="key">
    /// La clé JSON logique du son à charger.
    /// </param>
    /// <param name="alias">
    /// L’alias utilisateur à associer au son chargé.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Levée si <paramref name="alias"/> est vide ou composé uniquement d’espaces.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Levée si la clé audio fournie n’existe pas dans l’index.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Si le son n’est pas encore présent dans le cache technique, il est chargé et décodé.
    /// </para>
    /// <para>
    /// L’alias pointe ensuite vers ce même son en mémoire. Plusieurs alias peuvent donc
    /// référencer la même ressource audio décodée.
    /// </para>
    /// </remarks>
    public void Preload(string key, string alias)
    {
        string normalizedKey = Normalize(key);
        string normalizedAlias = NormalizeAlias(alias);

        if (string.IsNullOrWhiteSpace(normalizedAlias))
            throw new ArgumentException("L'alias ne peut pas être vide.", nameof(alias));

        if (!_cacheByKey.TryGetValue(normalizedKey, out var sound))
        {
            if (!_byKey.TryGetValue(normalizedKey, out var audio))
                throw new KeyNotFoundException($"Clé audio introuvable : {key}");

            sound = LoadSound(audio);
            _cacheByKey[normalizedKey] = sound;
        }

        _cacheByAlias[normalizedAlias] = sound;
    }

    /// <summary>
    /// Charge un son depuis un chemin de fichier local et l’associe à un alias utilisateur.
    /// </summary>
    /// <param name="filePath">
    /// Le chemin du fichier audio à charger. Les formats pris en charge sont ceux de
    /// <see cref="AudioDecodingHelper.FromFile(string)"/>.
    /// </param>
    /// <param name="alias">
    /// L’alias utilisateur à associer au son chargé.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Levée si <paramref name="alias"/> est vide ou si <paramref name="filePath"/> est vide.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Levée si le fichier demandé n’existe pas.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Levée si l’extension du fichier ne correspond à aucun format audio pris en charge.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Le son est décodé une seule fois, stocké en mémoire sous l’alias fourni, puis peut être
    /// joué autant de fois que nécessaire avec <see cref="Play(string, float)"/>.
    /// </para>
    /// <para>
    /// Si l’alias existe déjà, il est remplacé par le nouveau fichier chargé.
    /// </para>
    /// </remarks>
    public void LoadFromPath(string filePath, string alias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);

        string normalizedAlias = NormalizeAlias(alias);

        if (string.IsNullOrWhiteSpace(normalizedAlias))
            throw new ArgumentException("L'alias ne peut pas être vide.", nameof(alias));

        _cacheByAlias[normalizedAlias] = AudioDecodingHelper.FromFile(filePath);
    }

    /// <summary>
    /// Joue un son déjà associé à un alias utilisateur.
    /// </summary>
    /// <param name="alias">
    /// L’alias du son à jouer.
    /// </param>
    /// <param name="volume">
    /// Le volume de lecture souhaité.
    /// La valeur par défaut est <c>1f</c>.
    /// </param>
    /// <exception cref="KeyNotFoundException">
    /// Levée si aucun son n’est associé à l’alias fourni.
    /// </exception>
    /// <remarks>
    /// Le son doit avoir été préalablement associé à un alias avec <see cref="Preload(string, string)"/>.
    /// </remarks>
    public void Play(string alias, float volume = 1f)
    {
        string normalizedAlias = NormalizeAlias(alias);

        if (!_cacheByAlias.TryGetValue(normalizedAlias, out var sound))
            throw new KeyNotFoundException($"Alias audio introuvable : {alias}");

        _engine.PlaySound(sound, volume);
    }

    public LoopingSoundHandle PlayLooping(string alias, float volume = 1f)
    {
        string normalizedAlias = NormalizeAlias(alias);

        if (!_cacheByAlias.TryGetValue(normalizedAlias, out var sound))
            throw new KeyNotFoundException($"Alias audio introuvable : {alias}");

        return _engine.PlayLoopingSound(sound, volume);
    }

    /// <summary>
    /// Joue un son directement à partir de sa clé JSON.
    /// </summary>
    /// <param name="key">
    /// La clé JSON logique du son à jouer.
    /// </param>
    /// <param name="volume">
    /// Le volume de lecture souhaité.
    /// La valeur par défaut est <c>1f</c>.
    /// </param>
    /// <exception cref="KeyNotFoundException">
    /// Levée si la clé fournie n’existe pas dans l’index audio.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Cette méthode conserve l’ancien comportement consistant à jouer un son directement
    /// via sa clé JSON, sans passer par un alias.
    /// </para>
    /// <para>
    /// Si le son n’a pas encore été préchargé, il est chargé à la volée puis joué immédiatement.
    /// </para>
    /// </remarks>
    public void PlayByKey(string key, float volume = 1f)
    {
        string normalizedKey = Normalize(key);

        if (!_cacheByKey.TryGetValue(normalizedKey, out var sound))
        {
            if (!_byKey.TryGetValue(normalizedKey, out var audio))
                throw new KeyNotFoundException($"Clé audio introuvable : {key}");

            sound = LoadSound(audio);
            _cacheByKey[normalizedKey] = sound;
        }

        _engine.PlaySound(sound, volume);
    }

    /// <summary>
    /// Tente de précharger un son et de l’associer à un alias, sans propager d’exception.
    /// </summary>
    /// <param name="key">
    /// La clé JSON logique du son à charger.
    /// </param>
    /// <param name="alias">
    /// L’alias utilisateur à associer au son.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si le préchargement réussit ;
    /// sinon <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Cette méthode encapsule <see cref="Preload(string, string)"/> dans un bloc <c>try/catch</c>.
    /// </remarks>
    public bool TryPreload(string key, string alias)
    {
        try
        {
            Preload(key, alias);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tente de charger un son depuis un chemin de fichier local et de l’associer à un alias,
    /// sans propager d’exception.
    /// </summary>
    /// <param name="filePath">
    /// Le chemin du fichier audio à charger.
    /// </param>
    /// <param name="alias">
    /// L’alias utilisateur à associer au son.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si le chargement réussit ;
    /// sinon <see langword="false"/>.
    /// </returns>
    public bool TryLoadFromPath(string filePath, string alias)
    {
        try
        {
            LoadFromPath(filePath, alias);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tente de jouer un son par alias, sans propager d’exception.
    /// </summary>
    /// <param name="alias">
    /// L’alias du son à jouer.
    /// </param>
    /// <param name="volume">
    /// Le volume de lecture souhaité.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si la lecture a été lancée avec succès ;
    /// sinon <see langword="false"/>.
    /// </returns>
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
    /// Tente de jouer un son directement par clé JSON, sans propager d’exception.
    /// </summary>
    /// <param name="key">
    /// La clé JSON logique du son à jouer.
    /// </param>
    /// <param name="volume">
    /// Le volume de lecture souhaité.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si la lecture a été lancée avec succès ;
    /// sinon <see langword="false"/>.
    /// </returns>
    public bool TryPlayByKey(string key, float volume = 1f)
    {
        try
        {
            PlayByKey(key, volume);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Indique si une clé JSON audio existe dans l’index actuellement chargé.
    /// </summary>
    /// <param name="key">
    /// La clé JSON à vérifier.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si la clé existe dans l’index ;
    /// sinon <see langword="false"/>.
    /// </returns>
    public bool ContainsKey(string key)
    {
        return _byKey.ContainsKey(Normalize(key));
    }

    /// <summary>
    /// Indique si un alias utilisateur est actuellement enregistré dans le cache fonctionnel.
    /// </summary>
    /// <param name="alias">
    /// L’alias à vérifier.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si l’alias existe ;
    /// sinon <see langword="false"/>.
    /// </returns>
    public bool ContainsAlias(string alias)
    {
        return _cacheByAlias.ContainsKey(NormalizeAlias(alias));
    }

    /// <summary>
    /// Supprime un alias utilisateur du cache fonctionnel.
    /// </summary>
    /// <param name="alias">
    /// L’alias à supprimer.
    /// </param>
    /// <remarks>
    /// Cette méthode ne supprime pas forcément le son du cache technique.
    /// Elle retire uniquement l’association alias -&gt; son.
    /// </remarks>
    public void RemoveAlias(string alias)
    {
        _cacheByAlias.Remove(NormalizeAlias(alias));
    }

    /// <summary>
    /// Supprime tous les alias utilisateur actuellement enregistrés.
    /// </summary>
    /// <remarks>
    /// Le cache technique par clé JSON n’est pas vidé par cette méthode.
    /// </remarks>
    public void ClearAliases()
    {
        _cacheByAlias.Clear();
    }

    /// <summary>
    /// Vide l’ensemble des caches audio du service.
    /// </summary>
    /// <remarks>
    /// Cette méthode efface à la fois :
    /// <list type="bullet">
    /// <item><description>le cache technique par clé JSON ;</description></item>
    /// <item><description>le cache fonctionnel par alias.</description></item>
    /// </list>
    /// </remarks>
    public void ClearAllCache()
    {
        _cacheByKey.Clear();
        _cacheByAlias.Clear();
    }

    /// <summary>
    /// Retourne la liste actuelle des alias audio enregistrés.
    /// </summary>
    /// <returns>
    /// Une collection en lecture seule contenant tous les alias connus.
    /// </returns>
    public IReadOnlyCollection<string> GetAliases()
    {
        return _cacheByAlias.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Charge et décode un son à partir d’une référence audio.
    /// </summary>
    /// <param name="audio">
    /// La référence logique décrivant le son à charger.
    /// </param>
    /// <returns>
    /// Une instance de <c>CachedSound</c> contenant les données audio prêtes à être jouées.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Levée si aucune valeur n’est définie dans <see cref="AudioReference.Numbers"/>.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Levée si aucune entrée correspondante n’est trouvée dans les fichiers <c>.pak</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Le nom réel du fichier audio est reconstruit à partir du dernier segment du chemin
    /// et d’un numéro choisi dans <see cref="AudioReference.Numbers"/>.
    /// </para>
    /// <para>
    /// Si plusieurs numéros sont disponibles, l’un d’eux est sélectionné aléatoirement.
    /// </para>
    /// </remarks>
    private CachedSound LoadSound(AudioReference audio)
    {
        string expectedPakFileName = Path.GetFileName(audio.Pack);
        string folderName = audio.Path.Split('/', '\\').Last();

        if (audio.Numbers.Count == 0)
            throw new InvalidOperationException($"Aucun number défini pour {audio.JsonKey}");

        string selectedNumber = audio.Numbers.Count == 1
            ? audio.Numbers[0]
            : audio.Numbers[_rng.Next(audio.Numbers.Count)];

        string expectedFileName = $"{folderName}_{selectedNumber}.mp3";

        string expectedEntryPath = $"{audio.Path}/{expectedFileName}"
            .Replace('\\', '/')
            .Trim('/');

        var entry = _catalog.Entries.FirstOrDefault(e =>
            Path.GetFileName(e.PackPath).Equals(expectedPakFileName, StringComparison.OrdinalIgnoreCase) &&
            e.EntryPath.Equals(expectedEntryPath, StringComparison.OrdinalIgnoreCase));

        if (entry == null)
        {
            throw new FileNotFoundException(
                $"Entrée introuvable dans les .pak : pack={audio.Pack}, path={audio.Path}, file={expectedFileName}");
        }

        using var stream = _catalog.OpenEntryStream(entry);
        return AudioDecodingHelper.FromStream(stream, entry.FileName);
    }

    /// <summary>
    /// Parcourt récursivement un élément JSON afin de construire l’index des références audio.
    /// </summary>
    /// <param name="element">
    /// L’élément JSON courant à analyser.
    /// </param>
    /// <param name="currentPath">
    /// Le chemin logique accumulé jusqu’à l’élément courant.
    /// </param>
    /// <remarks>
    /// Cette méthode gère les objets et les tableaux.
    /// Lorsqu’un objet complet représentant une entrée audio est détecté,
    /// il est converti puis ajouté à l’index <c>_byKey</c>.
    /// </remarks>
    private void Walk(JsonElement element, string currentPath)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                if (TryCreateAudioReference(element, currentPath, out var audioRef))
                {
                    _byKey[Normalize(audioRef.JsonKey)] = audioRef;
                }

                foreach (JsonProperty property in element.EnumerateObject())
                {
                    string nextPath = AppendPath(currentPath, property.Name);
                    Walk(property.Value, nextPath);
                }

                break;
            }

            case JsonValueKind.Array:
            {
                foreach (JsonElement item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object &&
                        item.TryGetProperty("id", out JsonElement idProp) &&
                        idProp.ValueKind == JsonValueKind.String)
                    {
                        string id = idProp.GetString() ?? "";
                        string nextPath = AppendPath(currentPath, id);
                        Walk(item, nextPath);
                    }
                    else
                    {
                        Walk(item, currentPath);
                    }
                }

                break;
            }
        }
    }

    /// <summary>
    /// Tente de créer une <see cref="AudioReference"/> à partir d’un objet JSON.
    /// </summary>
    /// <param name="element">
    /// L’objet JSON à analyser.
    /// </param>
    /// <param name="currentPath">
    /// Le chemin logique courant dans l’arborescence JSON.
    /// </param>
    /// <param name="audioRef">
    /// La référence audio créée si l’opération réussit ; sinon une valeur non exploitable.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si l’objet JSON contient les informations minimales
    /// nécessaires à la création d’une référence audio ; sinon <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Les propriétés minimales attendues sont :
    /// <list type="bullet">
    /// <item><description><c>id</c></description></item>
    /// <item><description><c>pack</c></description></item>
    /// <item><description><c>path</c></description></item>
    /// </list>
    /// La propriété <c>number</c> peut être soit une chaîne unique, soit un tableau de chaînes.
    /// </remarks>
    private static bool TryCreateAudioReference(JsonElement element, string currentPath, out AudioReference audioRef)
    {
        audioRef = null!;

        if (!element.TryGetProperty("id", out JsonElement idProp) || idProp.ValueKind != JsonValueKind.String)
            return false;

        if (!element.TryGetProperty("pack", out JsonElement packProp) || packProp.ValueKind != JsonValueKind.String)
            return false;

        if (!element.TryGetProperty("path", out JsonElement pathProp) || pathProp.ValueKind != JsonValueKind.String)
            return false;

        string id = idProp.GetString() ?? "";
        string pack = packProp.GetString() ?? "";
        string path = pathProp.GetString() ?? "";

        string jsonKey = currentPath;
        if (string.IsNullOrWhiteSpace(jsonKey) || !jsonKey.EndsWith(id, StringComparison.OrdinalIgnoreCase))
            jsonKey = AppendPath(currentPath, id);

        List<string> numbers = new();

        if (element.TryGetProperty("number", out JsonElement numberProp))
        {
            if (numberProp.ValueKind == JsonValueKind.String)
            {
                string? value = numberProp.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    numbers.Add(value);
            }
            else if (numberProp.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in numberProp.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        string? value = item.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            numbers.Add(value);
                    }
                }
            }
        }

        audioRef = new AudioReference
        {
            Id = id,
            Pack = pack,
            Path = path,
            Numbers = numbers,
            JsonKey = jsonKey
        };

        return true;
    }

    /// <summary>
    /// Concatène deux segments de chemin logique en utilisant <c>/</c> comme séparateur.
    /// </summary>
    /// <param name="left">
    /// Le segment gauche.
    /// </param>
    /// <param name="right">
    /// Le segment droit.
    /// </param>
    /// <returns>
    /// Le chemin combiné, ou le segment non vide si l’un des deux est vide.
    /// </returns>

    private static string AppendPath(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left))
            return right;

        if (string.IsNullOrWhiteSpace(right))
            return left;

        return $"{left}/{right}";
    }

    /// <summary>
    /// Normalise une clé ou un chemin logique en remplaçant les séparateurs inverses
    /// et en supprimant les séparateurs superflus en bordure.
    /// </summary>
    /// <param name="path">
    /// Le chemin ou la clé à normaliser.
    /// </param>
    /// <returns>
    /// Une chaîne normalisée utilisant <c>/</c> comme séparateur.
    /// </returns>
    private static string Normalize(string path)
    {
        return path.Replace('\\', '/').Trim('/');
    }

    /// <summary>
    /// Normalise un alias utilisateur.
    /// </summary>
    /// <param name="alias">
    /// L’alias à normaliser.
    /// </param>
    /// <returns>
    /// L’alias après suppression des espaces en début et fin de chaîne.
    /// </returns>
    private static string NormalizeAlias(string alias)
    {
        return alias.Trim();
    }

    /// <summary>
    /// Libère les ressources utilisées par le service audio.
    /// </summary>
    /// <remarks>
    /// Cette méthode libère le moteur audio interne.
    /// Après appel, l’instance ne doit plus être utilisée pour jouer de nouveaux sons.
    /// </remarks>
    public void Dispose()
    {
        _engine.Dispose();
    }
}
