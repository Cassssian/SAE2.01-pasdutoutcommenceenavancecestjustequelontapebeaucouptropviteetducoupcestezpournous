using System.Linq;
using VraiPseudoSae.view.WizardSurvival.Abstractions;
using VraiPseudoSae.view.WizardSurvival.Core;
using VraiPseudoSae.view.WizardSurvival.Entities;

namespace VraiPseudoSae.view.WizardSurvival.Spells;

/// <summary>
/// Charged horizontal beam that damages every zombie in front of the wizard.
/// </summary>
public sealed class LaserSpell : CooldownSpell
{
    private double chargeRemaining;
    private double beamRemaining;
    private FacingDirection direction;

    public LaserSpell() : base("Laser", 10.0)
    {
        ChargeDuration = 1.0;
        BeamDuration = 2.0;
        Width = 26;
        Damage = 1;
    }

    public override bool IsActive => IsCharging || IsFiring;

    public bool IsCharging => chargeRemaining > 0;

    public bool IsFiring => beamRemaining > 0;

    public bool LocksMovement => IsActive;

    public double ChargeProgress => IsCharging ? 1 - chargeRemaining / ChargeDuration : 1;

    public double BeamProgress => IsFiring ? 1 - beamRemaining / BeamDuration : 0;

    public double ChargeDuration { get; }

    public double BeamDuration { get; }

    public double Width { get; }

    public int Damage { get; private set; }

    public FacingDirection Direction => direction;

    public void Upgrade() => Damage++;

    protected override bool CanCast(WizardSurvivalGame game) => !IsActive;

    protected override void CastCore(WizardSurvivalGame game)
    {
        direction = game.Player.Facing;
        chargeRemaining = ChargeDuration;
        beamRemaining = 0;
    }

    public override void Tick(WizardSurvivalGame game, double seconds)
    {
        base.Tick(game, seconds);

        if (chargeRemaining > 0)
        {
            chargeRemaining = System.Math.Max(0, chargeRemaining - seconds);
            if (chargeRemaining <= 0)
                beamRemaining = BeamDuration;

            return;
        }

        if (beamRemaining <= 0)
            return;

        beamRemaining = System.Math.Max(0, beamRemaining - seconds);
        WizardPlayer player = game.Player;
        double startX = player.CenterX;
        double endX = startX + (int)direction * 520;
        double minX = System.Math.Min(startX, endX);
        double maxX = System.Math.Max(startX, endX);

        foreach (ZombieEnemy zombie in game.Zombies.Where(zombie => zombie.IsActive))
        {
            bool inHorizontalRange = zombie.CenterX >= minX && zombie.CenterX <= maxX;
            bool inVerticalRange = System.Math.Abs(zombie.CenterY - player.CenterY) <= Width;
            if (inHorizontalRange
                && inVerticalRange
                && zombie.TakeDamage(new DamageRequest(Damage, "Laser")))
            {
                game.CreateBurst(zombie.CenterX, zombie.CenterY, "laser");
            }
        }
    }
}
