using System.IO;
using System.Text;

namespace VraiPseudoSae.Utils.GestionnaireSauvegarde;

/// <summary>
/// Écrit les primitives de jeu dans un flux binaire compact.
/// </summary>
public sealed class EcrivainDonneesSauvegarde : IDisposable
{
    private readonly BinaryWriter _ecrivain;

    public EcrivainDonneesSauvegarde(Stream flux)
    {
        _ecrivain = new BinaryWriter(flux, Encoding.UTF8, leaveOpen: true);
    }

    public long Position => _ecrivain.BaseStream.Position;

    public void EcrireBooleen(bool valeur) => _ecrivain.Write(valeur);

    public void EcrireOctet(byte valeur) => _ecrivain.Write(valeur);

    public void EcrireEntier16(short valeur) => _ecrivain.Write(valeur);

    public void EcrireEntierNonSigne16(ushort valeur) => _ecrivain.Write(valeur);

    public void EcrireEntier32(int valeur) => _ecrivain.Write(valeur);

    public void EcrireEntierNonSigne32(uint valeur) => _ecrivain.Write(valeur);

    public void EcrireEntier64(long valeur) => _ecrivain.Write(valeur);

    public void EcrireEntierNonSigne64(ulong valeur) => _ecrivain.Write(valeur);

    public void EcrireFlottant32(float valeur) => _ecrivain.Write(valeur);

    public void EcrireFlottant64(double valeur) => _ecrivain.Write(valeur);

    public void EcrireGuid(Guid valeur) => _ecrivain.Write(valeur.ToByteArray());

    public void EcrireEnum<TEnum>(TEnum valeur) where TEnum : struct, Enum
    {
        EcrireEntierCompact(Convert.ToInt32(valeur));
    }

    public void EcrireChaine(string valeur)
    {
        byte[] octets = Encoding.UTF8.GetBytes(valeur);
        EcrireNombre(octets.Length);
        _ecrivain.Write(octets);
    }

    public void EcrireChaineOptionnelle(string? valeur)
    {
        EcrireBooleen(valeur is not null);

        if (valeur is not null)
            EcrireChaine(valeur);
    }

    public void EcrireOctets(byte[] valeur)
    {
        EcrireNombre(valeur.Length);
        _ecrivain.Write(valeur);
    }

    public void EcrireOctetsBruts(byte[] valeur)
    {
        _ecrivain.Write(valeur);
    }

    public void EcrireNombre(int valeur)
    {
        if (valeur < 0)
            throw new ArgumentOutOfRangeException(nameof(valeur), "Un nombre ne peut pas être négatif.");

        EcrireEntier7Bits((uint)valeur);
    }

    public void EcrireEntierCompact(int valeur)
    {
        uint encode = unchecked((uint)((valeur << 1) ^ (valeur >> 31)));
        EcrireEntier7Bits(encode);
    }

    public void EcrireCollection<T>(IEnumerable<T> valeurs, Action<EcrivainDonneesSauvegarde, T> ecrireElement)
    {
        if (valeurs is ICollection<T> collection)
        {
            EcrireNombre(collection.Count);

            foreach (T valeur in collection)
                ecrireElement(this, valeur);

            return;
        }

        List<T> materialise = valeurs.ToList();
        EcrireNombre(materialise.Count);

        foreach (T valeur in materialise)
            ecrireElement(this, valeur);
    }

    public void Dispose()
    {
        _ecrivain.Dispose();
    }

    private void EcrireEntier7Bits(uint valeur)
    {
        while (valeur >= 0x80)
        {
            _ecrivain.Write((byte)(valeur | 0x80));
            valeur >>= 7;
        }

        _ecrivain.Write((byte)valeur);
    }
}

/// <summary>
/// Lit les primitives de jeu depuis un flux binaire compact.
/// </summary>
public sealed class LecteurDonneesSauvegarde : IDisposable
{
    private readonly BinaryReader _lecteur;
    private readonly int _tailleMaxChaineUtf8;

