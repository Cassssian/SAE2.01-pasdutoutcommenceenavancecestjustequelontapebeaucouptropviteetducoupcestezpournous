using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using VraiPseudoSae.data.GoalExplosion;
using VraiPseudoSae.Utils.AudioPlayer;

namespace VraiPseudoSae.view.RLS_Pages.GoalExplosions
{
    public partial class GoalExplosion_B89 : GoalExplosionBase, IDisposable
    {
        private readonly string _audioKey = "goal_sound/B89";
        private readonly Random _rng = new Random();

        public GoalExplosion_B89(Canvas gameCanvas, JsonPakAudioService audio) : base(gameCanvas, audio)
        {
            InitializeComponent();
            _audio?.Preload(_audioKey, "goal_b89"); 
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
            AnimateCross();
            AnimateShards();
            AnimateDust(center);
            _audio?.Play("goal_b89");
        }

        private void ResetVisualState()
        {
            ParticlesLayer.Children.Clear();

            CoreFlash.Opacity = 0;
            ShockRing.Opacity = 0;
            CrossH.Opacity = 0;
            CrossV.Opacity = 0;
            DustBack.Opacity = 0;

            ShardA.Opacity = 0;
            ShardB.Opacity = 0;
            ShardC.Opacity = 0;
            ShardD.Opacity = 0;

            CoreFlashScale.ScaleX = 0.3;
            CoreFlashScale.ScaleY = 0.3;

            ShockRingScale.ScaleX = 0.25;
            ShockRingScale.ScaleY = 0.25;

            CrossHScale.ScaleX = 0.2;
            CrossHScale.ScaleY = 1.0;

            CrossVScale.ScaleX = 1.0;
            CrossVScale.ScaleY = 0.2;

            DustBackScale.ScaleX = 0.6;
            DustBackScale.ScaleY = 0.6;

            ResetShard(ShardAScale, ShardARotate);
            ResetShard(ShardBScale, ShardBRotate);
            ResetShard(ShardCScale, ShardCRotate);
            ResetShard(ShardDScale, ShardDRotate);
        }

        private void ResetShard(ScaleTransform scale, RotateTransform rotate)
        {
            scale.ScaleX = 0.25;
            scale.ScaleY = 0.25;
            rotate.Angle = 0;
        }

        private void PositionElements(Point center)
        {
            Canvas.SetLeft(CoreFlash, center.X - 17);
            Canvas.SetTop(CoreFlash, center.Y - 17);

            Canvas.SetLeft(ShockRing, center.X - 35);
            Canvas.SetTop(ShockRing, center.Y - 35);

            Canvas.SetLeft(CrossH, center.X - 60);
            Canvas.SetTop(CrossH, center.Y - 5);

            Canvas.SetLeft(CrossV, center.X - 5);
            Canvas.SetTop(CrossV, center.Y - 60);

            Canvas.SetLeft(DustBack, center.X - 70);
            Canvas.SetTop(DustBack, center.Y - 35);

            Canvas.SetLeft(ShardA, center.X - 6);
            Canvas.SetTop(ShardA, center.Y - 45);

            Canvas.SetLeft(ShardB, center.X - 5);
            Canvas.SetTop(ShardB, center.Y - 35);

            Canvas.SetLeft(ShardC, center.X - 5);
            Canvas.SetTop(ShardC, center.Y - 40);

            Canvas.SetLeft(ShardD, center.X - 4);
            Canvas.SetTop(ShardD, center.Y - 31);

            ShardARotate.Angle = 18;
            ShardBRotate.Angle = 72;
            ShardCRotate.Angle = 128;
            ShardDRotate.Angle = -38;
        }

        private void AnimateCore()
        {
            AnimateOpacity(CoreFlash, 0, 1, 70);
            AnimateOpacity(CoreFlash, 1, 0, 170, 70);
            AnimateScale(CoreFlashScale, 0.3, 5.4, 240, new CubicEase { EasingMode = EasingMode.EaseOut });

            AnimateOpacity(ShockRing, 0, 0.95, 60);
            AnimateOpacity(ShockRing, 0.95, 0, 420, 60);
            AnimateScale(ShockRingScale, 0.25, 3.4, 480, new CircleEase { EasingMode = EasingMode.EaseOut });
        }

        private void AnimateCross()
        {
            AnimateOpacity(CrossH, 0, 0.85, 45, 20);
            AnimateOpacity(CrossH, 0.85, 0, 180, 65);
            AnimateScaleX(CrossHScale, 0.2, 1.5, 220, 20);

            AnimateOpacity(CrossV, 0, 0.75, 45, 20);
            AnimateOpacity(CrossV, 0.75, 0, 180, 65);
            AnimateScaleY(CrossVScale, 0.2, 1.5, 220, 20);
        }

