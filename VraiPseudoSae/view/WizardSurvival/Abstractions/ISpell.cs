using VraiPseudoSae.view.WizardSurvival.Core;

namespace VraiPseudoSae.view.WizardSurvival.Abstractions;

/// <summary>
/// Ability that can be cast by the wizard and updated by the game loop.
/// </summary>
public interface ISpell
{
    string Name { get; }

    CooldownMeter Cooldown { get; }

    bool IsActive { get; }

    bool TryCast(WizardSurvivalGame game);

    void Tick(WizardSurvivalGame game, double seconds);
}
