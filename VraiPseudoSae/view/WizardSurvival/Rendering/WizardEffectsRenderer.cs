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
            AddPixel(
                particle.X - camera.X,
                particle.Y - camera.Y,
                particle.Size,
                PickBrush(particle.Palette),
                System.Math.Max(0.2, particle.Life));
        }
    }

    private void DrawCelestial(CelestialStrikeEffect strike, Camera2D camera)
    {
        double x = strike.CenterX - camera.X;
        double y = strike.CenterY - camera.Y;

        DrawJaggedLightning(x, y, strike.LightningProgress);

        if (!strike.CircleStarted)
            return;

        DrawPixelCircle(x, y, strike.Radius, Brushes.MediumPurple, 4, 0);
        DrawPixelCircle(x, y, strike.Radius * 0.62, Brushes.DeepSkyBlue, 3, strike.Age * 3.5);
        DrawPixelCircle(x, y, strike.Radius * 0.34, Brushes.White, 2, -strike.Age * 4);

        for (int i = 0; i < 14; i++)
        {
            double angle = strike.Age * 5 + i * System.Math.PI * 2 / 14;
            double radius = strike.Radius + 8 + (i % 3) * 4;
            AddPixel(
                x + System.Math.Cos(angle) * radius,
                y + System.Math.Sin(angle) * radius,
                4,
                i % 2 == 0 ? Brushes.MediumPurple : Brushes.White,
                0.85);
        }
    }

    private void DrawJaggedLightning(double x, double targetY, double progress)
    {
        double endY = -36 + (targetY + 36) * progress;
        double previousX = x;
        double previousY = -36;

        for (int i = 1; i <= 12; i++)
        {
            double t = i / 12.0;
            double nextY = -36 + (endY + 36) * t;
            double nextX = x + ((i % 2 == 0 ? -1 : 1) * (14 - i));
            DrawPixelLine(previousX, previousY, nextX, nextY, Brushes.White, 4);
            DrawPixelLine(previousX + 3, previousY, nextX + 3, nextY, Brushes.Gold, 2);
            previousX = nextX;
            previousY = nextY;
        }
    }

    private void DrawPixelCircle(double x, double y, double radius, Brush brush, double size, double phase)
    {
        int count = System.Math.Max(20, (int)(radius / 2.5));
        for (int i = 0; i < count; i++)
        {
            double angle = phase + i * System.Math.PI * 2 / count;
            AddPixel(
                x + System.Math.Cos(angle) * radius,
                y + System.Math.Sin(angle) * radius,
                size,
                brush,
                0.85);
        }
    }

    private void DrawShield(WizardSurvivalGame game)
    {
        ShieldSpell shield = game.Shield;
        if (!shield.IsActive || game.Player is null)
            return;

        double x = game.Player.CenterX - game.Camera.X;
        double y = game.Player.CenterY - game.Camera.Y;
        double radius = shield.EffectiveRadius(game.Player);
        DrawPixelCircle(x, y, radius, Brushes.Cyan, 4, shield.ActiveRemaining * 3);
        DrawPixelCircle(x, y, radius * 0.76, Brushes.DeepSkyBlue, 3, -shield.ActiveRemaining * 4);

        for (int i = 0; i < 10; i++)
        {
            double angle = shield.ActiveRemaining * 5 + i * System.Math.PI * 2 / 10;
            AddPixel(
                x + System.Math.Cos(angle) * (radius - 10),
                y + System.Math.Sin(angle) * (radius - 10),
                5,
                Brushes.White,
                0.65);
        }
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
            for (int i = 0; i < 16; i++)
            {
                double angle = i * System.Math.PI * 2 / 16 + laser.ChargeProgress * 4;
                AddPixel(
                    x + System.Math.Cos(angle) * size / 2,
                    y + System.Math.Sin(angle) * size / 2,
                    5,
                    i % 2 == 0 ? Brushes.OrangeRed : Brushes.Gold,
                    0.9);
            }
            return;
        }

        if (!laser.IsFiring)
            return;

        double beamLength = laser.BaseBeamLength * game.Player.RangeMultiplier;
        double left = laser.Direction == FacingDirection.Right ? x : x - beamLength;
        AddRect(left, y - laser.Width, beamLength, laser.Width * 2, Brushes.OrangeRed, 0.75);
        AddRect(left, y - 10, beamLength, 20, Brushes.Gold, 0.85);
        AddRect(left, y - 4, beamLength, 8, Brushes.White, 0.8);

        for (int i = 0; i < 34; i++)
        {
            double px = left + i * 16;
            AddPixel(px, y - laser.Width - 4, 4, Brushes.Gold, 0.9);
            AddPixel(px + 8, y + laser.Width, 4, Brushes.OrangeRed, 0.9);
        }
    }

    private void DrawPixelLine(double x1, double y1, double x2, double y2, Brush brush, double size)
    {
        double dx = x2 - x1;
        double dy = y2 - y1;
        int steps = System.Math.Max(1, (int)(System.Math.Sqrt(dx * dx + dy * dy) / size));
        for (int i = 0; i <= steps; i++)
        {
            double t = i / (double)steps;
            AddPixel(x1 + dx * t, y1 + dy * t, size, brush, 0.95);
        }
    }

    private void AddPixel(double x, double y, double size, Brush brush, double opacity) =>
        AddRect(System.Math.Round(x), System.Math.Round(y), size, size, brush, opacity);

    private void AddRect(double x, double y, double width, double height, Brush brush, double opacity)
    {
        Rectangle rectangle = new()
        {
            Width = width,
            Height = height,
            Fill = brush,
            Opacity = opacity,
            SnapsToDevicePixels = true
        };
        RenderOptions.SetEdgeMode(rectangle, EdgeMode.Aliased);
        Canvas.SetLeft(rectangle, x);
        Canvas.SetTop(rectangle, y);
        layer.Children.Add(rectangle);
    }

    private static Brush PickBrush(string palette) =>
        palette switch
        {
            "fire" => Brushes.OrangeRed,
            "laser" => Brushes.Gold,
            "celestial" => Brushes.MediumPurple,
            "death" => Brushes.LimeGreen,
            "hurt" => Brushes.Red,
            "lake_buff" => Brushes.SpringGreen,
            "lake_nerf" => Brushes.IndianRed,
            _ => Brushes.White
        };
}
