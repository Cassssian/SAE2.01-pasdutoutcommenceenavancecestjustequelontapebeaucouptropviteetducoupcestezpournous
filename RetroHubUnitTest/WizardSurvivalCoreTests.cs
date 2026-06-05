using System.Windows;
using VraiPseudoSae.view.WizardSurvival.Core;
using VraiPseudoSae.view.WizardSurvival.Spells;

namespace RetroHubUnitTest;

public sealed class WizardSurvivalCoreTests
{
    [Fact]
    public void Camera_centering_clamps_to_world_edges()
    {
        Camera2D camera = new(200, 100, 500, 400);

        camera.CenterOn(10, 10);
        Assert.Equal(0, camera.X);
        Assert.Equal(0, camera.Y);

        camera.CenterOn(480, 390);
        Assert.Equal(300, camera.X);
        Assert.Equal(300, camera.Y);
    }

    [Fact]
    public void Arena_rejects_bounds_that_overlap_obstacles_or_leave_world()
    {
        WizardArenaMap map = new(
            100,
            80,
            new[] { new ArenaObstacle(new Rect(40, 20, 20, 20), "stone") });

        Assert.True(map.CanOccupy(new Rect(5, 5, 10, 10)));
        Assert.False(map.CanOccupy(new Rect(50, 25, 10, 10)));
        Assert.False(map.CanOccupy(new Rect(95, 10, 10, 10)));
    }

    [Fact]
    public void Arena_uses_solid_obstacle_bounds_instead_of_full_visual_bounds()
    {
        WizardArenaMap map = new(
            100,
            80,
            new[] { new ArenaObstacle(new Rect(10, 10, 50, 40), "house", new Rect(22, 30, 30, 18)) });

        Assert.True(map.CanOccupy(new Rect(12, 14, 8, 8)));
        Assert.False(map.CanOccupy(new Rect(30, 34, 8, 8)));
    }

    [Fact]
    public void Terrain_speed_multiplier_only_applies_inside_lake_shape()
    {
        WizardArenaMap map = new(
            100,
            80,
            Array.Empty<ArenaObstacle>(),
            new[] { new NormalLake(new Rect(20, 20, 40, 24)) });

        Assert.Equal(0.55, map.TerrainSpeedMultiplier(40, 32), 2);
        Assert.Equal(1, map.TerrainSpeedMultiplier(5, 5), 2);
    }

    [Fact]
    public void Movement_resolver_keeps_axis_that_hits_obstacle_and_applies_free_axis()
    {
        WizardArenaMap map = new(
            100,
            100,
            new[] { new ArenaObstacle(new Rect(20, 0, 10, 100), "wall") });

        Rect moved = MovementResolver.Move(
            new Rect(5, 10, 10, 10),
            new DoubleVector(10, 5),
            map);

        Assert.Equal(5, moved.X);
        Assert.Equal(15, moved.Y);
    }

    [Fact]
    public void Cooldown_meter_reports_readiness_after_elapsed_duration()
    {
        CooldownMeter meter = new(1.5);

        meter.Restart();
        meter.Tick(0.5);

        Assert.False(meter.IsReady);
        Assert.InRange(meter.Progress, 0.33, 0.34);

        meter.Tick(1.0);
        Assert.True(meter.IsReady);
        Assert.Equal(1, meter.Progress);
    }

    [Fact]
    public void Fireball_upgrade_increases_projectile_speed_and_reduces_cooldown()
    {
        FireballSpell spell = new();
        double initialSpeed = spell.ProjectileSpeed;
        double initialCooldown = spell.Cooldown.Duration;

        spell.Upgrade();

        Assert.True(spell.ProjectileSpeed > initialSpeed);
        Assert.True(spell.Cooldown.Duration < initialCooldown);
    }
}
