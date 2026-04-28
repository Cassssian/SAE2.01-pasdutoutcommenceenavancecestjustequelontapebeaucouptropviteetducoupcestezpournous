using System;
using System.IO;

namespace VraiPseudoSae.Utils.Sprite
{
    public static class SpritePaths
    {
        public static string OutputDirectory => AppDomain.CurrentDomain.BaseDirectory;

        public static string SpritesDirectory =>
            Path.Combine(OutputDirectory, "Resources", "Sprites");

        public static string SoundsDirectory =>
            Path.Combine(OutputDirectory, "Resources", "Sounds");

        public static string PlayerHubPng =>
            Path.Combine(SpritesDirectory, "player_hub.png");

        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(SpritesDirectory);
            Directory.CreateDirectory(SoundsDirectory);
        }
    }
}