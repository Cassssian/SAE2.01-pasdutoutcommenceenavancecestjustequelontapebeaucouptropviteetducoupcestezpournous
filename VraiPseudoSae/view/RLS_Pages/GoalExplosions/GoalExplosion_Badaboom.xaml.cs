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
    public partial class GoalExplosion_Badaboom : GoalExplosionBase, IDisposable
    {
        private readonly string _audioKey = "goal_sound/Badaboom";
        private readonly Random _rng = new Random();

        public GoalExplosion_Badaboom(Canvas gameCanvas, JsonPakAudioService audio) : base(gameCanvas, audio)
        {
            InitializeComponent();
            _audio?.Preload(_audioKey, "goal_badaboom");
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

            AnimateMainBurst();
            AnimateImpactBars();
            AnimateShards();
            AnimateStars();
            AnimateText(center);

            _audio?.Play("goal_badaboom");
        }

        private void ResetVisualState()
        {
            ParticlesLayer.Children.Clear();

            MainBurst.Opacity = 0;
            ImpactBarA.Opacity = 0;
            ImpactBarB.Opacity = 0;

            Shard1.Opacity = 0;
            Shard2.Opacity = 0;
            Shard3.Opacity = 0;
            Shard4.Opacity = 0;

            StarA.Opacity = 0;
            StarB.Opacity = 0;

            MainBurstScale.ScaleX = 0.2;
            MainBurstScale.ScaleY = 0.2;
            MainBurstRotate.Angle = 0;

            ImpactBarAScale.ScaleX = 0.2;
            ImpactBarAScale.ScaleY = 0.2;
            ImpactBarARotate.Angle = -22;

            ImpactBarBScale.ScaleX = 0.2;
            ImpactBarBScale.ScaleY = 0.2;
            ImpactBarBRotate.Angle = 28;

            ResetShardTransform(Shard1Scale, Shard1Rotate, -20);
            ResetShardTransform(Shard2Scale, Shard2Rotate, 40);
            ResetShardTransform(Shard3Scale, Shard3Rotate, 130);
            ResetShardTransform(Shard4Scale, Shard4Rotate, -120);

            StarAScale.ScaleX = 0.3;
            StarAScale.ScaleY = 0.3;
            StarARotate.Angle = 0;

            StarBScale.ScaleX = 0.3;
            StarBScale.ScaleY = 0.3;
            StarBRotate.Angle = 0;

            BoomTextHost.Opacity = 0;

            BoomTextScale.ScaleX = 0.2;
            BoomTextScale.ScaleY = 0.2;
            BoomTextRotate.Angle = -10;
            BoomTextSkew.AngleX = -18;
            BoomTextSkew.AngleY = 0;

            BoomTextHostTranslate.X = 0;
            BoomTextHostTranslate.Y = 0;
        }

        private void ResetShardTransform(ScaleTransform scale, RotateTransform rotate, double baseAngle)
        {
            scale.ScaleX = 0.25;
            scale.ScaleY = 0.25;
            rotate.Angle = baseAngle;
        }

        private void PositionElements(Point center)
        {
            bool leftGoal = center.X < 500;

            Canvas.SetLeft(MainBurst, center.X - 40);
            Canvas.SetTop(MainBurst, center.Y - 40);

            Canvas.SetLeft(ImpactBarA, center.X - 55);
            Canvas.SetTop(ImpactBarA, center.Y - 7);

            Canvas.SetLeft(ImpactBarB, center.X - 55);
            Canvas.SetTop(ImpactBarB, center.Y - 7);

            Canvas.SetLeft(Shard1, center.X - 6);
            Canvas.SetTop(Shard1, center.Y - 50);

            Canvas.SetLeft(Shard2, center.X + 6);
            Canvas.SetTop(Shard2, center.Y - 16);

            Canvas.SetLeft(Shard3, center.X - 40);
            Canvas.SetTop(Shard3, center.Y + 6);

            Canvas.SetLeft(Shard4, center.X + 20);
            Canvas.SetTop(Shard4, center.Y + 10);

            Canvas.SetLeft(StarA, center.X + 65);
            Canvas.SetTop(StarA, center.Y - 70);

            Canvas.SetLeft(StarB, center.X - 80);
            Canvas.SetTop(StarB, center.Y - 60);

            if (leftGoal)
            {
                Canvas.SetLeft(BoomTextHost, center.X + 20);
                Canvas.SetTop(BoomTextHost, center.Y - 140);

                BoomTextRotate.Angle = -12;
                BoomTextSkew.AngleX = -18;
            }
            else
            {
                Canvas.SetLeft(BoomTextHost, center.X - 280);
                Canvas.SetTop(BoomTextHost, center.Y - 140);

                BoomTextRotate.Angle = 12;
                BoomTextSkew.AngleX = 18;
            }
        }

        private void AnimateMainBurst()
        {
            AnimateOpacity(MainBurst, 0, 1, 90, 0);
            AnimateOpacity(MainBurst, 1, 0, 420, 320);

            AnimateScale(MainBurstScale, 0.2, 1.38, 340,
                new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.55 }, 0);

            var targetAngle = _rng.Next(-15, 16);
            AnimateRotate(MainBurstRotate, 0, targetAngle, 320, 0);
        }

        private void AnimateImpactBars()
        {
            AnimateOpacity(ImpactBarA, 0, 0.95, 55, 30);
            AnimateOpacity(ImpactBarA, 0.95, 0, 320, 180);
            AnimateScale(ImpactBarAScale, 0.2, 1.55, 320,
                new CircleEase { EasingMode = EasingMode.EaseOut }, 30);
            AnimateRotate(ImpactBarARotate, -22, -8, 320, 30);

            AnimateOpacity(ImpactBarB, 0, 0.95, 55, 30);
            AnimateOpacity(ImpactBarB, 0.95, 0, 320, 180);
            AnimateScale(ImpactBarBScale, 0.2, 1.55, 320,
                new CircleEase { EasingMode = EasingMode.EaseOut }, 30);
            AnimateRotate(ImpactBarBRotate, 28, 14, 320, 30);
        }

        private void AnimateShards()
        {
            AnimateShard(Shard1, Shard1Scale, Shard1Rotate, -20, -95, 820, 60);
            AnimateShard(Shard2, Shard2Scale, Shard2Rotate, 40, 145, 780, 110);
            AnimateShard(Shard3, Shard3Scale, Shard3Rotate, 130, 270, 900, 140);
            AnimateShard(Shard4, Shard4Scale, Shard4Rotate, -120, -240, 840, 95);
        }

        private void AnimateShard(Polygon shard, ScaleTransform scale, RotateTransform rotate,
            double startAngle, double endAngle, int durationMs, int delayMs)
        {
            AnimateOpacity(shard, 0, 1, 80, delayMs);
            AnimateOpacity(shard, 1, 0, durationMs - 140, delayMs + 140);

            AnimateScale(scale, 0.25, 1.95, durationMs,
                new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.45 }, delayMs);

            AnimateRotate(rotate, startAngle, endAngle, durationMs, delayMs);
        }
        
        private void AnimateStars()
        {
            AnimateOpacity(StarA, 0, 1, 90, 140);
            AnimateOpacity(StarA, 1, 0, 420, 360);
            AnimateScale(StarAScale, 0.3, 1.0, 420,
                new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 }, 140);
            AnimateRotate(StarARotate, 0, 180, 420, 140);

            AnimateOpacity(StarB, 0, 1, 90, 170);
            AnimateOpacity(StarB, 1, 0, 420, 390);
            AnimateScale(StarBScale, 0.3, 1.15, 420,
                new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 }, 170);
            AnimateRotate(StarBRotate, 0, -220, 420, 170);
        }

        private void AnimateText(Point center)
        {
            bool leftGoal = center.X < 500;

            double endRotate = leftGoal ? -4 : 4;
            double endSkew = leftGoal ? -10 : 10;
            double driftX = leftGoal ? 18 : -18;

            AnimateOpacity(BoomTextHost, 0, 1, 120, 70);
            AnimateOpacity(BoomTextHost, 1, 0, 620, 620);

            AnimateScale(BoomTextScale, 0.2, 1.08, 320,
                new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.55 }, 70);

            AnimateRotate(BoomTextRotate, BoomTextRotate.Angle, endRotate, 340, 70);
            AnimateSkewX(BoomTextSkew, BoomTextSkew.AngleX, endSkew, 340, 70);

            AnimateTranslateX(BoomTextHostTranslate, 0, driftX, 720, 70);
            AnimateTranslateY(BoomTextHostTranslate, 0, -26, 720, 70);
        }
        
        private void AnimateTranslateX(TranslateTransform target, double from, double to, int durationMs, int beginMs = 0)
        {
            var da = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                BeginTime = TimeSpan.FromMilliseconds(beginMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            target.BeginAnimation(TranslateTransform.XProperty, da);
        }

        private void AnimateTranslateY(TranslateTransform target, double from, double to, int durationMs, int beginMs = 0)
        {
            var da = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                BeginTime = TimeSpan.FromMilliseconds(beginMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            target.BeginAnimation(TranslateTransform.YProperty, da);
        }
        
        private void CreateParticles(Point center)
        {
            int total = _rng.Next(30, 44);

            for (int i = 0; i < total; i++)
            {
                bool longShard = i < total / 2;

                Shape particle;
                if (longShard)
                {
                    particle = new Rectangle
                    {
                        Width = _rng.Next(3, 7),
                        Height = _rng.Next(16, 34),
                        RadiusX = 2,
                        RadiusY = 2,
                        Fill = new SolidColorBrush(PickParticleColor()),
                        Stroke = Brushes.Black,
                        StrokeThickness = 1.5,
                        Opacity = 0
                    };
                }
                else
                {
                    particle = new Ellipse
                    {
                        Width = _rng.Next(4, 9),
                        Height = _rng.Next(4, 9),
                        Fill = new SolidColorBrush(PickParticleColor()),
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        Opacity = 0
                    };
                }

                double w = particle.Width;
                double h = particle.Height;

                Canvas.SetLeft(particle, center.X - w / 2);
                Canvas.SetTop(particle, center.Y - h / 2);
                ParticlesLayer.Children.Add(particle);

                double angle = _rng.NextDouble() * Math.PI * 2;
                double distanceBase = longShard ? 140 : 90;
                double distance = distanceBase + _rng.NextDouble() * (longShard ? 240 : 150);

                double targetX = center.X + Math.Cos(angle) * distance;
                double targetY = center.Y + Math.Sin(angle) * distance;

                var tg = new TransformGroup();
                var scale = new ScaleTransform(0.5, 0.5, w / 2, h / 2);
                var rotate = new RotateTransform(_rng.Next(0, 360), w / 2, h / 2);
                tg.Children.Add(scale);
                tg.Children.Add(rotate);
                particle.RenderTransform = tg;

                int delay = _rng.Next(0, 140);
                int duration = _rng.Next(700, 1250);

                AnimateOpacity(particle, 0, 1, 90, delay);
                AnimateOpacity(particle, 1, 0, duration - 140, delay + 140);

                AnimateCanvasLeft(particle, center.X - w / 2, targetX, duration, delay);
                AnimateCanvasTop(particle, center.Y - h / 2, targetY, duration, delay);

                AnimateScale(scale, 0.5, longShard ? 1.7 : 1.3, duration,
                    new QuadraticEase { EasingMode = EasingMode.EaseOut }, delay);

                var rotateAnim = new DoubleAnimation
                {
                    From = rotate.Angle,
                    To = rotate.Angle + _rng.Next(-260, 260),
                    Duration = TimeSpan.FromMilliseconds(duration),
                    BeginTime = TimeSpan.FromMilliseconds(delay),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                rotate.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
            }
        }

        private Color PickParticleColor()
        {
            Color[] colors =
            {
                (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                (Color)ColorConverter.ConvertFromString("#FFFFF59D"),
                (Color)ColorConverter.ConvertFromString("#FFFFCA28"),
                (Color)ColorConverter.ConvertFromString("#FFFF8A65"),
                (Color)ColorConverter.ConvertFromString("#FFFF7043")
            };

            return colors[_rng.Next(colors.Length)];
        }

        #region Helpers animation identiques à B89

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

        private void AnimateRotate(RotateTransform target, double from, double to, int durationMs, int beginMs = 0)
        {
            var da = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                BeginTime = TimeSpan.FromMilliseconds(beginMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            target.BeginAnimation(RotateTransform.AngleProperty, da);
        }

        private void AnimateCanvasLeft(UIElement target, double from, double to, int durationMs, int beginMs = 0)
        {
            var da = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                BeginTime = TimeSpan.FromMilliseconds(beginMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
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
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            target.BeginAnimation(Canvas.TopProperty, da);
        }
        
        private void AnimateSkewX(SkewTransform target, double from, double to, int durationMs, int beginMs = 0)
        {
            var da = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                BeginTime = TimeSpan.FromMilliseconds(beginMs),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            target.BeginAnimation(SkewTransform.AngleXProperty, da);
        }

        #endregion

        public override GoalExplosionType ToType()
        {
            return GoalExplosionType.Badaboom;
        }

        public void Dispose()
        {
            // rien de spécial à libérer pour le moment
        }
    }
}