using System;
using System.Windows;

namespace VraiPseudoSae.view.WizardSurvival.Core;

/// <summary>
/// Non-blocking water terrain. Every lake slows zombies that stand in it.
/// Special lakes also apply a timed player effect when their cooldown is ready.
/// </summary>
public abstract class ArenaLake
{
    private const double EdgeTolerance = 6;

    protected ArenaLake(Rect bounds, string kind, double zombieSlowMultiplier)
    {
        Bounds = bounds;
        Kind = kind;
        ZombieSlowMultiplier = zombieSlowMultiplier;
    }

    public Rect Bounds { get; }

    public string Kind { get; }

    public double ZombieSlowMultiplier { get; }

    public double Phase { get; private set; }

    public virtual bool IsReady => true;

    public virtual double CooldownProgress => 1;

    public bool Contains(double worldX, double worldY)
    {
        double rx = Bounds.Width / 2 + EdgeTolerance;
        double ry = Bounds.Height / 2 + EdgeTolerance;
        if (rx <= 0 || ry <= 0)
            return false;

        double dx = (worldX - (Bounds.Left + rx)) / rx;
        double dy = (worldY - (Bounds.Top + ry)) / ry;
        return dx * dx + dy * dy <= 1;
    }

    public double SpeedMultiplierAt(double worldX, double worldY) =>
        Contains(worldX, worldY) ? ZombieSlowMultiplier : 1;

    public virtual void Tick(WizardSurvivalGame game, double seconds)
    {
        Phase += seconds;
    }
}

/// <summary>
/// Basic water pool. It only slows zombies.
/// </summary>
public sealed class NormalLake : ArenaLake
{
    public NormalLake(Rect bounds) : base(bounds, "normal_lake", 0.55)
    {
    }
}

/// <summary>
/// Base class for lakes that can trigger a player buff or debuff, then wait for a cooldown.
/// </summary>
public abstract class CooldownLake : ArenaLake
{
    private double cooldownRemaining;

    protected CooldownLake(Rect bounds, string kind, double zombieSlowMultiplier, double cooldownDuration)
        : base(bounds, kind, zombieSlowMultiplier)
    {
        CooldownDuration = cooldownDuration;
    }

    public double CooldownDuration { get; }

    public override bool IsReady => cooldownRemaining <= 0;

    public override double CooldownProgress =>
        CooldownDuration <= 0 ? 1 : Math.Clamp(1 - cooldownRemaining / CooldownDuration, 0, 1);

    public override void Tick(WizardSurvivalGame game, double seconds)
    {
        base.Tick(game, seconds);

        if (cooldownRemaining > 0)
            cooldownRemaining = Math.Max(0, cooldownRemaining - seconds);

        if (!IsReady || game.Player is null || !Contains(game.Player.TerrainX, game.Player.TerrainY))
            return;

        ApplyReadyEffect(game);
        cooldownRemaining = CooldownDuration;
    }

    protected abstract void ApplyReadyEffect(WizardSurvivalGame game);
}

/// <summary>
/// Healing lake that gives a temporary speed and range bonus.
/// </summary>
public sealed class BuffLake : CooldownLake
{
    public BuffLake(Rect bounds) : base(bounds, "buff_lake", 0.58, 28)
    {
    }

    protected override void ApplyReadyEffect(WizardSurvivalGame game)
    {
        int missing = Math.Max(0, game.Player.MaxHealth - game.Player.Health);
        if (missing > 0)
        {
            int heal = Math.Min(missing, Math.Max(2, (int)Math.Ceiling(missing * 0.05)));
            game.Player.Heal(heal);
        }

        game.Player.ApplyLakeStatus(LakeStatusKind.Buffed, 1.22, 1.12, 15);
        game.CreateBurst(game.Player.CenterX, game.Player.CenterY, "lake_buff");
    }
}

/// <summary>
/// Cursed lake that applies the opposite timed effect: slower movement and shorter spell range.
/// </summary>
public sealed class NerfLake : CooldownLake
{
    public NerfLake(Rect bounds) : base(bounds, "nerf_lake", 0.5, 28)
    {
    }

    protected override void ApplyReadyEffect(WizardSurvivalGame game)
    {
        game.Player.ApplyLakeStatus(LakeStatusKind.Nerfed, 0.78, 0.9, 15);
        game.DamagePlayerFromLake(1);
    }
}
