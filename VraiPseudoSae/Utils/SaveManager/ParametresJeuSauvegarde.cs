namespace VraiPseudoSae.Utils.SaveManager;

public sealed record ParametresJeuSauvegarde(
    int VolumeGeneral,
    int VolumeDialogues,
    int VolumeSfx,
    int VitesseTexte,
    string ToucheAvancer,
    string ToucheReculer,
    string ToucheDroite,
    string ToucheGauche,
    string ToucheInteraction)
{
    public static ParametresJeuSauvegarde ParDefaut { get; } = new(
        VolumeGeneral: 100,
        VolumeDialogues: 50,
        VolumeSfx: 50,
        VitesseTexte: 50,
        ToucheAvancer: "Z",
        ToucheReculer: "S",
        ToucheDroite: "D",
        ToucheGauche: "Q",
        ToucheInteraction: "E");

    public ParametresJeuSauvegarde Normaliser()
    {
        return new ParametresJeuSauvegarde(VolumeGeneral: ClampPourcentage(VolumeGeneral), VolumeDialogues: ClampPourcentage(VolumeDialogues), VolumeSfx: ClampPourcentage(VolumeSfx), VitesseTexte: ClampPourcentage(VitesseTexte), ToucheAvancer: NormaliserTouche(ToucheAvancer, ParDefaut.ToucheAvancer), ToucheReculer: NormaliserTouche(ToucheReculer, ParDefaut.ToucheReculer), ToucheDroite: NormaliserTouche(ToucheDroite, ParDefaut.ToucheDroite), ToucheGauche: NormaliserTouche(ToucheGauche, ParDefaut.ToucheGauche), ToucheInteraction: NormaliserTouche(ToucheInteraction, ParDefaut.ToucheInteraction));
    }

    private static int ClampPourcentage(int valeur)
    {
        if (valeur < 0)
            return 0;

        if (valeur > 100)
            return 100;

        return valeur;
    }

    private static string NormaliserTouche(string? touche, string defaut)
    {
        return string.IsNullOrWhiteSpace(touche)
            ? defaut
            : touche.Trim();
    }
}

public sealed class ParametresJeuSauvegardeSerialiseur : SerialiseurBinaireSauvegarde<ParametresJeuSauvegarde>
{
    public override string CleType => "retrohub.parametres";

    public override int VersionActuelle => 1;

    public override void Ecrire(EcrivainDonneesSauvegarde ecrivain, ParametresJeuSauvegarde valeur)
    {
        ParametresJeuSauvegarde normalise = valeur.Normaliser();

        ecrivain.EcrireEntierCompact(normalise.VolumeGeneral);
        ecrivain.EcrireEntierCompact(normalise.VolumeDialogues);
        ecrivain.EcrireEntierCompact(normalise.VolumeSfx);
        ecrivain.EcrireEntierCompact(normalise.VitesseTexte);
        ecrivain.EcrireChaine(normalise.ToucheAvancer);
        ecrivain.EcrireChaine(normalise.ToucheReculer);
        ecrivain.EcrireChaine(normalise.ToucheDroite);
        ecrivain.EcrireChaine(normalise.ToucheGauche);
        ecrivain.EcrireChaine(normalise.ToucheInteraction);
    }

    public override ParametresJeuSauvegarde Lire(LecteurDonneesSauvegarde lecteur, int version)
    {
        VerifierVersionSupportee(version);

        return new ParametresJeuSauvegarde(
            lecteur.LireEntierCompact(),
            lecteur.LireEntierCompact(),
            lecteur.LireEntierCompact(),
            lecteur.LireEntierCompact(),
            lecteur.LireChaine(),
            lecteur.LireChaine(),
            lecteur.LireChaine(),
            lecteur.LireChaine(),
            lecteur.LireChaine()).Normaliser();
    }
}

public static class ParametresJeuSauvegardeDepot
{
    private static readonly GestionnaireSauvegarde<ParametresJeuSauvegarde> Gestionnaire =
        FabriqueGestionnaireSauvegarde.CreerParDefaut(new ParametresJeuSauvegardeSerialiseur());

    private static readonly EmplacementSauvegarde EmplacementParametres =
        EmplacementSauvegarde.Nomme("parametres");

    public static ParametresJeuSauvegarde ChargerOuDefaut()
    {
        ResultatChargementSauvegarde<ParametresJeuSauvegarde> chargement =
            Gestionnaire.Charger(EmplacementParametres);

        return chargement.EstReussite && chargement.Valeur is not null
            ? chargement.Valeur.Normaliser()
            : ParametresJeuSauvegarde.ParDefaut;
    }

    public static void Sauvegarder(ParametresJeuSauvegarde parametres)
    {
        Gestionnaire.Sauvegarder(parametres.Normaliser(), EmplacementParametres);
    }
}
