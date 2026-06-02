using System;

namespace VraiPseudoSae.view.WizardSurvival.Core;

/// <summary>
/// Random abstraction used to keep gameplay deterministic in unit tests.
/// </summary>
public interface IRandomSource
{
    int Next(int minValue, int maxValue);

    double NextDouble();

    double NextDouble(double minValue, double maxValue);
}

/// <summary>
/// Production random source backed by <see cref="Random"/>.
/// </summary>
public sealed class SystemRandomSource : IRandomSource
{
    private readonly Random random;

    public SystemRandomSource() : this(new Random())
    {
    }

    public SystemRandomSource(Random random)
    {
        this.random = random;
    }

    public int Next(int minValue, int maxValue) => random.Next(minValue, maxValue);

    public double NextDouble() => random.NextDouble();

    public double NextDouble(double minValue, double maxValue) =>
        minValue + random.NextDouble() * (maxValue - minValue);
}
