using System.IO;
using System.Text;

namespace VraiPseudoSae.Utils.SaveManager;

/// <summary>
/// Encodeur binaire optimisé pour le jeu: en-tête court, bloc compressé et CRC32.
/// </summary>
public sealed class EncodeurBinaireSauvegarde : IEncodeurSauvegarde
{
    private static readonly byte[] Signature = Encoding.ASCII.GetBytes("VPSAVE");
    private const byte VersionFormat = 1;

    private readonly Dictionary<ModeCompressionSauvegarde, IAlgorithmeCompressionSauvegarde> _algorithmesCompression;
    private readonly IControleIntegriteSauvegarde _controleIntegrite;

    public EncodeurBinaireSauvegarde(
        IEnumerable<IAlgorithmeCompressionSauvegarde>? algorithmesCompression = null,
        IControleIntegriteSauvegarde? controleIntegrite = null)
    {
        IEnumerable<IAlgorithmeCompressionSauvegarde> algorithmes = algorithmesCompression ??
            new IAlgorithmeCompressionSauvegarde[]
            {
                new AlgorithmeSansCompressionSauvegarde(),
                new AlgorithmeCompressionBrotliSauvegarde(),
                new AlgorithmeCompressionGZipSauvegarde()
            };

        _algorithmesCompression = algorithmes.ToDictionary(algorithme => algorithme.Mode);
        _controleIntegrite = controleIntegrite ?? new ControleIntegriteCrc32Sauvegarde();
    }

    public byte[] Encoder(string cleType, int versionSerialiseur, byte[] donnees, OptionsGestionnaireSauvegarde options)
    {
        if (string.IsNullOrWhiteSpace(cleType))
            throw new ArgumentException("La clé de type de sauvegarde est obligatoire.", nameof(cleType));

        (ModeCompressionSauvegarde compression, byte[] donneesStockees) = ChoisirCompression(donnees, options.Compression);
        ModeControleSauvegarde controle = options.Controle;
        uint sommeControle = controle == ModeControleSauvegarde.Crc32 ? _controleIntegrite.Calculer(donneesStockees) : 0;
        byte[] cleTypeOctets = Encoding.UTF8.GetBytes(cleType);

        using MemoryStream flux = new();
        using BinaryWriter ecrivain = new(flux, Encoding.UTF8, leaveOpen: true);
        ecrivain.Write(Signature);
        ecrivain.Write(VersionFormat);
        ecrivain.Write((byte)compression);
        ecrivain.Write((byte)controle);
        ecrivain.Write(versionSerialiseur);
        ecrivain.Write(DateTimeOffset.UtcNow.UtcDateTime.Ticks);
        ecrivain.Write(donnees.Length);
        ecrivain.Write(donneesStockees.Length);
        ecrivain.Write(sommeControle);
        ecrivain.Write(cleTypeOctets.Length);
        ecrivain.Write(cleTypeOctets);
        ecrivain.Write(donneesStockees);
        ecrivain.Flush();

        return flux.ToArray();
    }

    public DonneesSauvegardeDecodees Decoder(byte[] donnees)
    {
        EnteteSauvegarde entete = LireEntete(donnees);

        if (entete.Metadonnees.Controle == ModeControleSauvegarde.Crc32 &&
            !_controleIntegrite.Verifier(entete.DonneesStockees, entete.Metadonnees.SommeControle))
        {
            throw new ExceptionFormatSauvegarde(StatutOperationSauvegarde.SommeControleInvalide, "La somme de contrôle ne correspond pas.");
        }

        byte[] donneesDecodees = Decompresser(entete.Metadonnees.Compression, entete.DonneesStockees);

        if (donneesDecodees.Length != entete.Metadonnees.TailleDonnees)
        {
            throw new ExceptionFormatSauvegarde(
                StatutOperationSauvegarde.FormatInvalide,
                "La taille des données décompressées ne correspond pas à l'en-tête.");
        }

        return new DonneesSauvegardeDecodees(entete.Metadonnees, donneesDecodees);
    }

    public MetadonneesSauvegarde LireMetadonnees(byte[] donnees)
    {
        return LireEntete(donnees, lireDonneesStockees: false).Metadonnees;
    }

    private (ModeCompressionSauvegarde Compression, byte[] Donnees) ChoisirCompression(byte[] donnees, ModeCompressionSauvegarde modeDemande)
    {
        if (modeDemande == ModeCompressionSauvegarde.Aucune)
            return (ModeCompressionSauvegarde.Aucune, donnees);

        if (modeDemande != ModeCompressionSauvegarde.Automatique)
            return (modeDemande, ObtenirAlgorithme(modeDemande).Compresser(donnees));

        byte[] meilleuresDonnees = donnees;
        ModeCompressionSauvegarde meilleurMode = ModeCompressionSauvegarde.Aucune;

        foreach (IAlgorithmeCompressionSauvegarde algorithme in _algorithmesCompression.Values)
        {
            if (algorithme.Mode == ModeCompressionSauvegarde.Aucune)
                continue;

            byte[] compresse = algorithme.Compresser(donnees);

            if (compresse.Length < meilleuresDonnees.Length)
            {
                meilleuresDonnees = compresse;
                meilleurMode = algorithme.Mode;
            }
        }

        return (meilleurMode, meilleuresDonnees);
    }

