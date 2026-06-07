using System.IO;

namespace VraiPseudoSae.Utils.GestionnaireSauvegarde;

/// <summary>
/// Base pour les stratégies qui transforment un emplacement logique en chemin de fichier.
/// </summary>
public abstract class ResolveurCheminSauvegardeBase : IResolveurCheminSauvegarde
{
    public abstract string DossierSauvegardes { get; }

    public virtual string ObtenirChemin(EmplacementSauvegarde emplacement, string extensionFichier)
    {
        Directory.CreateDirectory(DossierSauvegardes);

        string extension = NormaliserExtension(extensionFichier);
        string nomFichier = NettoyerNomFichier(emplacement.Nom);
        return Path.Combine(DossierSauvegardes, $"{nomFichier}{extension}");
    }

    public virtual IEnumerable<string> EnumererFichiersSauvegarde(string extensionFichier)
    {
        if (!Directory.Exists(DossierSauvegardes))
            return Array.Empty<string>();

        return Directory.EnumerateFiles(DossierSauvegardes, $"*{NormaliserExtension(extensionFichier)}", SearchOption.TopDirectoryOnly);
    }

    public virtual EmplacementSauvegarde EmplacementDepuisChemin(string chemin, string extensionFichier)
    {
        string extension = NormaliserExtension(extensionFichier);
        string nom = Path.GetFileName(chemin);

        if (nom.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            nom = nom[..^extension.Length];

        if (nom.Equals(EmplacementSauvegarde.Automatique.Nom, StringComparison.OrdinalIgnoreCase))
            return EmplacementSauvegarde.Automatique;

        if (nom.StartsWith("slot", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(nom[4..], out int index) &&
            index > 0)
        {
            return EmplacementSauvegarde.DepuisIndex(index);
        }

        return EmplacementSauvegarde.Nomme(nom);
    }

    protected static string NormaliserExtension(string extensionFichier)
    {
        if (string.IsNullOrWhiteSpace(extensionFichier))
            return ".vpsave";

        string extension = extensionFichier.Trim();
        return extension.StartsWith('.') ? extension : $".{extension}";
    }

    protected static string NettoyerNomFichier(string nomFichier)
    {
        char[] caracteresInterdits = Path.GetInvalidFileNameChars();
        char[] resultat = nomFichier.Trim().ToCharArray();

        for (int i = 0; i < resultat.Length; i++)
        {
            if (caracteresInterdits.Contains(resultat[i]))
                resultat[i] = '_';
        }

        return new string(resultat);
    }
}

/// <summary>
/// Stocke les sauvegardes dans %LocalAppData%/nomJeu/Saves.
/// </summary>
public sealed class ResolveurCheminSauvegardeLocalAppData : ResolveurCheminSauvegardeBase
{
    public ResolveurCheminSauvegardeLocalAppData(string nomJeu = "VraiPseudoSae", string nomDossier = "Saves")
    {
        if (string.IsNullOrWhiteSpace(nomJeu))
            throw new ArgumentException("Le nom du jeu est obligatoire.", nameof(nomJeu));

        if (string.IsNullOrWhiteSpace(nomDossier))
            throw new ArgumentException("Le nom du dossier est obligatoire.", nameof(nomDossier));

        string racine = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        DossierSauvegardes = Path.Combine(racine, nomJeu.Trim(), nomDossier.Trim());
    }

    public override string DossierSauvegardes { get; }
}

/// <summary>
/// Stocke les sauvegardes dans un dossier fourni explicitement.
/// </summary>
public sealed class ResolveurCheminSauvegardeDossier : ResolveurCheminSauvegardeBase
{
    public ResolveurCheminSauvegardeDossier(string dossierSauvegardes)
    {
        if (string.IsNullOrWhiteSpace(dossierSauvegardes))
            throw new ArgumentException("Le dossier de sauvegarde est obligatoire.", nameof(dossierSauvegardes));

        DossierSauvegardes = dossierSauvegardes;
    }

    public override string DossierSauvegardes { get; }
}

/// <summary>
/// Base pour le stockage physique des fichiers de sauvegarde.
/// </summary>
public abstract class StockageSauvegardeBase : IStockageSauvegarde
{
    public abstract void EcrireAtomiquement(string chemin, byte[] donnees, bool creerCopie);

    public virtual byte[] LireTout(string chemin) => File.ReadAllBytes(chemin);

    public virtual bool Existe(string chemin) => File.Exists(chemin);

    public virtual void Supprimer(string chemin)
    {
        if (File.Exists(chemin))
            File.Delete(chemin);
    }

    public virtual FileInfo ObtenirInformations(string chemin) => new(chemin);
}

/// <summary>
/// Stockage disque avec remplacement atomique et copie .bak optionnelle.
/// </summary>
public sealed class StockageFichierSauvegarde : StockageSauvegardeBase
{
    public override void EcrireAtomiquement(string chemin, byte[] donnees, bool creerCopie)
    {
        string? dossier = Path.GetDirectoryName(chemin);
        if (!string.IsNullOrWhiteSpace(dossier))
            Directory.CreateDirectory(dossier);

        string cheminTemporaire = $"{chemin}.tmp";
        string cheminCopie = $"{chemin}.bak";

        File.WriteAllBytes(cheminTemporaire, donnees);

        if (File.Exists(chemin))
        {
            string? copie = creerCopie ? cheminCopie : null;

            if (creerCopie && File.Exists(cheminCopie))
                File.Delete(cheminCopie);

            File.Replace(cheminTemporaire, chemin, copie);
            return;
        }

        File.Move(cheminTemporaire, chemin);
    }
}
