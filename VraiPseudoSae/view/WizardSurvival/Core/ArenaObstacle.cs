using System.Windows;

namespace VraiPseudoSae.view.WizardSurvival.Core;

/// <summary>
/// Blocking scenery rectangle used by both collision logic and WPF background rendering.
/// </summary>
public sealed record ArenaObstacle(Rect Bounds, string Kind);
