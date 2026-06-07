using System.IO;

namespace VraiPseudoSae.Utils.GestionnaireSauvegarde;

/// <summary>
/// Convertit un type de données de jeu en flux binaire compact, et inversement.
/// </summary>
public interface ISerialiseurSauvegarde<T>
{
    string CleType { get; }

    int VersionActuelle { get; }

    void Ecrire(EcrivainDonneesSauvegarde ecrivain, T valeur);

    T Lire(LecteurDonneesSauvegarde lecteur, int version);
}

/// <summary>
/// Base utile pour les sérialiseurs de sauvegarde versionnés.
/// </summary>
public abstract class SerialiseurSauvegardeBase<T> : ISerialiseurSauvegarde<T>
{
    public abstract string CleType { get; }

    public abstract int VersionActuelle { get; }

    public abstract void Ecrire(EcrivainDonneesSauvegarde ecrivain, T valeur);

    public abstract T Lire(LecteurDonneesSauvegarde lecteur, int version);

    protected void VerifierVersionSupportee(int version, int versionMinimum = 1)
    {
        if (version < versionMinimum || version > VersionActuelle)
        {
            throw new ExceptionFormatSauvegarde(
                StatutOperationSauvegarde.VersionNonSupportee,
                $"Version {version} non supportée pour {CleType}.");
        }
    }
}

/// <summary>
/// Base à hériter pour déclarer explicitement un sérialiseur binaire de jeu.
/// </summary>
public abstract class SerialiseurBinaireSauvegarde<T> : SerialiseurSauvegardeBase<T>
{
}

/// <summary>
/// Contrat principal pour sauvegarder, charger et gérer les emplacements disponibles.
/// </summary>
public interface IDepotSauvegarde<T>
{
    ResultatOperationSauvegarde Sauvegarder(T valeur, EmplacementSauvegarde emplacement);

    ResultatChargementSauvegarde<T> Charger(EmplacementSauvegarde emplacement);

    T ChargerOuEchouer(EmplacementSauvegarde emplacement);

    bool EssayerCharger(EmplacementSauvegarde emplacement, out T? valeur);

    bool Existe(EmplacementSauvegarde emplacement);

    ResultatOperationSauvegarde Supprimer(EmplacementSauvegarde emplacement);

    IReadOnlyList<InformationsEmplacementSauvegarde> ListerEmplacements();
}

/// <summary>
/// Base commune des dépôts de sauvegarde typés.
/// </summary>
public abstract class DepotSauvegardeBase<T> : IDepotSauvegarde<T>
{
    public abstract ResultatOperationSauvegarde Sauvegarder(T valeur, EmplacementSauvegarde emplacement);

    public abstract ResultatChargementSauvegarde<T> Charger(EmplacementSauvegarde emplacement);

    public virtual T ChargerOuEchouer(EmplacementSauvegarde emplacement)
    {
        return Charger(emplacement).ValeurOuEchouer();
    }

    public virtual bool EssayerCharger(EmplacementSauvegarde emplacement, out T? valeur)
    {
        ResultatChargementSauvegarde<T> resultat = Charger(emplacement);
        valeur = resultat.Valeur;
        return resultat.EstReussite;
    }

    public abstract bool Existe(EmplacementSauvegarde emplacement);

    public abstract ResultatOperationSauvegarde Supprimer(EmplacementSauvegarde emplacement);

    public abstract IReadOnlyList<InformationsEmplacementSauvegarde> ListerEmplacements();
}

/// <summary>
/// Résout les chemins physiques associés aux emplacements de sauvegarde.
/// </summary>
public interface IResolveurCheminSauvegarde
{
    string DossierSauvegardes { get; }

    string ObtenirChemin(EmplacementSauvegarde emplacement, string extensionFichier);

    IEnumerable<string> EnumererFichiersSauvegarde(string extensionFichier);

    EmplacementSauvegarde EmplacementDepuisChemin(string chemin, string extensionFichier);
}

/// <summary>
/// Lit et écrit les fichiers de sauvegarde sans connaître le format binaire.
/// </summary>
public interface IStockageSauvegarde
{
    void EcrireAtomiquement(string chemin, byte[] donnees, bool creerCopie);

    byte[] LireTout(string chemin);

    bool Existe(string chemin);

    void Supprimer(string chemin);

    FileInfo ObtenirInformations(string chemin);
}

/// <summary>
/// Compresse et décompresse le bloc de données d'une sauvegarde.
/// </summary>
public interface IAlgorithmeCompressionSauvegarde
{
    ModeCompressionSauvegarde Mode { get; }

    byte[] Compresser(byte[] donnees);

    byte[] Decompresser(byte[] donnees);
}

/// <summary>
/// Calcule une somme de contrôle pour vérifier l'intégrité du fichier.
/// </summary>
public interface IControleIntegriteSauvegarde
{
    ModeControleSauvegarde Mode { get; }

    uint Calculer(ReadOnlySpan<byte> donnees);

    bool Verifier(ReadOnlySpan<byte> donnees, uint sommeAttendue);
}

/// <summary>
/// Encode l'enveloppe complète d'une sauvegarde et décode son contenu.
/// </summary>
public interface IEncodeurSauvegarde
{
    byte[] Encoder(string cleType, int versionSerialiseur, byte[] donnees, OptionsGestionnaireSauvegarde options);

    DonneesSauvegardeDecodees Decoder(byte[] donnees);

    MetadonneesSauvegarde LireMetadonnees(byte[] donnees);
}

/// <summary>
/// Données utiles après décodage de l'enveloppe de fichier.
/// </summary>
public sealed record DonneesSauvegardeDecodees(MetadonneesSauvegarde Metadonnees, byte[] Donnees);
