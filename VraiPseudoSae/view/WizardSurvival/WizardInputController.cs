using System;
using System.Collections.Generic;
using System.Windows.Input;
using IUTGame;
using VraiPseudoSae.view.WizardSurvival.Core;

namespace VraiPseudoSae.view.WizardSurvival;

/// <summary>
/// Invisible IUTGame item responsible for keyboard input and per-frame simulation ticks.
/// </summary>
public sealed class WizardInputController : GameItem, IAnimable, IKeyboardInteract, IMouseInteract
{
    private readonly WizardSurvivalGame game;
    private readonly HashSet<Key> keys = new();

    public WizardInputController(WizardSurvivalGame game)
        : base(0, 0, game, string.Empty, 0)
    {
        this.game = game;
        Collidable = false;
    }

    public override string TypeName => "WizardInputController";

    public double MouseX { get; private set; }

    public double MouseY { get; private set; }

    public void Animate(TimeSpan dt) => game.Tick(Math.Min(0.05, dt.TotalSeconds), BuildMoveVector());

    public void KeyDown(Key key)
    {
        keys.Add(key);

        if (key == Key.Enter && game.State == WizardGameState.Menu)
        {
            game.StartNewGame();
            return;
        }

        if (key == Key.R && game.State == WizardGameState.GameOver)
        {
            game.StartNewGame();
            return;
        }

        if (key == Key.Escape)
        {
            game.HandleEscape();
            return;
        }

        if (game.State != WizardGameState.Playing)
            return;

        if (key == Key.A)
            game.OpenUpgradePanel();
        else if (key == Key.Space)
            game.CelestialCall.TryCast(game);
        else if (key == Key.F)
            game.Fireball.TryCast(game);
        else if (key == Key.B)
            game.Shield.TryCast(game);
        else if (key == Key.L)
            game.Laser.TryCast(game);
    }

    public void KeyUp(Key key) => keys.Remove(key);

    public override void CollideEffect(GameItem other)
    {
    }

    public void MouseMoved(double x, double y)
    {
        MouseX = x;
        MouseY = y;
    }

    public void MouseLeftButtonDown(double x, double y)
    {
        MouseMoved(x, y);
    }

    public void MouseLeftButtonUp(double x, double y)
    {
        MouseMoved(x, y);
    }

    public void MouseRightButtonDown(double x, double y)
    {
        MouseMoved(x, y);
    }

    public void MouseRightButtonUp(double x, double y)
    {
        MouseMoved(x, y);
    }

    public void MouseWheel(int delta)
    {
    }

    private DoubleVector BuildMoveVector()
    {
        double x = 0;
        double y = 0;

        if (keys.Contains(Key.Left) || keys.Contains(Key.Q))
            x -= 1;
        if (keys.Contains(Key.Right) || keys.Contains(Key.D))
            x += 1;
        if (keys.Contains(Key.Up) || keys.Contains(Key.Z) || keys.Contains(Key.W))
            y -= 1;
        if (keys.Contains(Key.Down) || keys.Contains(Key.S))
            y += 1;

        return new DoubleVector(x, y);
    }
}
