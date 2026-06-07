using System.IO;
using VraiPseudoSae.Utils.GestionnaireSauvegarde;

namespace RetroHubUnitTest;

public sealed class SaveTests
{
    [Fact]
    public void Gestionnaire_sauvegarde_et_charge_un_emplacement_binaire()
    {
        string dossierSauvegardes = CreerDossierTemporaire();

        try
        {
            var gestionnaire = FabriqueGestionnaireSauvegarde.CreerPourDossier(new SauvegardeExempleSerialiseur(), dossierSauvegardes);
            SauvegardeExemple attendu = new("Alice", 42, new[] { 1, 2, 3, 5, 8 });

            ResultatOperationSauvegarde sauvegarde = gestionnaire.Sauvegarder(attendu, EmplacementSauvegarde.DepuisIndex(1));
            Assert.True(sauvegarde.EstReussite, sauvegarde.Message);

            attendu = new SauvegardeExemple("Alice", 51, new[] { 1, 2, 3, 5, 8, 13 });
            sauvegarde = gestionnaire.Sauvegarder(attendu, EmplacementSauvegarde.DepuisIndex(1));
            Assert.True(sauvegarde.EstReussite, sauvegarde.Message);

            ResultatChargementSauvegarde<SauvegardeExemple> chargement = gestionnaire.Charger(EmplacementSauvegarde.DepuisIndex(1));
            Assert.True(chargement.EstReussite, chargement.Message);
            Assert.NotNull(chargement.Valeur);
            Assert.Equal(attendu.NomJoueur, chargement.Valeur.NomJoueur);
            Assert.Equal(attendu.Score, chargement.Valeur.Score);
            Assert.Equal(attendu.ObjetsDebloques, chargement.Valeur.ObjetsDebloques);
            Assert.Equal("retrohub.exemple", chargement.Metadonnees?.CleType);
            Assert.Single(gestionnaire.ListerEmplacements(), emplacement => emplacement.Etat == EtatFichierSauvegarde.Valide);
        }
        finally
        {
            Directory.Delete(dossierSauvegardes, recursive: true);
        }
    }

    [Fact]
    public void Gestionnaire_signale_une_somme_de_controle_invalide()
    {
        string dossierSauvegardes = CreerDossierTemporaire();

        try
        {
            var gestionnaire = FabriqueGestionnaireSauvegarde.CreerPourDossier(new SauvegardeExempleSerialiseur(), dossierSauvegardes);
            EmplacementSauvegarde emplacement = EmplacementSauvegarde.Automatique;

            ResultatOperationSauvegarde sauvegarde = gestionnaire.Sauvegarder(new SauvegardeExemple("Bob", 7, new[] { 9 }), emplacement);
            Assert.True(sauvegarde.EstReussite, sauvegarde.Message);
            Assert.NotNull(sauvegarde.CheminFichier);

            byte[] octets = File.ReadAllBytes(sauvegarde.CheminFichier);
            octets[^1] ^= 0x7F;
            File.WriteAllBytes(sauvegarde.CheminFichier, octets);

            ResultatChargementSauvegarde<SauvegardeExemple> chargement = gestionnaire.Charger(emplacement);

            Assert.Equal(StatutOperationSauvegarde.SommeControleInvalide, chargement.Statut);
        }
        finally
        {
            Directory.Delete(dossierSauvegardes, recursive: true);
        }
    }

    private static string CreerDossierTemporaire()
    {
        string chemin = Path.Combine(Path.GetTempPath(), $"retrohub-save-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(chemin);
        return chemin;
    }

    private sealed record SauvegardeExemple(string NomJoueur, int Score, IReadOnlyList<int> ObjetsDebloques);

    private sealed class SauvegardeExempleSerialiseur : SerialiseurBinaireSauvegarde<SauvegardeExemple>
    {
        public override string CleType => "retrohub.exemple";

        public override int VersionActuelle => 1;

        public override void Ecrire(EcrivainDonneesSauvegarde ecrivain, SauvegardeExemple valeur)
        {
            ecrivain.EcrireChaine(valeur.NomJoueur);
            ecrivain.EcrireEntierCompact(valeur.Score);
            ecrivain.EcrireCollection(valeur.ObjetsDebloques, static (ecrivainElement, objet) => ecrivainElement.EcrireEntierCompact(objet));
        }

        public override SauvegardeExemple Lire(LecteurDonneesSauvegarde lecteur, int version)
        {
            VerifierVersionSupportee(version);

            string nomJoueur = lecteur.LireChaine();
            int score = lecteur.LireEntierCompact();
            IReadOnlyList<int> objetsDebloques = lecteur.LireListe(static lecteurElement => lecteurElement.LireEntierCompact());

            return new SauvegardeExemple(nomJoueur, score, objetsDebloques);
        }
    }
}
