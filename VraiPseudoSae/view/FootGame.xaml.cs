using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Globalization;
using IUTGame;
using IUTGame.WPF;
using VraiPseudoSae.Utils.Sprite;

namespace VraiPseudoSae.view
{
    public partial class FootGame : Window
    {
        private FootGameLogic? gameLogic;
        private WPFScreen? screen;

        public FootGame()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            screen = new WPFScreen(GameCanvas);

            // Exportation et injection des sprites XAML pour IUTGame
            var playerBitmap = XamlSpriteExporter.RenderToBitmapImage(PlayerSpriteSource, 24, 24);
            var botBitmap = XamlSpriteExporter.RenderToBitmapImage(BotSpriteSource, 24, 24);
            var ballBitmap = XamlSpriteExporter.RenderToBitmapImage(BallSpriteSource, 14, 14);

            SpriteInjector.PreRegister(screen, "player_foot.png", playerBitmap);
            SpriteInjector.PreRegister(screen, "bot_foot.png", botBitmap);
            SpriteInjector.PreRegister(screen, "ball_foot.png", ballBitmap);

            gameLogic = new FootGameLogic(screen, "Resources/Sprites", "Resources/Sounds", this);
            gameLogic.Run();
            
            GameCanvas.Focus();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (gameLogic != null)
            {
                if (gameLogic.IntroVisible)
                {
                    if (e.Key == Key.Enter)
                    {
                        gameLogic.IntroVisible = false;
                        IntroOverlay.Visibility = Visibility.Collapsed;
                        gameLogic.UpdateHud("Match en cours.");
                    }
                    return;
                }

                if (e.Key == Key.Escape)
                {
                    if (gameLogic.IsRunning) gameLogic.Pause(); else gameLogic.Resume();
                    PauseOverlay.Visibility = gameLogic.IsRunning ? Visibility.Collapsed : Visibility.Visible;
                    return;
                }

                if (!gameLogic.IsRunning)
                {
                    if (e.Key == Key.C) ToggleHud();
                    if (e.Key == Key.R)
                    {
                        gameLogic.ResetScore();
                        gameLogic.Resume();
                        PauseOverlay.Visibility = Visibility.Collapsed;
                        gameLogic.UpdateHud("Match relancé.");
                    }
                    return;
                }

                if (e.Key == Key.C) ToggleHud();
                if (e.Key == Key.R)
                {
                    gameLogic.ResetScore();
                    gameLogic.UpdateHud("Match relancé.");
                }
            }
        }

        private void ToggleHud()
        {
            bool isVisible = HudPanel.Visibility == Visibility.Visible;
            HudPanel.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
            CompactHud.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        protected override void OnClosed(EventArgs e)
        {
            gameLogic?.Pause();
            base.OnClosed(e);
        }
    }

    public class FootGameLogic : Game
    {
        private readonly FootGame window;
        public bool IntroVisible { get; set; } = true;
        
        public const double FieldWidth = 960;
        public const double FieldHeight = 560;
        public const double GoalHeight = 120;
        public const double GoalTop = 220;
        public const double BallFriction = 0.987;

        private readonly Random random = new Random();
        internal FootPlayer Human = null!;
        internal FootPlayer Bot = null!;
        internal FootBall Ball = null!;

        private readonly List<Supporter> supporters = new();
        internal bool GoalAnimationPlaying;
        private double goalAnimationTimer;
        internal double RestartCooldown;
        private GoalCelebration? celebration;

        internal readonly HashSet<Key> PressedKeys = new();

        public FootGameLogic(IScreen screen, string sprites, string sounds, FootGame window) 
            : base(screen, sprites, sounds, 60)
        {
            this.window = window;
        }

        protected override void InitItems()
        {
            InitializeSkillIcons();
            BuildStands();

            Human = new FootPlayer(150, FieldHeight / 2 - 12, this, "player_foot.png", "Rouge");
            Bot = new FootPlayer(FieldWidth - 150 - 24, FieldHeight / 2 - 12, this, "bot_foot.png", "Bot");
            Ball = new FootBall(FieldWidth / 2 - 7, FieldHeight / 2 - 7, this);

            AddItem(Human);
            AddItem(Bot);
            AddItem(Ball);
            AddItem(new FootController(this));

            ResetPositions(false);
        }

