# Utilisation du gestionnaire de sauvegarde

Le module se trouve dans l'espace de noms:

```csharp
using VraiPseudoSae.Utils.GestionnaireSauvegarde;
```

## 1. Créer le modèle de données

Ce modèle doit contenir uniquement les données utiles à reconstruire l'état du jeu.
Évite d'y mettre des objets WPF, des images, des sons ou des références runtime.

```csharp
public sealed record SauvegardeJoueur(
    string NomJoueur,
    int Score,
    int Niveau,
    IReadOnlyList<int> ObjetsDebloques);
```

## 2. Créer le sérialiseur binaire

Le sérialiseur contrôle exactement l'ordre et le format des données écrites.
C'est ce qui rend le fichier plus compact qu'une sauvegarde JSON.

```csharp
public sealed class SauvegardeJoueurSerialiseur : SerialiseurBinaireSauvegarde<SauvegardeJoueur>
{
    public override string CleType => "retrohub.joueur";

    public override int VersionActuelle => 1;

    public override void Ecrire(EcrivainDonneesSauvegarde ecrivain, SauvegardeJoueur valeur)
    {
        ecrivain.EcrireChaine(valeur.NomJoueur);
        ecrivain.EcrireEntierCompact(valeur.Score);
        ecrivain.EcrireEntierCompact(valeur.Niveau);
        ecrivain.EcrireCollection(
            valeur.ObjetsDebloques,
            static (ecrivainElement, objet) => ecrivainElement.EcrireEntierCompact(objet));
    }

    public override SauvegardeJoueur Lire(LecteurDonneesSauvegarde lecteur, int version)
    {
        VerifierVersionSupportee(version);

        string nomJoueur = lecteur.LireChaine();
        int score = lecteur.LireEntierCompact();
        int niveau = lecteur.LireEntierCompact();
        IReadOnlyList<int> objetsDebloques = lecteur.LireListe(
            static lecteurElement => lecteurElement.LireEntierCompact());

        return new SauvegardeJoueur(nomJoueur, score, niveau, objetsDebloques);
    }
}
```

## 3. Créer le gestionnaire

Par défaut, les sauvegardes vont dans `%LocalAppData%/VraiPseudoSae/Saves`.

```csharp
var gestionnaire = FabriqueGestionnaireSauvegarde.CreerParDefaut(
    new SauvegardeJoueurSerialiseur());
```

Pour utiliser un dossier précis:

```csharp
var gestionnaire = FabriqueGestionnaireSauvegarde.CreerPourDossier(
    new SauvegardeJoueurSerialiseur(),
    @"C:\Temp\MesSauvegardes");
```

## 4. Sauvegarder

```csharp
var donnees = new SauvegardeJoueur("Alice", 1500, 3, new[] { 1, 4, 7 });

ResultatOperationSauvegarde resultat = gestionnaire.Sauvegarder(
    donnees,
    EmplacementSauvegarde.Automatique);

if (!resultat.EstReussite)
{
    // resultat.Statut et resultat.Message indiquent la cause.
}
```

## 5. Charger

```csharp
ResultatChargementSauvegarde<SauvegardeJoueur> chargement = gestionnaire.Charger(
    EmplacementSauvegarde.Automatique);

if (chargement.EstReussite)
{
    SauvegardeJoueur donnees = chargement.Valeur!;
}
```

Si tu veux échouer directement en cas de problème:

```csharp
SauvegardeJoueur donnees = gestionnaire.ChargerOuEchouer(EmplacementSauvegarde.Automatique);
```

## 6. Slots disponibles

```csharp
EmplacementSauvegarde auto = EmplacementSauvegarde.Automatique;
EmplacementSauvegarde slot1 = EmplacementSauvegarde.DepuisIndex(1);
EmplacementSauvegarde profilAlex = EmplacementSauvegarde.Nomme("alex");
```

## 7. Lister et supprimer

```csharp
IReadOnlyList<InformationsEmplacementSauvegarde> emplacements = gestionnaire.ListerEmplacements();

foreach (InformationsEmplacementSauvegarde emplacement in emplacements)
{
    Console.WriteLine($"{emplacement.Emplacement.Nom}: {emplacement.Etat}");
}

gestionnaire.Supprimer(EmplacementSauvegarde.DepuisIndex(1));
```

## 8. Options utiles

```csharp
var options = new OptionsGestionnaireSauvegarde
{
    ExtensionFichier = ".vpsave",
    Compression = ModeCompressionSauvegarde.Automatique,
    Controle = ModeControleSauvegarde.Crc32,
    CreerCopieAvantRemplacement = true
};
```

`Automatique` teste les compressions disponibles et garde le plus petit résultat.
Si la compression n'améliore pas la taille, le fichier reste non compressé.

## 9. Faire évoluer une sauvegarde

Quand tu ajoutes des champs:

1. Incrémente `VersionActuelle`.
2. Écris les nouveaux champs à la fin.
3. Dans `Lire`, teste la version avant de lire les champs récents.

Exemple:

```csharp
if (version >= 2)
{
    int pieces = lecteur.LireEntierCompact();
}
```

Le fichier contient aussi une clé de type (`CleType`), une version de sérialiseur,
la date de création UTC, le mode de compression, la taille des données et une somme CRC32.
