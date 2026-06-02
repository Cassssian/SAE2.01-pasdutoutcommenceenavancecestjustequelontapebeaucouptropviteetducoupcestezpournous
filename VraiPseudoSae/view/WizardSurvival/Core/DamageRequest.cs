namespace VraiPseudoSae.view.WizardSurvival.Core;

/// <summary>
/// Describes a damage application without coupling the target to a concrete spell.
/// </summary>
public readonly record struct DamageRequest(int Amount, string Source);
