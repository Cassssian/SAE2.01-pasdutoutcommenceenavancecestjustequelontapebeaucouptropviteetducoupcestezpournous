using System.Windows;

namespace VraiPseudoSae.view.WizardSurvival.Core;

/// <summary>
/// Static scenery drawn in the arena. The visual bounds can be larger than the solid bounds
/// so roofs, cracks, shadows, and debris do not push the player away from the visible wall.
/// </summary>
public sealed record ArenaObstacle(Rect Bounds, string Kind, Rect? CollisionBounds = null)
{
    public Rect SolidBounds => CollisionBounds ?? Bounds;
}