        public void ResetScore()
        {
            Human.Score = 0;
            Bot.Score = 0;
            ResetPositions(true);
        }

        public void ResetPositions(bool preserveScore)
        {
            Human.PutXY(150, FieldHeight / 2 - 12);
            Human.VX = 0; Human.VY = 0;

            Bot.PutXY(FieldWidth - 150 - 24, FieldHeight / 2 - 12);
            Bot.VX = 0; Bot.VY = 0;

            Ball.PutXY(FieldWidth / 2 - 7, FieldHeight / 2 - 7);
            Ball.VX = 0; Ball.VY = 0;

            Ball.DribbleActive = false;
            GoalAnimationPlaying = false;
            RestartCooldown = 0;
            window.EffectsCanvas.Children.Clear();

            UpdateHud("Match en cours.");
        }

        public void Update(double dt)
        {
            AnimateSupporters(DateTime.Now);

            if (GoalAnimationPlaying)
            {
                UpdateGoalCelebration(dt);
                UpdateCooldownTexts();
                return;
            }

            if (RestartCooldown > 0)
            {
                RestartCooldown -= dt;
                window.StatusText.Text = $"Reprise dans {Math.Ceiling(RestartCooldown)}...";
                window.CompactStatusText.Text = window.StatusText.Text;
                UpdateCooldownTexts();
                if (RestartCooldown <= 0) ResetPositions(true);
                return;
            }

            if (IntroVisible || !IsRunning) return;

            UpdateCooldowns(Human, dt);
            UpdateCooldowns(Bot, dt);
            UpdateCooldownTexts();
        }

        private void UpdateCooldowns(FootPlayer p, double dt)
        {
            p.DashCooldown = Math.Max(0, p.DashCooldown - dt);
            p.ShotCooldown = Math.Max(0, p.ShotCooldown - dt);
            p.DribbleCooldown = Math.Max(0, p.DribbleCooldown - dt);
        }

        public void CheckGoal()
        {
            bool inGoalZoneY = Ball.Top + Ball.Height >= GoalTop && Ball.Top <= GoalTop + GoalHeight;
            if (!inGoalZoneY || GoalAnimationPlaying || RestartCooldown > 0) return;

            if (Ball.Left <= -8) { Bot.Score++; StartGoalCelebration(true); }
            else if (Ball.Left + Ball.Width >= FieldWidth + 8) { Human.Score++; StartGoalCelebration(false); }
        }

        private void StartGoalCelebration(bool botScored)
        {
            GoalAnimationPlaying = true;
            goalAnimationTimer = 1.8;
            celebration = new GoalCelebration { BotScored = botScored, Variant = botScored ? random.Next(0, 8) : random.Next(8, 16), Time = 0 };
            UpdateHud(botScored ? "But du bot !" : "But du joueur !");
        }

        private void UpdateGoalCelebration(double dt)
        {
            if (celebration == null) return;
            celebration.Time += dt;
            goalAnimationTimer -= dt;
            window.EffectsCanvas.Children.Clear();

            bool blueTheme = celebration.BotScored;
            Brush mainBrush = blueTheme ? Brushes.DeepSkyBlue : Brushes.Orange;
            Brush secondaryBrush = blueTheme ? Brushes.LightBlue : Brushes.Gold;
            double t = celebration.Time;
            int variant = celebration.Variant % 8;

            switch (variant)
            {
                case 0: DrawRadialBurst(mainBrush, secondaryBrush, t); break;
                case 1: DrawConcentricRings(mainBrush, secondaryBrush, t); break;
                case 2: DrawDiagonalRain(mainBrush, secondaryBrush, t); break;
                case 3: DrawSpiral(mainBrush, secondaryBrush, t); break;
                case 4: DrawEnergyCross(mainBrush, secondaryBrush, t); break;
                case 5: DrawVerticalColumns(mainBrush, secondaryBrush, t); break;
                case 6: DrawHorizontalWave(mainBrush, secondaryBrush, t); break;
                case 7: DrawPulseCenter(mainBrush, secondaryBrush, t); break;
            }

            TextBlock goalText = new TextBlock { Text = blueTheme ? "BUT BLEU !" : "BUT ROUGE !", Foreground = mainBrush, FontWeight = FontWeights.ExtraBold, FontSize = 42, Opacity = Math.Max(0.25, 1.0 - t / 1.8) };
            window.EffectsCanvas.Children.Add(goalText);
            Canvas.SetLeft(goalText, FieldWidth / 2 - 120);
            Canvas.SetTop(goalText, 60 + Math.Sin(t * 7) * 10);

            if (goalAnimationTimer <= 0) { window.EffectsCanvas.Children.Clear(); GoalAnimationPlaying = false; RestartCooldown = 2.0; }
        }

