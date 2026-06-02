using VraiPseudoSae.view.WizardSurvival.Core;

namespace VraiPseudoSae.view.WizardSurvival.Abstractions;

/// <summary>
/// Shared cooldown behavior for active and projectile spells.
/// </summary>
public abstract class CooldownSpell : ISpell
{
    protected CooldownSpell(string name, double cooldownSeconds)
    {
        Name = name;
        Cooldown = new CooldownMeter(cooldownSeconds);
    }

    public string Name { get; }

    public CooldownMeter Cooldown { get; }

    public virtual bool IsActive => false;

    public bool TryCast(WizardSurvivalGame game)
    {
        if (!Cooldown.IsReady || !CanCast(game))
            return false;

        CastCore(game);
        Cooldown.Restart();
        return true;
    }

    public virtual void Tick(WizardSurvivalGame game, double seconds) => Cooldown.Tick(seconds);

    protected virtual bool CanCast(WizardSurvivalGame game) => true;

    protected abstract void CastCore(WizardSurvivalGame game);
}
