using System.Linq;
using VraiPseudoSae.view.WizardSurvival.Abstractions;
using VraiPseudoSae.view.WizardSurvival.Core;
using VraiPseudoSae.view.WizardSurvival.Entities;

namespace VraiPseudoSae.view.WizardSurvival.Rendering;

/// <summary>
/// Expanding area spell preceded by a lightning strike.
/// </summary>
public sealed class CelestialStrikeEffect : IVisualEffect
{
    private readonly double startRadius;
    private readonly double maxRadius;
    private double timer;

    public CelestialStrikeEffect(double centerX, double centerY, double startRadius, double maxRadius)
    {
        CenterX = centerX;
        CenterY = centerY;
        this.startRadius = startRadius;
        this.maxRadius = maxRadius;
    }

    public double CenterX { get; }

    public double CenterY { get; }

    public double Radius { get; private set; }

    public double Age => timer;

    public double LightningProgress => System.Math.Min(1, timer / 0.28);

    public bool CircleStarted => timer >= 0.28;

    public bool IsActive => timer < 2.0;

    public void Tick(WizardSurvivalGame game, double seconds)
    {
        timer += seconds;

        if (!CircleStarted)
            return;

        double localProgress = System.Math.Min(1, (timer - 0.28) / 1.72);
        Radius = startRadius + (maxRadius - startRadius) * (1 - System.Math.Pow(1 - localProgress, 3));

        foreach (ZombieEnemy zombie in game.Zombies.Where(zombie => zombie.IsActive))
        {
            double dx = zombie.CenterX - CenterX;
            double dy = zombie.CenterY - CenterY;
            if (dx * dx + dy * dy <= Radius * Radius
                && zombie.TakeDamage(new DamageRequest(1, "CelestialCall")))
            {
                game.CreateBurst(zombie.CenterX, zombie.CenterY, "celestial");
            }
        }
    }
}