        private void DrawRadialBurst(Brush mainBrush, Brush secondaryBrush, double t)
        {
            int particles = 28;
            for (int i = 0; i < particles; i++)
            {
                double angle = (Math.PI * 2 / particles) * i;
                double radius = 40 + t * 180;
                Ellipse p = new Ellipse { Width = 10, Height = 10, Fill = i % 2 == 0 ? mainBrush : secondaryBrush, Opacity = Math.Max(0.15, 1 - t / 1.8) };
                window.EffectsCanvas.Children.Add(p);
                Canvas.SetLeft(p, FieldWidth / 2 + Math.Cos(angle) * radius);
                Canvas.SetTop(p, FieldHeight / 2 + Math.Sin(angle) * radius);
            }
        }

        private void DrawConcentricRings(Brush mainBrush, Brush secondaryBrush, double t)
        {
            for (int i = 0; i < 4; i++)
            {
                double size = 60 + i * 50 + t * 90;
                Ellipse ring = new Ellipse { Width = size, Height = size, Stroke = i % 2 == 0 ? mainBrush : secondaryBrush, StrokeThickness = 5, Opacity = Math.Max(0.12, 1 - t / 1.8), Fill = Brushes.Transparent };
                window.EffectsCanvas.Children.Add(ring);
                Canvas.SetLeft(ring, FieldWidth / 2 - size / 2);
                Canvas.SetTop(ring, FieldHeight / 2 - size / 2);
            }
        }

        private void DrawDiagonalRain(Brush mainBrush, Brush secondaryBrush, double t)
        {
            for (int i = 0; i < 18; i++)
            {
                Rectangle r = new Rectangle { Width = 10, Height = 26, Fill = i % 2 == 0 ? mainBrush : secondaryBrush, Opacity = 0.8 };
                window.EffectsCanvas.Children.Add(r);
                Canvas.SetLeft(r, 80 + i * 45 + t * 90);
                Canvas.SetTop(r, 30 + (i % 5) * 85 + t * 40);
                r.RenderTransform = new RotateTransform(25);
            }
        }

        private void DrawSpiral(Brush mainBrush, Brush secondaryBrush, double t)
        {
            for (int i = 0; i < 26; i++)
            {
                double angle = i * 0.45 + t * 8;
                double radius = 15 + i * 7 + t * 18;
                Ellipse p = new Ellipse { Width = 8, Height = 8, Fill = i % 2 == 0 ? mainBrush : secondaryBrush, Opacity = 0.85 };
                window.EffectsCanvas.Children.Add(p);
                Canvas.SetLeft(p, FieldWidth / 2 + Math.Cos(angle) * radius);
                Canvas.SetTop(p, FieldHeight / 2 + Math.Sin(angle) * radius);
            }
        }

        private void DrawEnergyCross(Brush mainBrush, Brush secondaryBrush, double t)
        {
            double len = 80 + t * 160;
            Line h = new Line { X1 = FieldWidth / 2 - len, Y1 = FieldHeight / 2, X2 = FieldWidth / 2 + len, Y2 = FieldHeight / 2, Stroke = mainBrush, StrokeThickness = 8, Opacity = 0.7 };
            Line v = new Line { X1 = FieldWidth / 2, Y1 = FieldHeight / 2 - len, X2 = FieldWidth / 2, Y2 = FieldHeight / 2 + len, Stroke = secondaryBrush, StrokeThickness = 8, Opacity = 0.7 };
            window.EffectsCanvas.Children.Add(h); window.EffectsCanvas.Children.Add(v);
        }

