using System.IO;
using System.IO.Compression;

namespace VraiPseudoSae.Utils.SaveManager;

/// <summary>
/// Base commune des algorithmes de compression de sauvegarde.
/// </summary>
public abstract class AlgorithmeCompressionSauvegardeBase : IAlgorithmeCompressionSauvegarde
{
    public abstract ModeCompressionSauvegarde Mode { get; }

    public abstract byte[] Compresser(byte[] donnees);

    public abstract byte[] Decompresser(byte[] donnees);

    protected static byte[] CompresserAvecFlux(byte[] donnees, Func<Stream, Stream> creerFluxCompression)
    {
        using MemoryStream sortie = new();
        using (Stream fluxCompression = creerFluxCompression(sortie))
        {
            fluxCompression.Write(donnees, 0, donnees.Length);
        }

        return sortie.ToArray();
    }

    protected static byte[] DecompresserAvecFlux(byte[] donnees, Func<Stream, Stream> creerFluxDecompression)
    {
        using MemoryStream entree = new(donnees);
        using Stream fluxCompression = creerFluxDecompression(entree);
        using MemoryStream sortie = new();
        fluxCompression.CopyTo(sortie);
        return sortie.ToArray();
    }
}

public sealed class AlgorithmeSansCompressionSauvegarde : AlgorithmeCompressionSauvegardeBase
{
    public override ModeCompressionSauvegarde Mode => ModeCompressionSauvegarde.Aucune;

    public override byte[] Compresser(byte[] donnees) => donnees.ToArray();

    public override byte[] Decompresser(byte[] donnees) => donnees.ToArray();
}

public sealed class AlgorithmeCompressionBrotliSauvegarde : AlgorithmeCompressionSauvegardeBase
{
    public override ModeCompressionSauvegarde Mode => ModeCompressionSauvegarde.Brotli;

    public override byte[] Compresser(byte[] donnees)
    {
        return CompresserAvecFlux(donnees, sortie => new BrotliStream(sortie, CompressionLevel.Optimal, leaveOpen: true));
    }

    public override byte[] Decompresser(byte[] donnees)
    {
        return DecompresserAvecFlux(donnees, entree => new BrotliStream(entree, CompressionMode.Decompress, leaveOpen: false));
    }
}

public sealed class AlgorithmeCompressionGZipSauvegarde : AlgorithmeCompressionSauvegardeBase
{
    public override ModeCompressionSauvegarde Mode => ModeCompressionSauvegarde.GZip;

    public override byte[] Compresser(byte[] donnees)
    {
        return CompresserAvecFlux(donnees, sortie => new GZipStream(sortie, CompressionLevel.Optimal, leaveOpen: true));
    }

    public override byte[] Decompresser(byte[] donnees)
    {
        return DecompresserAvecFlux(donnees, entree => new GZipStream(entree, CompressionMode.Decompress, leaveOpen: false));
    }
}

/// <summary>
/// Calcul CRC32 standard utilisé pour détecter les fichiers modifiés ou tronqués.
/// </summary>
public sealed class ControleIntegriteCrc32Sauvegarde : IControleIntegriteSauvegarde
{
    private static readonly uint[] Table = CreerTable();

    public ModeControleSauvegarde Mode => ModeControleSauvegarde.Crc32;

    public uint Calculer(ReadOnlySpan<byte> donnees)
    {
        uint crc = 0xFFFFFFFFu;

        foreach (byte valeur in donnees)
            crc = (crc >> 8) ^ Table[(crc ^ valeur) & 0xFF];

        return ~crc;
    }

    public bool Verifier(ReadOnlySpan<byte> donnees, uint sommeAttendue)
    {
        return Calculer(donnees) == sommeAttendue;
    }

    private static uint[] CreerTable()
    {
        uint[] table = new uint[256];

        for (uint i = 0; i < table.Length; i++)
        {
            uint crc = i;

            for (int bit = 0; bit < 8; bit++)
                crc = (crc & 1) == 1 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;

            table[i] = crc;
        }

        return table;
    }
}
