using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VraiPseudoSae.view.WizardSurvival.Rendering;

/// <summary>
/// Builds small pixel-art sprites directly in memory for IUTGame.
/// </summary>
public static class PixelSpriteFactory
{
    private const int Pixel = 2;

    private static readonly Dictionary<char, Color> WizardPalette = new()
    {
        ['K'] = Color.FromRgb(31, 52, 67),
        ['P'] = Color.FromRgb(103, 55, 134),
        ['p'] = Color.FromRgb(73, 44, 104),
        ['C'] = Color.FromRgb(151, 217, 215),
        ['c'] = Color.FromRgb(105, 173, 180),
        ['M'] = Color.FromRgb(225, 31, 95),
        ['Y'] = Color.FromRgb(226, 172, 69),
        ['W'] = Color.FromRgb(229, 236, 190),
        ['S'] = Color.FromRgb(132, 116, 104)
    };

    private static readonly Dictionary<char, Color> ZombiePalette = new()
    {
        ['K'] = Color.FromRgb(29, 48, 50),
        ['G'] = Color.FromRgb(64, 135, 86),
        ['g'] = Color.FromRgb(94, 174, 110),
        ['M'] = Color.FromRgb(210, 27, 83),
        ['C'] = Color.FromRgb(137, 199, 184),
        ['P'] = Color.FromRgb(116, 64, 144),
        ['Y'] = Color.FromRgb(220, 178, 65)
    };

    private static readonly Dictionary<char, Color> FirePalette = new()
    {
        ['R'] = Color.FromRgb(198, 41, 43),
        ['O'] = Color.FromRgb(237, 103, 45),
        ['Y'] = Color.FromRgb(248, 205, 70),
        ['W'] = Color.FromRgb(255, 238, 162)
    };

    public static BitmapImage WizardFrame(int frame, bool mirrored = false)
    {
        string[] frames =
        {
            """
            ....................
            ......KKKKK.........
            .....KPPPPPK........
            ....KPPPPPPPK.......
            ....KPPKPKPPK.......
            ...KPPPKKPPPK.......
            ...KCCCCCCCCK.......
            ..KCCCMCCCKKKK.....
            ..KCCCMCCCKYYYYK...
            ..KCCCCCCCCKYYK....
            ...KCCCCCCK........
            ....KCCCCK.........
            ...KCCCCCCK........
            ..KCCCCCCCCK.......
            ..KCCKCCKCCK.......
            .KCCKKCCKKCCK......
            .KCCKCCCCKCCK......
            ..KKCCCCCCKK.......
            ...KCCKKCCK........
            ...KCC..CCK........
            ..KCC....CCK.......
            ..KK......KK.......
            ....................
            ....................
            """,
            """
            ....................
            ......KKKKK.........
            .....KPPPPPK........
            ....KPPPPPPPK.......
            ....KPPKPKPPK.......
            ...KPPPKKPPPK.......
            ...KCCCCCCCCK.......
            ..KCCCMCCCKKKK.....
            ..KCCCMCCCKYYYYK...
            ..KCCCCCCCCKYYK....
            ...KCCCCCCK........
            ....KCCCCK.........
            ...KCCCCCCK........
            ..KCCCCCCCCK.......
            ..KCCKCCKCCK.......
            .KCCKKCCKKCCK......
            .KCCKCCCCKCCK......
            ..KKCCCCCCKK.......
            ....KCCKCCK........
            ....KCCKCCK........
            ...KCC...CCK.......
            ...KK.....KK.......
            ....................
            ....................
            """,
            """
            ....................
            ......KKKKK.........
            .....KPPPPPK........
            ....KPPPPPPPK.......
            ....KPPKPKPPK.......
            ...KPPPKKPPPK.......
            ...KCCCCCCCCK.......
            ..KCCCMCCCKKKK.....
            ..KCCCMCCCKYYYYK...
            ..KCCCCCCCCKYYK....
            ...KCCCCCCK........
            ....KCCCCK.........
            ...KCCCCCCK........
            ..KCCCCCCCCK.......
            ..KCCKCCKCCK.......
            .KCCKKCCKKCCK......
            .KCCKCCCCKCCK......
            ..KKCCCCCCKK.......
            ...KCCKKCCK........
            ...KCC..CCK........
            ....CCK.KCC........
            ....KK...KK........
            ....................
            ....................
            """
        };

        return Render(frames[frame % frames.Length], WizardPalette, mirrored);
    }

