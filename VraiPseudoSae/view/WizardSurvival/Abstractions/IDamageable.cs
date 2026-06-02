using VraiPseudoSae.view.WizardSurvival.Core;

namespace VraiPseudoSae.view.WizardSurvival.Abstractions;

/// <summary>
/// Entity that can receive combat damage.
/// </summary>
public interface IDamageable
{
    int Health { get; }

    bool CanTakeDamage { get; }

    bool TakeDamage(DamageRequest damage);
}
