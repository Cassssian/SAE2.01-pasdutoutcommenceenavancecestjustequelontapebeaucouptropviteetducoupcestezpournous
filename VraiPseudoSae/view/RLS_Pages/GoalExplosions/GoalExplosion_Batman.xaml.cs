using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VraiPseudoSae.data.GoalExplosion;
using VraiPseudoSae.Utils.AudioPlayer;

namespace VraiPseudoSae.view.RLS_Pages.GoalExplosions
{
    public partial class GoalExplosion_Batman : GoalExplosionBase, IDisposable
    {
        private readonly string _audioKey = "goal_sound/Batman";
        private readonly Stopwatch _watch = new Stopwatch();

        private bool _isPlaying;
        private bool _towardsRight;

        private readonly Queue<Point> _heroHistory = new Queue<Point>();

        private const double TotalDuration = 2.8;
        private const double IntroEnd = 0.45;
        private const double BeamEnd = 1.10;
        private const double HeroStart = 1.00;
        private const double HeroHit = 1.95;
        private const double TextStart = 1.32;
        private const double FlashStart = 1.92;

        public GoalExplosion_Batman(Canvas gameCanvas, JsonPakAudioService audio) : base(gameCanvas, audio)
        {
            InitializeComponent();
            _audio?.Preload(_audioKey, "goal_batman");
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

        private void StartSequence(bool towardsRight)
        {
            StopInternal();

            _towardsRight = towardsRight;
            ResetState();
            ShowAll();

            _audio?.Play("goal_batman");

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
            RoofTop.Visibility = Visibility.Collapsed;
            SearchLight.Visibility = Visibility.Collapsed;
            Beam.Visibility = Visibility.Collapsed;
            BatEmblem.Visibility = Visibility.Collapsed;
            Hero.Visibility = Visibility.Collapsed;
            HeroTrail1.Visibility = Visibility.Collapsed;
            HeroTrail2.Visibility = Visibility.Collapsed;
            ImpactFlash.Visibility = Visibility.Collapsed;
            ComicText.Visibility = Visibility.Collapsed;
        }

        private void ShowAll()
        {
            NightFade.Visibility = Visibility.Visible;
            Moon.Visibility = Visibility.Visible;
            BackBuildings.Visibility = Visibility.Visible;
            FrontBuildings.Visibility = Visibility.Visible;
            RoofTop.Visibility = Visibility.Visible;
            SearchLight.Visibility = Visibility.Visible;
            Beam.Visibility = Visibility.Visible;
            BatEmblem.Visibility = Visibility.Visible;
            Hero.Visibility = Visibility.Visible;
            HeroTrail1.Visibility = Visibility.Visible;
            HeroTrail2.Visibility = Visibility.Visible;
            ImpactFlash.Visibility = Visibility.Visible;
            ComicText.Visibility = Visibility.Visible;
        }

        private void ResetState()
        {
            _heroHistory.Clear();

            NightFade.Opacity = 0;
            Moon.Opacity = 0;
            BackBuildings.Opacity = 0;
            FrontBuildings.Opacity = 0;
            RoofTop.Opacity = 0;
            SearchLight.Opacity = 0;
            Beam.Opacity = 0;
            BatEmblem.Opacity = 0;
            ImpactFlash.Opacity = 0;
            ComicText.Opacity = 0;

            ImpactFlashScale.ScaleX = 0.2;
            ImpactFlashScale.ScaleY = 0.2;

            HeroRotate.Angle = _towardsRight ? -10 : 10;
            HeroScale.ScaleX = _towardsRight ? 1 : -1;
            HeroScale.ScaleY = 1;

            double startX = _towardsRight ? -260 : 1030;
            double startY = 215;

            Canvas.SetLeft(Hero, startX);
            Canvas.SetTop(Hero, startY);

            Canvas.SetLeft(HeroTrail1, startX);
            Canvas.SetTop(HeroTrail1, startY);

            Canvas.SetLeft(HeroTrail2, startX);
            Canvas.SetTop(HeroTrail2, startY);

            Canvas.SetLeft(ImpactFlash, 440);
            Canvas.SetTop(ImpactFlash, 220);

            Canvas.SetLeft(ComicText, 255);
            Canvas.SetTop(ComicText, 82);

            if (!_towardsRight)
            {
                Canvas.SetLeft(SearchLight, 110);
                Canvas.SetLeft(RoofTop, 90);
                Beam.Data = Geometry.Parse("M 180,240 L 70,20 L 390,20 Z");
                Canvas.SetLeft(BatEmblem, 145);
                ComicText.Text = "GOTHAM GUARD!";
            }
            else
            {
                Canvas.SetLeft(SearchLight, 690);
                Canvas.SetLeft(RoofTop, 650);
                Beam.Data = Geometry.Parse("M 740,240 L 610,20 L 920,20 Z");
                Canvas.SetLeft(BatEmblem, 675);
                ComicText.Text = "NIGHT STRIKE!";
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
            UpdateHero(t);
            UpdateText(t);
            UpdateFlash(t);
            UpdateTrails();
        }

        private void UpdateScene(double t)
        {
            NightFade.Opacity = Clamp01(t / 0.25);

            Moon.Opacity = Clamp01((t - 0.08) / 0.30) * 0.95;
            BackBuildings.Opacity = Clamp01((t - 0.10) / 0.32);
            FrontBuildings.Opacity = Clamp01((t - 0.16) / 0.35);
            RoofTop.Opacity = Clamp01((t - 0.18) / 0.25);

            SearchLight.Opacity = Clamp01((t - 0.25) / 0.22);

            if (t < IntroEnd)
            {
                Beam.Opacity = 0;
                BatEmblem.Opacity = 0;
                return;
            }

            double p = Clamp01((t - IntroEnd) / (BeamEnd - IntroEnd));

            Beam.Opacity = 0.55 + p * 0.20;
            BatEmblem.Opacity = p;
        }

        private void UpdateHero(double t)
        {
            if (t < HeroStart)
            {
                Hero.Opacity = 0;
                return;
            }

            Hero.Opacity = 1;

            double p = Clamp01((t - HeroStart) / (HeroHit - HeroStart));

            double startX = _towardsRight ? -260 : 1030;
            double endX = _towardsRight ? 390 : 380;
            double yBase = 215;

            double x = Lerp(startX, endX, p);
            double y = yBase - Math.Sin(p * Math.PI) * 35;

            Canvas.SetLeft(Hero, x);
            Canvas.SetTop(Hero, y);

            HeroRotate.Angle = _towardsRight
                ? Lerp(-10, 8, p)
                : Lerp(10, -8, p);

            _heroHistory.Enqueue(new Point(x, y));
            while (_heroHistory.Count > 8)
                _heroHistory.Dequeue();
        }

        private void UpdateText(double t)
        {
            if (t < TextStart)
            {
                ComicText.Opacity = 0;
                return;
            }

            double p = Clamp01((t - TextStart) / 0.25);
            ComicText.Opacity = p;
        }

        private void UpdateFlash(double t)
        {
            if (t < FlashStart)
            {
                ImpactFlash.Opacity = 0;
                return;
            }

            double p = Clamp01((t - FlashStart) / 0.35);

            ImpactFlash.Opacity = 1 - p;
            ImpactFlashScale.ScaleX = 0.2 + p * 4.5;
            ImpactFlashScale.ScaleY = 0.2 + p * 4.5;
        }

        private void UpdateTrails()
        {
            var history = _heroHistory.ToArray();
            if (history.Length == 0)
                return;

            int idx1 = Math.Max(0, history.Length - 3);
            int idx2 = Math.Max(0, history.Length - 6);

            Canvas.SetLeft(HeroTrail1, history[idx1].X);
            Canvas.SetTop(HeroTrail1, history[idx1].Y);

            Canvas.SetLeft(HeroTrail2, history[idx2].X);
            Canvas.SetTop(HeroTrail2, history[idx2].Y);
        }

        private double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        private double Clamp01(double value)
        {
            if (value < 0) return 0;
            if (value > 1) return 1;
            return value;
        }

        public override GoalExplosionType ToType()
        {
            return GoalExplosionType.Batman;
        }
        
        public void Dispose()
        {
            EndSequence();
        }
    }
}