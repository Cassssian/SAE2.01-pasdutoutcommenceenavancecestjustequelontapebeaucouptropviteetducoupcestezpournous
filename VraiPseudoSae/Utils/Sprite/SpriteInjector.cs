using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Media.Imaging;
using IUTGame.WPF;

namespace VraiPseudoSae.Utils.Sprite
{
    /// <summary>
    /// Injecte des BitmapImage générés dynamiquement dans le SpriteStore de la DLL IUTGame,
    /// en contournant le chargement pack:// via réflexion.
    /// À appeler AVANT Game.Run().
    /// </summary>
    public static class SpriteInjector
    {
        /// <summary>
        /// Pré-enregistre un BitmapImage dans le SpriteStore sous le nom <paramref name="spriteName"/>.
        /// La DLL retrouvera ce bitmap dans son dictionnaire interne au moment du LoadSprite.
        /// </summary>
        /// <param name="screen">Le WPFScreen passé au Game.</param>
        /// <param name="spriteName">Le nom exact utilisé dans le constructeur du GameItem (ex: "player_hub.png").</param>
        /// <param name="bitmap">Le BitmapImage généré dynamiquement (doit être Frozen).</param>
        public static void PreRegister(WPFScreen screen, string spriteName, BitmapImage bitmap)
        {
            // Récupère le SpriteStore privé du WPFScreen
            FieldInfo? spriteStoreField = typeof(WPFScreen).GetField(
                "spriteStore",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (spriteStoreField == null)
                throw new InvalidOperationException("[SpriteInjector] Champ 'spriteStore' introuvable dans WPFScreen. La version de la DLL a peut-être changé.");

            SpriteStore spriteStore = (SpriteStore)spriteStoreField.GetValue(screen)!;

            // Récupère le dictionnaire privé Dictionary<string, BitmapImage> dans SpriteStore
            FieldInfo? bitmapsField = typeof(SpriteStore).GetField(
                "bitmaps",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (bitmapsField == null)
                throw new InvalidOperationException("[SpriteInjector] Champ 'bitmaps' introuvable dans SpriteStore. La version de la DLL a peut-être changé.");

            Dictionary<string, BitmapImage> bitmaps = (Dictionary<string, BitmapImage>)bitmapsField.GetValue(spriteStore)!;

            // Injecte ou remplace le bitmap sous ce nom
            bitmaps[spriteName] = bitmap;

            System.Diagnostics.Debug.WriteLine($"[SpriteInjector] '{spriteName}' injecté avec succès.");
        }
    }
}
