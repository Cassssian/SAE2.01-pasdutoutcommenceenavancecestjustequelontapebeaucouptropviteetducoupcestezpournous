using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace VraiPseudoSae.data.GoalExplosion
{
    public sealed class GoalExplosionFlashController
    {
        public sealed class CarPalette
        {
            public Color BaseColor { get; set; }
            public Color AccentColor { get; set; }

            public CarPalette() { }

            public CarPalette(Color baseColor, Color accentColor)
            {
                BaseColor = baseColor;
                AccentColor = accentColor;
            }
        }

        private readonly Rectangle _whiteOverlay;
        private readonly Rectangle _powderOverlay;
        private readonly SolidColorBrush _powderFill;
        private readonly Ellipse _maskEllipse;
        private readonly RadialGradientBrush _powderMaskBrush;

        public CarPalette LeftCarPalette { get; private set; } =
            new CarPalette(
                (Color)ColorConverter.ConvertFromString("#FFE94560"),
                (Color)ColorConverter.ConvertFromString("#FFB83048"));

        public CarPalette RightCarPalette { get; private set; } =
            new CarPalette(
                (Color)ColorConverter.ConvertFromString("#FF0F9D58"),
                (Color)ColorConverter.ConvertFromString("#FF0A6E3E"));

        public GoalExplosionFlashController(
            Rectangle whiteOverlay,
            Rectangle powderOverlay,
            SolidColorBrush powderFill,
            RadialGradientBrush powderMaskBrush)
        {
            _whiteOverlay = whiteOverlay;
            _powderOverlay = powderOverlay;
            _powderFill = powderFill;
            _powderMaskBrush = powderMaskBrush;
        }

        public void SetCarPalettes(CarPalette leftPalette, CarPalette rightPalette)
        {
            LeftCarPalette = leftPalette;
            RightCarPalette = rightPalette;
        }

        public void PlayP2GoalFlash()
        {
            // But à gauche => point P2 => couleur voiture droite
            PlayPowderFlash(0, 365, RightCarPalette);
        }

        public void PlayP1GoalFlash()
        {
            // But à droite => point P1 => couleur voiture gauche
            PlayPowderFlash(974, 365, LeftCarPalette);
        }

        private void PlayPowderFlash(double goalX, double goalY, CarPalette palette)
        {
            _powderFill.Color = LerpColor(palette.AccentColor, palette.BaseColor, 0.28);

            double relX = goalX / 1000.0;
            double relY = goalY / 520.0;

            _powderMaskBrush.BeginAnimation(RadialGradientBrush.RadiusXProperty, null);
            _powderMaskBrush.BeginAnimation(RadialGradientBrush.RadiusYProperty, null);

            _powderMaskBrush.Center = new Point(relX, relY);
            _powderMaskBrush.GradientOrigin = new Point(relX, relY);
            _powderMaskBrush.RadiusX = 0.03;
            _powderMaskBrush.RadiusY = 0.06;

            PlayWhiteFlash();
            PlayPowderFlashOverlay();
            ExpandMask();
        }

        private void PlayWhiteFlash()
        {
            _whiteOverlay.BeginAnimation(UIElement.OpacityProperty, null);
            _whiteOverlay.Opacity = 0.88;

            var anim = new DoubleAnimation
            {
                From = 0.88,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.Stop
            };

            anim.Completed += (_, __) => _whiteOverlay.Opacity = 0.0;

            _whiteOverlay.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void PlayPowderFlashOverlay()
        {
            _powderOverlay.Opacity = 0.78;

            var anim = new DoubleAnimation
            {
                From = 0.78,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(1220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            _powderOverlay.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void ExpandMask()
        {
            var radiusXAnim = new DoubleAnimation
            {
                From = 0.03,
                To = 1.25,
                Duration = TimeSpan.FromMilliseconds(780),
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };

            var radiusYAnim = new DoubleAnimation
            {
                From = 0.06,
                To = 1.65,
                Duration = TimeSpan.FromMilliseconds(780),
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };

            _powderMaskBrush.BeginAnimation(RadialGradientBrush.RadiusXProperty, radiusXAnim);
            _powderMaskBrush.BeginAnimation(RadialGradientBrush.RadiusYProperty, radiusYAnim);
        }
        
        private static Color LerpColor(Color from, Color to, double t)
        {
            byte a = (byte)(from.A + (to.A - from.A) * t);
            byte r = (byte)(from.R + (to.R - from.R) * t);
            byte g = (byte)(from.G + (to.G - from.G) * t);
            byte b = (byte)(from.B + (to.B - from.B) * t);
            return Color.FromArgb(a, r, g, b);
        }
    }
}