        private void DrawVerticalColumns(Brush mainBrush, Brush secondaryBrush, double t)
        {
            for (int i = 0; i < 10; i++)
            {
                Rectangle col = new Rectangle { Width = 26, Height = 100 + Math.Sin(t * 8 + i) * 50, Fill = i % 2 == 0 ? mainBrush : secondaryBrush, Opacity = 0.55 };
                window.EffectsCanvas.Children.Add(col);
                Canvas.SetLeft(col, 120 + i * 70);
                Canvas.SetTop(col, 180 - t * 40);
            }
        }

        private void DrawHorizontalWave(Brush mainBrush, Brush secondaryBrush, double t)
        {
            for (int i = 0; i < 11; i++)
            {
                Ellipse wave = new Ellipse { Width = 34, Height = 34, Fill = i % 2 == 0 ? mainBrush : secondaryBrush, Opacity = 0.65 };
                window.EffectsCanvas.Children.Add(wave);
                Canvas.SetLeft(wave, 80 + i * 72);
                Canvas.SetTop(wave, FieldHeight / 2 + Math.Sin(t * 8 + i) * 70);
            }
        }

        private void DrawPulseCenter(Brush mainBrush, Brush secondaryBrush, double t)
        {
            double size = 80 + Math.Abs(Math.Sin(t * 9)) * 170;
            Ellipse core = new Ellipse { Width = size, Height = size, Fill = mainBrush, Opacity = 0.28 };
            Ellipse ring = new Ellipse { Width = size + 40, Height = size + 40, Stroke = secondaryBrush, StrokeThickness = 6, Opacity = 0.55, Fill = Brushes.Transparent };
            window.EffectsCanvas.Children.Add(core); window.EffectsCanvas.Children.Add(ring);
            Canvas.SetLeft(core, FieldWidth / 2 - size / 2);
            Canvas.SetTop(core, FieldHeight / 2 - size / 2);
            Canvas.SetLeft(ring, FieldWidth / 2 - (size + 40) / 2);
            Canvas.SetTop(ring, FieldHeight / 2 - (size + 40) / 2);
        }

        private void BuildStands()
        {
            supporters.Clear();
            window.TopStand.Children.Clear(); window.BottomStand.Children.Clear();
            window.LeftStand.Children.Clear(); window.RightStand.Children.Clear();
            CreateHorizontalStand(window.TopStand, true);
            CreateHorizontalStand(window.BottomStand, false);
            CreateVerticalStand(window.LeftStand, true);
            CreateVerticalStand(window.RightStand, false);
        }

        private void CreateHorizontalStand(Canvas canvas, bool top)
        {
            for (int i = 0; i < 42; i++)
            {
                double x = 12 + i * 22, baseY = top ? 48 : 22;
                Rectangle seat = new Rectangle { Width = 18, Height = 10, RadiusX = 3, RadiusY = 3, Fill = new SolidColorBrush(Color.FromRgb(120, 120, 120)) };
                Ellipse head = new Ellipse { Width = 8, Height = 8, Fill = RandomSupporterBrush() };
                Rectangle body = new Rectangle { Width = 10, Height = 12, RadiusX = 2, RadiusY = 2, Fill = RandomSupporterBrush() };
                canvas.Children.Add(seat); canvas.Children.Add(body); canvas.Children.Add(head);
                Canvas.SetLeft(seat, x); Canvas.SetTop(seat, top ? 70 : 12);
                Canvas.SetLeft(body, x + 4); Canvas.SetTop(body, baseY);
                Canvas.SetLeft(head, x + 5); Canvas.SetTop(head, baseY - 8);
                supporters.Add(new Supporter { Head = head, Body = body, BaseX = x + 4, BaseY = baseY, JumpOffset = random.NextDouble() * Math.PI * 2, VerticalAmplitude = 4 + random.NextDouble() * 4 });
            }
        }

