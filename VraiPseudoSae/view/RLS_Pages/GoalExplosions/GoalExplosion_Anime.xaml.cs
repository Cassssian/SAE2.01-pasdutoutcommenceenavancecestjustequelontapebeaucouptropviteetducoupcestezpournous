using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using VraiPseudoSae.data.GoalExplosion;
using VraiPseudoSae.Utils.AudioPlayer;

namespace VraiPseudoSae.view.RLS_Pages.GoalExplosions
{
    public partial class GoalExplosion_Anime : GoalExplosionBase, IDisposable
    {
        private readonly string _audioKey = "goal_sound/Anime";
        private readonly Random _rng = new Random();

        public GoalExplosion_Anime(Canvas gameCanvas, JsonPakAudioService audio) : base(gameCanvas, audio)
        {
            InitializeComponent();
            _audio?.Preload(_audioKey, "goal_anime");
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
            // Positionner l’onde
            Canvas.SetLeft(Wave, center.X - 20);
            Canvas.SetTop(Wave, center.Y - 20);

            // Créer quelques particules autour du centre
            CreateParticles(center);

            // Animation onde
            var sbWave = new Storyboard();

            var scaleAnim = new DoubleAnimation
            {
                From = 0.2,
                To = 8,
                Duration = TimeSpan.FromMilliseconds(600),
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(scaleAnim, WaveScale);
            Storyboard.SetTargetProperty(scaleAnim, new PropertyPath("ScaleX"));
            sbWave.Children.Add(scaleAnim);

            var scaleAnimY = scaleAnim.Clone();
            Storyboard.SetTarget(scaleAnimY, WaveScale);
            Storyboard.SetTargetProperty(scaleAnimY, new PropertyPath("ScaleY"));
            sbWave.Children.Add(scaleAnimY);

            var opacityAnim = new DoubleAnimation
            {
                From = 0.9,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(600)
            };
            Storyboard.SetTarget(opacityAnim, Wave);
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));
            sbWave.Children.Add(opacityAnim);

            sbWave.Begin();

            // Lancer le son principal via son alias
            _audio?.Play("goal_anime");
        }

        private void CreateParticles(Point center)
        {
            ParticlesCanvas.Children.Clear();

            for (int i = 0; i < 20; i++)
            {
                var e = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(255, (byte)_rng.Next(100, 255), 255)),
                    Opacity = 0.0
                };

                Canvas.SetLeft(e, center.X);
                Canvas.SetTop(e, center.Y);
                ParticlesCanvas.Children.Add(e);

                double angle = _rng.NextDouble() * Math.PI * 2;
                double distance = 80 + _rng.NextDouble() * 120;

                double targetX = center.X + Math.Cos(angle) * distance;
                double targetY = center.Y + Math.Sin(angle) * distance;

                var sb = new Storyboard();

                var animX = new DoubleAnimation
                {
                    From = center.X,
                    To = targetX,
                    Duration = TimeSpan.FromMilliseconds(500 + _rng.Next(200)),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(animX, e);
                Storyboard.SetTargetProperty(animX, new PropertyPath("(Canvas.Left)"));
                sb.Children.Add(animX);

                var animY = new DoubleAnimation
                {
                    From = center.Y,
                    To = targetY,
                    Duration = animX.Duration,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(animY, e);
                Storyboard.SetTargetProperty(animY, new PropertyPath("(Canvas.Top)"));
                sb.Children.Add(animY);

                var op = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(100),
                    AutoReverse = true,
                    BeginTime = TimeSpan.FromMilliseconds(_rng.Next(50, 250))
                };
                Storyboard.SetTarget(op, e);
                Storyboard.SetTargetProperty(op, new PropertyPath("Opacity"));
                sb.Children.Add(op);

                sb.Begin();
            }
        }

        public void Dispose()
        {
            // Rien de spécial ici pour l’instant
        }
    }
}