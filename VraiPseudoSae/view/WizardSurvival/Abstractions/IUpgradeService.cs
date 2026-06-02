using VraiPseudoSae.view.WizardSurvival.Core;

namespace VraiPseudoSae.view.WizardSurvival.Abstractions;

/// <summary>
/// Applies score-gated upgrades to the current game run.
/// </summary>
public interface IUpgradeService
{
    int UpgradeCost { get; }

    bool CanUpgrade(WizardSurvivalGame game);

    bool Apply(WizardSurvivalGame game, UpgradeKind upgrade);
}