    public LecteurDonneesSauvegarde(Stream flux, int tailleMaxChaineUtf8 = 1024 * 1024)
    {
        _lecteur = new BinaryReader(flux, Encoding.UTF8, leaveOpen: true);
        _tailleMaxChaineUtf8 = tailleMaxChaineUtf8;
    }

    public long Position => _lecteur.BaseStream.Position;

    public long Longueur => _lecteur.BaseStream.Length;

    public bool LireBooleen() => _lecteur.ReadBoolean();

    public byte LireOctet() => _lecteur.ReadByte();

    public short LireEntier16() => _lecteur.ReadInt16();

    public ushort LireEntierNonSigne16() => _lecteur.ReadUInt16();

    public int LireEntier32() => _lecteur.ReadInt32();

    public uint LireEntierNonSigne32() => _lecteur.ReadUInt32();

    public long LireEntier64() => _lecteur.ReadInt64();

    public ulong LireEntierNonSigne64() => _lecteur.ReadUInt64();

    public float LireFlottant32() => _lecteur.ReadSingle();

    public double LireFlottant64() => _lecteur.ReadDouble();

    public Guid LireGuid() => new(LireOctetsBruts(16));

    public TEnum LireEnum<TEnum>() where TEnum : struct, Enum
    {
        return (TEnum)Enum.ToObject(typeof(TEnum), LireEntierCompact());
    }

    public string LireChaine()
    {
        int longueur = LireNombre();

        if (longueur > _tailleMaxChaineUtf8)
        {
            throw new ExceptionFormatSauvegarde(
                StatutOperationSauvegarde.FormatInvalide,
                $"Chaîne trop volumineuse: {longueur} octets.");
        }

        return Encoding.UTF8.GetString(LireOctetsBruts(longueur));
    }

    public string? LireChaineOptionnelle()
    {
        return LireBooleen() ? LireChaine() : null;
    }

    public byte[] LireOctets()
    {
        return LireOctetsBruts(LireNombre());
    }

    public byte[] LireOctetsBruts(int nombre)
    {
        byte[] octets = _lecteur.ReadBytes(nombre);

        if (octets.Length != nombre)
            throw new ExceptionFormatSauvegarde(StatutOperationSauvegarde.FormatInvalide, "Fin de sauvegarde inattendue.");

        return octets;
    }

    public int LireNombre()
    {
        uint valeur = LireEntier7Bits();

        if (valeur > int.MaxValue)
            throw new ExceptionFormatSauvegarde(StatutOperationSauvegarde.FormatInvalide, "Nombre encodé trop grand.");

        return (int)valeur;
    }

    public int LireEntierCompact()
    {
        uint valeur = LireEntier7Bits();
        return unchecked((int)(valeur >> 1) ^ -((int)valeur & 1));
    }

    public IReadOnlyList<T> LireListe<T>(Func<LecteurDonneesSauvegarde, T> lireElement, int nombreMax = 1_000_000)
    {
        int nombre = LireNombre();

        if (nombre > nombreMax)
            throw new ExceptionFormatSauvegarde(StatutOperationSauvegarde.FormatInvalide, $"Collection trop grande: {nombre} éléments.");

        List<T> elements = new(nombre);

        for (int i = 0; i < nombre; i++)
            elements.Add(lireElement(this));

        return elements;
    }

    public void VerifierFinDonnees()
    {
        if (Position != Longueur)
            throw new ExceptionFormatSauvegarde(StatutOperationSauvegarde.FormatInvalide, "La sauvegarde contient des octets en trop.");
    }

    public void Dispose()
    {
        _lecteur.Dispose();
    }

    private uint LireEntier7Bits()
    {
        uint resultat = 0;
        int decalage = 0;

        while (decalage < 35)
        {
            byte courant = _lecteur.ReadByte();
            resultat |= (uint)(courant & 0x7F) << decalage;

            if ((courant & 0x80) == 0)
                return resultat;

            decalage += 7;
        }

        throw new ExceptionFormatSauvegarde(StatutOperationSauvegarde.FormatInvalide, "Entier compact invalide.");
    }
}
