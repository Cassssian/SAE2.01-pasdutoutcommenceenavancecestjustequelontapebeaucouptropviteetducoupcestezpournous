using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using VraiPseudoSae.data.GoalExplosion;
using VraiPseudoSae.Utils.AudioPlayer;

namespace VraiPseudoSae.view.RLS_Pages.GoalExplosions
{
    public partial class GoalExplosion_AnimeSmoke : GoalExplosionBase, IDisposable
    {
        private readonly string _audioKey = "goal_sound/AnimeSmoke";
        private readonly Random _rng = new Random();

        public GoalExplosion_AnimeSmoke(Canvas gameCanvas, JsonPakAudioService audio) : base(gameCanvas, audio)
        {
            InitializeComponent();
            _audio?.Preload(_audioKey, "goal_animesmoke");
        }

        public override void PlayLeftGoal()
        {
            PlayAt(new Point(60, 430));
        }

        public override void PlayRightGoal()
        {
            PlayAt(new Point(940, 430));
        }

        private void PlayAt(Point center)
        {
            ResetVisualState();
            PositionElements(center);
            CreateParticles(center);
            AnimateCore();
            AnimateSmoke(center);
            AnimateStars(center);
            _audio?.Play("goal_animesmoke");
        }

        private void ResetVisualState()
        {
            ParticlesLayer.Children.Clear();
            RingMain.Opacity = 0;
            RingSecondary.Opacity = 0;
            Smoke1.Opacity = 0;
            Smoke2.Opacity = 0;
            Smoke3.Opacity = 0;
            Star1.Opacity = 0;
            Star2.Opacity = 0;
            Star3.Opacity = 0;

            RingMainScale.ScaleX = 1;
            RingMainScale.ScaleY = 1;

            RingSecondaryScale.ScaleX = 1;
            RingSecondaryScale.ScaleY = 1;

            Smoke1Scale.ScaleX = 0.7;
            Smoke1Scale.ScaleY = 0.7;

            Smoke2Scale.ScaleX = 0.65;
            Smoke2Scale.ScaleY = 0.65;

            Smoke3Scale.ScaleX = 0.6;
            Smoke3Scale.ScaleY = 0.6;

            Star1Scale.ScaleX = 0.4;
            Star1Scale.ScaleY = 0.4;
            Star1Rotate.Angle = 0;

            Star2Scale.ScaleX = 0.4;
            Star2Scale.ScaleY = 0.4;
            Star2Rotate.Angle = 0;

            Star3Scale.ScaleX = 0.4;
            Star3Scale.ScaleY = 0.4;
            Star3Rotate.Angle = 0;
        }

        private void PositionElements(Point center)
        {
            Canvas.SetLeft(RingMain, center.X - 32);
            Canvas.SetTop(RingMain, center.Y - 32);

            Canvas.SetLeft(RingSecondary, center.X - 27);
            Canvas.SetTop(RingSecondary, center.Y - 27);

            Canvas.SetLeft(Smoke1, center.X - 55);
            Canvas.SetTop(Smoke1, center.Y - 55);

            Canvas.SetLeft(Smoke2, center.X - 75);
            Canvas.SetTop(Smoke2, center.Y - 75);

            Canvas.SetLeft(Smoke3, center.X - 95);
            Canvas.SetTop(Smoke3, center.Y - 95);

            Canvas.SetLeft(Star1, center.X - 10);
            Canvas.SetTop(Star1, center.Y - 10);

            Canvas.SetLeft(Star2, center.X + 48);
            Canvas.SetTop(Star2, center.Y - 28);

            Canvas.SetLeft(Star3, center.X - 62);
            Canvas.SetTop(Star3, center.Y + 14);
        }

