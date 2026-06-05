using System.Collections.Generic;
using System.Windows;

namespace VraiPseudoSae.view.WizardSurvival.Core;

/// <summary>
/// Provides world bounds and static obstacle collision checks.
/// </summary>
public interface ICollisionMap
{
    double Width { get; }

    double Height { get; }

    IReadOnlyList<ArenaObstacle> Obstacles { get; }

    IReadOnlyList<ArenaLake> Lakes { get; }

    bool CanOccupy(Rect bounds);

    double TerrainSpeedMultiplier(double worldX, double worldY);
}
