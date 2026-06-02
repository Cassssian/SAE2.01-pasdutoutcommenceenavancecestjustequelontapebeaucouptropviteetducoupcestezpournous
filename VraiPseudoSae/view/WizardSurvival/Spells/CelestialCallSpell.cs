using VraiPseudoSae.view.WizardSurvival.Abstractions;
using VraiPseudoSae.view.WizardSurvival.Rendering;

namespace VraiPseudoSae.view.WizardSurvival.Spells;

/// <summary>
/// Creates the celestial strike area spell at the player's position.
/// </summary>
public sealed class CelestialCallSpell : CooldownSpell
{
    public CelestialCallSpell() : base("Celestial", 4.0)
    {
        StartRadius = 12;
        MaxRadius = 96;
    }

    public double StartRadius { get; private set; }

    public double MaxRadius { get; private set; }

    public void Upgrade()
    {
        StartRadius += 12;
        MaxRadius += 24;
    }

    protected override void CastCore(WizardSurvivalGame game)
    {
        game.AddEffect(new CelestialStrikeEffect(
            game.Player.CenterX,
            game.Player.CenterY,
            StartRadius,
            MaxRadius));
    }
}