        private void CreateVerticalStand(Canvas canvas, bool left)
        {
            for (int i = 0; i < 18; i++)
            {
                double y = 16 + i * 28, baseX = left ? 52 : 38;
                Rectangle seat = new Rectangle { Width = 12, Height = 20, RadiusX = 3, RadiusY = 3, Fill = new SolidColorBrush(Color.FromRgb(120, 120, 120)) };
                Ellipse head = new Ellipse { Width = 8, Height = 8, Fill = RandomSupporterBrush() };
                Rectangle body = new Rectangle { Width = 10, Height = 12, RadiusX = 2, RadiusY = 2, Fill = RandomSupporterBrush() };
                canvas.Children.Add(seat); canvas.Children.Add(body); canvas.Children.Add(head);
                Canvas.SetLeft(seat, left ? 16 : 68); Canvas.SetTop(seat, y);
                Canvas.SetLeft(body, baseX); Canvas.SetTop(body, y + 4);
                Canvas.SetLeft(head, baseX + 1); Canvas.SetTop(head, y - 4);
                supporters.Add(new Supporter { Head = head, Body = body, BaseX = baseX, BaseY = y + 4, JumpOffset = random.NextDouble() * Math.PI * 2, VerticalAmplitude = 3 + random.NextDouble() * 3 });
            }
        }

        private Brush RandomSupporterBrush()
        {
            Brush[] palette = { Brushes.Gold, Brushes.Tomato, Brushes.DeepSkyBlue, Brushes.White, Brushes.Orange, Brushes.Cyan, Brushes.LightGreen, Brushes.HotPink };
            return palette[random.Next(palette.Length)];
        }

        private void AnimateSupporters(DateTime now)
        {
            double t = now.TimeOfDay.TotalSeconds * 4.0;
            foreach (var s in supporters)
            {
                double jump = Math.Abs(Math.Sin(t + s.JumpOffset)) * s.VerticalAmplitude;
                Canvas.SetTop(s.Body, s.BaseY - jump);
                Canvas.SetTop(s.Head, s.BaseY - 8 - jump);
            }
        }

        private void InitializeSkillIcons()
        {
            DrawDashIcon(window.DashIcon, Brushes.LightGreen, 1.0);
            DrawShotIcon(window.ShotIcon, Brushes.LightGreen, 1.0);
            DrawDribbleIcon(window.DribbleIcon, Brushes.LightGreen, 1.0);
            DrawDashIcon(window.CompactDashIcon, Brushes.LightGreen, 0.85);
            DrawShotIcon(window.CompactShotIcon, Brushes.LightGreen, 0.85);
            DrawDribbleIcon(window.CompactDribbleIcon, Brushes.LightGreen, 0.85);
        }

        private void DrawDashIcon(Canvas canvas, Brush brush, double scale)
        {
            canvas.Children.Clear();
            Polygon bolt = new Polygon { Fill = brush, Points = new PointCollection { new Point(16*scale, 2*scale), new Point(9*scale, 13*scale), new Point(15*scale, 13*scale), new Point(10*scale, 26*scale), new Point(22*scale, 11*scale), new Point(15*scale, 11*scale) } };
            canvas.Children.Add(bolt);
        }

        private void DrawShotIcon(Canvas canvas, Brush brush, double scale)
        {
            canvas.Children.Clear();
            Ellipse b = new Ellipse { Width = 14 * scale, Height = 14 * scale, Stroke = brush, StrokeThickness = 2, Fill = Brushes.Transparent };
            Line l1 = new Line { X1 = 16*scale, Y1 = 2*scale, X2 = 26*scale, Y2 = 2*scale, Stroke = brush, StrokeThickness = 2 };
            Line l2 = new Line { X1 = 18*scale, Y1 = 7*scale, X2 = 28*scale, Y2 = 7*scale, Stroke = brush, StrokeThickness = 2 };
            canvas.Children.Add(b); canvas.Children.Add(l1); canvas.Children.Add(l2);
            Canvas.SetLeft(b, 2 * scale); Canvas.SetTop(b, 7 * scale);
        }

        private void DrawDribbleIcon(Canvas canvas, Brush brush, double scale)
        {
            canvas.Children.Clear();
            string data = string.Format(CultureInfo.InvariantCulture, "M {0},{1} A {2},{3} 0 0 1 {4},{5}", 3 * scale, 22 * scale, 10 * scale, 10 * scale, 25 * scale, 22 * scale);
            Path path = new Path { Stroke = brush, StrokeThickness = 2, Data = Geometry.Parse(data) };
            Ellipse dot = new Ellipse { Width = 6 * scale, Height = 6 * scale, Fill = brush };
            canvas.Children.Add(path); canvas.Children.Add(dot);
            Canvas.SetLeft(dot, 20 * scale); Canvas.SetTop(dot, 19 * scale);
        }