        private void AnimateCore()
        {
            var easeOut = new CubicEase { EasingMode = EasingMode.EaseOut };
            var easeSoft = new QuadraticEase { EasingMode = EasingMode.EaseOut };

            var ringStoryboard = new Storyboard();

            var ringMainOpacityIn = new DoubleAnimation
            {
                From = 0,
                To = 0.95,
                Duration = TimeSpan.FromMilliseconds(80)
            };
            Storyboard.SetTarget(ringMainOpacityIn, RingMain);
            Storyboard.SetTargetProperty(ringMainOpacityIn, new PropertyPath("Opacity"));
            ringStoryboard.Children.Add(ringMainOpacityIn);

            var ringMainOpacityOut = new DoubleAnimation
            {
                From = 0.95,
                To = 0,
                BeginTime = TimeSpan.FromMilliseconds(80),
                Duration = TimeSpan.FromMilliseconds(520)
            };
            Storyboard.SetTarget(ringMainOpacityOut, RingMain);
            Storyboard.SetTargetProperty(ringMainOpacityOut, new PropertyPath("Opacity"));
            ringStoryboard.Children.Add(ringMainOpacityOut);

            var ringMainScaleX = new DoubleAnimation
            {
                From = 0.35,
                To = 4.2,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = easeOut
            };
            Storyboard.SetTarget(ringMainScaleX, RingMainScale);
            Storyboard.SetTargetProperty(ringMainScaleX, new PropertyPath("ScaleX"));
            ringStoryboard.Children.Add(ringMainScaleX);

            var ringMainScaleY = ringMainScaleX.Clone();
            Storyboard.SetTarget(ringMainScaleY, RingMainScale);
            Storyboard.SetTargetProperty(ringMainScaleY, new PropertyPath("ScaleY"));
            ringStoryboard.Children.Add(ringMainScaleY);

            var ringSecondaryOpacityIn = new DoubleAnimation
            {
                From = 0,
                To = 0.85,
                BeginTime = TimeSpan.FromMilliseconds(40),
                Duration = TimeSpan.FromMilliseconds(120)
            };
            Storyboard.SetTarget(ringSecondaryOpacityIn, RingSecondary);
            Storyboard.SetTargetProperty(ringSecondaryOpacityIn, new PropertyPath("Opacity"));
            ringStoryboard.Children.Add(ringSecondaryOpacityIn);

            var ringSecondaryOpacityOut = new DoubleAnimation
            {
                From = 0.85,
                To = 0,
                BeginTime = TimeSpan.FromMilliseconds(140),
                Duration = TimeSpan.FromMilliseconds(460)
            };
            Storyboard.SetTarget(ringSecondaryOpacityOut, RingSecondary);
            Storyboard.SetTargetProperty(ringSecondaryOpacityOut, new PropertyPath("Opacity"));
            ringStoryboard.Children.Add(ringSecondaryOpacityOut);

            var ringSecondaryScaleX = new DoubleAnimation
            {
                From = 0.45,
                To = 3.7,
                BeginTime = TimeSpan.FromMilliseconds(40),
                Duration = TimeSpan.FromMilliseconds(560),
                EasingFunction = easeSoft
            };
            Storyboard.SetTarget(ringSecondaryScaleX, RingSecondaryScale);
            Storyboard.SetTargetProperty(ringSecondaryScaleX, new PropertyPath("ScaleX"));
            ringStoryboard.Children.Add(ringSecondaryScaleX);

            var ringSecondaryScaleY = ringSecondaryScaleX.Clone();
            Storyboard.SetTarget(ringSecondaryScaleY, RingSecondaryScale);
            Storyboard.SetTargetProperty(ringSecondaryScaleY, new PropertyPath("ScaleY"));
            ringStoryboard.Children.Add(ringSecondaryScaleY);

            ringStoryboard.Begin();
        }

        private void AnimateSmoke(Point center)
        {
            AnimateSmokeElement(Smoke1, Smoke1Scale, center.X - 55, center.Y - 55, center.Y - 90, 0.70, 2.1, 40, 820);
            AnimateSmokeElement(Smoke2, Smoke2Scale, center.X - 75, center.Y - 75, center.Y - 120, 0.50, 2.35, 80, 980);
            AnimateSmokeElement(Smoke3, Smoke3Scale, center.X - 95, center.Y - 95, center.Y - 165, 0.36, 2.6, 120, 1200);
        }

