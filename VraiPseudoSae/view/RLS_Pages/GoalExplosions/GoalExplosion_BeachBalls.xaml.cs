using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using VraiPseudoSae.data.GoalExplosion;
using VraiPseudoSae.Utils.AudioPlayer;

namespace VraiPseudoSae.view.RLS_Pages.GoalExplosions
{
    public partial class GoalExplosion_Beachballs : GoalExplosionBase, IDisposable
    {
        private readonly string _audioKey = "goal_sound/Beachballs";
        private readonly Stopwatch _watch = new Stopwatch();
        private readonly Random _rng = new Random();

        private bool _isPlaying;
        private bool _towardsRight;

        // ---- Timings ----
        private const double TotalDuration  = 4.5;
        private const double BallTravelEnd  = 1.8;
        private const double ImpactTime     = 1.8;
        private const double TextStart      = 1.83;
        private const double LensDropStart  = 1.85;

        // ---- Particules eau volantes ----
        private const int WaterDropCount = 90;
        private readonly List<WaterDrop> _waterDrops = new List<WaterDrop>();

        // ---- Débris ballon ----
        private const int DebrisCount = 14;
        private readonly List<BallDebris> _debris = new List<BallDebris>();

        // ---- Gouttes lentille caméra ----
        private const int LensDropCount = 10;
        private readonly List<LensDrop> _lensDrops = new List<LensDrop>();

        // =================== Classes internes ===================

        private class WaterDrop
        {
            public Ellipse Visual   { get; set; }
            public Point   Start    { get; set; }
            public Vector  Velocity { get; set; }
            public double  Gravity  { get; set; }
            public double  Delay    { get; set; }
            public double  Lifetime { get; set; }
        }

        private class BallDebris
        {
            public Path   Visual     { get; set; }
            public Point  Start      { get; set; }
            public Vector Velocity   { get; set; }
            public double Gravity    { get; set; }
            public double Delay      { get; set; }
            public double Lifetime   { get; set; }
            public double RotSpeed   { get; set; }
            public double StartAngle { get; set; }
        }

        private class LensDrop
        {
            public Ellipse    Body       { get; set; }
            public BlurEffect Blur       { get; set; }
            public double X              { get; set; }
            public double Y              { get; set; }
            public double W              { get; set; }
            public double H              { get; set; }
            public double Delay          { get; set; }
            public double Lifetime       { get; set; }
            public double SlideSpeed     { get; set; }
        }

        // =================== Constructeur ===================

        public GoalExplosion_Beachballs(Canvas gameCanvas, JsonPakAudioService audio) : base(gameCanvas, audio)
        {
            InitializeComponent();
            _audio?.Preload(_audioKey, "goal_beachballs");
            HideAll();
            BuildWaterDrops();
            BuildDebris();
            BuildLensDrops();
        }

        // =================== Entrées publiques ===================

        public override void PlayLeftGoal()  => StartSequence(false);
        public override void PlayRightGoal() => StartSequence(true);

        // =================== Contrôle de la séquence ===================

        private void StartSequence(bool towardsRight)
        {
            StopInternal();
            _towardsRight = towardsRight;
            ResetState();
            ShowAll();
            _audio?.Play("goal_beachballs");
            _isPlaying = true;
            _watch.Restart();
            CompositionTarget.Rendering += OnRendering;
        }

        private void StopInternal()
        {
            if (!_isPlaying) return;
            CompositionTarget.Rendering -= OnRendering;
            _watch.Stop();
            _isPlaying = false;
        }

        private void EndSequence()
        {
            StopInternal();
            HideAll();
        }

        // =================== Visibilité ===================

        private void HideAll()
        {
            SandPileL0.Visibility = Visibility.Collapsed;
            SandPileL1.Visibility = Visibility.Collapsed;
            SandPileR0.Visibility = Visibility.Collapsed;
            SandPileR1.Visibility = Visibility.Collapsed;
            Bucket1.Visibility    = Visibility.Collapsed;
            Shovel1.Visibility    = Visibility.Collapsed;
            Bucket2.Visibility    = Visibility.Collapsed;
            Shovel2.Visibility    = Visibility.Collapsed;
            Shell1.Visibility     = Visibility.Collapsed;
            Shell2.Visibility     = Visibility.Collapsed;
            Shell3.Visibility     = Visibility.Collapsed;
            BeachBall.Visibility          = Visibility.Collapsed;
            BallDebrisLayer.Visibility    = Visibility.Collapsed;
            ShockRing.Visibility          = Visibility.Collapsed;
            WaterParticleLayer.Visibility = Visibility.Collapsed;
            SplashBurst.Visibility        = Visibility.Collapsed;
            SplashText.Visibility         = Visibility.Collapsed;
            LensDropLayer.Visibility      = Visibility.Collapsed;
        }