        public void UpdateHud(string status)
        {
            window.ScoreText.Text = $"Rouge {Human.Score} - {Bot.Score} Bleu";
            window.CompactScoreText.Text = $"{Human.Score} - {Bot.Score}";
            window.StatusText.Text = status;
            window.CompactStatusText.Text = status;
        }

        private void UpdateCooldownTexts()
        {
            SetSkillVisual(window.DashText, window.DashIcon, window.CompactDashIcon, Human.DashCooldown, "Dash", Brushes.Gold);
            SetSkillVisual(window.ShotText, window.ShotIcon, window.CompactShotIcon, Human.ShotCooldown, "Tir", Brushes.OrangeRed);
            SetSkillVisual(window.DribbleText, window.DribbleIcon, window.CompactDribbleIcon, Human.DribbleCooldown, "Dribble", Brushes.DeepSkyBlue);
        }

        private void SetSkillVisual(TextBlock text, Canvas icon, Canvas compactIcon, double cooldown, string label, Brush readyBrush)
        {
            bool ready = cooldown <= 0;
            double pulse = 0.75 + 0.25 * Math.Abs(Math.Sin(DateTime.Now.TimeOfDay.TotalSeconds * 5));
            text.Text = ready ? $"{label} : prêt" : $"{label} : {cooldown:F1}s";
            text.Foreground = ready ? readyBrush : Brushes.Orange;
            icon.Opacity = ready ? pulse : 0.35;
            compactIcon.Opacity = ready ? pulse : 0.35;
        }

        protected override void RunWhenWin() { }
        protected override void RunWhenLoose() { }
    }

    public class FootPlayer : GameItem, IAnimable
    {
        public double VX, VY;
        public int Score;
        public double DashCooldown, ShotCooldown, DribbleCooldown;
        public bool IsDashing;
        public double DashTimeLeft, DashDirX, DashDirY;
        public string Name;
        private FootGameLogic logic;

        public FootPlayer(double x, double y, FootGameLogic game, string sprite, string name) 
            : base(x, y, game, sprite, 10) { logic = game; Name = name; Collidable = false; }

        public override string TypeName => "FootPlayer";

        public void Animate(TimeSpan interval)
        {
            if (logic.IntroVisible || !logic.IsRunning || logic.GoalAnimationPlaying || logic.RestartCooldown > 0) return;
            if (Name == "Rouge") UpdateHuman(interval.TotalSeconds); else UpdateBot(interval.TotalSeconds);
            KeepInsideField();
        }

        private void UpdateHuman(double dt)
        {
            double dx = 0, dy = 0;
            if (logic.PressedKeys.Contains(Key.Left)) dx -= 1;
            if (logic.PressedKeys.Contains(Key.Right)) dx += 1;
            if (logic.PressedKeys.Contains(Key.Up)) dy -= 1;
            if (logic.PressedKeys.Contains(Key.Down)) dy += 1;
            Normalize(ref dx, ref dy);

            if (logic.PressedKeys.Contains(Key.LeftShift) && DashCooldown <= 0 && (dx != 0 || dy != 0) && !IsDashing)
            {
                IsDashing = true; DashTimeLeft = 0.11; DashDirX = dx; DashDirY = dy; DashCooldown = 3.4;
                logic.UpdateHud("Dash !");
            }

            if (IsDashing)
            {
                DashTimeLeft -= dt; VX = DashDirX * 9.5; VY = DashDirY * 9.5;
                if (DashTimeLeft <= 0) { IsDashing = false; VX = 0; VY = 0; }
            }
            else { VX = dx * 2.0; VY = dy * 2.0; }

            MoveXY(VX, VY);

            if (logic.PressedKeys.Contains(Key.Space) && ShotCooldown <= 0 && logic.Ball.DistanceTo(this) <= 32)
            {
                logic.Ball.Shoot(this, false); ShotCooldown = 2.2; logic.UpdateHud("Tir puissant !");
            }
            if (logic.PressedKeys.Contains(Key.LeftCtrl) && DribbleCooldown <= 0 && logic.Ball.DistanceTo(this) <= 34)
            {
                logic.Ball.StartDribble(false); DribbleCooldown = 4.0; logic.UpdateHud("Dribble demi-cercle !");
            }
        }

