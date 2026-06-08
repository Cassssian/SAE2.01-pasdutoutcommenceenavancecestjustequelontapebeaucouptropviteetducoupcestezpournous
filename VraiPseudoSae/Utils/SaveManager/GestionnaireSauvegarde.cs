using System.IO;

namespace VraiPseudoSae.Utils.SaveManager;

/// <summary>
/// Gestionnaire de sauvegarde typé qui orchestre sérialisation, encodage et stockage disque.
/// </summary>
public sealed class GestionnaireSauvegarde<T> : DepotSauvegardeBase<T>
{
    private readonly ISerialiseurSauvegarde<T> _serialiseur;
    private readonly IResolveurCheminSauvegarde _resolveurChemin;
    private readonly IEncodeurSauvegarde _encodeur;
    private readonly IStockageSauvegarde _stockage;
    private readonly OptionsGestionnaireSauvegarde _options;
    private readonly object _verrou = new();

    public GestionnaireSauvegarde(
        ISerialiseurSauvegarde<T> serialiseur,
        IResolveurCheminSauvegarde resolveurChemin,
        IEncodeurSauvegarde? encodeur = null,
        IStockageSauvegarde? stockage = null,
        OptionsGestionnaireSauvegarde? options = null)
    {
        _serialiseur = serialiseur ?? throw new ArgumentNullException(nameof(serialiseur));
        _resolveurChemin = resolveurChemin ?? throw new ArgumentNullException(nameof(resolveurChemin));
        _encodeur = encodeur ?? new EncodeurBinaireSauvegarde();
        _stockage = stockage ?? new StockageFichierSauvegarde();
        _options = options ?? new OptionsGestionnaireSauvegarde();
    }

