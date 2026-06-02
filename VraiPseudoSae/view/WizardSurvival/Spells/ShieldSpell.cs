using System.Linq;
using VraiPseudoSae.view.WizardSurvival.Abstractions;
using VraiPseudoSae.view.WizardSurvival.Core;
using VraiPseudoSae.view.WizardSurvival.Entities;

namespace VraiPseudoSae.view.WizardSurvival.Spells;

/// <summary>
/// Temporary shield that blocks contact damage and pushes nearby zombies away.
/// </summary>
public sealed class ShieldSpell : CooldownSpell
{
    private double activeRemaining;

    public ShieldSpell() : base("Shield", 20.0)
    {
        Duration = 10.0;
        Radius = 58;
    }

    public override bool IsActive => activeRemaining > 0;

    public double Duration { get; private set; }

    public double ActiveRemaining => activeRemaining;

    public double Radius { get; }

    public void Upgrade() => Duration += 5.0;

    protected override bool CanCast(WizardSurvivalGame game) => !IsActive;

    protected override void CastCore(WizardSurvivalGame game) => activeRemaining = Duration;

    public override void Tick(WizardSurvivalGame game, double seconds)
    {
        base.Tick(game, seconds);

        if (activeRemaining <= 0)
            return;

        activeRemaining = System.Math.Max(0, activeRemaining - seconds);
        WizardPlayer player = game.Player;

        foreach (ZombieEnemy zombie in game.Zombies.Where(zombie => zombie.IsActive))
        {
            DoubleVector fromPlayer = new(zombie.CenterX - player.CenterX, zombie.CenterY - player.CenterY);
            if (fromPlayer.Length <= Radius)
            {
                DoubleVector push = fromPlayer.Normalize() * (180 * seconds);
                var moved = MovementResolver.Move(zombie.Bounds, push, game.Map);
                zombie.SetWorldPosition(moved.X, moved.Y);
            }
        }
    }
}
