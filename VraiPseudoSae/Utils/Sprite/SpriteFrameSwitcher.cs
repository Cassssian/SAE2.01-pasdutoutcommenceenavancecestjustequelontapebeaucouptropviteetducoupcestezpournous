using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using IUTGame;
using IUTGame.WPF;

namespace VraiPseudoSae.Utils.Sprite;

/// <summary>
/// Swaps the bitmap source of an already loaded IUTGame sprite without allocating a new sprite id.
/// </summary>
public static class SpriteFrameSwitcher
{
    private static readonly FieldInfo GameField =
        typeof(GameItem).GetField("game", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("[SpriteFrameSwitcher] Champ GameItem.game introuvable.");

    private static readonly FieldInfo SpriteIdField =
        typeof(GameItem).GetField("spriteID", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("[SpriteFrameSwitcher] Champ GameItem.spriteID introuvable.");

    private static readonly FieldInfo SpritesField =
        typeof(WPFScreen).GetField("sprites", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("[SpriteFrameSwitcher] Champ WPFScreen.sprites introuvable.");

    private static readonly FieldInfo SpriteStoreField =
        typeof(WPFScreen).GetField("spriteStore", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("[SpriteFrameSwitcher] Champ WPFScreen.spriteStore introuvable.");

    private static readonly FieldInfo BitmapsField =
        typeof(SpriteStore).GetField("bitmaps", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("[SpriteFrameSwitcher] Champ SpriteStore.bitmaps introuvable.");

    public static void SwitchFrame(GameItem item, string spriteName)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            Game game = (Game)GameField.GetValue(item)!;
            if (game.Screen is not WPFScreen screen)
                return;

            int spriteId = (int)SpriteIdField.GetValue(item)!;
            if (spriteId < 0)
                return;

            var sprites = (List<IUTGame.WPF.Sprite>)SpritesField.GetValue(screen)!;
            if (spriteId >= sprites.Count)
                return;

            SpriteStore spriteStore = (SpriteStore)SpriteStoreField.GetValue(screen)!;
            var bitmaps = (Dictionary<string, BitmapImage>)BitmapsField.GetValue(spriteStore)!;
            if (!bitmaps.TryGetValue(spriteName, out BitmapImage? bitmap))
                throw new InvalidOperationException($"[SpriteFrameSwitcher] Sprite '{spriteName}' non pre-enregistre.");

            sprites[spriteId].Image.Source = bitmap;
            sprites[spriteId].Image.InvalidateMeasure();
            sprites[spriteId].Image.InvalidateArrange();
        });
    }
}
