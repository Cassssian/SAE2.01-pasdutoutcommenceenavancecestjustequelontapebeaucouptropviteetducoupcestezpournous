using System.Windows;

namespace VraiPseudoSae.view.WizardSurvival.Core;

/// <summary>
/// Resolves movement against the arena one axis at a time.
/// </summary>
public static class MovementResolver
{
    public static Rect Move(Rect current, DoubleVector delta, ICollisionMap map)
    {
        Rect movedX = new(current.X + delta.X, current.Y, current.Width, current.Height);
        if (map.CanOccupy(movedX))
            current = movedX;

        Rect movedY = new(current.X, current.Y + delta.Y, current.Width, current.Height);
        if (map.CanOccupy(movedY))
            current = movedY;

        return current;
    }
}
