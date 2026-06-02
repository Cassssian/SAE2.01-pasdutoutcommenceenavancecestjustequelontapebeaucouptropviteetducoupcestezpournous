using VraiPseudoSae.view.WizardSurvival.Core;

namespace VraiPseudoSae.view.WizardSurvival.Abstractions;

/// <summary>
/// Base class for world objects with health and temporary invulnerability.
/// </summary>
public abstract class LivingWorldItem : WorldItem, IDamageable
{
    protected LivingWorldItem(
        double worldX,
        double worldY,
        WizardSurvivalGame game,
        string spriteName,
        double spriteWidth,
        double spriteHeight,
        double collisionRadius,
        int maxHealth,
        int zIndex)
        : base(worldX, worldY, game, spriteName, spriteWidth, spriteHeight, collisionRadius, zIndex)
    {
        MaxHealth = maxHealth;
        Health = maxHealth;
    }

    public int MaxHealth { get; protected set; }

    public int Health { get; protected set; }

    public double InvulnerabilityRemaining { get; protected set; }

    public bool CanTakeDamage => InvulnerabilityRemaining <= 0 && Health > 0;

    public virtual bool TakeDamage(DamageRequest damage)
    {
        if (!CanTakeDamage)
            return false;

        Health = System.Math.Max(0, Health - System.Math.Max(0, damage.Amount));
        InvulnerabilityRemaining = 0.35;

        if (Health <= 0)
            Deactivate();

        return true;
    }

    public virtual void TickLiving(double seconds)
    {
        if (InvulnerabilityRemaining > 0)
            InvulnerabilityRemaining = System.Math.Max(0, InvulnerabilityRemaining - seconds);
    }
}
