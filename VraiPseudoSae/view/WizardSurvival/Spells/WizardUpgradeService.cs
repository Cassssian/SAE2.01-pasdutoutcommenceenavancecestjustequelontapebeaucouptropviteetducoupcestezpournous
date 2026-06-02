using VraiPseudoSae.view.WizardSurvival.Abstractions;
using VraiPseudoSae.view.WizardSurvival.Core;

namespace VraiPseudoSae.view.WizardSurvival.Spells;

/// <summary>
/// Applies the four upgrades from the original Pyxel prototype.
/// </summary>
public sealed class WizardUpgradeService : IUpgradeService
{
    public int UpgradeCost => 100;

    public bool CanUpgrade(WizardSurvivalGame game) => game.Score >= UpgradeCost;

    public bool Apply(WizardSurvivalGame game, UpgradeKind upgrade)
    {
        if (!CanUpgrade(game))
            return false;

        switch (upgrade)
        {
            case UpgradeKind.Fireball:
                game.Fireball.Upgrade();
                break;
            case UpgradeKind.CelestialCall:
                game.CelestialCall.Upgrade();
                break;
            case UpgradeKind.Shield:
                game.Shield.Upgrade();
                break;
            case UpgradeKind.Laser:
                game.Laser.Upgrade();
                break;
        }

        game.AddScore(-UpgradeCost);
        return true;
    }
}
