using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using VraiPseudoSae.data.GoalExplosion;
using VraiPseudoSae.Utils.AudioPlayer;

namespace VraiPseudoSae.view.RLS_Pages.GoalExplosions
{
    public partial class GoalExplosion_Ballistic : GoalExplosionBase, IDisposable
    {
        private readonly string _audioKey = "goal_sound/Ballistic";
        private readonly Random _rng = new Random();

        public GoalExplosion_Ballistic(Canvas gameCanvas, JsonPakAudioService audio) : base(gameCanvas, audio)
        {
            InitializeComponent();
            _audio?.Preload(_audioKey, "goal_ballistic");
        }

        public override void PlayLeftGoal()
        {
            PlayAt(new Point(60, 430));
        }

        public override void PlayRightGoal()
        {
            PlayAt(new Point(940, 430));
        }

        private void PlayAt(Point goalCenter)
        {
            ResetVisualState();

            CreateMissileBarrage(goalCenter);

            _audio?.Play("goal_ballistic");
        }

        private void ResetVisualState()
        {
            TrailsLayer.Children.Clear();
            ParticlesLayer.Children.Clear();

            MissileA.Opacity = 0;
            MissileB.Opacity = 0;
            MissileC.Opacity = 0;

            ImpactFlash.Opacity = 0;
            ShockRing.Opacity = 0;
            SmokeCloud.Opacity = 0;
            GroundBlast.Opacity = 0;

            ImpactFlashScale.ScaleX = 0.25;
            ImpactFlashScale.ScaleY = 0.25;

            ShockRingScale.ScaleX = 0.2;
            ShockRingScale.ScaleY = 0.2;

            SmokeCloudScale.ScaleX = 0.5;
            SmokeCloudScale.ScaleY = 0.5;

            GroundBlastScale.ScaleX = 0.4;
            GroundBlastScale.ScaleY = 0.4;
        }

        private void CreateMissileBarrage(Point goalCenter)
        {
            int missileCount = _rng.Next(5, 7); // 5 ou 6 missiles

            double spawnCenterX = 500;   // milieu du terrain
            double spawnY = -90;         // ciel
            double groundY = 500;        // niveau du sol proche du but

            for (int i = 0; i < missileCount; i++)
            {
                double startX = spawnCenterX + _rng.Next(-90, 91);

                double targetX = goalCenter.X + _rng.Next(-55, 56);
                double targetY = groundY + _rng.Next(-10, 18);

                int delay = i * 130 + _rng.Next(0, 40);
                int duration = _rng.Next(500, 760);

                CreateSingleMissile(
                    new Point(startX, spawnY),
                    new Point(targetX, targetY),
                    delay,
                    duration,
                    i == missileCount - 1
                );
            }
        }

        private void CreateSingleMissile(Point start, Point impact, int delayMs, int durationMs, bool heavierImpact)
        {
            var missile = BuildMissileVisual();

            Canvas.SetLeft(missile, start.X);
            Canvas.SetTop(missile, start.Y);
            missile.Opacity = 0;
            ParticlesLayer.Children.Add(missile);

            double dx = impact.X - start.X;
            double dy = impact.Y - start.Y;
            double angle = Math.Atan2(dy, dx) * 180 / Math.PI;

            if (missile.RenderTransform is TransformGroup tg &&
                tg.Children[0] is RotateTransform rotate)
            {
                rotate.Angle = angle + _rng.Next(-5, 6);
            }

            CreateTrail(start, impact, delayMs, durationMs, heavierImpact);

            AnimateOpacity(missile, 0, 1, 60, delayMs);
            AnimateOpacity(missile, 1, 1, durationMs - 50, delayMs + 60);
            AnimateOpacity(missile, 1, 0, 50, delayMs + durationMs - 20);

            AnimateCanvasLeft(missile, start.X, impact.X - 26, durationMs, delayMs);
            AnimateCanvasTop(missile, start.Y, impact.Y - 10, durationMs, delayMs);

            int impactDelay = delayMs + durationMs - 30;
            CreateImpactAt(impact, impactDelay, heavierImpact);
        }

