using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using VraiPseudoSae.data.GoalExplosion;
using VraiPseudoSae.Utils.AudioPlayer;

namespace VraiPseudoSae.view.RLS_Pages.GoalExplosions
{
    public partial class GoalExplosion_Bats : GoalExplosionBase, IDisposable
    {
        private readonly string _audioKey = "goal_sound/Bats";
        private readonly Stopwatch _watch = new Stopwatch();
        private readonly Random _rng = new Random();

        private bool _isPlaying;
        private bool _towardsRight;

        private const double TotalDuration = 2.7;
        private const double SwarmStart = 0.55;
        private const double SwarmHit = 1.85;
        private const double FlashStart = 1.82;
        private const int BatCount = 18;

        private readonly List<BatSprite> _bats = new List<BatSprite>();

        private class BatSprite
        {
            public Canvas Visual { get; set; }
            public ScaleTransform Scale { get; set; }
            public RotateTransform Rotate { get; set; }
            public Point Start { get; set; }
            public Point Mid { get; set; }
            public Point End { get; set; }
            public double Size { get; set; }
            public double Delay { get; set; }
            public double FlapOffset { get; set; }
        }

        public GoalExplosion_Bats(Canvas gameCanvas, JsonPakAudioService audio) : base(gameCanvas, audio)
        {
            InitializeComponent();
            _audio?.Preload(_audioKey, "goal_bats");
            HideAll();
            BuildBats();
        }

        public override void PlayLeftGoal()
        {
            StartSequence(true);
        }

        public override void PlayRightGoal()
        {
            StartSequence(false);
        }

        private void StartSequence(bool towardsRight)
        {
            StopInternal();

            _towardsRight = towardsRight;
            ResetState();
            ShowAll();

            _audio?.Play("goal_bats");

            _isPlaying = true;
            _watch.Restart();
            CompositionTarget.Rendering += OnRendering;
        }

        private void StopInternal()
        {
            if (_isPlaying)
            {
                CompositionTarget.Rendering -= OnRendering;
                _watch.Stop();
                _isPlaying = false;
            }
        }

        private void EndSequence()
        {
            StopInternal();
            HideAll();
        }

        private void HideAll()
        {
            NightFade.Visibility = Visibility.Collapsed;
            Moon.Visibility = Visibility.Collapsed;
            BackBuildings.Visibility = Visibility.Collapsed;
            FrontBuildings.Visibility = Visibility.Collapsed;
            Fog1.Visibility = Visibility.Collapsed;
            Fog2.Visibility = Visibility.Collapsed;
            BatLayer.Visibility = Visibility.Collapsed;
            ImpactFlash.Visibility = Visibility.Collapsed;
            ComicText.Visibility = Visibility.Collapsed;
        }

        private void ShowAll()
        {
            NightFade.Visibility = Visibility.Visible;
            Moon.Visibility = Visibility.Visible;
            BackBuildings.Visibility = Visibility.Visible;
            FrontBuildings.Visibility = Visibility.Visible;
            Fog1.Visibility = Visibility.Visible;
            Fog2.Visibility = Visibility.Visible;
            BatLayer.Visibility = Visibility.Visible;
            ImpactFlash.Visibility = Visibility.Visible;
            ComicText.Visibility = Visibility.Visible;
        }

        private void BuildBats()
        {
            BatLayer.Children.Clear();
            _bats.Clear();

            for (int i = 0; i < BatCount; i++)
            {
                double size = 36 + _rng.Next(0, 42);

                Canvas bat = CreateBatVisual(size);
                ScaleTransform flapScale = new ScaleTransform(1, 1);
                RotateTransform rotate = new RotateTransform(0);

                TransformGroup tg = new TransformGroup();
                tg.Children.Add(flapScale);
                tg.Children.Add(rotate);

                bat.RenderTransformOrigin = new Point(0.5, 0.5);
                bat.RenderTransform = tg;
                bat.Visibility = Visibility.Collapsed;
                bat.Opacity = 0;

                BatLayer.Children.Add(bat);

                _bats.Add(new BatSprite
                {
                    Visual = bat,
                    Scale = flapScale,
                    Rotate = rotate,
                    Size = size,
                    Delay = i * 0.028,
                    FlapOffset = _rng.NextDouble() * Math.PI * 2
                });
            }
        }

        private Canvas CreateBatVisual(double size)
        {
            Canvas c = new Canvas
            {
                Width = size,
                Height = size * 0.62
            };

            Path p = new Path
            {
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF050505")),
                Stroke = Brushes.Black,
                StrokeThickness = Math.Max(1.0, size / 28.0),
                Stretch = Stretch.Fill,
                Width = size,
                Height = size * 0.62,
                Data = Geometry.Parse(
                    "M 5,28 " +
                    "C 10,16 20,12 29,18 " +
                    "C 33,9 42,5 50,14 " +
                    "C 53,3 64,3 67,14 " +
                    "C 75,5 84,9 88,18 " +
                    "C 97,12 107,16 112,28 " +
                    "C 102,24 95,27 88,33 " +
                    "C 81,39 72,42 63,37 " +
                    "C 60,45 53,50 50,40 " +
                    "C 47,50 40,45 37,37 " +
                    "C 28,42 19,39 12,33 " +
                    "C 5,27 -2,24 5,28 Z")
            };

            c.Children.Add(p);
            return c;
        }

