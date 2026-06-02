namespace VraiPseudoSae.view.WizardSurvival.Core;

/// <summary>
/// Immutable data sent from the simulation to the WPF HUD.
/// </summary>
public sealed record WizardHudSnapshot(
    int Score,
    int FinalScore,
    int Lives,
    int MaxLives,
    int ZombiesKilled,
    double FireballProgress,
    double CelestialProgress,
    double ShieldProgress,
    double LaserProgress,
    bool ShieldActive,
    bool LaserActive,
    LakeStatusKind LakeStatus,
    double LakeStatusRemaining);
