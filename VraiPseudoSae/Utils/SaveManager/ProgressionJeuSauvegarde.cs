namespace VraiPseudoSae.Utils.GestionnaireSauvegarde;

public sealed record ProgressionJeuSauvegarde(bool IntroductionTerminee);

public sealed class ProgressionJeuSauvegardeSerialiseur : SerialiseurBinaireSauvegarde<ProgressionJeuSauvegarde>
{
    public override string CleType => "retrohub.progression";

    public override int VersionActuelle => 1;

    public override void Ecrire(EcrivainDonneesSauvegarde ecrivain, ProgressionJeuSauvegarde valeur)
    {
        ecrivain.EcrireBooleen(valeur.IntroductionTerminee);
    }

    public override ProgressionJeuSauvegarde Lire(LecteurDonneesSauvegarde lecteur, int version)
    {
        VerifierVersionSupportee(version);

        return new ProgressionJeuSauvegarde(lecteur.LireBooleen());
    }
}

public static class ProgressionJeuSauvegardeDepot
{
    private static readonly GestionnaireSauvegarde<ProgressionJeuSauvegarde> Gestionnaire =
        FabriqueGestionnaireSauvegarde.CreerParDefaut(new ProgressionJeuSauvegardeSerialiseur());

    private static readonly EmplacementSauvegarde EmplacementProgression =
        EmplacementSauvegarde.Nomme("progression");

    public static bool IntroductionTerminee()
    {
        ResultatChargementSauvegarde<ProgressionJeuSauvegarde> chargement =
            Gestionnaire.Charger(EmplacementProgression);

        return chargement.EstReussite && chargement.Valeur?.IntroductionTerminee == true;
    }

    public static void MarquerIntroductionTerminee()
    {
        Gestionnaire.Sauvegarder(
            new ProgressionJeuSauvegarde(IntroductionTerminee: true),
            EmplacementProgression);
    }
}
