namespace VraiPseudoSae.Utils.SaveManager;

/// <summary>
/// Définit la compression appliquée au bloc binaire de sauvegarde.
/// </summary>
public enum ModeCompressionSauvegarde : byte
{
    Aucune = 0,
    Brotli = 1,
    GZip = 2,
    Automatique = 255
}

/// <summary>
/// Définit le mécanisme de vérification utilisé pour détecter une sauvegarde corrompue.
/// </summary>
public enum ModeControleSauvegarde : byte
{
    Aucun = 0,
    Crc32 = 1
}

/// <summary>
/// Représente l'état d'une opération de sauvegarde ou de chargement.
/// </summary>
public enum StatutOperationSauvegarde
{
    Reussite,
    Introuvable,
    FormatInvalide,
    VersionNonSupportee,
    SommeControleInvalide,
    TypeIncompatible,
    ErreurEntreeSortie,
    ErreurSerialiseur
}

/// <summary>
/// Décrit la catégorie logique d'un emplacement de sauvegarde.
/// </summary>
public enum TypeEmplacementSauvegarde
{
    Automatique,
    Indexe,
    Nomme
}

/// <summary>
/// Indique si un fichier listé semble lisible ou corrompu.
/// </summary>
public enum EtatFichierSauvegarde
{
    Valide,
    Corrompu
}

/// <summary>
/// Identifie un emplacement de sauvegarde sans exposer directement un chemin disque.
/// </summary>
public readonly record struct EmplacementSauvegarde
{
    private EmplacementSauvegarde(TypeEmplacementSauvegarde type, string nom)
    {
        Type = type;
        Nom = nom;
    }

    public TypeEmplacementSauvegarde Type { get; }

    public string Nom { get; }

    public static EmplacementSauvegarde Automatique { get; } = new(TypeEmplacementSauvegarde.Automatique, "auto");

    public static EmplacementSauvegarde DepuisIndex(int index)
    {
        if (index <= 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Un index d'emplacement doit être positif.");

        return new EmplacementSauvegarde(TypeEmplacementSauvegarde.Indexe, $"slot{index:00}");
    }

    public static EmplacementSauvegarde Nomme(string nom)
    {
        if (string.IsNullOrWhiteSpace(nom))
            throw new ArgumentException("Le nom d'un emplacement ne peut pas être vide.", nameof(nom));

        return new EmplacementSauvegarde(TypeEmplacementSauvegarde.Nomme, nom.Trim());
    }

    public override string ToString()
    {
        return Nom;
    }
}

/// <summary>
/// Regroupe les réglages du gestionnaire de sauvegarde.
/// </summary>
public sealed record OptionsGestionnaireSauvegarde
{
    public string ExtensionFichier { get; init; } = ".vpsave";

    public ModeCompressionSauvegarde Compression { get; init; } = ModeCompressionSauvegarde.Automatique;

    public ModeControleSauvegarde Controle { get; init; } = ModeControleSauvegarde.Crc32;

    public bool AutoriserVersionFutureSerialiseur { get; init; }

    public bool CreerCopieAvantRemplacement { get; init; } = true;

    public int TailleMaxChaineUtf8 { get; init; } = 1024 * 1024;
}

/// <summary>
/// Décrit l'en-tête d'un fichier de sauvegarde déjà encodé.
/// </summary>
public sealed record MetadonneesSauvegarde(
    string CleType,
    int VersionSerialiseur,
    DateTimeOffset CreeLeUtc,
    ModeCompressionSauvegarde Compression,
    ModeControleSauvegarde Controle,
    int TailleDonnees,
    int TailleDonneesStockees,
    uint SommeControle);

/// <summary>
/// Résultat d'une opération qui ne retourne pas de données de jeu.
/// </summary>
public sealed record ResultatOperationSauvegarde(
    StatutOperationSauvegarde Statut,
    string? CheminFichier = null,
    string? Message = null,
    Exception? Exception = null)
{
    public bool EstReussite => Statut == StatutOperationSauvegarde.Reussite;

    public static ResultatOperationSauvegarde Reussite(string? cheminFichier = null)
    {
        return new ResultatOperationSauvegarde(StatutOperationSauvegarde.Reussite, cheminFichier);
    }
}

/// <summary>
/// Résultat d'un chargement de sauvegarde typé.
/// </summary>
public sealed record ResultatChargementSauvegarde<T>(
    StatutOperationSauvegarde Statut,
    T? Valeur = default,
    MetadonneesSauvegarde? Metadonnees = null,
    string? CheminFichier = null,
    string? Message = null,
    Exception? Exception = null)
{
    public bool EstReussite => Statut == StatutOperationSauvegarde.Reussite;

    public T ValeurOuEchouer()
    {
        if (EstReussite)
            return Valeur!;

        throw new InvalidOperationException(Message ?? $"Chargement impossible: {Statut}.", Exception);
    }
}

/// <summary>
/// Informations retournées lors de l'inventaire des fichiers de sauvegarde.
/// </summary>
public sealed record InformationsEmplacementSauvegarde(
    EmplacementSauvegarde Emplacement,
    string CheminFichier,
    long TailleOctets,
    DateTimeOffset DerniereModificationUtc,
    EtatFichierSauvegarde Etat,
    MetadonneesSauvegarde? Metadonnees = null);

/// <summary>
/// Exception spécialisée pour les erreurs de format de sauvegarde.
/// </summary>
public sealed class ExceptionFormatSauvegarde : Exception
{
    public ExceptionFormatSauvegarde(StatutOperationSauvegarde statut, string message)
        : base(message)
    {
        Statut = statut;
    }

    public ExceptionFormatSauvegarde(StatutOperationSauvegarde statut, string message, Exception innerException)
        : base(message, innerException)
    {
        Statut = statut;
    }

    public StatutOperationSauvegarde Statut { get; }
}