        private void UpdateBot(double dt)
        {
            bool canKick = logic.Ball.DistanceTo(this) <= 30;
            bool playerNear = logic.Ball.DistanceTo(logic.Human) < 70;

            if (canKick && playerNear && DribbleCooldown <= 0)
            { logic.Ball.StartDribble(true); DribbleCooldown = 3.5; logic.UpdateHud("Le bot dribble."); return; }

            if (canKick && ShotCooldown <= 0)
            { logic.Ball.Shoot(this, true); ShotCooldown = 1.8; logic.UpdateHud("Le bot frappe."); return; }

            double targetX = logic.Ball.Left, targetY = logic.Ball.Top;
            if (logic.Ball.DistanceTo(this) > 140) { targetX = logic.Ball.Left - 8; }
            double dx = targetX - Left, dy = targetY - Top;
            Normalize(ref dx, ref dy);

            if (logic.Ball.DistanceTo(this) > 160 && DashCooldown <= 0 && !IsDashing)
            {
                double dashDx = logic.Ball.Left - Left, dashDy = logic.Ball.Top - Top;
                Normalize(ref dashDx, ref dashDy);
                IsDashing = true; DashTimeLeft = 0.10; DashDirX = dashDx; DashDirY = dashDy; DashCooldown = 3.0;
            }

            if (IsDashing)
            {
                DashTimeLeft -= dt; VX = DashDirX * 8.5; VY = DashDirY * 8.5;
                if (DashTimeLeft <= 0) IsDashing = false;
                MoveXY(VX, VY); return;
            }
            VX = dx * 2.15; VY = dy * 2.15;
            MoveXY(VX, VY);
        }

        private void KeepInsideField()
        {
            if (Left < 0) PutXY(0, Top); if (Top < 0) PutXY(Left, 0);
            if (Left + Width > FootGameLogic.FieldWidth) PutXY(FootGameLogic.FieldWidth - Width, Top);
            if (Top + Height > FootGameLogic.FieldHeight) PutXY(Left, FootGameLogic.FieldHeight - Height);
        }

        private void Normalize(ref double x, ref double y) { double len = Math.Sqrt(x * x + y * y); if (len <= 0.0001) return; x /= len; y /= len; }
        public override void CollideEffect(GameItem other) { }
    }

    public class FootBall : GameItem, IAnimable
    {
        public double VX, VY;
        public bool DribbleActive;
        public double DribbleElapsed, DribbleDuration, DribbleCenterX, DribbleCenterY, DribbleRadius, DribbleStartAngle, DribbleEndAngle, DribbleForwardBoost;
        private FootGameLogic logic;

        public FootBall(double x, double y, FootGameLogic game) : base(x, y, game, "ball_foot.png", 11) { logic = game; Collidable = false; }
        public override string TypeName => "FootBall";

        public void Animate(TimeSpan interval)
        {
            if (logic.IntroVisible || !logic.IsRunning || logic.GoalAnimationPlaying || logic.RestartCooldown > 0) return;
            double dt = interval.TotalSeconds;
            if (DribbleActive) { UpdateDribble(dt); return; }

            MoveXY(VX, VY);
            VX *= FootGameLogic.BallFriction; VY *= FootGameLogic.BallFriction;
            if (Math.Abs(VX) < 0.02) VX = 0; if (Math.Abs(VY) < 0.02) VY = 0;

            if (Top <= 0) { PutXY(Left, 0); VY *= -1; }
            if (Top + Height >= FootGameLogic.FieldHeight) { PutXY(Left, FootGameLogic.FieldHeight - Height); VY *= -1; }

            bool inGoalZoneY = Top + Height >= FootGameLogic.GoalTop && Top <= FootGameLogic.GoalTop + FootGameLogic.GoalHeight;
            if (!inGoalZoneY)
            {
                if (Left <= 0) { PutXY(0, Top); VX *= -1; }
                if (Left + Width >= FootGameLogic.FieldWidth) { PutXY(FootGameLogic.FieldWidth - Width, Top); VX *= -1; }
            }

            CheckPlayerCollision(logic.Human);
            CheckPlayerCollision(logic.Bot);
            logic.CheckGoal();
        }