        private void ShowAll()
        {
            SandPileL0.Visibility = Visibility.Visible;
            SandPileL1.Visibility = Visibility.Visible;
            SandPileR0.Visibility = Visibility.Visible;
            SandPileR1.Visibility = Visibility.Visible;
            Bucket1.Visibility    = Visibility.Visible;
            Shovel1.Visibility    = Visibility.Visible;
            Bucket2.Visibility    = Visibility.Visible;
            Shovel2.Visibility    = Visibility.Visible;
            Shell1.Visibility     = Visibility.Visible;
            Shell2.Visibility     = Visibility.Visible;
            Shell3.Visibility     = Visibility.Visible;
            BeachBall.Visibility          = Visibility.Visible;
            BallDebrisLayer.Visibility    = Visibility.Visible;
            ShockRing.Visibility          = Visibility.Visible;
            WaterParticleLayer.Visibility = Visibility.Visible;
            SplashBurst.Visibility        = Visibility.Visible;
            SplashText.Visibility         = Visibility.Visible;
            LensDropLayer.Visibility      = Visibility.Visible;
        }

        // =================== Construction des particules ===================

        private void BuildWaterDrops()
        {
            WaterParticleLayer.Children.Clear();
            _waterDrops.Clear();
            for (int i = 0; i < WaterDropCount; i++)
            {
                double size = 6 + _rng.Next(0, 14);
                var drop = new Ellipse
                {
                    Width  = size,
                    Height = size * (1.0 + _rng.NextDouble() * 0.55),
                    Fill   = new SolidColorBrush(Color.FromArgb(200, 140, 220, 255)),
                    Stroke = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
                    StrokeThickness = 1.2,
                    Opacity    = 0,
                    Visibility = Visibility.Collapsed
                };
                WaterParticleLayer.Children.Add(drop);
                _waterDrops.Add(new WaterDrop { Visual = drop });
            }
        }

        private static readonly string[] DebrisColors =
        {
            "#FFFF4455", "#FFFFD700", "#FF22DDCC", "#FFFFFFFF",
            "#FFFF4455", "#FFFFD700", "#FF22DDCC", "#FFFFFFFF"
        };

        private void BuildDebris()
        {
            BallDebrisLayer.Children.Clear();
            _debris.Clear();
            for (int i = 0; i < DebrisCount; i++)
            {
                var piece = new Path
                {
                    Fill = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString(DebrisColors[i % DebrisColors.Length])),
                    Data = Geometry.Parse("M 0,0 L 28,10 L 22,32 Z"),
                    Opacity    = 0,
                    Visibility = Visibility.Collapsed,
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };
                BallDebrisLayer.Children.Add(piece);
                _debris.Add(new BallDebris { Visual = piece });
            }
        }