        private void AnimateSmokeElement(
            Ellipse target,
            ScaleTransform scale,
            double startLeft,
            double startTop,
            double endTop,
            double maxOpacity,
            double maxScale,
            int delayMs,
            int durationMs)
        {
            var sb = new Storyboard();

            var opacityIn = new DoubleAnimation
            {
                From = 0,
                To = maxOpacity,
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                Duration = TimeSpan.FromMilliseconds(150)
            };
            Storyboard.SetTarget(opacityIn, target);
            Storyboard.SetTargetProperty(opacityIn, new PropertyPath("Opacity"));
            sb.Children.Add(opacityIn);

            var opacityOut = new DoubleAnimation
            {
                From = maxOpacity,
                To = 0,
                BeginTime = TimeSpan.FromMilliseconds(delayMs + 150),
                Duration = TimeSpan.FromMilliseconds(durationMs - 150)
            };
            Storyboard.SetTarget(opacityOut, target);
            Storyboard.SetTargetProperty(opacityOut, new PropertyPath("Opacity"));
            sb.Children.Add(opacityOut);

            var scaleX = new DoubleAnimation
            {
                From = scale.ScaleX,
                To = maxScale,
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleX, scale);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("ScaleX"));
            sb.Children.Add(scaleX);

            var scaleY = scaleX.Clone();
            Storyboard.SetTarget(scaleY, scale);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("ScaleY"));
            sb.Children.Add(scaleY);

            var topAnim = new DoubleAnimation
            {
                From = startTop,
                To = endTop,
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(topAnim, target);
            Storyboard.SetTargetProperty(topAnim, new PropertyPath("(Canvas.Top)"));
            sb.Children.Add(topAnim);

            var leftAnim = new DoubleAnimation
            {
                From = startLeft,
                To = startLeft - 18,
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(leftAnim, target);
            Storyboard.SetTargetProperty(leftAnim, new PropertyPath("(Canvas.Left)"));
            sb.Children.Add(leftAnim);

            sb.Begin();
        }

        private void AnimateStars(Point center)
        {
            AnimateStar(Star1, Star1Scale, Star1Rotate, center.X - 10, center.Y - 10, center.X + 26, center.Y - 58, 140, 50, 520);
            AnimateStar(Star2, Star2Scale, Star2Rotate, center.X + 48, center.Y - 28, center.X + 100, center.Y - 78, -180, 90, 540);
            AnimateStar(Star3, Star3Scale, Star3Rotate, center.X - 62, center.Y + 14, center.X - 122, center.Y + 56, 200, 120, 560);
        }

        private void AnimateStar(
            Polygon star,
            ScaleTransform scale,
            RotateTransform rotate,
            double fromLeft,
            double fromTop,
            double toLeft,
            double toTop,
            double angle,
            int delayMs,
            int durationMs)
        {
            var sb = new Storyboard();

            var opacityIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                Duration = TimeSpan.FromMilliseconds(80)
            };
            Storyboard.SetTarget(opacityIn, star);
            Storyboard.SetTargetProperty(opacityIn, new PropertyPath("Opacity"));
            sb.Children.Add(opacityIn);

            var opacityOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                BeginTime = TimeSpan.FromMilliseconds(delayMs + 80),
                Duration = TimeSpan.FromMilliseconds(durationMs - 80)
            };
            Storyboard.SetTarget(opacityOut, star);
            Storyboard.SetTargetProperty(opacityOut, new PropertyPath("Opacity"));
            sb.Children.Add(opacityOut);