        private void CheckPlayerCollision(FootPlayer p)
        {
            Rect pr = new Rect(p.Left, p.Top, p.Width, p.Height);
            Rect br = new Rect(Left, Top, Width, Height);
            if (!pr.IntersectsWith(br)) return;
            double dx = (Left + Width / 2) - (p.Left + p.Width / 2), dy = (Top + Height / 2) - (p.Top + p.Height / 2);
            double len = Math.Sqrt(dx*dx + dy*dy); if (len < 0.001) return; dx /= len; dy /= len;
            double impact = 3.0 + Math.Sqrt(p.VX * p.VX + p.VY * p.VY) * 0.9;
            VX = dx * impact; VY = dy * impact;
        }

        public void Shoot(FootPlayer p, bool toLeftGoal)
        {
            double gx = toLeftGoal ? 0 : FootGameLogic.FieldWidth, gy = FootGameLogic.GoalTop + FootGameLogic.GoalHeight / 2;
            double dx = gx - (Left + Width / 2), dy = gy - (Top + Height / 2);
            double len = Math.Sqrt(dx*dx + dy*dy); if (len < 0.001) return; dx /= len; dy /= len;
            DribbleActive = false; VX = dx * 7.4; VY = dy * 7.4;
        }

        public void StartDribble(bool toLeftGoal)
        {
            DribbleActive = true; DribbleElapsed = 0; DribbleDuration = 0.62;
            DribbleCenterX = Left + Width / 2; DribbleCenterY = Top + Height / 2;
            DribbleRadius = 46; DribbleStartAngle = toLeftGoal ? 0 : Math.PI; DribbleEndAngle = toLeftGoal ? Math.PI : 0;
            DribbleForwardBoost = toLeftGoal ? -4.3 : 4.3;
        }

        private void UpdateDribble(double dt)
        {
            DribbleElapsed += dt; double t = Math.Min(DribbleElapsed / DribbleDuration, 1.0);
            double angle = DribbleStartAngle + (DribbleEndAngle - DribbleStartAngle) * t;
            double cx = DribbleCenterX + DribbleForwardBoost * 12 * t;
            double bx = cx + Math.Cos(angle) * DribbleRadius, by = DribbleCenterY + Math.Sin(angle) * DribbleRadius;
            PutXY(bx - Width / 2, by - Height / 2);
            if (t >= 1.0) { DribbleActive = false; VX = DribbleForwardBoost; VY = 0; }
        }

        public double DistanceTo(FootPlayer p) { double dx = (Left + Width / 2) - (p.Left + p.Width / 2), dy = (Top + Height / 2) - (p.Top + p.Height / 2); return Math.Sqrt(dx * dx + dy * dy); }
        public override void CollideEffect(GameItem other) { }
    }

    public class FootController : GameItem, IAnimable, IKeyboardInteract
    {
        private readonly FootGameLogic logic;
        public FootController(FootGameLogic logic) : base(0, 0, logic, "", 0) { this.logic = logic; Collidable = false; }
        public override string TypeName => "FootController";
        public void Animate(TimeSpan interval) => logic.Update(interval.TotalSeconds);
        public void KeyDown(Key key) => logic.PressedKeys.Add(key);
        public void KeyUp(Key key) => logic.PressedKeys.Remove(key);
        public override void CollideEffect(GameItem other) { }
    }

    public class Supporter
    {
        public Ellipse Head { get; set; } = null!;
        public Rectangle Body { get; set; } = null!;
        public double BaseX { get; set; }
        public double BaseY { get; set; }
        public double JumpOffset { get; set; }
        public double VerticalAmplitude { get; set; }
    }

    public class GoalCelebration
    {
        public bool BotScored { get; set; }
        public int Variant { get; set; }
        public double Time { get; set; }
    }
}
