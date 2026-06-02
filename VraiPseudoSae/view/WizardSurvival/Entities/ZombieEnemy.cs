using System;
using System.Windows;
using VraiPseudoSae.view.WizardSurvival.Abstractions;
using VraiPseudoSae.view.WizardSurvival.Core;

namespace VraiPseudoSae.view.WizardSurvival.Entities;

/// <summary>
/// Enemy that wanders until the wizard enters its detection radius.
/// </summary>
public sealed class ZombieEnemy : LivingWorldItem
{
    private const double SpriteW = 34;
    private const double SpriteH = 38;

    private DoubleVector wanderDirection;
    private double wanderTimer;

    public ZombieEnemy(double worldX, double worldY, WizardSurvivalGame game, ZombieKind kind)
        : base(
            worldX,
            worldY,
            game,
            kind == ZombieKind.Normal ? "wizard_zombie.png" : "wizard_zombie_evolved.png",
            SpriteW,
            SpriteH,
            16,
            kind == ZombieKind.Normal ? 2 : 3,
            25)
    {
        Kind = kind;
        Speed = kind == ZombieKind.Normal ? 105 : 145;
        DetectionRadius = kind == ZombieKind.Normal ? 230 : 270;
        ScoreValue = kind == ZombieKind.Normal ? 10 : 30;
        SpawnChancePenalty = kind == ZombieKind.Normal ? 0.05 : 0.15;
        Facing = FacingDirection.Left;
    }

    public override string TypeName => "ZombieEnemy";

    public ZombieKind Kind { get; }

    public double Speed { get; }

    public double DetectionRadius { get; }

    public int ScoreValue { get; }

    public double SpawnChancePenalty { get; }

    public FacingDirection Facing { get; private set; }

    public void Tick(WizardPlayer player, double seconds, ICollisionMap map, IRandomSource random)
    {
        TickLiving(seconds);

        DoubleVector toPlayer = new(player.CenterX - CenterX, player.CenterY - CenterY);
        DoubleVector desired;

        if (toPlayer.Length <= DetectionRadius)
        {
            desired = toPlayer.Normalize();
        }
        else
        {
            wanderTimer -= seconds;
            if (wanderTimer <= 0)
            {
                double angle = random.NextDouble(0, Math.PI * 2);
                wanderDirection = new DoubleVector(Math.Cos(angle), Math.Sin(angle)).Normalize();
                wanderTimer = random.NextDouble(0.5, 1.5);
            }

            desired = wanderDirection;
        }

        if (desired.X < -0.01)
            Facing = FacingDirection.Left;
        else if (desired.X > 0.01)
            Facing = FacingDirection.Right;

        Rect moved = MovementResolver.Move(Bounds, desired * (Speed * seconds), map);
        SetWorldPosition(moved.X, moved.Y);
        ChangeScale(Facing == FacingDirection.Right ? 1 : -1, 1);
        SyncScreenPosition();
    }

    public override void CollideEffect(IUTGame.GameItem other)
    {
    }
}