        private void ResetState()
        {
            NightFade.Opacity = 0;
            Moon.Opacity = 0;
            BackBuildings.Opacity = 0;
            FrontBuildings.Opacity = 0;
            Fog1.Opacity = 0;
            Fog2.Opacity = 0;
            ImpactFlash.Opacity = 0;
            ComicText.Opacity = 0;

            ImpactFlashScale.ScaleX = 0.2;
            ImpactFlashScale.ScaleY = 0.2;

            ComicText.Text = _towardsRight ? "BATS!" : "SWARM!";

            for (int i = 0; i < _bats.Count; i++)
            {
                BatSprite bat = _bats[i];

                double startX = _towardsRight ? -120 - _rng.Next(0, 260) : 1020 + _rng.Next(0, 260);
                double startY = 80 + _rng.Next(0, 250);

                double midX = _towardsRight ? 370 + _rng.Next(-70, 90) : 630 + _rng.Next(-90, 70);
                double midY = 130 + _rng.Next(0, 170);

                double endX = _towardsRight ? 520 + _rng.Next(-65, 75) : 480 + _rng.Next(-75, 65);
                double endY = 180 + _rng.Next(20, 130);

                bat.Start = new Point(startX, startY);
                bat.Mid = new Point(midX, midY);
                bat.End = new Point(endX, endY);

                Canvas.SetLeft(bat.Visual, startX);
                Canvas.SetTop(bat.Visual, startY);
                bat.Visual.Visibility = Visibility.Collapsed;
                bat.Visual.Opacity = 0;
                bat.Rotate.Angle = _towardsRight ? -10 : 10;
                bat.Scale.ScaleX = _towardsRight ? 1 : -1;
                bat.Scale.ScaleY = 1;
            }
        }

        private void OnRendering(object sender, EventArgs e)
        {
            double t = _watch.Elapsed.TotalSeconds;

            if (t >= TotalDuration)
            {
                EndSequence();
                return;
            }

            UpdateScene(t);
            UpdateBats(t);
            UpdateFlash(t);
            UpdateText(t);
        }

        private void UpdateScene(double t)
        {
            NightFade.Opacity = Clamp01(t / 0.22);
            Moon.Opacity = Clamp01((t - 0.06) / 0.28) * 0.95;
            BackBuildings.Opacity = Clamp01((t - 0.10) / 0.30);
            FrontBuildings.Opacity = Clamp01((t - 0.14) / 0.34);
            Fog1.Opacity = Clamp01((t - 0.25) / 0.40) * 0.28;
            Fog2.Opacity = Clamp01((t - 0.33) / 0.45) * 0.22;
        }

        private void UpdateBats(double t)
        {
            for (int i = 0; i < _bats.Count; i++)
            {
                BatSprite bat = _bats[i];
                double localT = t - SwarmStart - bat.Delay;

                if (localT <= 0)
                {
                    bat.Visual.Visibility = Visibility.Collapsed;
                    continue;
                }

                bat.Visual.Visibility = Visibility.Visible;
                bat.Visual.Opacity = 1;

                double p = Math.Min(1.0, localT / (SwarmHit - SwarmStart));

                Point pos;
                if (p < 0.68)
                {
                    double p1 = p / 0.68;
                    pos = QuadraticBezier(bat.Start, bat.Mid, bat.End, p1);
                }
                else
                {
                    double p2 = (p - 0.68) / 0.32;
                    Point cluster = _towardsRight
                        ? new Point(505, 255)
                        : new Point(495, 255);
                    pos = Lerp(bat.End, cluster, p2);
                }

                Canvas.SetLeft(bat.Visual, pos.X);
                Canvas.SetTop(bat.Visual, pos.Y);

                double flap = Math.Sin((t * 18.0) + bat.FlapOffset);
                bat.Scale.ScaleY = 0.82 + ((flap + 1) * 0.18);

                double tiltBase = _towardsRight ? -12 : 12;
                bat.Rotate.Angle = tiltBase + flap * 8;

                if (p > 0.86)
                {
                    bat.Visual.Opacity = 1.0 - ((p - 0.86) / 0.14);
                }
            }
        }

        private void UpdateFlash(double t)
        {
            if (t < FlashStart)
            {
                ImpactFlash.Opacity = 0;
                return;
            }

            double p = Clamp01((t - FlashStart) / 0.38);
            ImpactFlash.Opacity = 1 - p;
            ImpactFlashScale.ScaleX = 0.2 + p * 4.8;
            ImpactFlashScale.ScaleY = 0.2 + p * 4.8;
        }

        private void UpdateText(double t)
        {
            if (t < 1.25)
            {
                ComicText.Opacity = 0;
                return;
            }

            double p = Clamp01((t - 1.25) / 0.22);
            ComicText.Opacity = p;
        }

        private Point Lerp(Point a, Point b, double t)
        {
            return new Point(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t);
        }

        private Point QuadraticBezier(Point p0, Point p1, Point p2, double t)
        {
            double u = 1.0 - t;
            return new Point(
                u * u * p0.X + 2 * u * t * p1.X + t * t * p2.X,
                u * u * p0.Y + 2 * u * t * p1.Y + t * t * p2.Y
            );
        }

        private double Clamp01(double value)
        {
            if (value < 0) return 0;
            if (value > 1) return 1;
            return value;
        }
        
        public override GoalExplosionType ToType()
        {
            return GoalExplosionType.Bats;
        }

        public void Dispose()
        {
            EndSequence();
        }
    }
}