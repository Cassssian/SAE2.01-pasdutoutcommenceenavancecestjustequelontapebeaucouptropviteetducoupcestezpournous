using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IUTGame.WPF;
using VraiPseudoSae.Utils.Sprite;

namespace VraiPseudoSae.view.WizardSurvival.Rendering;

/// <summary>
/// Extracts the wizard animation frames from data/Wizard/AnimationSheet_Character.png.
/// </summary>
public static class WizardSpriteSheetFactory
{
    private const int FrameSize = 32;
    private const int Columns = 8;

    public static WizardPlayerSpriteSet Register(WPFScreen screen)
    {
        BitmapImage sheet = LoadSheet();

        IReadOnlyList<Int32Rect> idleCells = GetNonEmptyCells(sheet, 0)
            .Concat(GetNonEmptyCells(sheet, 1))
            .ToArray();
        IReadOnlyList<Int32Rect> walkCells = GetNonEmptyCells(sheet, 3).ToArray();
        IReadOnlyList<Int32Rect> deathCellsA = GetNonEmptyCells(sheet, 6).ToArray();
        IReadOnlyList<Int32Rect> deathCellsB = GetNonEmptyCells(sheet, 7).ToArray();

        IReadOnlyList<string> idleRight = BuildPingPong(RegisterFrames(screen, sheet, idleCells, "wizard_player_idle_right", mirrored: false));
        IReadOnlyList<string> idleLeft = BuildPingPong(RegisterFrames(screen, sheet, idleCells, "wizard_player_idle_left", mirrored: true));
        IReadOnlyList<string> walkRight = RegisterFrames(screen, sheet, walkCells, "wizard_player_walk_right", mirrored: false);
        IReadOnlyList<string> walkLeft = RegisterFrames(screen, sheet, walkCells, "wizard_player_walk_left", mirrored: true);
        IReadOnlyList<string> deathRightA = RegisterFrames(screen, sheet, deathCellsA, "wizard_player_death_a_right", mirrored: false);
        IReadOnlyList<string> deathLeftA = RegisterFrames(screen, sheet, deathCellsA, "wizard_player_death_a_left", mirrored: true);
        IReadOnlyList<string> deathRightB = RegisterFrames(screen, sheet, deathCellsB, "wizard_player_death_b_right", mirrored: false);
        IReadOnlyList<string> deathLeftB = RegisterFrames(screen, sheet, deathCellsB, "wizard_player_death_b_left", mirrored: true);

        return new WizardPlayerSpriteSet(
            idleRight,
            idleLeft,
            walkRight,
            walkLeft,
            deathRightA,
            deathLeftA,
            deathRightB,
            deathLeftB);
    }

    private static BitmapImage LoadSheet()
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri("pack://application:,,,/data/Wizard/AnimationSheet_Character.png", UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static IEnumerable<Int32Rect> GetNonEmptyCells(BitmapSource sheet, int row)
    {
        for (int column = 0; column < Columns; column++)
        {
            var cell = new Int32Rect(column * FrameSize, row * FrameSize, FrameSize, FrameSize);
            if (HasVisiblePixel(sheet, cell))
                yield return cell;
        }
    }

    private static bool HasVisiblePixel(BitmapSource sheet, Int32Rect cell)
    {
        CroppedBitmap cropped = new(sheet, cell);
        FormatConvertedBitmap converted = new(cropped, PixelFormats.Bgra32, null, 0);
        int stride = FrameSize * 4;
        byte[] pixels = new byte[stride * FrameSize];
        converted.CopyPixels(pixels, stride, 0);

        for (int i = 3; i < pixels.Length; i += 4)
        {
            if (pixels[i] > 0)
                return true;
        }

        return false;
    }

    private static IReadOnlyList<string> RegisterFrames(
        WPFScreen screen,
        BitmapSource sheet,
        IReadOnlyList<Int32Rect> cells,
        string prefix,
        bool mirrored)
    {
        var names = new List<string>();
        for (int i = 0; i < cells.Count; i++)
        {
            string name = $"{prefix}_{i}.png";
            SpriteInjector.PreRegister(screen, name, RenderFrame(sheet, cells[i], mirrored));
            names.Add(name);
        }

        if (names.Count == 0)
            throw new InvalidDataException($"Aucune frame detectee pour '{prefix}'.");

        return names;
    }

    private static IReadOnlyList<string> BuildPingPong(IReadOnlyList<string> forward)
    {
        if (forward.Count <= 2)
            return forward;

        return forward
            .Concat(forward.Skip(1).Take(forward.Count - 2).Reverse())
            .ToArray();
    }

    private static BitmapImage RenderFrame(BitmapSource sheet, Int32Rect cell, bool mirrored)
    {
        CroppedBitmap cropped = new(sheet, cell);
        DrawingVisual visual = new();

        using (DrawingContext context = visual.RenderOpen())
        {
            if (mirrored)
                context.PushTransform(new ScaleTransform(-1, 1, FrameSize / 2.0, FrameSize / 2.0));

            context.DrawImage(cropped, new Rect(0, 0, FrameSize, FrameSize));

            if (mirrored)
                context.Pop();
        }

        RenderTargetBitmap target = new(FrameSize, FrameSize, 96, 96, PixelFormats.Pbgra32);
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
