using System.Linq;
using VraiPseudoSae.view.WizardSurvival.Abstractions;
using VraiPseudoSae.view.WizardSurvival.Core;

namespace VraiPseudoSae.view.WizardSurvival.Entities;

/// <summary>
/// Directional projectile spawned by the fireball spell.
/// </summary>
public sealed class FireballProjectile : WorldItem
{
    private const double SpriteW = 28;
    private const double SpriteH = 20;
    private double lifetime = 1.65;
    private double trailTimer;

    public FireballProjectile(
        WizardSurvivalGame game,
        double worldX,
        double worldY,
        FacingDirection direction,
        double speed,
        int damage,
        double rangeMultiplier)
        : base(
            worldX,
            worldY,
            game,
            direction == FacingDirection.Right ? "wizard_fireball_right.png" : "wizard_fireball_left.png",
            SpriteW,
            SpriteH,
            10,
            28)
    {
        Direction = direction;
        Speed = speed;
        Damage = damage;
        lifetime *= rangeMultiplier;
    }

    public override string TypeName => "FireballProjectile";

    public FacingDirection Direction { get; }

    public double Speed { get; }

    public int Damage { get; }

    public void Tick(double seconds)
    {
        lifetime -= seconds;
        trailTimer -= seconds;
        MoveWorld((int)Direction * Speed * seconds, 0);
        SyncScreenPosition();

        if (trailTimer <= 0)
        {
            trailTimer = 0.045;
            double offset = Direction == FacingDirection.Right ? -8 : 8;
            Game.CreateParticles(CenterX + offset, CenterY, "fire", 4);
        }

        if (lifetime <= 0 || !Game.Map.CanOccupy(Bounds))
        {
            Deactivate();
            return;
        }

        ZombieEnemy? hit = Game.Zombies.FirstOrDefault(zombie => zombie.IsActive && CircleIntersects(zombie));
        if (hit is null)
            return;

        if (hit.TakeDamage(new DamageRequest(Damage, "Fireball")))
            Game.CreateBurst(CenterX, CenterY, "fire");

        Deactivate();
    }

    public override void CollideEffect(IUTGame.GameItem other)
    {
    }
}
