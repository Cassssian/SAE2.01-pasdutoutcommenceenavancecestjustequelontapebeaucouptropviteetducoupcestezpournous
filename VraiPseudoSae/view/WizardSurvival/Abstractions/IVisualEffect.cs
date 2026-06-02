namespace VraiPseudoSae.view.WizardSurvival.Abstractions;

/// <summary>
/// Time-limited effect updated by gameplay and rendered by the WPF effect renderer.
/// </summary>
public interface IVisualEffect
{
    bool IsActive { get; }

    void Tick(WizardSurvivalGame game, double seconds);
}