            var scaleX = new DoubleAnimation
            {
                From = 0.3,
                To = 2.2,
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 }
            };
            Storyboard.SetTarget(scaleX, scale);
            Storyboard.SetTargetProperty(scaleX, new PropertyPath("ScaleX"));
            sb.Children.Add(scaleX);

            var scaleY = scaleX.Clone();
            Storyboard.SetTarget(scaleY, scale);
            Storyboard.SetTargetProperty(scaleY, new PropertyPath("ScaleY"));
            sb.Children.Add(scaleY);

            var rotateAnim = new DoubleAnimation
            {
                From = 0,
                To = angle,
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(rotateAnim, rotate);
            Storyboard.SetTargetProperty(rotateAnim, new PropertyPath("Angle"));
            sb.Children.Add(rotateAnim);

            var leftAnim = new DoubleAnimation
            {
                From = fromLeft,
                To = toLeft,
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(leftAnim, star);
            Storyboard.SetTargetProperty(leftAnim, new PropertyPath("(Canvas.Left)"));
            sb.Children.Add(leftAnim);

            var topAnim = new DoubleAnimation
            {
                From = fromTop,
                To = toTop,
                BeginTime = TimeSpan.FromMilliseconds(delayMs),
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(topAnim, star);
            Storyboard.SetTargetProperty(topAnim, new PropertyPath("(Canvas.Top)"));
            sb.Children.Add(topAnim);

            sb.Begin();
        }

        private void CreateParticles(Point center)
        {
            ParticlesLayer.Children.Clear();

            for (int i = 0; i < 22; i++)
            {
                var p = new Ellipse
                {
                    Width = _rng.Next(6, 14),
                    Height = _rng.Next(6, 14),
                    Opacity = 0,
                    Fill = new SolidColorBrush(PickParticleColor())
                };

                Canvas.SetLeft(p, center.X);
                Canvas.SetTop(p, center.Y);
                ParticlesLayer.Children.Add(p);

                double angle = _rng.NextDouble() * Math.PI * 2;
                double distance = 70 + _rng.NextDouble() * 120;
                double dx = Math.Cos(angle) * distance;
                double dy = Math.Sin(angle) * distance - _rng.Next(10, 40);

                int delay = _rng.Next(20, 180);
                int duration = _rng.Next(450, 850);

                var scale = new ScaleTransform(1, 1, p.Width / 2, p.Height / 2);
                p.RenderTransform = scale;

                var sb = new Storyboard();

                var opacityIn = new DoubleAnimation
                {
                    From = 0,
                    To = 0.95,
                    BeginTime = TimeSpan.FromMilliseconds(delay),
                    Duration = TimeSpan.FromMilliseconds(90)
                };
                Storyboard.SetTarget(opacityIn, p);
                Storyboard.SetTargetProperty(opacityIn, new PropertyPath("Opacity"));
                sb.Children.Add(opacityIn);

                var opacityOut = new DoubleAnimation
                {
                    From = 0.95,
                    To = 0,
                    BeginTime = TimeSpan.FromMilliseconds(delay + 120),
                    Duration = TimeSpan.FromMilliseconds(duration)
                };
                Storyboard.SetTarget(opacityOut, p);
                Storyboard.SetTargetProperty(opacityOut, new PropertyPath("Opacity"));
                sb.Children.Add(opacityOut);

                var leftAnim = new DoubleAnimation
                {
                    From = center.X,
                    To = center.X + dx,
                    BeginTime = TimeSpan.FromMilliseconds(delay),
                    Duration = TimeSpan.FromMilliseconds(duration),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(leftAnim, p);
                Storyboard.SetTargetProperty(leftAnim, new PropertyPath("(Canvas.Left)"));
                sb.Children.Add(leftAnim);

                var topAnim = new DoubleAnimation
                {
                    From = center.Y,
                    To = center.Y + dy,
                    BeginTime = TimeSpan.FromMilliseconds(delay),
                    Duration = TimeSpan.FromMilliseconds(duration),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(topAnim, p);
                Storyboard.SetTargetProperty(topAnim, new PropertyPath("(Canvas.Top)"));
                sb.Children.Add(topAnim);

                var scaleX = new DoubleAnimation
                {
                    From = 0.5,
                    To = 1.6,
                    BeginTime = TimeSpan.FromMilliseconds(delay),
                    Duration = TimeSpan.FromMilliseconds(duration),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(scaleX, scale);
                Storyboard.SetTargetProperty(scaleX, new PropertyPath("ScaleX"));
                sb.Children.Add(scaleX);

                var scaleY = scaleX.Clone();
                Storyboard.SetTarget(scaleY, scale);
                Storyboard.SetTargetProperty(scaleY, new PropertyPath("ScaleY"));
                sb.Children.Add(scaleY);

                sb.Begin();
            }
        }

        private Color PickParticleColor()
        {
            Color[] colors =
            {
                (Color)ColorConverter.ConvertFromString("#FFF6D365"),
                (Color)ColorConverter.ConvertFromString("#FFFF8CEB"),
                (Color)ColorConverter.ConvertFromString("#FF7EE7FF"),
                (Color)ColorConverter.ConvertFromString("#FFD6A4FF"),
                (Color)ColorConverter.ConvertFromString("#FFC8FFD6")
            };

            return colors[_rng.Next(colors.Length)];
        }

        public void Dispose()
        {
            // Rien de spécial ici
        }
    }
}