using System;
using System.Collections.Generic;
using System.Linq;
using VraiPseudoSae.view.WizardSurvival.Abstractions;
using VraiPseudoSae.view.WizardSurvival.Core;

namespace VraiPseudoSae.view.WizardSurvival.Rendering;

/// <summary>
/// Burst of particles used for hits, deaths and spell feedback.
/// </summary>
public sealed class ParticleEffect : IVisualEffect
{
    private readonly List<Particle> particles = new();

    public ParticleEffect(double x, double y, string palette, int count, IRandomSource random)
    {
        for (int i = 0; i < count; i++)
        {
            double angle = random.NextDouble(0, Math.PI * 2);
            double speed = random.NextDouble(35, 145);
            particles.Add(new Particle
            {
                X = x,
                Y = y,
                VelocityX = Math.Cos(angle) * speed,
                VelocityY = Math.Sin(angle) * speed,
                Life = random.NextDouble(0.25, 0.7),
                Palette = palette,
                Size = random.Next(2, 5)
            });
        }
    }

    public IReadOnlyList<Particle> Particles => particles;

    public bool IsActive => particles.Count > 0;

    public void Tick(WizardSurvivalGame game, double seconds)
    {
        foreach (Particle particle in particles)
        {
            particle.X += particle.VelocityX * seconds;
            particle.Y += particle.VelocityY * seconds;
            particle.VelocityY += 110 * seconds;
            particle.Life -= seconds;
        }

        particles.RemoveAll(particle => particle.Life <= 0);
    }
}
