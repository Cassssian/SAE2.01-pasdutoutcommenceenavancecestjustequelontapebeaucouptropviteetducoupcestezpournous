using System;

namespace VraiPseudoSae.view.WizardSurvival.Core;

/// <summary>
/// Small immutable vector used by the gameplay simulation.
/// </summary>
public readonly record struct DoubleVector(double X, double Y)
{
    public static readonly DoubleVector Zero = new(0, 0);

    public double Length => Math.Sqrt(X * X + Y * Y);

    public DoubleVector Normalize()
    {
        double length = Length;
        return length <= double.Epsilon ? Zero : new DoubleVector(X / length, Y / length);
    }

    public static DoubleVector operator +(DoubleVector left, DoubleVector right) =>
        new(left.X + right.X, left.Y + right.Y);

    public static DoubleVector operator -(DoubleVector left, DoubleVector right) =>
        new(left.X - right.X, left.Y - right.Y);

    public static DoubleVector operator *(DoubleVector vector, double scalar) =>
        new(vector.X * scalar, vector.Y * scalar);
}
