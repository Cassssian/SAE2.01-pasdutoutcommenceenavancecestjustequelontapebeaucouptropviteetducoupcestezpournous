using VraiPseudoSae.view.WizardSurvival.Abstractions;
using VraiPseudoSae.view.WizardSurvival.Entities;

namespace VraiPseudoSae.view.WizardSurvival.Spells;

/// <summary>
/// Spawns a directional fireball projectile.
/// </summary>
public sealed class FireballSpell : CooldownSpell
{
    public FireballSpell() : base("Fireball", 1.0)
    {
        ProjectileSpeed = 310;
        Damage = 1;
    }

    public double ProjectileSpeed { get; private set; }

    public int Damage { get; private set; }

    public void Upgrade()
    {
        ProjectileSpeed += 70;
        Cooldown.SetDuration(System.Math.Max(0.18, Cooldown.Duration - 0.08));
    }

    protected override void CastCore(WizardSurvivalGame game)
    {
        WizardPlayer player = game.Player;
        double x = player.Facing == Core.FacingDirection.Right ? player.CenterX + 8 : player.CenterX - 26;
        double y = player.CenterY - 9;
        game.AddProjectile(new FireballProjectile(game, x, y, player.Facing, ProjectileSpeed, Damage, player.RangeMultiplier));
    }
}
