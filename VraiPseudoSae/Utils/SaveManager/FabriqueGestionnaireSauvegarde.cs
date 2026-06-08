namespace VraiPseudoSae.Utils.SaveManager;

/// <summary>
/// Fabrique pratique pour créer un gestionnaire avec les composants par défaut.
/// </summary>
public static class FabriqueGestionnaireSauvegarde
{
    public static GestionnaireSauvegarde<T> CreerParDefaut<T>(
        ISerialiseurSauvegarde<T> serialiseur,
        string nomJeu = "VraiPseudoSae",
        OptionsGestionnaireSauvegarde? options = null)
    {
        return new GestionnaireSauvegarde<T>(
            serialiseur,
            new ResolveurCheminSauvegardeLocalAppData(nomJeu),
            new EncodeurBinaireSauvegarde(),
            new StockageFichierSauvegarde(),
            options);
    }

    public static GestionnaireSauvegarde<T> CreerPourDossier<T>(
        ISerialiseurSauvegarde<T> serialiseur,
        string dossierSauvegardes,
        OptionsGestionnaireSauvegarde? options = null)
    {
        return new GestionnaireSauvegarde<T>(
            serialiseur,
            new ResolveurCheminSauvegardeDossier(dossierSauvegardes),
            new EncodeurBinaireSauvegarde(),
            new StockageFichierSauvegarde(),
            options);
    }
}
