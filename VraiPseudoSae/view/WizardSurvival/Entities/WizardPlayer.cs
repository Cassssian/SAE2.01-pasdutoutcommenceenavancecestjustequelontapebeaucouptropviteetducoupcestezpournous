using System.Windows;
using VraiPseudoSae.view.WizardSurvival.Abstractions;
using VraiPseudoSae.view.WizardSurvival.Core;

namespace VraiPseudoSae.view.WizardSurvival.Entities;

/// <summary>
/// Player-controlled wizard. Movement and damage are kept separate from WPF input.
/// </summary>
public sealed class WizardPlayer : LivingWorldItem
{
    private const double SpriteW = 34;
    private const double SpriteH = 44;
    private const double NormalSpeed = 210;
    private const double ImmuneSpeedBonus = 90;

    public WizardPlayer(double worldX, double worldY, WizardSurvivalGame game)
        : base(worldX, worldY, game, "wizard_player.png", SpriteW, SpriteH, 17, 4, 30)
    {
        Facing = FacingDirection.Right;
    }

    public override string TypeName => "WizardPlayer";

    public FacingDirection Facing { get; private set; }

    public double CurrentSpeed => NormalSpeed + (InvulnerabilityRemaining > 0 ? ImmuneSpeedBonus : 0);

    public void Move(DoubleVector input, double seconds, ICollisionMap map)
    {
        if (input.X < 0)
            Facing = FacingDirection.Left;
        else if (input.X > 0)
            Facing = FacingDirection.Right;

        DoubleVector direction = input.Normalize();
        DoubleVector delta = direction * (CurrentSpeed * seconds);
        Rect moved = MovementResolver.Move(Bounds, delta, map);
        SetWorldPosition(moved.X, moved.Y);
    }

    public override bool TakeDamage(DamageRequest damage)
    {
        bool applied = base.TakeDamage(damage);
        if (applied)
            InvulnerabilityRemaining = 2.0;

        return applied;
    }

    public void HealOne()
    {
        if (Health < MaxHealth)
            Health++;
    }

    public override void TickLiving(double seconds)
    {
        base.TickLiving(seconds);
        ChangeScale(Facing == FacingDirection.Right ? 1 : -1, 1);
        SyncScreenPosition();
    }

    public override void CollideEffect(IUTGame.GameItem other)
    {
    }
}