    private byte[] Decompresser(ModeCompressionSauvegarde mode, byte[] donnees)
    {
        return ObtenirAlgorithme(mode).Decompresser(donnees);
    }

    private IAlgorithmeCompressionSauvegarde ObtenirAlgorithme(ModeCompressionSauvegarde mode)
    {
        if (_algorithmesCompression.TryGetValue(mode, out IAlgorithmeCompressionSauvegarde? algorithme))
            return algorithme;

        throw new ExceptionFormatSauvegarde(StatutOperationSauvegarde.FormatInvalide, $"Mode de compression non supporté: {mode}.");
    }

    private static EnteteSauvegarde LireEntete(byte[] donnees, bool lireDonneesStockees = true)
    {
        try
        {
            using MemoryStream flux = new(donnees);
            using BinaryReader lecteur = new(flux, Encoding.UTF8, leaveOpen: true);

            byte[] signature = lecteur.ReadBytes(Signature.Length);
            if (!signature.SequenceEqual(Signature))
                throw new ExceptionFormatSauvegarde(StatutOperationSauvegarde.FormatInvalide, "Signature de sauvegarde invalide.");

            byte versionFormat = lecteur.ReadByte();
            if (versionFormat != VersionFormat)
                throw new ExceptionFormatSauvegarde(StatutOperationSauvegarde.VersionNonSupportee, $"Format de sauvegarde non supporté: {versionFormat}.");

            ModeCompressionSauvegarde compression = (ModeCompressionSauvegarde)lecteur.ReadByte();
            ModeControleSauvegarde controle = (ModeControleSauvegarde)lecteur.ReadByte();
            int versionSerialiseur = lecteur.ReadInt32();
            long ticksCreation = lecteur.ReadInt64();
            int tailleDonnees = lecteur.ReadInt32();
            int tailleDonneesStockees = lecteur.ReadInt32();
            uint sommeControle = lecteur.ReadUInt32();
            int tailleCleType = lecteur.ReadInt32();

            if (tailleDonnees < 0 || tailleDonneesStockees < 0 || tailleCleType <= 0 || tailleCleType > 512)
                throw new ExceptionFormatSauvegarde(StatutOperationSauvegarde.FormatInvalide, "Longueur invalide dans l'en-tête de sauvegarde.");

            if (controle is not ModeControleSauvegarde.Aucun and not ModeControleSauvegarde.Crc32)
                throw new ExceptionFormatSauvegarde(StatutOperationSauvegarde.FormatInvalide, $"Mode de contrôle non supporté: {controle}.");

            byte[] cleTypeOctets = lecteur.ReadBytes(tailleCleType);
            if (cleTypeOctets.Length != tailleCleType)
                throw new ExceptionFormatSauvegarde(StatutOperationSauvegarde.FormatInvalide, "Fin inattendue de l'en-tête de sauvegarde.");

            if (lireDonneesStockees && flux.Length - flux.Position != tailleDonneesStockees)
                throw new ExceptionFormatSauvegarde(StatutOperationSauvegarde.FormatInvalide, "Longueur invalide des données de sauvegarde.");

            byte[] donneesStockees = lireDonneesStockees ? lecteur.ReadBytes(tailleDonneesStockees) : Array.Empty<byte>();
            if (lireDonneesStockees && donneesStockees.Length != tailleDonneesStockees)
                throw new ExceptionFormatSauvegarde(StatutOperationSauvegarde.FormatInvalide, "Fin inattendue des données de sauvegarde.");

            MetadonneesSauvegarde metadonnees = new(
                Encoding.UTF8.GetString(cleTypeOctets),
                versionSerialiseur,
                new DateTimeOffset(ticksCreation, TimeSpan.Zero),
                compression,
                controle,
                tailleDonnees,
                tailleDonneesStockees,
                sommeControle);

            return new EnteteSauvegarde(metadonnees, donneesStockees);
        }
        catch (ExceptionFormatSauvegarde)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or EndOfStreamException or ArgumentOutOfRangeException)
        {
            throw new ExceptionFormatSauvegarde(StatutOperationSauvegarde.FormatInvalide, "La sauvegarde est tronquée ou invalide.", ex);
        }
    }

    private sealed record EnteteSauvegarde(MetadonneesSauvegarde Metadonnees, byte[] DonneesStockees);
}
