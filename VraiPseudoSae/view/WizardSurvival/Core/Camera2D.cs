using System;

namespace VraiPseudoSae.view.WizardSurvival.Core;

/// <summary>
/// Keeps a viewport inside a larger world and follows a target center.
/// </summary>
public sealed class Camera2D
{
    public Camera2D(double viewportWidth, double viewportHeight, double worldWidth, double worldHeight)
    {
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        WorldWidth = worldWidth;
        WorldHeight = worldHeight;
    }

    public double ViewportWidth { get; }

    public double ViewportHeight { get; }

    public double WorldWidth { get; }

    public double WorldHeight { get; }

    public double X { get; private set; }

    public double Y { get; private set; }

    /// <summary>
    /// Centers the viewport on a world point while respecting world borders.
    /// </summary>
    public void CenterOn(double worldX, double worldY)
    {
        X = Clamp(worldX - ViewportWidth / 2.0, 0, Math.Max(0, WorldWidth - ViewportWidth));
        Y = Clamp(worldY - ViewportHeight / 2.0, 0, Math.Max(0, WorldHeight - ViewportHeight));
    }

    private static double Clamp(double value, double min, double max) =>
        Math.Min(max, Math.Max(min, value));
}
