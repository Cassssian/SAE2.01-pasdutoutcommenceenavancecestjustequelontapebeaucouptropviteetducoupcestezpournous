using System.Windows;

namespace VraiPseudoSae.view.WizardSurvival.Abstractions;

/// <summary>
/// Object located in arena coordinates.
/// </summary>
public interface IWorldObject
{
    double WorldX { get; }

    double WorldY { get; }

    double Width { get; }

    double Height { get; }

    double CollisionRadius { get; }

    bool IsActive { get; }

    double CenterX { get; }

    double CenterY { get; }

    Rect Bounds { get; }
}