    public override ResultatOperationSauvegarde Sauvegarder(T valeur, EmplacementSauvegarde emplacement)
    {
        string chemin = _resolveurChemin.ObtenirChemin(emplacement, _options.ExtensionFichier);

        lock (_verrou)
        {
            try
            {
                using MemoryStream fluxDonnees = new();
                using (EcrivainDonneesSauvegarde ecrivain = new(fluxDonnees))
                {
                    _serialiseur.Ecrire(ecrivain, valeur);
                }

                byte[] donnees = fluxDonnees.ToArray();
                byte[] fichier = _encodeur.Encoder(_serialiseur.CleType, _serialiseur.VersionActuelle, donnees, _options);
                _stockage.EcrireAtomiquement(chemin, fichier, _options.CreerCopieAvantRemplacement);
                return ResultatOperationSauvegarde.Reussite(chemin);
            }
            catch (ExceptionFormatSauvegarde ex)
            {
                return new ResultatOperationSauvegarde(ex.Statut, chemin, ex.Message, ex);
            }
            catch (IOException ex)
            {
                return new ResultatOperationSauvegarde(StatutOperationSauvegarde.ErreurEntreeSortie, chemin, ex.Message, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                return new ResultatOperationSauvegarde(StatutOperationSauvegarde.ErreurEntreeSortie, chemin, ex.Message, ex);
            }
            catch (Exception ex)
            {
                return new ResultatOperationSauvegarde(StatutOperationSauvegarde.ErreurSerialiseur, chemin, ex.Message, ex);
            }
        }
    }

    public override ResultatChargementSauvegarde<T> Charger(EmplacementSauvegarde emplacement)
    {
        string chemin = _resolveurChemin.ObtenirChemin(emplacement, _options.ExtensionFichier);

        lock (_verrou)
        {
            if (!_stockage.Existe(chemin))
                return new ResultatChargementSauvegarde<T>(StatutOperationSauvegarde.Introuvable, CheminFichier: chemin, Message: "Fichier de sauvegarde introuvable.");

            try
            {
                byte[] fichier = _stockage.LireTout(chemin);
                DonneesSauvegardeDecodees decode = _encodeur.Decoder(fichier);

                if (!decode.Metadonnees.CleType.Equals(_serialiseur.CleType, StringComparison.Ordinal))
                {
                    return new ResultatChargementSauvegarde<T>(
                        StatutOperationSauvegarde.TypeIncompatible,
                        Metadonnees: decode.Metadonnees,
                        CheminFichier: chemin,
                        Message: $"Type attendu '{_serialiseur.CleType}', type trouvé '{decode.Metadonnees.CleType}'.");
                }

                if (!_options.AutoriserVersionFutureSerialiseur &&
                    decode.Metadonnees.VersionSerialiseur > _serialiseur.VersionActuelle)
                {
                    return new ResultatChargementSauvegarde<T>(
                        StatutOperationSauvegarde.VersionNonSupportee,
                        Metadonnees: decode.Metadonnees,
                        CheminFichier: chemin,
                        Message: $"Version de sauvegarde {decode.Metadonnees.VersionSerialiseur} plus récente que le sérialiseur {_serialiseur.VersionActuelle}.");
                }

                using MemoryStream fluxDonnees = new(decode.Donnees);
                using LecteurDonneesSauvegarde lecteur = new(fluxDonnees, _options.TailleMaxChaineUtf8);
                T valeur = _serialiseur.Lire(lecteur, decode.Metadonnees.VersionSerialiseur);
                lecteur.VerifierFinDonnees();

                return new ResultatChargementSauvegarde<T>(
                    StatutOperationSauvegarde.Reussite,
                    valeur,
                    decode.Metadonnees,
                    chemin);
            }
            catch (ExceptionFormatSauvegarde ex)
            {
                return new ResultatChargementSauvegarde<T>(ex.Statut, CheminFichier: chemin, Message: ex.Message, Exception: ex);
            }
            catch (EndOfStreamException ex)
            {
                return new ResultatChargementSauvegarde<T>(StatutOperationSauvegarde.FormatInvalide, CheminFichier: chemin, Message: ex.Message, Exception: ex);
            }
            catch (IOException ex)
            {
                return new ResultatChargementSauvegarde<T>(StatutOperationSauvegarde.ErreurEntreeSortie, CheminFichier: chemin, Message: ex.Message, Exception: ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                return new ResultatChargementSauvegarde<T>(StatutOperationSauvegarde.ErreurEntreeSortie, CheminFichier: chemin, Message: ex.Message, Exception: ex);
            }
            catch (Exception ex)
            {
                return new ResultatChargementSauvegarde<T>(StatutOperationSauvegarde.ErreurSerialiseur, CheminFichier: chemin, Message: ex.Message, Exception: ex);
            }
        }
    }

    public override bool Existe(EmplacementSauvegarde emplacement)
    {
        string chemin = _resolveurChemin.ObtenirChemin(emplacement, _options.ExtensionFichier);
        return _stockage.Existe(chemin);
    }

    public override ResultatOperationSauvegarde Supprimer(EmplacementSauvegarde emplacement)
    {
        string chemin = _resolveurChemin.ObtenirChemin(emplacement, _options.ExtensionFichier);

        try
        {
            _stockage.Supprimer(chemin);
            return ResultatOperationSauvegarde.Reussite(chemin);
        }
        catch (IOException ex)
        {
            return new ResultatOperationSauvegarde(StatutOperationSauvegarde.ErreurEntreeSortie, chemin, ex.Message, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new ResultatOperationSauvegarde(StatutOperationSauvegarde.ErreurEntreeSortie, chemin, ex.Message, ex);
        }
    }

    public override IReadOnlyList<InformationsEmplacementSauvegarde> ListerEmplacements()
    {
        List<InformationsEmplacementSauvegarde> emplacements = new();

        foreach (string chemin in _resolveurChemin.EnumererFichiersSauvegarde(_options.ExtensionFichier))
        {
            FileInfo information = _stockage.ObtenirInformations(chemin);
            EmplacementSauvegarde emplacement = _resolveurChemin.EmplacementDepuisChemin(chemin, _options.ExtensionFichier);

            try
            {
                MetadonneesSauvegarde metadonnees = _encodeur.LireMetadonnees(_stockage.LireTout(chemin));
                emplacements.Add(new InformationsEmplacementSauvegarde(
                    emplacement,
                    chemin,
                    information.Length,
                    information.LastWriteTimeUtc,
                    EtatFichierSauvegarde.Valide,
                    metadonnees));
            }
            catch
            {
                emplacements.Add(new InformationsEmplacementSauvegarde(
                    emplacement,
                    chemin,
                    information.Exists ? information.Length : 0,
                    information.Exists ? information.LastWriteTimeUtc : DateTimeOffset.MinValue,
                    EtatFichierSauvegarde.Corrompu));
            }
        }

        return emplacements
            .OrderBy(emplacement => emplacement.Emplacement.Type)
            .ThenBy(emplacement => emplacement.Emplacement.Nom, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