        private void AnimateShards()
        {
            AnimateShard(ShardA, ShardAScale, ShardARotate, 18, 72, 560, 20);
            AnimateShard(ShardB, ShardBScale, ShardBRotate, 72, 150, 520, 40);
            AnimateShard(ShardC, ShardCScale, ShardCRotate, 128, 230, 600, 70);
            AnimateShard(ShardD, ShardDScale, ShardDRotate, -38, -120, 500, 55);
        }

        private void AnimateShard(Rectangle shard, ScaleTransform scale, RotateTransform rotate, double startAngle, double endAngle, int durationMs, int delayMs)
        {
            AnimateOpacity(shard, 0, 1, 60, delayMs);
            AnimateOpacity(shard, 1, 0, durationMs - 80, delayMs + 80);
            AnimateScale(scale, 0.25, 1.7, durationMs, new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 }, delayMs);
            AnimateRotate(rotate, startAngle, endAngle, durationMs, delayMs);
        }

        private void AnimateDust(Point center)
        {
            AnimateOpacity(DustBack, 0, 0.45, 150, 40);
            AnimateOpacity(DustBack, 0.45, 0, 650, 160);
            AnimateScale(DustBackScale, 0.6, 2.4, 850, new CircleEase { EasingMode = EasingMode.EaseOut }, 40);
            AnimateCanvasTop(DustBack, center.Y - 35, center.Y - 72, 850, 40);
        }

        private void CreateParticles(Point center)
        {
            for (int i = 0; i < 26; i++)
            {
                bool longShard = i < 10;

                Shape particle;
                if (longShard)
                {
                    particle = new Rectangle
                    {
                        Width = _rng.Next(3, 6),
                        Height = _rng.Next(14, 30),
                        RadiusX = 2,
                        RadiusY = 2,
                        Fill = new SolidColorBrush(PickParticleColor()),
                        Opacity = 0
                    };
                }
                else
                {
                    particle = new Ellipse
                    {
                        Width = _rng.Next(4, 10),
                        Height = _rng.Next(4, 10),
                        Fill = new SolidColorBrush(PickParticleColor()),
                        Opacity = 0
                    };
                }

                double w = particle.Width;
                double h = particle.Height;

                Canvas.SetLeft(particle, center.X - w / 2);
                Canvas.SetTop(particle, center.Y - h / 2);
                ParticlesLayer.Children.Add(particle);

                double angle = _rng.NextDouble() * Math.PI * 2;
                double distance = longShard ? 100 + _rng.NextDouble() * 160 : 60 + _rng.NextDouble() * 110;

                double targetX = center.X + Math.Cos(angle) * distance;
                double targetY = center.Y + Math.Sin(angle) * distance;

                var tg = new TransformGroup();
                var scale = new ScaleTransform(0.5, 0.5, w / 2, h / 2);
                var rotate = new RotateTransform(_rng.Next(0, 360), w / 2, h / 2);
                tg.Children.Add(scale);
                tg.Children.Add(rotate);
                particle.RenderTransform = tg;

                int delay = _rng.Next(0, 90);
                int duration = _rng.Next(380, 760);

                AnimateOpacity(particle, 0, 1, 70, delay);
                AnimateOpacity(particle, 1, 0, duration - 80, delay + 80);

                AnimateCanvasLeft(particle, center.X - w / 2, targetX, duration, delay);
                AnimateCanvasTop(particle, center.Y - h / 2, targetY, duration, delay);

                AnimateScale(scale, 0.5, longShard ? 1.8 : 1.3, duration, new QuadraticEase { EasingMode = EasingMode.EaseOut }, delay);

                var rotateAnim = new DoubleAnimation
                {
                    From = ((RotateTransform)tg.Children[1]).Angle,
                    To = ((RotateTransform)tg.Children[1]).Angle + _rng.Next(-140, 140),
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
                (Color)ColorConverter.ConvertFromString("#FFF6E27A"),
                (Color)ColorConverter.ConvertFromString("#FFFFA03A"),
                (Color)ColorConverter.ConvertFromString("#FFFF6A00")
            };

            return colors[_rng.Next(colors.Length)];
        }

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

        private void AnimateScaleX(ScaleTransform target, double from, double to, int durationMs, int beginMs = 0)
        {
            var da = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                BeginTime = TimeSpan.FromMilliseconds(beginMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            target.BeginAnimation(ScaleTransform.ScaleXProperty, da);
        }

        private void AnimateScaleY(ScaleTransform target, double from, double to, int durationMs, int beginMs = 0)
        {
            var da = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                BeginTime = TimeSpan.FromMilliseconds(beginMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            target.BeginAnimation(ScaleTransform.ScaleYProperty, da);
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

        public override GoalExplosionType ToType()
        {
            return GoalExplosionType.B89;
        }    
        
        public void Dispose()
        {
            // Rien de spécial ici pour l’instant
        }
    }
}