        private void BuildLensDrops()
        {
            LensDropLayer.Children.Clear();
            _lensDrops.Clear();
            for (int i = 0; i < LensDropCount; i++)
            {
                double w = 14 + _rng.NextDouble() * 26;
                double h = w  * (1.2 + _rng.NextDouble() * 0.6);
                var blur = new BlurEffect { Radius = 0 };
                var body = new Ellipse
                {
                    Width  = w, Height = h,
                    Opacity = 0, Visibility = Visibility.Collapsed,
                    Effect = blur,
                    RenderTransformOrigin = new Point(0.5, 0.5)
                };
                body.Fill = new RadialGradientBrush(new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb(0,   200, 230, 255), 0.0),
                    new GradientStop(Color.FromArgb(15,  160, 210, 255), 0.50),
                    new GradientStop(Color.FromArgb(55,  120, 185, 240), 0.78),
                    new GradientStop(Color.FromArgb(110,  80, 150, 220), 1.0)
                });
                LensDropLayer.Children.Add(body);
                _lensDrops.Add(new LensDrop
                {
                    Body = body, Blur = blur,
                    X = 50 + _rng.NextDouble() * 880,
                    Y = 50 + _rng.NextDouble() * 420,
                    W = w, H = h,
                    Delay      = _rng.NextDouble() * 0.60,
                    Lifetime   = 2.2 + _rng.NextDouble() * 1.2,
                    SlideSpeed = 8   + _rng.NextDouble() * 18
                });
            }
        }

        // =================== Reset ===================

        private void ResetState()
        {
            // ======================================================
            // CAGES (RLSGame / RLSController) :
            //   Cage GAUCHE  : X=0,   Y=365, sprite 26x160
            //     poteau bord-terrain : X=26  |  poteau fond : X=0    |  sol : Y=520
            //   Cage DROITE  : X=974, Y=365, sprite 26x160 retourne
            //     poteau bord-terrain : X=974 |  poteau fond : X=1000 |  sol : Y=520
            //
            // PlayRightGoal (_towardsRight=true)  : GOAL P1 -> cage DROITE touchee
            // PlayLeftGoal  (_towardsRight=false) : GOAL P2 -> cage GAUCHE touchee
            // ======================================================

            double poteauBord = _towardsRight ? 974  : 26;
            double poteauFond = _towardsRight ? 1000 : 0;
            double solY       = 510;
            double signIn     = _towardsRight ? -1 : 1; // direction vers centre terrain

            // Tas de sable centrés sur chaque poteau
            Canvas.SetLeft(SandPileL0, poteauBord - SandPileL0.Width / 2);
            Canvas.SetTop (SandPileL0, solY - SandPileL0.Height / 2);
            Canvas.SetLeft(SandPileL1, poteauBord - SandPileL1.Width / 2);
            Canvas.SetTop (SandPileL1, solY - SandPileL0.Height / 2 - 7);

            Canvas.SetLeft(SandPileR0, poteauFond - SandPileR0.Width / 2);
            Canvas.SetTop (SandPileR0, solY - SandPileR0.Height / 2);
            Canvas.SetLeft(SandPileR1, poteauFond - SandPileR1.Width / 2);
            Canvas.SetTop (SandPileR1, solY - SandPileR0.Height / 2 - 7);

            // Accessoires autour du poteau bord-terrain
            Canvas.SetLeft(Bucket1, poteauBord + signIn * 8);
            Canvas.SetTop (Bucket1, solY - 44);

            Canvas.SetLeft(Shovel1, poteauBord + signIn * 52);
            Canvas.SetTop (Shovel1, solY - 50);

            // Accessoires autour du poteau fond
            Canvas.SetLeft(Bucket2, poteauFond + signIn * 5);
            Canvas.SetTop (Bucket2, solY - 38);

            Canvas.SetLeft(Shovel2, poteauFond + signIn * 38);
            Canvas.SetTop (Shovel2, solY - 44);

            // Coquillages repartis dans la cage
            double cageCx = (poteauBord + poteauFond) / 2.0;
            Canvas.SetLeft(Shell1, cageCx + signIn * 2);
            Canvas.SetTop (Shell1, solY - 8);
            Canvas.SetLeft(Shell2, poteauFond + signIn * 18);
            Canvas.SetTop (Shell2, solY - 6);
            Canvas.SetLeft(Shell3, poteauBord + signIn * 25);
            Canvas.SetTop (Shell3, solY - 5);

            // Opacités à zéro
            SandPileL0.Opacity = 0; SandPileL1.Opacity = 0;
            SandPileR0.Opacity = 0; SandPileR1.Opacity = 0;
            Bucket1.Opacity = 0; Shovel1.Opacity = 0;
            Bucket2.Opacity = 0; Shovel2.Opacity = 0;
            Shell1.Opacity  = 0; Shell2.Opacity  = 0; Shell3.Opacity = 0;

            // Ballon : part du côté opposé à la cage visée
            // PlayRightGoal -> cage droite -> balle arrive depuis la gauche
            // PlayLeftGoal  -> cage gauche -> balle arrive depuis la droite
            BeachBall.Opacity = 0;
            Canvas.SetLeft(BeachBall, _towardsRight ? 80 : 740);
            Canvas.SetTop (BeachBall, 340);
            BeachBallRotate.Angle = 0;
            BeachBallScale.ScaleX = 1;
            BeachBallScale.ScaleY = 1;

            // Onde de choc
            ShockRing.Opacity     = 0;
            ShockRingScale.ScaleX = 1;
            ShockRingScale.ScaleY = 1;

            // Texte Splash (centré sur les 1000px)
            SplashBurst.Opacity    = 0;
            SplashText.Opacity     = 0;
            Canvas.SetLeft(SplashText, 310);
            Canvas.SetTop (SplashText, 300);

            // Point d'impact = fond de la cage
            double impactX = _towardsRight ? 990 : 10;
            double impactY = 440;
            Point  burst   = new Point(impactX, impactY);

            // Débris : demi-cercle vers l'intérieur du terrain
            for (int i = 0; i < _debris.Count; i++)
            {
                var    d        = _debris[i];
                double midAngle = _towardsRight ? 180.0 : 0.0;
                double angle    = midAngle - 90 + _rng.NextDouble() * 180;
                double speed    = 260 + _rng.NextDouble() * 320;
                double rad      = angle * Math.PI / 180.0;

                d.Start      = new Point(burst.X + _rng.Next(-10, 11), burst.Y + _rng.Next(-12, 13));
                d.Velocity   = new Vector(Math.Cos(rad) * speed, Math.Sin(rad) * speed - 60);
                d.Gravity    = 680 + _rng.NextDouble() * 200;
                d.Delay      = _rng.NextDouble() * 0.04;
                d.Lifetime   = 0.70 + _rng.NextDouble() * 0.50;
                d.RotSpeed   = (_rng.NextDouble() > 0.5 ? 1 : -1) * (180 + _rng.NextDouble() * 360);
                d.StartAngle = _rng.NextDouble() * 360;

                Canvas.SetLeft(d.Visual, d.Start.X);
                Canvas.SetTop (d.Visual, d.Start.Y);
                d.Visual.Opacity         = 0;
                d.Visual.Visibility      = Visibility.Collapsed;
                d.Visual.RenderTransform = new TransformGroup();
            }

            // Gouttes d'eau volantes : même sens
            for (int i = 0; i < _waterDrops.Count; i++)
            {
                var    drop     = _waterDrops[i];
                double midAngle = _towardsRight ? 180.0 : 0.0;
                double angle    = midAngle - 90 + _rng.NextDouble() * 180;
                double speed    = 180 + _rng.NextDouble() * 420;
                double rad      = angle * Math.PI / 180.0;

                drop.Start    = new Point(burst.X + _rng.Next(-18, 19), burst.Y + _rng.Next(-12, 16));
                drop.Velocity = new Vector(Math.Cos(rad) * speed, Math.Sin(rad) * speed - 30);
                drop.Gravity  = 540 + _rng.NextDouble() * 200;
                drop.Delay    = _rng.NextDouble() * 0.10;
                drop.Lifetime = 0.85 + _rng.NextDouble() * 0.55;

                Canvas.SetLeft(drop.Visual, drop.Start.X);
                Canvas.SetTop (drop.Visual, drop.Start.Y);
                drop.Visual.Opacity         = 0;
                drop.Visual.Visibility      = Visibility.Collapsed;
                drop.Visual.RenderTransform = null;
            }

            // Gouttes lentille : position aléatoire sur tout l'écran
            for (int i = 0; i < _lensDrops.Count; i++)
            {
                var ld = _lensDrops[i];
                ld.X          = 30 + _rng.NextDouble() * 940;
                ld.Y          = 30 + _rng.NextDouble() * 500;
                ld.Delay      = _rng.NextDouble() * 0.50;
                ld.Lifetime   = 2.0 + _rng.NextDouble() * 1.6;
                ld.SlideSpeed = 8   + _rng.NextDouble() * 18;

                Canvas.SetLeft(ld.Body, ld.X);
                Canvas.SetTop (ld.Body, ld.Y);
                ld.Body.Width      = ld.W;
                ld.Body.Height     = ld.H;
                ld.Body.Opacity    = 0;
                ld.Body.Visibility = Visibility.Collapsed;
                ld.Blur.Radius     = 0;
            }
        }

        // =================== Boucle de rendu ===================

        private void OnRendering(object sender, EventArgs e)
        {
            double t = _watch.Elapsed.TotalSeconds;
            if (t >= TotalDuration) { EndSequence(); return; }

            UpdateProps(t);
            UpdateBeachBall(t);
            UpdateShockRing(t);
            UpdateDebris(t);
            UpdateSplashBurst(t);
            UpdateSplashText(t);
            UpdateWaterDrops(t);
            UpdateLensDrops(t);
        }

        // ---- Décors de plage ----
        private void UpdateProps(double t)
        {
            double p     = Clamp01(t / 0.32);
            double pSlow = Clamp01(t / 0.55);

            SandPileL0.Opacity = p;
            SandPileL1.Opacity = p;
            SandPileR0.Opacity = p;
            SandPileR1.Opacity = p;

            Bucket1.Opacity = pSlow;
            Shovel1.Opacity = pSlow;
            Bucket2.Opacity = pSlow;
            Shovel2.Opacity = pSlow;
            Shell1.Opacity  = pSlow;
            Shell2.Opacity  = pSlow;
            Shell3.Opacity  = Clamp01((t - 0.12) / 0.40);
        }

        // ---- Gros ballon ----
        private void UpdateBeachBall(double t)
        {
            if (t > ImpactTime + 0.25) { BeachBall.Opacity = 0; return; }

            BeachBall.Opacity = 1;

            double p = Clamp01(t / BallTravelEnd);

            // PlayRightGoal -> cage droite (X=974..1000)
            //   Left=80 -> Left=820  (centre ballon 180px -> 910, bien dans cage)
            // PlayLeftGoal  -> cage gauche (X=0..26)
            //   Left=740 -> Left=-100 (centre ballon -> 10, bien dans cage)
            double startX = _towardsRight ? 80  : 740;
            double endX   = _towardsRight ? 820 : -100;
            double startY = 340;
            double endY   = 355; // centre cage Y=365..520 -> 442, ballon 180px -> top=352

            double x = Lerp(startX, endX, EaseOutCubic(p));
            double y = Lerp(startY, endY, p) - Math.Sin(p * Math.PI) * 60;

            Canvas.SetLeft(BeachBall, x);
            Canvas.SetTop (BeachBall, y);

            BeachBallRotate.Angle = (_towardsRight ? 1 : -1) * p * 720;

            if (t >= ImpactTime)
            {
                double ip = Clamp01((t - ImpactTime) / 0.18);
                BeachBallScale.ScaleX = 1.0 + ip * 0.60;
                BeachBallScale.ScaleY = 1.0 - ip * 0.52;
                BeachBall.Opacity     = 1.0 - ip;
            }
            else
            {
                double wobble = Math.Sin(t * 9.0) * 0.055;
                BeachBallScale.ScaleX = 1.0 + wobble;
                BeachBallScale.ScaleY = 1.0 - wobble * 0.7;
            }
        }

        // ---- Onde de choc ----
        private void UpdateShockRing(double t)
        {
            if (t < ImpactTime) { ShockRing.Visibility = Visibility.Collapsed; return; }

            double ip = Clamp01((t - ImpactTime) / 0.40);
            ShockRing.Visibility = Visibility.Visible;

            double cx = _towardsRight ? 990 : 10;
            Canvas.SetLeft(ShockRing, cx - ShockRing.Width  / 2);
            Canvas.SetTop (ShockRing, 440 - ShockRing.Height / 2);

            ShockRingScale.ScaleX = 1.0 + ip * 22;
            ShockRingScale.ScaleY = 1.0 + ip * 22;
            ShockRing.Opacity     = Math.Max(0, 1.0 - ip * 1.4);
        }

        // ---- Débris ballon ----
        private void UpdateDebris(double t)
        {
            for (int i = 0; i < _debris.Count; i++)
            {
                var    d  = _debris[i];
                double lt = t - ImpactTime - d.Delay;

                if (lt <= 0 || lt >= d.Lifetime) { d.Visual.Visibility = Visibility.Collapsed; continue; }

                d.Visual.Visibility = Visibility.Visible;

                double x     = d.Start.X + d.Velocity.X * lt;
                double y     = d.Start.Y + d.Velocity.Y * lt + 0.5 * d.Gravity * lt * lt;
                double lifeP = lt / d.Lifetime;

                Canvas.SetLeft(d.Visual, x);
                Canvas.SetTop (d.Visual, y);
                d.Visual.Opacity         = 1.0 - lifeP * lifeP;
                d.Visual.RenderTransform = new RotateTransform(d.StartAngle + d.RotSpeed * lt, 14, 16);
            }
        }

        // ---- Éclaboussures statiques ----
        private void UpdateSplashBurst(double t)
        {
            if (t < ImpactTime) { SplashBurst.Opacity = 0; return; }
            SplashBurst.Opacity = 1.0 - Clamp01((t - ImpactTime) / 0.35);
        }

        // ---- Texte « Splash ! » squishy ----
        private void UpdateSplashText(double t)
        {
            if (t < TextStart) { SplashText.Opacity = 0; SplashText.Visibility = Visibility.Collapsed; return; }

            SplashText.Visibility = Visibility.Visible;

            double p       = Clamp01((t - TextStart) / 0.70);
            const double startY  = 300;
            const double peakY   = 160;
            const double settleY = 180;

            if (p < 0.40)
            {
                double bp = p / 0.40;
                Canvas.SetTop(SplashText, Lerp(startY, peakY, EaseOutBack(bp)));
                SplashText.Opacity     = Lerp(0, 1, bp);
            }
            else
            {
                double sp     = (p - 0.40) / 0.60;
                double squish = Math.Sin(sp * Math.PI * 3.5) * 0.20 * (1.0 - sp);
                Canvas.SetTop(SplashText, Lerp(peakY, settleY, EaseOutCubic(sp)));
                SplashText.Opacity     = 1.0;
            }
        }

        // ---- Particules d'eau volantes ----
        private void UpdateWaterDrops(double t)
        {
            for (int i = 0; i < _waterDrops.Count; i++)
            {
                var    drop = _waterDrops[i];
                double lt   = t - ImpactTime - drop.Delay;

                if (lt <= 0 || lt >= drop.Lifetime) { drop.Visual.Visibility = Visibility.Collapsed; continue; }

                drop.Visual.Visibility = Visibility.Visible;

                double x     = drop.Start.X + drop.Velocity.X * lt;
                double y     = drop.Start.Y + drop.Velocity.Y * lt + 0.5 * drop.Gravity * lt * lt;
                double lifeP = lt / drop.Lifetime;
                double vy    = drop.Velocity.Y + drop.Gravity * lt;
                double stretch = 1.0 + Math.Min(0.8, Math.Sqrt(drop.Velocity.X * drop.Velocity.X + vy * vy) / 600.0);

                Canvas.SetLeft(drop.Visual, x);
                Canvas.SetTop (drop.Visual, y);
                drop.Visual.Opacity = 1.0 - lifeP;
                drop.Visual.RenderTransformOrigin = new Point(0.5, 0.5);
                drop.Visual.RenderTransform = new ScaleTransform(1.0 / stretch, Math.Max(0.5, stretch - lifeP * 0.4));
            }
        }

        // ---- Gouttes sur lentille caméra ----
        private void UpdateLensDrops(double t)
        {
            double tLocal = t - LensDropStart;
            for (int i = 0; i < _lensDrops.Count; i++)
            {
                var    ld = _lensDrops[i];
                double lt = tLocal - ld.Delay;

                if (lt <= 0 || lt >= ld.Lifetime) { ld.Body.Visibility = Visibility.Collapsed; ld.Blur.Radius = 0; continue; }

                ld.Body.Visibility = Visibility.Visible;

                double lifeP    = lt / ld.Lifetime;
                Canvas.SetLeft(ld.Body, ld.X);
                Canvas.SetTop (ld.Body, ld.Y + ld.SlideSpeed * lt);

                double alpha;
                if      (lifeP < 0.12) alpha = lifeP / 0.12;
                else if (lifeP < 0.70) alpha = 1.0;
                else                   alpha = 1.0 - (lifeP - 0.70) / 0.30;
                ld.Body.Opacity  = alpha;

                double blurPeak  = 1.0 - Math.Abs(lifeP - 0.4) / 0.6;
                ld.Blur.Radius   = Math.Max(0, blurPeak * ld.W * 0.12);
                ld.Body.Height   = ld.H * (1.0 + lifeP * 0.18);
                ld.Body.Width    = ld.W / (1.0 + lifeP * 0.06);
            }
        }

        // =================== Utilitaires math ===================

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;
        private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
        private static double EaseOutCubic(double t) { double inv = 1 - t; return 1 - inv * inv * inv; }
        private static double EaseOutBack(double t)
        {
            const double c1 = 1.70158, c3 = c1 + 1;
            double x = t - 1;
            return 1 + c3 * x * x * x + c1 * x * x;
        }

        public void Dispose() => EndSequence();
    }
}