    public static BitmapImage Zombie(bool evolved, bool mirrored = false)
    {
        string[] normal =
        {
            "....................",
            "......KKKKKK........",
            ".....KGGGGGGK.......",
            "....KGGGGGGGGK......",
            "....KGMGGGMGK.......",
            "....KGGGGGGGK.......",
            ".....KGGGGGK........",
            "....KCCCCCCK........",
            "...KCCCCCCCCK.......",
            "...KCCKCCKCCK.......",
            "..KCCKCCCCKCCK......",
            "..KCCCCCCCCCCK......",
            "...KKCCCCCCKK.......",
            "....KCCKKCCK........",
            "...KCCK..KCCK.......",
            "...KK.....KK........",
            "....................",
            "...................."
        };

        string[] big =
        {
            "....................",
            ".....KYYK..KYYK.....",
            ".....KPPPPPPPPK.....",
            "....KPPPPPPPPPPK....",
            "....KPMMPPPPMMPK....",
            "....KPPPPPPPPPPK....",
            ".....KPPPPPPPPK.....",
            "....KCCCCCCCCK......",
            "...KCCCCCCCCCCK.....",
            "...KCCKCCCCKCCK.....",
            "..KCCKCCCCCCKCCK....",
            "..KCCCCCCCCCCCCK....",
            "...KKCCCCCCCCKK.....",
            "....KCCKKCCK.......",
            "...KCCK..KCCK......",
            "...KK.....KK.......",
            "....................",
            "...................."
        };

        return Render(evolved ? big : normal, ZombiePalette, mirrored);
    }

    public static BitmapImage Fireball(bool mirrored = false)
    {
        string[] fire =
        {
            "..............",
            "....RRR.......",
            "...ROOOR......",
            "..ROYYYOR.....",
            ".ROYYWYYOR....",
            ".ROYYYYYORR...",
            "..ROYYYORR....",
            "...ROOOR......",
            "....RRR.......",
            ".............."
        };
        return Render(fire, FirePalette, mirrored);
    }

    private static BitmapImage Render(string block, IReadOnlyDictionary<char, Color> palette, bool mirrored = false)
    {
        string[] rows = block.Split('\n')
            .Select(row => row.Trim())
            .Where(row => row.Length > 0)
            .ToArray();

        if (mirrored)
            rows = rows.Select(row => new string(row.Reverse().ToArray())).ToArray();

        return Render(rows, palette);
    }

    private static BitmapImage Render(IReadOnlyList<string> sourceRows, IReadOnlyDictionary<char, Color> palette, bool mirrored = false)
    {
        IReadOnlyList<string> rows = mirrored
            ? sourceRows.Select(row => new string(row.Reverse().ToArray())).ToArray()
            : sourceRows;

        int width = rows.Max(row => row.Length) * Pixel;
        int height = rows.Count * Pixel;
        DrawingVisual visual = new();

        using (DrawingContext context = visual.RenderOpen())
        {
            for (int y = 0; y < rows.Count; y++)
            {
                for (int x = 0; x < rows[y].Length; x++)
                {
                    char key = rows[y][x];
                    if (!palette.TryGetValue(key, out Color color))
                        continue;

                    context.DrawRectangle(
                        new SolidColorBrush(color),
                        null,
                        new Rect(x * Pixel, y * Pixel, Pixel, Pixel));
                }
            }
        }

        RenderTargetBitmap target = new(width, height, 96, 96, PixelFormats.Pbgra32);
        target.Render(visual);

        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(target));

        using MemoryStream stream = new();
        encoder.Save(stream);
        stream.Position = 0;

        BitmapImage image = new();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
