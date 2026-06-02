namespace VraiPseudoSae.view.WizardSurvival.Rendering;

/// <summary>
/// Single particle in world coordinates.
/// </summary>
public sealed class Particle
{
    public double X { get; set; }

    public double Y { get; set; }

    public double VelocityX { get; set; }

    public double VelocityY { get; set; }

    public double Life { get; set; }

    public string Palette { get; init; } = "normal";

    public double Size { get; init; } = 2;
}
