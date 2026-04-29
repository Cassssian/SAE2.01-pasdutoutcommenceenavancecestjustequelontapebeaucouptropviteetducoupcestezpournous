using System;
using System.IO;

namespace VraiPseudoSae.Utils.Sprite
{
    public static class SpritePaths
    {
        public static string SpritesDirectory =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Sprites");

        public static string SoundsDirectory =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Sounds");

        // RLS
        public static string RlsCar1Png  => Path.Combine(SpritesDirectory, "rls_car1.png");
        public static string RlsCar2Png  => Path.Combine(SpritesDirectory, "rls_car2.png");
        public static string RlsBallPng  => Path.Combine(SpritesDirectory, "rls_ball.png");
        public static string RlsGoalPng  => Path.Combine(SpritesDirectory, "rls_goal.png");
        public static string RlsFloorPng => Path.Combine(SpritesDirectory, "rls_floor.png");

        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(SpritesDirectory);
            Directory.CreateDirectory(SoundsDirectory);
        }
    }
}
