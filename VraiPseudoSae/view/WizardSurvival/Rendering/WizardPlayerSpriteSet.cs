namespace VraiPseudoSae.view.WizardSurvival.Rendering;

/// <summary>
/// Names of the pre-registered wizard animation frames.
/// </summary>
public sealed record WizardPlayerSpriteSet(
    IReadOnlyList<string> IdleRight,
    IReadOnlyList<string> IdleLeft,
    IReadOnlyList<string> WalkRight,
    IReadOnlyList<string> WalkLeft,
    IReadOnlyList<string> DeathRightA,
    IReadOnlyList<string> DeathLeftA,
    IReadOnlyList<string> DeathRightB,
    IReadOnlyList<string> DeathLeftB)
{
    public string InitialSprite => IdleRight[0];

    public static WizardPlayerSpriteSet SingleFrameFallback { get; } = new(
        new[] { "wizard_player_right.png" },
        new[] { "wizard_player_left.png" },
        new[] { "wizard_player_right.png" },
        new[] { "wizard_player_left.png" },
        new[] { "wizard_player_right.png" },
        new[] { "wizard_player_left.png" },
        new[] { "wizard_player_right.png" },
        new[] { "wizard_player_left.png" });
}
