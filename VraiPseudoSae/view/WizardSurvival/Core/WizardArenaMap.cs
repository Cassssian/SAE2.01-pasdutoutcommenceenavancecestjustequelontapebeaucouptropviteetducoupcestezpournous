using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace VraiPseudoSae.view.WizardSurvival.Core;

/// <summary>
/// Static arena used by the wizard survival game.
/// </summary>
public sealed class WizardArenaMap : ICollisionMap
{
    public WizardArenaMap(double width, double height, IEnumerable<ArenaObstacle> obstacles)
    {
        Width = width;
        Height = height;
        Obstacles = obstacles.ToList();
    }

    public double Width { get; }

    public double Height { get; }

    public IReadOnlyList<ArenaObstacle> Obstacles { get; }

    public bool CanOccupy(Rect bounds)
    {
        if (bounds.Left < 0 || bounds.Top < 0 || bounds.Right > Width || bounds.Bottom > Height)
            return false;

        return Obstacles.All(obstacle => !obstacle.Bounds.IntersectsWith(bounds));
    }

    public static WizardArenaMap CreateDefault() =>
        new(
            1200,
            900,
            new[]
            {
                new ArenaObstacle(new Rect(210, 170, 120, 56), "stone"),
                new ArenaObstacle(new Rect(640, 150, 170, 48), "stone"),
                new ArenaObstacle(new Rect(930, 260, 92, 118), "tree"),
                new ArenaObstacle(new Rect(420, 395, 138, 72), "ruin"),
                new ArenaObstacle(new Rect(120, 620, 180, 55), "stone"),
                new ArenaObstacle(new Rect(760, 610, 155, 68), "ruin"),
                new ArenaObstacle(new Rect(1010, 720, 85, 120), "tree")
            });
}
