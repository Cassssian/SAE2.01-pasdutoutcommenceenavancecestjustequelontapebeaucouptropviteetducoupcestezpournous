using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IUTGame.WPF;
using VraiPseudoSae.Utils.Sprite;

namespace VraiPseudoSae.view.gameintro;

public static class GameIntroSpriteSheetFactory
{
    private const int CharacterFrameWidth = 16;
    private const int CharacterFrameHeight = 24;
    private const int CharacterFrameOffsetX = 4;
    private const int CharacterFrameStrideX = 24;
    private const int CharacterExpressionFrameSize = 16;

    public static GameIntroPlayerSpriteSet Register(WPFScreen screen)
    {
        GameIntroPlayerSpriteSet spriteSet = new();

        RegisterAnimation(screen, spriteSet, GameIntroCharacterAnimation.Idle, "idle", "Assets/perso hub/16x16 Idle-Sheet.png", 4);
        RegisterAnimation(screen, spriteSet, GameIntroCharacterAnimation.Walk, "walk", "Assets/perso hub/16x16 Walk-Sheet.png", 4);
        RegisterAnimation(screen, spriteSet, GameIntroCharacterAnimation.Run, "run", "Assets/perso hub/16x16 Run-Sheet.png", 6);
        RegisterAnimation(screen, spriteSet, GameIntroCharacterAnimation.Attack, "attack", "Assets/perso hub/16x16 Attack-Sheet.png", 6);
        RegisterAnimation(screen, spriteSet, GameIntroCharacterAnimation.Interact, "interact", "Assets/perso hub/16x16 Interact-Sheet.png", 4);

        return spriteSet;
    }

    public static ImageSource CreateExpressionPortrait(int column, int row)
    {
        BitmapImage expressionSheet = LoadSheet("Assets/perso hub/expression.png");
        return new CroppedBitmap(
            expressionSheet,
            new Int32Rect(
                column * CharacterExpressionFrameSize,
                row * CharacterExpressionFrameSize,
                CharacterExpressionFrameSize,
                CharacterExpressionFrameSize));
    }

    private static void RegisterAnimation(
        WPFScreen screen,
        GameIntroPlayerSpriteSet spriteSet,
        GameIntroCharacterAnimation animation,
        string animationName,
        string resourcePath,
        int frameCount)
    {
        BitmapImage sheet = LoadSheet(resourcePath);

        foreach (GameIntroCharacterDirection direction in Enum.GetValues<GameIntroCharacterDirection>())
        {
            RegisterDirectionFrames(screen, spriteSet, sheet, animation, animationName, direction, mirrored: false, frameCount);
            RegisterDirectionFrames(screen, spriteSet, sheet, animation, animationName, direction, mirrored: true, frameCount);
        }
    }

    private static void RegisterDirectionFrames(
        WPFScreen screen,
        GameIntroPlayerSpriteSet spriteSet,
        BitmapSource sheet,
        GameIntroCharacterAnimation animation,
        string animationName,
        GameIntroCharacterDirection direction,
        bool mirrored,
        int frameCount)
    {
        string mirrorName = mirrored ? "left" : "right";
        IReadOnlyList<string> frames = Enumerable.Range(0, frameCount)
            .Select(frame =>
            {
                string spriteName = $"intro_player_{animationName}_{direction.ToString().ToLowerInvariant()}_{mirrorName}_{frame}.png";
                SpriteInjector.PreRegister(screen, spriteName, RenderFrame(sheet, GetCharacterFrameRect(frame, direction), mirrored));
                return spriteName;
            })
            .ToArray();

        spriteSet.AddFrames(animation, direction, mirrored, frames);
    }

    private static Int32Rect GetCharacterFrameRect(int frame, GameIntroCharacterDirection direction)
    {
        return new Int32Rect(
            CharacterFrameOffsetX + frame * CharacterFrameStrideX,
            (int)direction * CharacterFrameHeight,
            CharacterFrameWidth,
            CharacterFrameHeight);
    }

    private static BitmapImage LoadSheet(string resourcePath)
    {
        BitmapImage image = new();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri("pack://application:,,,/" + resourcePath.Replace(" ", "%20"), UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static BitmapImage RenderFrame(BitmapSource sheet, Int32Rect cell, bool mirrored)
    {
        CroppedBitmap cropped = new(sheet, cell);
        DrawingVisual visual = new();

        using (DrawingContext context = visual.RenderOpen())
        {
            if (mirrored)
                context.PushTransform(new ScaleTransform(-1, 1, CharacterFrameWidth / 2.0, CharacterFrameHeight / 2.0));

            context.DrawImage(cropped, new Rect(0, 0, CharacterFrameWidth, CharacterFrameHeight));

            if (mirrored)
                context.Pop();
        }

        RenderTargetBitmap target = new(
            CharacterFrameWidth,
            CharacterFrameHeight,
            96,
            96,
            PixelFormats.Pbgra32);
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