        private Canvas BuildMissileVisual()
        {
            var missile = new Canvas
            {
                Width = 64,
                Height = 24,
                RenderTransformOrigin = new Point(0.5, 0.5),
                IsHitTestVisible = false
            };

            var tg = new TransformGroup();
            tg.Children.Add(new RotateTransform());
            tg.Children.Add(new ScaleTransform(1, 1));
            missile.RenderTransform = tg;

            var body = new Rectangle
            {
                Width = 34,
                Height = 10,
                RadiusX = 3,
                RadiusY = 3,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6D6D6D")),
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            Canvas.SetLeft(body, 14);
            Canvas.SetTop(body, 7);

            var nose = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(48, 7),
                    new Point(62, 12),
                    new Point(48, 17)
                },
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF6F00")),
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            var tail = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(10, 12),
                    new Point(0, 4),
                    new Point(0, 20)
                },
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCFD8DC")),
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            var finA = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(18, 8),
                    new Point(10, 2),
                    new Point(12, 10)
                },
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF7043"))
            };

            var finB = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(18, 16),
                    new Point(10, 22),
                    new Point(12, 14)
                },
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF7043"))
            };

            missile.Children.Add(body);
            missile.Children.Add(nose);
            missile.Children.Add(tail);
            missile.Children.Add(finA);
            missile.Children.Add(finB);

            return missile;
        }

        private void CreateTrail(Point start, Point impact, int delayMs, int durationMs, bool heavierImpact)
        {
            var trail = new Line
            {
                X1 = start.X + 20,
                Y1 = start.Y + 8,
                X2 = impact.X,
                Y2 = impact.Y,
                Stroke = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString("#00FFB74D"),
                    (Color)ColorConverter.ConvertFromString(heavierImpact ? "#EEFF3D00" : "#CCFF7043"),
                    new Point(0, 0),
                    new Point(1, 1)),
                StrokeThickness = heavierImpact ? 7 : 5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Opacity = 0
            };

            TrailsLayer.Children.Add(trail);

            AnimateOpacity(trail, 0, 0.9, 100, delayMs + 30);
            AnimateOpacity(trail, 0.9, 0, 280, delayMs + durationMs - 120);
        }

        private void CreateImpactAt(Point impact, int delayMs, bool heavierImpact)
        {
            CreateImpactFlashAt(impact, delayMs, heavierImpact);
            CreateSmokeAt(impact, delayMs + 30, heavierImpact);
            CreateGroundBlastAt(impact, delayMs + 20, heavierImpact);
            CreateExplosionDebrisAt(impact, delayMs + 10, heavierImpact);
            CreateSparkBurstsAt(impact, delayMs + 10, heavierImpact);
        }

        private void CreateImpactFlashAt(Point impact, int delayMs, bool heavierImpact)
        {
            var flash = new Ellipse
            {
                Width = heavierImpact ? 58 : 46,
                Height = heavierImpact ? 58 : 46,
                Opacity = 0,
                RenderTransformOrigin = new Point(0.5, 0.5),
                Fill = new RadialGradientBrush
                {
                    GradientOrigin = new Point(0.35, 0.35),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Colors.White, 0),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#FFFFF59D"), 0.45),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#00FFF59D"), 1)
                    }
                },
                RenderTransform = new ScaleTransform(0.2, 0.2)
            };

            Canvas.SetLeft(flash, impact.X - flash.Width / 2);
            Canvas.SetTop(flash, impact.Y - flash.Height / 2);
            ParticlesLayer.Children.Add(flash);

            var scale = (ScaleTransform)flash.RenderTransform;

            AnimateOpacity(flash, 0, 1, 55, delayMs);
            AnimateOpacity(flash, 1, 0, heavierImpact ? 240 : 180, delayMs + 55);
            AnimateScale(scale, 0.2, heavierImpact ? 5.4 : 4.2, heavierImpact ? 320 : 240,
                new CubicEase { EasingMode = EasingMode.EaseOut }, delayMs);

            var ring = new Ellipse
            {
                Width = heavierImpact ? 110 : 92,
                Height = heavierImpact ? 110 : 92,
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFC107")),
                StrokeThickness = heavierImpact ? 9 : 7,
                Opacity = 0,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(0.2, 0.2)
            };

            Canvas.SetLeft(ring, impact.X - ring.Width / 2);
            Canvas.SetTop(ring, impact.Y - ring.Height / 2);
            ParticlesLayer.Children.Add(ring);

            var ringScale = (ScaleTransform)ring.RenderTransform;

            AnimateOpacity(ring, 0, 0.95, 70, delayMs + 10);
            AnimateOpacity(ring, 0.95, 0, heavierImpact ? 520 : 400, delayMs + 90);
            AnimateScale(ringScale, 0.2, heavierImpact ? 3.4 : 2.8, heavierImpact ? 580 : 460,
                new CircleEase { EasingMode = EasingMode.EaseOut }, delayMs + 10);
        }

        private void CreateSmokeAt(Point impact, int delayMs, bool heavierImpact)
        {
            var smoke = new Ellipse
            {
                Width = heavierImpact ? 210 : 165,
                Height = heavierImpact ? 120 : 92,
                Opacity = 0,
                RenderTransformOrigin = new Point(0.5, 0.5),
                Fill = new RadialGradientBrush
                {
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop((Color)ColorConverter.ConvertFromString("#CC5D4037"), 0),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#885D4037"), 0.55),
                        new GradientStop((Color)ColorConverter.ConvertFromString("#005D4037"), 1)
                    }
                },
                RenderTransform = new ScaleTransform(0.45, 0.45)
            };

            Canvas.SetLeft(smoke, impact.X - smoke.Width / 2);
            Canvas.SetTop(smoke, impact.Y - smoke.Height / 2 - 18);
            ParticlesLayer.Children.Add(smoke);

            var scale = (ScaleTransform)smoke.RenderTransform;
            double startTop = Canvas.GetTop(smoke);

            AnimateOpacity(smoke, 0, heavierImpact ? 0.8 : 0.68, 170, delayMs);
            AnimateOpacity(smoke, heavierImpact ? 0.8 : 0.68, 0, heavierImpact ? 1200 : 900, delayMs + 180);
            AnimateScale(scale, 0.45, heavierImpact ? 2.7 : 2.2, heavierImpact ? 1300 : 1000,
                new CircleEase { EasingMode = EasingMode.EaseOut }, delayMs);
            AnimateCanvasTop(smoke, startTop, startTop - (heavierImpact ? 100 : 70), heavierImpact ? 1300 : 1000, delayMs);
        }

        private void CreateGroundBlastAt(Point impact, int delayMs, bool heavierImpact)
        {
            var ground = new Ellipse
            {
                Width = heavierImpact ? 150 : 126,
                Height = heavierImpact ? 42 : 34,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#66FF7043")),
                Stroke = new SolidColorBrush(Color.FromArgb(170, 17, 17, 17)),
                StrokeThickness = 2,
                Opacity = 0,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(0.35, 0.35)
            };

            Canvas.SetLeft(ground, impact.X - ground.Width / 2);
            Canvas.SetTop(ground, impact.Y - ground.Height / 2 + 6);
            ParticlesLayer.Children.Add(ground);

            var scale = (ScaleTransform)ground.RenderTransform;

            AnimateOpacity(ground, 0, 0.85, 70, delayMs);
            AnimateOpacity(ground, 0.85, 0, heavierImpact ? 520 : 420, delayMs + 150);
            AnimateScale(scale, 0.35, heavierImpact ? 2.0 : 1.6, heavierImpact ? 560 : 460,
                new QuadraticEase { EasingMode = EasingMode.EaseOut }, delayMs);
        }

        private void CreateExplosionDebrisAt(Point impact, int delayMs, bool heavierImpact)
        {
            int total = heavierImpact ? _rng.Next(28, 38) : _rng.Next(20, 28);

            for (int i = 0; i < total; i++)
            {
                bool metallic = i < total * 0.65;

                Shape particle;
                if (metallic)
                {
                    particle = new Rectangle
                    {
                        Width = _rng.Next(5, 10),
                        Height = _rng.Next(12, 28),
                        RadiusX = 2,
                        RadiusY = 2,
                        Fill = new SolidColorBrush(PickDebrisColor()),
                        Stroke = Brushes.Black,
                        StrokeThickness = 1.2,
                        Opacity = 0
                    };
                }
                else
                {
                    particle = new Polygon
                    {
                        Points = new PointCollection
                        {
                            new Point(0, 0),
                            new Point(_rng.Next(7, 14), _rng.Next(2, 6)),
                            new Point(_rng.Next(3, 9), _rng.Next(10, 16))
                        },
                        Fill = new SolidColorBrush(PickDebrisColor()),
                        Stroke = Brushes.Black,
                        StrokeThickness = 1.2,
                        Opacity = 0
                    };
                }

                double w = particle.Width > 0 ? particle.Width : 14;
                double h = particle.Height > 0 ? particle.Height : 14;

                Canvas.SetLeft(particle, impact.X - w / 2);
                Canvas.SetTop(particle, impact.Y - h / 2);
                ParticlesLayer.Children.Add(particle);

                double angle = _rng.NextDouble() * Math.PI * 2;
                double distance = (heavierImpact ? 110 : 80) + _rng.NextDouble() * (heavierImpact ? 260 : 180);

                double targetX = impact.X + Math.Cos(angle) * distance;
                double targetY = impact.Y + Math.Sin(angle) * distance;

                targetY -= _rng.Next(30, heavierImpact ? 180 : 130);

                var tg = new TransformGroup();
                var scale = new ScaleTransform(0.45, 0.45, w / 2, h / 2);
                var rotate = new RotateTransform(_rng.Next(0, 360), w / 2, h / 2);
                tg.Children.Add(scale);
                tg.Children.Add(rotate);
                particle.RenderTransform = tg;

                int localDelay = delayMs + _rng.Next(0, 110);
                int duration = heavierImpact ? _rng.Next(850, 1350) : _rng.Next(650, 1100);

                AnimateOpacity(particle, 0, 1, 70, localDelay);
                AnimateOpacity(particle, 1, 0, duration - 160, localDelay + 160);
                AnimateCanvasLeft(particle, impact.X - w / 2, targetX, duration, localDelay);
                AnimateCanvasTop(particle, impact.Y - h / 2, targetY, duration, localDelay);
                AnimateScale(scale, 0.45, metallic ? 1.6 : 1.35, duration,
                    new QuadraticEase { EasingMode = EasingMode.EaseOut }, localDelay);

                var rotateAnim = new DoubleAnimation
                {
                    From = rotate.Angle,
                    To = rotate.Angle + _rng.Next(-320, 320),
                    Duration = TimeSpan.FromMilliseconds(duration),
                    BeginTime = TimeSpan.FromMilliseconds(localDelay),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                rotate.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
            }
        }

        private void CreateSparkBurstsAt(Point impact, int delayMs, bool heavierImpact)
        {
            int total = heavierImpact ? _rng.Next(22, 32) : _rng.Next(14, 22);

            for (int i = 0; i < total; i++)
            {
                var spark = new Rectangle
                {
                    Width = _rng.Next(2, 4),
                    Height = _rng.Next(10, 22),
                    RadiusX = 1,
                    RadiusY = 1,
                    Fill = new SolidColorBrush(PickSparkColor()),
                    Opacity = 0
                };

                double w = spark.Width;
                double h = spark.Height;

                Canvas.SetLeft(spark, impact.X - w / 2);
                Canvas.SetTop(spark, impact.Y - h / 2);
                ParticlesLayer.Children.Add(spark);

                double angle = _rng.NextDouble() * Math.PI * 2;
                double distance = (heavierImpact ? 90 : 60) + _rng.NextDouble() * (heavierImpact ? 200 : 130);

                double targetX = impact.X + Math.Cos(angle) * distance;
                double targetY = impact.Y + Math.Sin(angle) * distance - _rng.Next(20, heavierImpact ? 90 : 60);

                var tg = new TransformGroup();
                var scale = new ScaleTransform(0.4, 0.4, w / 2, h / 2);
                var rotate = new RotateTransform(angle * 180 / Math.PI, w / 2, h / 2);
                tg.Children.Add(scale);
                tg.Children.Add(rotate);
                spark.RenderTransform = tg;

                int localDelay = delayMs + _rng.Next(0, 80);
                int duration = heavierImpact ? _rng.Next(420, 760) : _rng.Next(300, 560);

                AnimateOpacity(spark, 0, 1, 40, localDelay);
                AnimateOpacity(spark, 1, 0, duration - 60, localDelay + 60);
                AnimateCanvasLeft(spark, impact.X - w / 2, targetX, duration, localDelay);
                AnimateCanvasTop(spark, impact.Y - h / 2, targetY, duration, localDelay);
                AnimateScale(scale, 0.4, 1.15, duration,
                    new QuadraticEase { EasingMode = EasingMode.EaseOut }, localDelay);
            }
        }

        private Color PickDebrisColor()
        {
            Color[] colors =
            {
                (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                (Color)ColorConverter.ConvertFromString("#FFB0BEC5"),
                (Color)ColorConverter.ConvertFromString("#FF90A4AE"),
                (Color)ColorConverter.ConvertFromString("#FFFF7043"),
                (Color)ColorConverter.ConvertFromString("#FFFFCA28")
            };

            return colors[_rng.Next(colors.Length)];
        }

        private Color PickSparkColor()
        {
            Color[] colors =
            {
                (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                (Color)ColorConverter.ConvertFromString("#FFFFF59D"),
                (Color)ColorConverter.ConvertFromString("#FFFFC107"),
                (Color)ColorConverter.ConvertFromString("#FFFF6F00")
            };

            return colors[_rng.Next(colors.Length)];
        }

        #region Helpers

        private void AnimateOpacity(UIElement target, double from, double to, int durationMs, int beginMs = 0)
        {
            var da = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                BeginTime = TimeSpan.FromMilliseconds(beginMs)
            };
            target.BeginAnimation(OpacityProperty, da);
        }

        private void AnimateScale(ScaleTransform target, double from, double to, int durationMs, IEasingFunction easing = null, int beginMs = 0)
        {
            var dx = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                BeginTime = TimeSpan.FromMilliseconds(beginMs),
                EasingFunction = easing
            };

            var dy = dx.Clone();
            target.BeginAnimation(ScaleTransform.ScaleXProperty, dx);
            target.BeginAnimation(ScaleTransform.ScaleYProperty, dy);
        }

        private void AnimateCanvasLeft(UIElement target, double from, double to, int durationMs, int beginMs = 0)
        {
            var da = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                BeginTime = TimeSpan.FromMilliseconds(beginMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            target.BeginAnimation(Canvas.LeftProperty, da);
        }

        private void AnimateCanvasTop(UIElement target, double from, double to, int durationMs, int beginMs = 0)
        {
            var da = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                BeginTime = TimeSpan.FromMilliseconds(beginMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            target.BeginAnimation(Canvas.TopProperty, da);
        }

        #endregion

        public override GoalExplosionType ToType()
        {
            return GoalExplosionType.Ballistic;
        }

        public void Dispose()
        {
            // Rien de spécial pour l’instant
        }
    }
}