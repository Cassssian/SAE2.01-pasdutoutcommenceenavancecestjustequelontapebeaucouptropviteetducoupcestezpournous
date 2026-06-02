using System.Linq;
using VraiPseudoSae.view.WizardSurvival.Abstractions;
using VraiPseudoSae.view.WizardSurvival.Core;

namespace VraiPseudoSae.view.WizardSurvival.Entities;

/// <summary>
/// Directional projectile spawned by the fireball spell.
/// </summary>
public sealed class FireballProjectile : WorldItem
{
    private const double SpriteW = 18;
    private const double SpriteH = 18;
    private double lifetime = 1.65;

    public FireballProjectile(
        WizardSurvivalGame game,
        double worldX,
        double worldY,
        FacingDirection direction,
        double speed,
        int damage)
        : base(worldX, worldY, game, "wizard_fireball.png", SpriteW, SpriteH, 10, 28)
    {
        Direction = direction;
        Speed = speed;
        Damage = damage;
        ChangeScale(direction == FacingDirection.Right ? 1 : -1, 1);
    }

    public override string TypeName => "FireballProjectile";

    public FacingDirection Direction { get; }

    public double Speed { get; }

    public int Damage { get; }

    public void Tick(double seconds)
    {
        lifetime -= seconds;
        MoveWorld((int)Direction * Speed * seconds, 0);
        SyncScreenPosition();

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
