using System.Windows;
using VraiPseudoSae.view.WizardSurvival.Abstractions;
using VraiPseudoSae.view.WizardSurvival.Core;

namespace VraiPseudoSae.view.WizardSurvival.Entities;

/// <summary>
/// Player-controlled wizard. Movement and damage are kept separate from WPF input.
/// </summary>
public sealed class WizardPlayer : LivingWorldItem
{
    private const double SpriteW = 40;
    private const double SpriteH = 48;
    private const double NormalSpeed = 210;
    private const double ImmuneSpeedBonus = 90;
    private FacingDirection renderedFacing;
    private double lakeSpeedMultiplier = 1;
    private double lakeRangeMultiplier = 1;
    private double lakeStatusRemaining;

    public WizardPlayer(double worldX, double worldY, WizardSurvivalGame game)
        : base(worldX, worldY, game, "wizard_player_right.png", SpriteW, SpriteH, 17, 4, 30)
    {
        Facing = FacingDirection.Right;
        renderedFacing = FacingDirection.Right;
        SetLocalCollisionBounds(12, 25, 16, 18);
    }

    public override string TypeName => "WizardPlayer";

    public FacingDirection Facing { get; private set; }

    public double CurrentSpeed => (NormalSpeed + (InvulnerabilityRemaining > 0 ? ImmuneSpeedBonus : 0)) * lakeSpeedMultiplier;

    public double RangeMultiplier => lakeRangeMultiplier;

    public LakeStatusKind LakeStatus { get; private set; }

    public double LakeStatusRemaining => lakeStatusRemaining;

    public void Move(DoubleVector input, double seconds, ICollisionMap map)
    {
        if (input.X < 0)
            SetFacing(FacingDirection.Left);
        else if (input.X > 0)
            SetFacing(FacingDirection.Right);

        DoubleVector direction = input.Normalize();
        DoubleVector delta = direction * (CurrentSpeed * seconds);
        Rect moved = MovementResolver.Move(CollisionBounds, delta, map);
        SetWorldPositionFromCollisionBounds(moved);
    }

    public override bool TakeDamage(DamageRequest damage)
    {
        bool applied = base.TakeDamage(damage);
        if (applied)
            InvulnerabilityRemaining = 2.0;

        return applied;
    }

    public void HealOne() => Heal(1);

    public void Heal(int amount)
    {
        if (amount > 0 && Health < MaxHealth)
            Health = System.Math.Min(MaxHealth, Health + amount);
    }

    public void ApplyLakeStatus(LakeStatusKind status, double speedMultiplier, double rangeMultiplier, double duration)
    {
        LakeStatus = status;
        lakeSpeedMultiplier = speedMultiplier;
        lakeRangeMultiplier = rangeMultiplier;
        lakeStatusRemaining = duration;
    }

    public bool TakeEnvironmentalDamage(DamageRequest damage)
    {
        if (Health <= 0)
            return false;

        Health = System.Math.Max(0, Health - System.Math.Max(0, damage.Amount));
        InvulnerabilityRemaining = System.Math.Max(InvulnerabilityRemaining, 0.7);
        if (Health <= 0)
            Deactivate();

        return true;
    }

    public override void TickLiving(double seconds)
    {
        base.TickLiving(seconds);
        if (lakeStatusRemaining > 0)
        {
            lakeStatusRemaining = System.Math.Max(0, lakeStatusRemaining - seconds);
            if (lakeStatusRemaining <= 0)
            {
                LakeStatus = LakeStatusKind.None;
                lakeSpeedMultiplier = 1;
                lakeRangeMultiplier = 1;
            }
        }

        SyncScreenPosition();
    }

    private void SetFacing(FacingDirection facing)
    {
        Facing = facing;
        if (renderedFacing == facing)
            return;

        renderedFacing = facing;
        ChangeSprite(facing == FacingDirection.Right ? "wizard_player_right.png" : "wizard_player_left.png");
        SyncScreenPosition();
    }

    public override void CollideEffect(IUTGame.GameItem other)
    {
    }
}
