using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace VraiPseudoSae.view.WizardSurvival.Core;

/// <summary>
/// Static arena used by the wizard survival game.
/// </summary>
public sealed class WizardArenaMap : ICollisionMap
{
    public WizardArenaMap(
        double width,
        double height,
        IEnumerable<ArenaObstacle> obstacles,
        IEnumerable<ArenaLake>? lakes = null)
    {
        Width = width;
        Height = height;
        Obstacles = obstacles.ToList();
        Lakes = (lakes ?? Enumerable.Empty<ArenaLake>()).ToList();
    }

    public double Width { get; }

    public double Height { get; }

    public IReadOnlyList<ArenaObstacle> Obstacles { get; }

    public IReadOnlyList<ArenaLake> Lakes { get; private set; }

    public bool CanOccupy(Rect bounds)
    {
        if (bounds.Left < 0 || bounds.Top < 0 || bounds.Right > Width || bounds.Bottom > Height)
            return false;

        return Obstacles.All(obstacle => !obstacle.SolidBounds.IntersectsWith(bounds));
    }

    public double TerrainSpeedMultiplier(double worldX, double worldY) =>
        Lakes
            .Where(lake => lake.Contains(worldX, worldY))
            .Select(lake => lake.ZombieSlowMultiplier)
            .DefaultIfEmpty(1)
            .Min();

    public double ZombieLakeSpeedMultiplier(double worldX, double worldY) =>
        TerrainSpeedMultiplier(worldX, worldY);

    public void RegenerateLakes() =>
        Lakes = CreateRandomLakes(Width, Height, Obstacles);

    public static WizardArenaMap CreateDefault()
    {
        const double width = 1200;
        const double height = 900;
        var obstacles = new[]
        {
            new ArenaObstacle(new Rect(86, 128, 184, 104), "manor", new Rect(98, 154, 160, 72)),
            new ArenaObstacle(new Rect(365, 98, 132, 82), "broken_house", new Rect(378, 134, 106, 46)),
            new ArenaObstacle(new Rect(650, 124, 176, 72), "collapsed_roof", new Rect(664, 154, 148, 42)),
            new ArenaObstacle(new Rect(960, 162, 112, 124), "dead_tree", new Rect(1007, 202, 18, 84)),
            new ArenaObstacle(new Rect(206, 354, 158, 88), "rotten_garden", new Rect(212, 360, 146, 76)),
            new ArenaObstacle(new Rect(492, 360, 138, 78), "ruin", new Rect(506, 386, 110, 52)),
            new ArenaObstacle(new Rect(822, 348, 154, 92), "cracked_house", new Rect(836, 382, 126, 58)),
            new ArenaObstacle(new Rect(96, 608, 204, 68), "rotten_garden", new Rect(102, 614, 192, 56)),
            new ArenaObstacle(new Rect(428, 648, 92, 74), "well", new Rect(446, 680, 56, 42)),
            new ArenaObstacle(new Rect(692, 620, 178, 94), "manor_wing", new Rect(708, 652, 146, 62)),
            new ArenaObstacle(new Rect(1010, 694, 92, 130), "dead_tree", new Rect(1048, 734, 18, 90)),
            new ArenaObstacle(new Rect(990, 420, 132, 64), "stone", new Rect(1000, 442, 112, 42))
        };

        return new WizardArenaMap(width, height, obstacles, CreateRandomLakes(width, height, obstacles));
    }

    private static IReadOnlyList<ArenaLake> CreateRandomLakes(
        double width,
        double height,
        IReadOnlyList<ArenaObstacle> obstacles)
    {
        var random = Random.Shared;
        var lakes = new List<ArenaLake>();
        var factories = new Func<Rect, ArenaLake>[]
        {
            bounds => new NormalLake(bounds),
            bounds => new BuffLake(bounds),
            bounds => new NerfLake(bounds)
        };

        foreach (Func<Rect, ArenaLake> createLake in factories.OrderBy(_ => random.Next()))
        {
            for (int attempt = 0; attempt < 120; attempt++)
            {
                double lakeWidth = random.Next(90, 146);
                double lakeHeight = random.Next(44, 72);
                double x = random.Next(48, (int)(width - lakeWidth - 48));
                double y = random.Next(72, (int)(height - lakeHeight - 72));
                var candidate = new Rect(x, y, lakeWidth, lakeHeight);

                if (IsNearPlayerStart(candidate) || TouchesSolidScenery(candidate, obstacles) || TouchesOtherLake(candidate, lakes))
                    continue;

                lakes.Add(createLake(candidate));
                break;
            }
        }

        return lakes;
    }

    private static bool IsNearPlayerStart(Rect candidate)
    {
        var startArea = new Rect(0, 350, 190, 170);
        return startArea.IntersectsWith(candidate);
    }

    private static bool TouchesSolidScenery(Rect candidate, IReadOnlyList<ArenaObstacle> obstacles)
    {
        Rect inflated = Inflate(candidate, 28);
        return obstacles.Any(obstacle => inflated.IntersectsWith(obstacle.SolidBounds));
    }

    private static bool TouchesOtherLake(Rect candidate, IReadOnlyList<ArenaLake> lakes)
    {
        Rect inflated = Inflate(candidate, 72);
        return lakes.Any(lake => inflated.IntersectsWith(lake.Bounds));
    }

    private static Rect Inflate(Rect bounds, double padding)
    {
        bounds.Inflate(padding, padding);
        return bounds;
    }
}
