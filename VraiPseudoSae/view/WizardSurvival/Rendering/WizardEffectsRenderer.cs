using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VraiPseudoSae.view.WizardSurvival.Core;
using VraiPseudoSae.view.WizardSurvival.Spells;

namespace VraiPseudoSae.view.WizardSurvival.Rendering;

/// <summary>
/// Draws lightweight WPF shapes for effects that are too dynamic to be IUTGame sprites.
/// </summary>
public sealed class WizardEffectsRenderer
{
    private readonly Canvas layer;

    public WizardEffectsRenderer(Canvas layer)
    {
        this.layer = layer;
    }

    public void Render(WizardSurvivalGame game)
    {
        layer.Children.Clear();

        DrawShield(game);
        DrawLaser(game);

        foreach (var effect in game.Effects)
        {
            if (effect is ParticleEffect particleEffect)
                DrawParticles(particleEffect, game.Camera);
            else if (effect is CelestialStrikeEffect strike)
                DrawCelestial(strike, game.Camera);
        }
    }

    private void DrawParticles(ParticleEffect effect, Camera2D camera)
    {
        foreach (Particle particle in effect.Particles)
        {
            Ellipse dot = new()
            {
                Width = particle.Size,
                Height = particle.Size,
                Fill = PickBrush(particle.Palette),
                Opacity = System.Math.Max(0.15, particle.Life)
            };
            Canvas.SetLeft(dot, particle.X - camera.X);
            Canvas.SetTop(dot, particle.Y - camera.Y);
            layer.Children.Add(dot);
        }
    }

    private void DrawCelestial(CelestialStrikeEffect strike, Camera2D camera)
    {
        double x = strike.CenterX - camera.X;
        double y = strike.CenterY - camera.Y;

        Line lightning = new()
        {
            X1 = x,
            X2 = x + 18 * (1 - strike.LightningProgress),
            Y1 = -40,
            Y2 = -40 + (y + 40) * strike.LightningProgress,
            Stroke = Brushes.White,
            StrokeThickness = 4,
            Opacity = 0.9
        };
        layer.Children.Add(lightning);

        if (!strike.CircleStarted)
            return;

        Ellipse circle = new()
        {
            Width = strike.Radius * 2,
            Height = strike.Radius * 2,
            Stroke = Brushes.MediumPurple,
            StrokeThickness = 3,
            Fill = new SolidColorBrush(Color.FromArgb(35, 160, 110, 255))
        };
        Canvas.SetLeft(circle, x - strike.Radius);
        Canvas.SetTop(circle, y - strike.Radius);
        layer.Children.Add(circle);

        Ellipse inner = new()
        {
            Width = strike.Radius,
            Height = strike.Radius,
            Stroke = Brushes.DeepSkyBlue,
            StrokeThickness = 1,
            Opacity = 0.8
        };
        Canvas.SetLeft(inner, x - strike.Radius / 2.0);
        Canvas.SetTop(inner, y - strike.Radius / 2.0);
        layer.Children.Add(inner);
    }

    private void DrawShield(WizardSurvivalGame game)
    {
        ShieldSpell shield = game.Shield;
        if (!shield.IsActive || game.Player is null)
            return;

        double x = game.Player.CenterX - game.Camera.X;
        double y = game.Player.CenterY - game.Camera.Y;
        Ellipse shell = new()
        {
            Width = shield.Radius * 2,
            Height = shield.Radius * 2,
            Stroke = Brushes.Cyan,
            StrokeThickness = 3,
            Fill = new SolidColorBrush(Color.FromArgb(28, 0, 210, 255)),
            Opacity = 0.85
        };
        Canvas.SetLeft(shell, x - shield.Radius);
        Canvas.SetTop(shell, y - shield.Radius);
        layer.Children.Add(shell);
    }

    private void DrawLaser(WizardSurvivalGame game)
    {
        LaserSpell laser = game.Laser;
        if (!laser.IsActive || game.Player is null)
            return;

        double x = game.Player.CenterX - game.Camera.X;
        double y = game.Player.CenterY - game.Camera.Y;

        if (laser.IsCharging)
        {
            double size = 34 + 28 * laser.ChargeProgress;
            Ellipse charge = new()
            {
                Width = size,
                Height = size,
                Stroke = Brushes.Orange,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(40, 255, 180, 0))
            };
            Canvas.SetLeft(charge, x - size / 2);
            Canvas.SetTop(charge, y - size / 2);
            layer.Children.Add(charge);
            return;
        }

        if (!laser.IsFiring)
            return;

        double beamLength = 520;
        double left = laser.Direction == FacingDirection.Right ? x : x - beamLength;
        Rectangle beam = new()
        {
            Width = beamLength,
            Height = laser.Width * 2,
            Fill = new LinearGradientBrush(
                Color.FromArgb(220, 255, 70, 20),
                Color.FromArgb(180, 255, 230, 40),
                0),
            Opacity = 0.9
        };
        Canvas.SetLeft(beam, left);
        Canvas.SetTop(beam, y - laser.Width);
        layer.Children.Add(beam);

        Rectangle core = new()
        {
            Width = beamLength,
            Height = 8,
            Fill = Brushes.White,
            Opacity = 0.75
        };
        Canvas.SetLeft(core, left);
        Canvas.SetTop(core, y - 4);
        layer.Children.Add(core);
    }

    private static Brush PickBrush(string palette) =>
        palette switch
        {
            "fire" => Brushes.OrangeRed,
            "laser" => Brushes.Gold,
            "celestial" => Brushes.MediumPurple,
            "death" => Brushes.LimeGreen,
            "hurt" => Brushes.Red,
            _ => Brushes.White
        };
}
