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
    public partial class GoalExplosion_Baseball : GoalExplosionBase, IDisposable
    {
        private readonly string _audioKey = "goal_sound/Baseball";
        private readonly Random _rng = new Random();
        private readonly Stopwatch _watch = new Stopwatch();

        private bool _isPlaying;
        private bool _firstTowardsRight;

        private Point _startPoint;
        private Point _batZonePoint;
        private Point _finalGoalPoint;

        private const double BallSize = 150;
        private const double TotalDuration = 2.25;

        private const double Phase1End = 1.02;

        private const double IdleEnd = 0.58;
        private const double BackswingEnd = 0.84;
        private const double HitTime = 1.02;
        private const double FollowEnd = 1.55;
        private const double BatNorthOffset = -90;
        private double _currentBatLogicalAngle;

        private const double ImpactTime = 1.72;

        private readonly Queue<Point> _ballHistory = new Queue<Point>();
        private readonly Queue<double> _ballAngleHistory = new Queue<double>();
        private readonly Queue<double> _batAngleHistory = new Queue<double>();

        public GoalExplosion_Baseball(Canvas gameCanvas, JsonPakAudioService audio) : base(gameCanvas, audio)
        {
            InitializeComponent();
            _audio?.Preload(_audioKey, "goal_baseball");
            HideAll();
        }

        public override void PlayLeftGoal()
        {
            StartSequence(true);
        }

        public override void PlayRightGoal()
        {
            StartSequence(false);
        }

        private void StartSequence(bool towardsRightFirst)
        {
            StopSequenceInternal();

            _firstTowardsRight = towardsRightFirst;

            _startPoint = new Point(500, 300);

            _batZonePoint = towardsRightFirst
                ? new Point(850, 398)
                : new Point(210, 398);

            _finalGoalPoint = towardsRightFirst
                ? new Point(70, 430)
                : new Point(930, 430);

            ResetState();
            ShowAllForPlay();

            _audio?.Play("goal_baseball");

            _isPlaying = true;
            _watch.Restart();
            CompositionTarget.Rendering += OnRendering;
        }

        private void StopSequenceInternal()
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
            StopSequenceInternal();
            HideAll();
            ParticlesLayer.Children.Clear();
        }

        private void HideAll()
        {
            BatTrail1.Visibility = Visibility.Collapsed;
            BatTrail2.Visibility = Visibility.Collapsed;
            BatTrail3.Visibility = Visibility.Collapsed;

            GiantBat.Visibility = Visibility.Collapsed;

            BallTrail1.Visibility = Visibility.Collapsed;
            BallTrail2.Visibility = Visibility.Collapsed;
            BallTrail3.Visibility = Visibility.Collapsed;

            BaseballBall.Visibility = Visibility.Collapsed;

            HitFlash.Visibility = Visibility.Collapsed;
            HitRing.Visibility = Visibility.Collapsed;
            DustCloud.Visibility = Visibility.Collapsed;

            ParticlesLayer.Visibility = Visibility.Collapsed;
        }

        private void ShowAllForPlay()
        {
            GiantBat.Visibility = Visibility.Visible;
            BaseballBall.Visibility = Visibility.Visible;

            HitFlash.Visibility = Visibility.Visible;
            HitRing.Visibility = Visibility.Visible;
            DustCloud.Visibility = Visibility.Visible;

            ParticlesLayer.Visibility = Visibility.Visible;

            BatTrail1.Visibility = Visibility.Collapsed;
            BatTrail2.Visibility = Visibility.Collapsed;
            BatTrail3.Visibility = Visibility.Collapsed;

            BallTrail1.Visibility = Visibility.Collapsed;
            BallTrail2.Visibility = Visibility.Collapsed;
            BallTrail3.Visibility = Visibility.Collapsed;
        }

        private void ResetState()
        {
            ParticlesLayer.Children.Clear();

            _ballHistory.Clear();
            _ballAngleHistory.Clear();
            _batAngleHistory.Clear();

            SetBallPosition(_startPoint);
            BaseballBallRotate.Angle = 0;
            BaseballBall.Opacity = 1;

            SetBallTrail(BallTrail1, _startPoint, 0);
            SetBallTrail(BallTrail2, _startPoint, 0);
            SetBallTrail(BallTrail3, _startPoint, 0);

            PositionBatBase();
            PositionBatTrailBase(BatTrail1);
            PositionBatTrailBase(BatTrail2);
            PositionBatTrailBase(BatTrail3);

            double initialSpriteAngle = 0 + BatNorthOffset;

            GiantBatRotate.Angle = initialSpriteAngle;
            BatTrail1Rotate.Angle = initialSpriteAngle;
            BatTrail2Rotate.Angle = initialSpriteAngle;
            BatTrail3Rotate.Angle = initialSpriteAngle;
            
            HitFlash.Opacity = 0;
            HitRing.Opacity = 0;
            DustCloud.Opacity = 0;

            HitFlashScale.ScaleX = 0.2;
            HitFlashScale.ScaleY = 0.2;

            HitRingScale.ScaleX = 0.2;
            HitRingScale.ScaleY = 0.2;

            DustCloudScale.ScaleX = 0.5;
            DustCloudScale.ScaleY = 0.5;

            Canvas.SetLeft(HitFlash, _finalGoalPoint.X - 30);
            Canvas.SetTop(HitFlash, _finalGoalPoint.Y - 30);

            Canvas.SetLeft(HitRing, _finalGoalPoint.X - 60);
            Canvas.SetTop(HitRing, _finalGoalPoint.Y - 60);

            Canvas.SetLeft(DustCloud, _finalGoalPoint.X - 95);
            Canvas.SetTop(DustCloud, _finalGoalPoint.Y - 50);
        }

        private void OnRendering(object sender, EventArgs e)
        {
            double t = _watch.Elapsed.TotalSeconds;

            if (t >= TotalDuration)
            {
                EndSequence();
                return;
            }

            UpdateBall(t);
            UpdateBat(t);
            UpdateTrailVisibility(t);
            UpdateTrails();
            UpdateImpactVisuals(t);

            if (t >= ImpactTime && ParticlesLayer.Children.Count == 0)
            {
                CreateDebris(_finalGoalPoint);
            }
        }

        private void UpdateBall(double t)
        {
            Point pos;
            double angle;

            double idleEnd = 0.60;
            double hitEnd = !_firstTowardsRight ? 1.2: 0.7;

            double hitLogicalAngle = _firstTowardsRight ? 190 : -190;
            Point contactPoint = GetBatTipOnCanvas(hitLogicalAngle);

            if (t <= hitEnd)
            {
                double p = t / hitEnd;

                // trajectoire d'approche adaptée vers le bout de la batte
                pos = Lerp(_startPoint, contactPoint, p);

                angle = _firstTowardsRight ? -1100 * p : 1100 * p;
            }
            else
            {
                double p = Math.Min(1.0, (t - hitEnd) / (ImpactTime - hitEnd));

                // départ exact depuis le point de contact
                Point midPoint = _firstTowardsRight
                    ? new Point(contactPoint.X - 260, contactPoint.Y - 80)
                    : new Point(contactPoint.X + 260, contactPoint.Y - 80);

                pos = QuadraticBezier(contactPoint, midPoint, _finalGoalPoint, p);  

                angle = _firstTowardsRight ? -1100 - 1500 * p : 1100 + 1500 * p;
            }

            SetBallPosition(pos);
            BaseballBallRotate.Angle = angle;

            _ballHistory.Enqueue(pos);
            _ballAngleHistory.Enqueue(angle);

            while (_ballHistory.Count > 14)
                _ballHistory.Dequeue();

            while (_ballAngleHistory.Count > 14)
                _ballAngleHistory.Dequeue();

            if (t > ImpactTime - 0.08)
            {
                BaseballBall.Opacity = 1.0 - ((t - (ImpactTime - 0.08)) / 0.08);
            }
            else
            {
                BaseballBall.Opacity = 1;
            }
        }
        
        private Point QuadraticBezier(Point p0, Point p1, Point p2, double t)
        {
            double u = 1 - t;
            return new Point(
                u * u * p0.X + 2 * u * t * p1.X + t * t * p2.X,
                u * u * p0.Y + 2 * u * t * p1.Y + t * t * p2.Y
            );
        }

        private void UpdateBat(double t)
        {
            double logicalAngle;

            double idleEnd = 0.60;
            double hitEnd = 1.02;
            double finishEnd = 1.62;

            double logicalHitAngle = _firstTowardsRight ? 190 : -190;
            double logicalFinishAngle = _firstTowardsRight ? 380 : -380;

            if (t < idleEnd)
            {
                logicalAngle = 0;
            }
            else if (t < hitEnd)
            {
                double p = (t - idleEnd) / (hitEnd - idleEnd);
                logicalAngle = Lerp(0, logicalHitAngle, p); // LINÉAIRE
            }
            else if (t < finishEnd)
            {
                double p = (t - hitEnd) / (finishEnd - hitEnd);
                logicalAngle = Lerp(logicalHitAngle, logicalFinishAngle, p); // LINÉAIRE
            }
            else
            {
                logicalAngle = logicalFinishAngle;
            }

            _currentBatLogicalAngle = logicalAngle;

            double spriteAngle = logicalAngle + BatNorthOffset;
            GiantBatRotate.Angle = spriteAngle;

            _batAngleHistory.Enqueue(spriteAngle);
            while (_batAngleHistory.Count > 18)
                _batAngleHistory.Dequeue();
        }

        private Point GetBatPivotOnCanvas()
        {
            double batLeft = Canvas.GetLeft(GiantBat);
            double batTop = Canvas.GetTop(GiantBat);

            double pivotX = !_firstTowardsRight ? batLeft + (300 * 0.1) : batLeft - 140;
            double pivotY = batTop + 70;

            return new Point(pivotX, pivotY);
        }

        private Point GetBatTipOnCanvas(double logicalAngle)
        {
            Point pivot = GetBatPivotOnCanvas();

            double spriteAngle = logicalAngle + BatNorthOffset;
            double radians = spriteAngle * Math.PI / 180.0;

            // distance pivot -> extrémité de frappe
            double tipDistance = 270;

            // 0° logique = nord, mais on applique l’offset sprite dans spriteAngle
            double x = pivot.X + Math.Sin(radians) * tipDistance;
            double y = pivot.Y - Math.Cos(radians) * tipDistance;

            return new Point(x, y);
        }
        
        private void UpdateTrailVisibility(double t)
        {
            bool showBallTrails = t > 0.10 && t < ImpactTime;
            bool showBatTrails = t > IdleEnd && t < 1.60;

            BallTrail1.Visibility = showBallTrails ? Visibility.Visible : Visibility.Collapsed;
            BallTrail2.Visibility = showBallTrails ? Visibility.Visible : Visibility.Collapsed;
            BallTrail3.Visibility = showBallTrails ? Visibility.Visible : Visibility.Collapsed;

            BatTrail1.Visibility = showBatTrails ? Visibility.Visible : Visibility.Collapsed;
            BatTrail2.Visibility = showBatTrails ? Visibility.Visible : Visibility.Collapsed;
            BatTrail3.Visibility = showBatTrails ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateTrails()
        {
            var ballPositions = _ballHistory.ToArray();
            var ballAngles = _ballAngleHistory.ToArray();
            var batAngles = _batAngleHistory.ToArray();

            ApplyBallTrail(BallTrail1, BallTrail1Rotate, ballPositions, ballAngles, 3);
            ApplyBallTrail(BallTrail2, BallTrail2Rotate, ballPositions, ballAngles, 6);
            ApplyBallTrail(BallTrail3, BallTrail3Rotate, ballPositions, ballAngles, 9);

            ApplyBatTrail(BatTrail1Rotate, batAngles, 2);
            ApplyBatTrail(BatTrail2Rotate, batAngles, 4);
            ApplyBatTrail(BatTrail3Rotate, batAngles, 6);
        }

        private void UpdateImpactVisuals(double t)
        {
            if (t < ImpactTime)
            {
                HitFlash.Opacity = 0;
                HitRing.Opacity = 0;
                DustCloud.Opacity = 0;
                return;
            }

            double p = Math.Min(1.0, (t - ImpactTime) / (TotalDuration - ImpactTime));

            HitFlash.Opacity = Math.Max(0, 1.0 - p * 2.2);
            HitFlashScale.ScaleX = 0.2 + p * 5.5;
            HitFlashScale.ScaleY = 0.2 + p * 5.5;

            HitRing.Opacity = Math.Max(0, 0.95 - p * 1.1);
            HitRingScale.ScaleX = 0.2 + p * 3.4;
            HitRingScale.ScaleY = 0.2 + p * 3.4;

            DustCloud.Opacity = Math.Max(0, 0.75 - p * 0.8);
            DustCloudScale.ScaleX = 0.5 + p * 2.0;
            DustCloudScale.ScaleY = 0.5 + p * 2.0;

            Canvas.SetTop(DustCloud, (_finalGoalPoint.Y - 50) - (p * 70));
        }

        private void PositionBatBase()
        {
            if (_firstTowardsRight)
            {
                Canvas.SetLeft(GiantBat, _batZonePoint.X - 70);
                Canvas.SetTop(GiantBat, _batZonePoint.Y - 18);

                Canvas.SetLeft(BatTrail1, _batZonePoint.X - 70);
                Canvas.SetTop(BatTrail1, _batZonePoint.Y - 18);

                Canvas.SetLeft(BatTrail2, _batZonePoint.X - 70);
                Canvas.SetTop(BatTrail2, _batZonePoint.Y - 18);

                Canvas.SetLeft(BatTrail3, _batZonePoint.X - 70);
                Canvas.SetTop(BatTrail3, _batZonePoint.Y - 18);
            }
            else
            {
                Canvas.SetLeft(GiantBat, _batZonePoint.X - 120);
                Canvas.SetTop(GiantBat, _batZonePoint.Y - 18);

                Canvas.SetLeft(BatTrail1, _batZonePoint.X - 120);
                Canvas.SetTop(BatTrail1, _batZonePoint.Y - 18);

                Canvas.SetLeft(BatTrail2, _batZonePoint.X - 120);
                Canvas.SetTop(BatTrail2, _batZonePoint.Y - 18);

                Canvas.SetLeft(BatTrail3, _batZonePoint.X - 120);
                Canvas.SetTop(BatTrail3, _batZonePoint.Y - 18);
            }
        }

        private void PositionBatTrailBase(Canvas batTrail)
        {
            Canvas.SetLeft(batTrail, Canvas.GetLeft(GiantBat));
            Canvas.SetTop(batTrail, Canvas.GetTop(GiantBat));
        }

        private void SetBallPosition(Point center)
        {
            Canvas.SetLeft(BaseballBall, center.X - BallSize / 2);
            Canvas.SetTop(BaseballBall, center.Y - BallSize / 2);
        }

        private void SetBallTrail(Canvas trail, Point center, double angle)
        {
            Canvas.SetLeft(trail, center.X - BallSize / 2);
            Canvas.SetTop(trail, center.Y - BallSize / 2);

            if (trail.RenderTransform is RotateTransform rt)
                rt.Angle = angle;
        }

        private void ApplyBallTrail(Canvas trail, RotateTransform rotate, Point[] positions, double[] angles, int historyIndex)
        {
            if (positions.Length == 0)
                return;

            int idx = positions.Length - 1 - historyIndex;
            if (idx < 0) idx = 0;

            Canvas.SetLeft(trail, positions[idx].X - BallSize / 2);
            Canvas.SetTop(trail, positions[idx].Y - BallSize / 2);

            if (angles.Length > idx)
                rotate.Angle = angles[idx];
        }

        private void ApplyBatTrail(RotateTransform rotate, double[] angles, int historyIndex)
        {
            if (angles.Length == 0)
                return;

            int idx = angles.Length - 1 - historyIndex;
            if (idx < 0) idx = 0;

            rotate.Angle = angles[idx];
        }

        private void CreateDebris(Point center)
        {
            for (int i = 0; i < 26; i++)
            {
                Shape p = (i % 2 == 0)
                    ? new Ellipse
                    {
                        Width = _rng.Next(6, 12),
                        Height = _rng.Next(6, 12),
                        Fill = new SolidColorBrush(PickImpactColor()),
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        Opacity = 0.95
                    }
                    : new Rectangle
                    {
                        Width = _rng.Next(5, 10),
                        Height = _rng.Next(14, 26),
                        RadiusX = 2,
                        RadiusY = 2,
                        Fill = new SolidColorBrush(PickWoodColor()),
                        Stroke = Brushes.Black,
                        StrokeThickness = 1,
                        Opacity = 0.95
                    };

                double w = p.Width;
                double h = p.Height;

                Canvas.SetLeft(p, center.X - w / 2);
                Canvas.SetTop(p, center.Y - h / 2);
                ParticlesLayer.Children.Add(p);
            }
        }

        private Point Lerp(Point a, Point b, double t)
        {
            return new Point(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t);
        }

        private double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        private double EaseInOut(double t)
        {
            return t < 0.5
                ? 2 * t * t
                : 1 - Math.Pow(-2 * t + 2, 2) / 2;
        }

        private double EaseOutCubic(double t)
        {
            return 1 - Math.Pow(1 - t, 3);
        }

        private Color PickWoodColor()
        {
            Color[] colors =
            {
                (Color)ColorConverter.ConvertFromString("#FFD7A86E"),
                (Color)ColorConverter.ConvertFromString("#FFC68A52"),
                (Color)ColorConverter.ConvertFromString("#FFB77A43"),
                (Color)ColorConverter.ConvertFromString("#FF8D6E63")
            };
            return colors[_rng.Next(colors.Length)];
        }

        private Color PickImpactColor()
        {
            Color[] colors =
            {
                (Color)ColorConverter.ConvertFromString("#FFFFFFFF"),
                (Color)ColorConverter.ConvertFromString("#FFFFF176"),
                (Color)ColorConverter.ConvertFromString("#FFFFB300"),
                (Color)ColorConverter.ConvertFromString("#FFFF7043")
            };
            return colors[_rng.Next(colors.Length)];
        }

        public void Dispose()
        {
            EndSequence();
        }
    }
}