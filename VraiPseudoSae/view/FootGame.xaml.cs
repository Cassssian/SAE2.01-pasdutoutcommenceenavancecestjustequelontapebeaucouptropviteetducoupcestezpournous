using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Globalization;

namespace VraiPseudoSae.view
{
    
    public partial class FootGame : Window
    {
        private bool introVisible = true;
        private bool isPaused = false;
        private bool hasShownIntro = false;
        
        private const double FieldWidth = 960;
        private const double FieldHeight = 560;

        private const double GoalHeight = 120;
        private const double GoalTop = 220;

        private const double PlayerSize = 24;
        private const double BallSize = 14;

        private const double PlayerBaseSpeed = 2.0;
        private const double BotBaseSpeed = 2.15;
        private const double BallFriction = 0.987;

        private readonly Random random = new Random();

        private Player human = null!;
        private Player bot = null!;
        private Ball ball = null!;

        private Ellipse humanShape = null!;
        private Ellipse botShape = null!;
        private Ellipse ballShape = null!;

        private readonly List<Supporter> supporters = new();
        private DateTime lastFrameTime;
        private bool gameRunning;
        private bool hudVisible = true;

        private bool keyUp;
        private bool keyDown;
        private bool keyLeft;
        private bool keyRight;

        private bool shiftPressed;
        private bool spacePressed;
        private bool ctrlPressed;

        private bool goalAnimationPlaying;
        private double goalAnimationTimer;
        private double restartCooldown;
        private GoalCelebration? celebration;

        public FootGame()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeSkillIcons();
            BuildStands();
            InitializeGameObjects();
            ResetPositions(false);

            lastFrameTime = DateTime.Now;
            gameRunning = true;

            if (!hasShownIntro)
            {
                introVisible = true;
                hasShownIntro = true;
                IntroOverlay.Visibility = Visibility.Visible;
                PauseOverlay.Visibility = Visibility.Collapsed;
            }
            else
            {
                introVisible = false;
                IntroOverlay.Visibility = Visibility.Collapsed;
            }

            CompositionTarget.Rendering += GameLoop;
            Keyboard.Focus(GameCanvas);
            GameCanvas.Focus();
        }

        private void InitializeGameObjects()
        {
            human = new Player
            {
                X = 150,
                Y = FieldHeight / 2 - PlayerSize / 2,
                Width = PlayerSize,
                Height = PlayerSize,
                Color = Brushes.OrangeRed,
                Name = "Rouge"
            };

            bot = new Player
            {
                X = FieldWidth - 150,
                Y = FieldHeight / 2 - PlayerSize / 2,
                Width = PlayerSize,
                Height = PlayerSize,
                Color = Brushes.DodgerBlue,
                Name = "Bot"
            };

            ball = new Ball
            {
                X = FieldWidth / 2 - BallSize / 2,
                Y = FieldHeight / 2 - BallSize / 2,
                Width = BallSize,
                Height = BallSize
            };

            humanShape = new Ellipse { Width = PlayerSize, Height = PlayerSize, Fill = human.Color, Stroke = Brushes.White, StrokeThickness = 2 };
            botShape = new Ellipse { Width = PlayerSize, Height = PlayerSize, Fill = bot.Color, Stroke = Brushes.White, StrokeThickness = 2 };
            ballShape = new Ellipse { Width = BallSize, Height = BallSize, Fill = Brushes.WhiteSmoke, Stroke = Brushes.Black, StrokeThickness = 1.2 };

            GameCanvas.Children.Add(humanShape);
            GameCanvas.Children.Add(botShape);
            GameCanvas.Children.Add(ballShape);
        }

        private void BuildStands()
        {
            supporters.Clear();
            TopStand.Children.Clear();
            BottomStand.Children.Clear();
            LeftStand.Children.Clear();
            RightStand.Children.Clear();

            CreateHorizontalStand(TopStand, true);
            CreateHorizontalStand(BottomStand, false);
            CreateVerticalStand(LeftStand, true);
            CreateVerticalStand(RightStand, false);
        }

        private void CreateHorizontalStand(System.Windows.Controls.Canvas canvas, bool top)
        {
            for (int i = 0; i < 42; i++)
            {
                double x = 12 + i * 22;
                double baseY = top ? 48 : 22;

                Rectangle seat = new Rectangle
                {
                    Width = 18,
                    Height = 10,
                    RadiusX = 3,
                    RadiusY = 3,
                    Fill = new SolidColorBrush(Color.FromRgb(120, 120, 120))
                };

                Ellipse head = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = RandomSupporterBrush()
                };

                Rectangle body = new Rectangle
                {
                    Width = 10,
                    Height = 12,
                    RadiusX = 2,
                    RadiusY = 2,
                    Fill = RandomSupporterBrush()
                };

                canvas.Children.Add(seat);
                canvas.Children.Add(body);
                canvas.Children.Add(head);

                Canvas.SetLeft(seat, x);
                Canvas.SetTop(seat, top ? 70 : 12);

                Canvas.SetLeft(body, x + 4);
                Canvas.SetTop(body, baseY);

                Canvas.SetLeft(head, x + 5);
                Canvas.SetTop(head, baseY - 8);

                supporters.Add(new Supporter
                {
                    Head = head,
                    Body = body,
                    BaseX = x + 4,
                    BaseY = baseY,
                    JumpOffset = random.NextDouble() * Math.PI * 2,
                    VerticalAmplitude = 4 + random.NextDouble() * 4
                });
            }
        }

        private void CreateVerticalStand(System.Windows.Controls.Canvas canvas, bool left)
        {
            for (int i = 0; i < 18; i++)
            {
                double y = 16 + i * 28;
                double baseX = left ? 52 : 38;

                Rectangle seat = new Rectangle
                {
                    Width = 12,
                    Height = 20,
                    RadiusX = 3,
                    RadiusY = 3,
                    Fill = new SolidColorBrush(Color.FromRgb(120, 120, 120))
                };

                Ellipse head = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = RandomSupporterBrush()
                };

                Rectangle body = new Rectangle
                {
                    Width = 10,
                    Height = 12,
                    RadiusX = 2,
                    RadiusY = 2,
                    Fill = RandomSupporterBrush()
                };

                canvas.Children.Add(seat);
                canvas.Children.Add(body);
                canvas.Children.Add(head);

                Canvas.SetLeft(seat, left ? 16 : 68);
                Canvas.SetTop(seat, y);

                Canvas.SetLeft(body, baseX);
                Canvas.SetTop(body, y + 4);

                Canvas.SetLeft(head, baseX + 1);
                Canvas.SetTop(head, y - 4);

                supporters.Add(new Supporter
                {
                    Head = head,
                    Body = body,
                    BaseX = baseX,
                    BaseY = y + 4,
                    JumpOffset = random.NextDouble() * Math.PI * 2,
                    VerticalAmplitude = 3 + random.NextDouble() * 3
                });
            }
        }

        private Brush RandomSupporterBrush()
        {
            Brush[] palette =
            {
                Brushes.Gold, Brushes.Tomato, Brushes.DeepSkyBlue, Brushes.White,
                Brushes.Orange, Brushes.Cyan, Brushes.LightGreen, Brushes.HotPink
            };

            return palette[random.Next(palette.Length)];
        }

        private void ResetPositions(bool preserveScore)
        {
            if (!preserveScore)
            {
                human.Score = bot.Score = 0;
            }

            human.X = 150;
            human.Y = FieldHeight / 2 - PlayerSize / 2;
            human.VX = 0;
            human.VY = 0;

            bot.X = FieldWidth - 150;
            bot.Y = FieldHeight / 2 - PlayerSize / 2;
            bot.VX = 0;
            bot.VY = 0;

            ball.X = FieldWidth / 2 - BallSize / 2;
            ball.Y = FieldHeight / 2 - BallSize / 2;
            ball.VX = 0;
            ball.VY = 0;

            ball.DribbleActive = false;
            ball.CurveStrength = 0;
            ball.CurveTicksLeft = 0;

            goalAnimationPlaying = false;
            goalAnimationTimer = 0;
            restartCooldown = 0;
            celebration = null;
            EffectsCanvas.Children.Clear();

            UpdateVisuals();
            UpdateHud("Match en cours.");
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (!gameRunning) return;

            DateTime now = DateTime.Now;
            double dt = Math.Min((now - lastFrameTime).TotalSeconds, 0.03);
            lastFrameTime = now;

            AnimateSupporters(now);

            if (goalAnimationPlaying)
            {
                UpdateGoalCelebration(dt);
                UpdateCooldownTexts();
                return;
            }

            if (restartCooldown > 0)
            {
                restartCooldown -= dt;
                StatusText.Text = $"Reprise dans {Math.Ceiling(restartCooldown)}...";
                CompactStatusText.Text = StatusText.Text;
                UpdateCooldownTexts();
                if (restartCooldown <= 0)
                {
                    ResetPositions(true);
                }
                return;
            }
            
            if (introVisible || isPaused)
            {
                AnimateSupporters(DateTime.Now);
                UpdateCooldownTexts();
                return;
            }

            UpdateCooldowns(human, dt);
            UpdateCooldowns(bot, dt);

            UpdateHuman(dt);
            UpdateBot(dt);
            UpdateBall(dt);

            HandlePlayerBallCollision(human);
            HandlePlayerBallCollision(bot);

            CheckGoal();
            KeepInsideField(human);
            KeepInsideField(bot);

            UpdateVisuals();
            UpdateCooldownTexts();
        }

        private void AnimateSupporters(DateTime now)
        {
            double t = now.TimeOfDay.TotalSeconds * 4.0;

            foreach (var s in supporters)
            {
                double jump = Math.Abs(Math.Sin(t + s.JumpOffset)) * s.VerticalAmplitude;
                Canvas.SetLeft(s.Body, s.BaseX);
                Canvas.SetTop(s.Body, s.BaseY - jump);
                Canvas.SetLeft(s.Head, s.BaseX + 1);
                Canvas.SetTop(s.Head, s.BaseY - 8 - jump);
            }
        }

        private void InitializeSkillIcons()
        {
            DrawDashIcon(DashIcon, Brushes.LightGreen, 1.0);
            DrawShotIcon(ShotIcon, Brushes.LightGreen, 1.0);
            DrawDribbleIcon(DribbleIcon, Brushes.LightGreen, 1.0);

            DrawDashIcon(CompactDashIcon, Brushes.LightGreen, 0.85);
            DrawShotIcon(CompactShotIcon, Brushes.LightGreen, 0.85);
            DrawDribbleIcon(CompactDribbleIcon, Brushes.LightGreen, 0.85);
        }

        private void DrawDashIcon(System.Windows.Controls.Canvas canvas, Brush brush, double scale)
        {
            canvas.Children.Clear();

            Polygon bolt = new Polygon
            {
                Fill = brush,
                Points = new PointCollection
                {
                    new Point(16*scale, 2*scale),
                    new Point(9*scale, 13*scale),
                    new Point(15*scale, 13*scale),
                    new Point(10*scale, 26*scale),
                    new Point(22*scale, 11*scale),
                    new Point(15*scale, 11*scale)
                }
            };

            canvas.Children.Add(bolt);
        }

        private void DrawShotIcon(System.Windows.Controls.Canvas canvas, Brush brush, double scale)
        {
            canvas.Children.Clear();

            Ellipse ball = new Ellipse
            {
                Width = 14 * scale,
                Height = 14 * scale,
                Stroke = brush,
                StrokeThickness = 2,
                Fill = Brushes.Transparent
            };

            Line l1 = new Line { X1 = 16*scale, Y1 = 2*scale, X2 = 26*scale, Y2 = 2*scale, Stroke = brush, StrokeThickness = 2 };
            Line l2 = new Line { X1 = 18*scale, Y1 = 7*scale, X2 = 28*scale, Y2 = 7*scale, Stroke = brush, StrokeThickness = 2 };

            canvas.Children.Add(ball);
            canvas.Children.Add(l1);
            canvas.Children.Add(l2);

            Canvas.SetLeft(ball, 2 * scale);
            Canvas.SetTop(ball, 7 * scale);
        }

        private void DrawDribbleIcon(Canvas canvas, Brush brush, double scale)
        {
            canvas.Children.Clear();

            string data = string.Format(
                CultureInfo.InvariantCulture,
                "M {0},{1} A {2},{3} 0 0 1 {4},{5}",
                3 * scale, 22 * scale,
                10 * scale, 10 * scale,
                25 * scale, 22 * scale
            );

            Path path = new Path
            {
                Stroke = brush,
                StrokeThickness = 2,
                Data = Geometry.Parse(data)
            };

            Ellipse dot = new Ellipse
            {
                Width = 6 * scale,
                Height = 6 * scale,
                Fill = brush
            };

            canvas.Children.Add(path);
            canvas.Children.Add(dot);

            Canvas.SetLeft(dot, 20 * scale);
            Canvas.SetTop(dot, 19 * scale);
        }

        private void UpdateHuman(double dt)
        {
            double dx = 0;
            double dy = 0;

            if (keyLeft) dx -= 1;
            if (keyRight) dx += 1;
            if (keyUp) dy -= 1;
            if (keyDown) dy += 1;

            Normalize(ref dx, ref dy);

            if (shiftPressed && human.DashCooldown <= 0 && (dx != 0 || dy != 0) && !human.IsDashing)
            {
                human.IsDashing = true;
                human.DashTimeLeft = 0.11;
                human.DashDirX = dx;
                human.DashDirY = dy;
                human.DashCooldown = 3.4;
                shiftPressed = false;
                UpdateHud("Dash !");
            }

            if (human.IsDashing)
            {
                human.DashTimeLeft -= dt;
                human.VX = human.DashDirX * 9.5;
                human.VY = human.DashDirY * 9.5;

                if (human.DashTimeLeft <= 0)
                {
                    human.IsDashing = false;
                    human.VX = 0;
                    human.VY = 0;
                }
            }
            else
            {
                human.VX = dx * PlayerBaseSpeed;
                human.VY = dy * PlayerBaseSpeed;
            }

            human.X += human.VX;
            human.Y += human.VY;

            if (spacePressed && human.ShotCooldown <= 0 && IsNearBall(human, 32))
            {
                ShootBallTowardsGoal(human, false);
                human.ShotCooldown = 2.2;
                UpdateHud("Tir puissant !");
                spacePressed = false;
            }

            if (ctrlPressed && human.DribbleCooldown <= 0 && IsNearBall(human, 34))
            {
                StartHalfCircleDribble(false);
                human.DribbleCooldown = 4.0;
                UpdateHud("Dribble demi-cercle !");
                ctrlPressed = false;
            }
        }

        private void UpdateBot(double dt)
        {
            bool canKick = IsNearBall(bot, 30);
            bool playerNear = DistanceCenters(bot, human) < 70;

            if (canKick && playerNear && bot.DribbleCooldown <= 0)
            {
                StartHalfCircleDribble(true);
                bot.DribbleCooldown = 3.5;
                UpdateHud("Le bot utilise un dribble parfait.");
                return;
            }

            if (canKick && bot.ShotCooldown <= 0)
            {
                ShootBallTowardsGoal(bot, true);
                bot.ShotCooldown = 1.8;
                UpdateHud("Le bot frappe.");
                return;
            }

            double targetX = ball.X;
            double targetY = ball.Y;

            if (DistanceCenters(bot, ball) > 140)
            {
                targetX = ball.X - 8;
                targetY = ball.Y;
            }

            double dx = targetX - bot.X;
            double dy = targetY - bot.Y;
            Normalize(ref dx, ref dy);

            double speed = BotBaseSpeed;

            if (DistanceCenters(bot, ball) > 160 && bot.DashCooldown <= 0 && !bot.IsDashing)
            {
                double dashDx = ball.X - bot.X;
                double dashDy = ball.Y - bot.Y;
                Normalize(ref dashDx, ref dashDy);

                bot.IsDashing = true;
                bot.DashTimeLeft = 0.10;
                bot.DashDirX = dashDx;
                bot.DashDirY = dashDy;
                bot.DashCooldown = 3.0;
                UpdateHud("Le bot dash.");
            }

            if (bot.IsDashing)
            {
                bot.DashTimeLeft -= dt;
                bot.VX = bot.DashDirX * 8.5;
                bot.VY = bot.DashDirY * 8.5;

                if (bot.DashTimeLeft <= 0)
                {
                    bot.IsDashing = false;
                }

                bot.X += bot.VX;
                bot.Y += bot.VY;
                return;
            }

            bot.VX = dx * speed;
            bot.VY = dy * speed;

            bot.X += bot.VX;
            bot.Y += bot.VY;
        }

        private void UpdateBall(double dt)
        {
            if (ball.DribbleActive)
            {
                UpdateHalfCircleDribble(dt);
                return;
            }

            ball.X += ball.VX;
            ball.Y += ball.VY;

            ball.VX *= BallFriction;
            ball.VY *= BallFriction;

            if (Math.Abs(ball.VX) < 0.02) ball.VX = 0;
            if (Math.Abs(ball.VY) < 0.02) ball.VY = 0;

            if (ball.Y <= 0)
            {
                ball.Y = 0;
                ball.VY *= -1;
            }

            if (ball.Y + ball.Height >= FieldHeight)
            {
                ball.Y = FieldHeight - ball.Height;
                ball.VY *= -1;
            }

            bool inGoalZoneY = ball.Y + ball.Height >= GoalTop && ball.Y <= GoalTop + GoalHeight;

            if (!inGoalZoneY)
            {
                if (ball.X <= 0)
                {
                    ball.X = 0;
                    ball.VX *= -1;
                }

                if (ball.X + ball.Width >= FieldWidth)
                {
                    ball.X = FieldWidth - ball.Width;
                    ball.VX *= -1;
                }
            }
        }

        private void StartHalfCircleDribble(bool toLeftGoal)
        {
            ball.DribbleActive = true;
            ball.DribbleElapsed = 0;
            ball.DribbleDuration = 0.62;

            ball.DribbleCenterX = ball.X + ball.Width / 2;
            ball.DribbleCenterY = ball.Y + ball.Height / 2;

            ball.DribbleRadius = 46;
            ball.DribbleStartAngle = toLeftGoal ? 0 : Math.PI;
            ball.DribbleEndAngle = toLeftGoal ? Math.PI : 0;
            ball.DribbleForwardBoost = toLeftGoal ? -4.3 : 4.3;
        }

        private void UpdateHalfCircleDribble(double dt)
        {
            ball.DribbleElapsed += dt;
            double t = Math.Min(ball.DribbleElapsed / ball.DribbleDuration, 1.0);

            double angle = ball.DribbleStartAngle + (ball.DribbleEndAngle - ball.DribbleStartAngle) * t;
            double cx = ball.DribbleCenterX + ball.DribbleForwardBoost * 12 * t;

            double bx = cx + Math.Cos(angle) * ball.DribbleRadius;
            double by = ball.DribbleCenterY + Math.Sin(angle) * ball.DribbleRadius;

            ball.X = bx - ball.Width / 2;
            ball.Y = by - ball.Height / 2;

            if (t >= 1.0)
            {
                ball.DribbleActive = false;
                ball.VX = ball.DribbleForwardBoost;
                ball.VY = 0;
            }
        }

        private void CheckGoal()
        {
            bool inGoalZoneY = ball.Y + ball.Height >= GoalTop && ball.Y <= GoalTop + GoalHeight;

            if (!inGoalZoneY || goalAnimationPlaying || restartCooldown > 0)
                return;

            if (ball.X <= -8)
            {
                bot.Score++;
                StartGoalCelebration(true);
            }
            else if (ball.X + ball.Width >= FieldWidth + 8)
            {
                human.Score++;
                StartGoalCelebration(false);
            }
        }

        private void StartGoalCelebration(bool botScored)
        {
            goalAnimationPlaying = true;
            goalAnimationTimer = 1.8;

            celebration = new GoalCelebration
            {
                BotScored = botScored,
                Variant = botScored ? random.Next(0, 8) : random.Next(8, 16),
                Time = 0
            };

            UpdateHud(botScored ? "But du bot !" : "But du joueur !");
            ScoreText.Text = $"Rouge {human.Score} - {bot.Score} Bleu";
            CompactScoreText.Text = $"{human.Score} - {bot.Score}";
        }

        private void UpdateGoalCelebration(double dt)
        {
            if (celebration == null) return;

            celebration.Time += dt;
            goalAnimationTimer -= dt;

            EffectsCanvas.Children.Clear();

            bool blueTheme = celebration.BotScored;
            Brush mainBrush = blueTheme ? Brushes.DeepSkyBlue : Brushes.Orange;
            Brush secondaryBrush = blueTheme ? Brushes.LightBlue : Brushes.Gold;

            double t = celebration.Time;
            int variant = celebration.Variant % 8;

            switch (variant)
            {
                case 0:
                    DrawRadialBurst(mainBrush, secondaryBrush, t);
                    break;
                case 1:
                    DrawConcentricRings(mainBrush, secondaryBrush, t);
                    break;
                case 2:
                    DrawDiagonalRain(mainBrush, secondaryBrush, t);
                    break;
                case 3:
                    DrawSpiral(mainBrush, secondaryBrush, t);
                    break;
                case 4:
                    DrawEnergyCross(mainBrush, secondaryBrush, t);
                    break;
                case 5:
                    DrawVerticalColumns(mainBrush, secondaryBrush, t);
                    break;
                case 6:
                    DrawHorizontalWave(mainBrush, secondaryBrush, t);
                    break;
                case 7:
                    DrawPulseCenter(mainBrush, secondaryBrush, t);
                    break;
            }

            TextBlock goalText = new TextBlock
            {
                Text = blueTheme ? "BUT BLEU !" : "BUT ROUGE !",
                Foreground = mainBrush,
                FontWeight = FontWeights.ExtraBold,
                FontSize = 42,
                Opacity = Math.Max(0.25, 1.0 - t / 1.8)
            };

            EffectsCanvas.Children.Add(goalText);
            Canvas.SetLeft(goalText, FieldWidth / 2 - 120);
            Canvas.SetTop(goalText, 60 + Math.Sin(t * 7) * 10);

            if (goalAnimationTimer <= 0)
            {
                EffectsCanvas.Children.Clear();
                goalAnimationPlaying = false;
                restartCooldown = 2.0;
            }
        }
        
        private void DrawRadialBurst(Brush mainBrush, Brush secondaryBrush, double t)
        {
            int particles = 28;
            for (int i = 0; i < particles; i++)
            {
                double angle = (Math.PI * 2 / particles) * i;
                double radius = 40 + t * 180;

                Ellipse p = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = i % 2 == 0 ? mainBrush : secondaryBrush,
                    Opacity = Math.Max(0.15, 1 - t / 1.8)
                };

                EffectsCanvas.Children.Add(p);
                Canvas.SetLeft(p, FieldWidth / 2 + Math.Cos(angle) * radius);
                Canvas.SetTop(p, FieldHeight / 2 + Math.Sin(angle) * radius);
            }
        }

        private void DrawConcentricRings(Brush mainBrush, Brush secondaryBrush, double t)
        {
            for (int i = 0; i < 4; i++)
            {
                double size = 60 + i * 50 + t * 90;
                Ellipse ring = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Stroke = i % 2 == 0 ? mainBrush : secondaryBrush,
                    StrokeThickness = 5,
                    Opacity = Math.Max(0.12, 1 - t / 1.8),
                    Fill = Brushes.Transparent
                };

                EffectsCanvas.Children.Add(ring);
                Canvas.SetLeft(ring, FieldWidth / 2 - size / 2);
                Canvas.SetTop(ring, FieldHeight / 2 - size / 2);
            }
        }

        private void DrawDiagonalRain(Brush mainBrush, Brush secondaryBrush, double t)
        {
            for (int i = 0; i < 18; i++)
            {
                Rectangle r = new Rectangle
                {
                    Width = 10,
                    Height = 26,
                    Fill = i % 2 == 0 ? mainBrush : secondaryBrush,
                    Opacity = 0.8
                };

                EffectsCanvas.Children.Add(r);
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

                Ellipse p = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = i % 2 == 0 ? mainBrush : secondaryBrush,
                    Opacity = 0.85
                };

                EffectsCanvas.Children.Add(p);
                Canvas.SetLeft(p, FieldWidth / 2 + Math.Cos(angle) * radius);
                Canvas.SetTop(p, FieldHeight / 2 + Math.Sin(angle) * radius);
            }
        }

        private void DrawEnergyCross(Brush mainBrush, Brush secondaryBrush, double t)
        {
            double len = 80 + t * 160;

            Line h = new Line
            {
                X1 = FieldWidth / 2 - len,
                Y1 = FieldHeight / 2,
                X2 = FieldWidth / 2 + len,
                Y2 = FieldHeight / 2,
                Stroke = mainBrush,
                StrokeThickness = 8,
                Opacity = 0.7
            };

            Line v = new Line
            {
                X1 = FieldWidth / 2,
                Y1 = FieldHeight / 2 - len,
                X2 = FieldWidth / 2,
                Y2 = FieldHeight / 2 + len,
                Stroke = secondaryBrush,
                StrokeThickness = 8,
                Opacity = 0.7
            };

            EffectsCanvas.Children.Add(h);
            EffectsCanvas.Children.Add(v);
        }

        private void DrawVerticalColumns(Brush mainBrush, Brush secondaryBrush, double t)
        {
            for (int i = 0; i < 10; i++)
            {
                Rectangle col = new Rectangle
                {
                    Width = 26,
                    Height = 100 + Math.Sin(t * 8 + i) * 50,
                    Fill = i % 2 == 0 ? mainBrush : secondaryBrush,
                    Opacity = 0.55
                };

                EffectsCanvas.Children.Add(col);
                Canvas.SetLeft(col, 120 + i * 70);
                Canvas.SetTop(col, 180 - t * 40);
            }
        }

        private void DrawHorizontalWave(Brush mainBrush, Brush secondaryBrush, double t)
        {
            for (int i = 0; i < 11; i++)
            {
                Ellipse wave = new Ellipse
                {
                    Width = 34,
                    Height = 34,
                    Fill = i % 2 == 0 ? mainBrush : secondaryBrush,
                    Opacity = 0.65
                };

                EffectsCanvas.Children.Add(wave);
                Canvas.SetLeft(wave, 80 + i * 72);
                Canvas.SetTop(wave, FieldHeight / 2 + Math.Sin(t * 8 + i) * 70);
            }
        }

        private void DrawPulseCenter(Brush mainBrush, Brush secondaryBrush, double t)
        {
            double size = 80 + Math.Abs(Math.Sin(t * 9)) * 170;

            Ellipse core = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = mainBrush,
                Opacity = 0.28
            };

            Ellipse ring = new Ellipse
            {
                Width = size + 40,
                Height = size + 40,
                Stroke = secondaryBrush,
                StrokeThickness = 6,
                Opacity = 0.55,
                Fill = Brushes.Transparent
            };

            EffectsCanvas.Children.Add(core);
            EffectsCanvas.Children.Add(ring);

            Canvas.SetLeft(core, FieldWidth / 2 - size / 2);
            Canvas.SetTop(core, FieldHeight / 2 - size / 2);

            Canvas.SetLeft(ring, FieldWidth / 2 - (size + 40) / 2);
            Canvas.SetTop(ring, FieldHeight / 2 - (size + 40) / 2);
        }

        private void ShootBallTowardsGoal(Player shooter, bool toLeftGoal)
        {
            double goalX = toLeftGoal ? 0 : FieldWidth;
            double goalY = GoalTop + GoalHeight / 2;

            double dx = goalX - (ball.X + ball.Width / 2);
            double dy = goalY - (ball.Y + ball.Height / 2);
            Normalize(ref dx, ref dy);

            ball.DribbleActive = false;
            ball.VX = dx * 7.4;
            ball.VY = dy * 7.4;
        }

        private void HandlePlayerBallCollision(Player player)
        {
            if (goalAnimationPlaying || restartCooldown > 0 || ball.DribbleActive)
                return;

            Rect playerRect = new Rect(player.X, player.Y, player.Width, player.Height);
            Rect ballRect = new Rect(ball.X, ball.Y, ball.Width, ball.Height);

            if (!playerRect.IntersectsWith(ballRect))
                return;

            double dx = (ball.X + ball.Width / 2) - (player.X + player.Width / 2);
            double dy = (ball.Y + ball.Height / 2) - (player.Y + player.Height / 2);
            Normalize(ref dx, ref dy);

            double impact = 3.0 + Math.Sqrt(player.VX * player.VX + player.VY * player.VY) * 0.9;
            ball.VX = dx * impact;
            ball.VY = dy * impact;
        }

        private bool IsNearBall(Player p, double range)
        {
            return DistanceCenters(p, ball) <= range;
        }

        private double DistanceCenters(Player p, Ball b)
        {
            double px = p.X + p.Width / 2;
            double py = p.Y + p.Height / 2;
            double bx = b.X + b.Width / 2;
            double by = b.Y + b.Height / 2;

            double dx = bx - px;
            double dy = by - py;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private double DistanceCenters(Player a, Player b)
        {
            double ax = a.X + a.Width / 2;
            double ay = a.Y + a.Height / 2;
            double bx = b.X + b.Width / 2;
            double by = b.Y + b.Height / 2;

            double dx = bx - ax;
            double dy = by - ay;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private void KeepInsideField(Player p)
        {
            if (p.X < 0) p.X = 0;
            if (p.Y < 0) p.Y = 0;
            if (p.X + p.Width > FieldWidth) p.X = FieldWidth - p.Width;
            if (p.Y + p.Height > FieldHeight) p.Y = FieldHeight - p.Height;
        }

        private void Normalize(ref double x, ref double y)
        {
            double length = Math.Sqrt(x * x + y * y);
            if (length <= 0.0001) return;
            x /= length;
            y /= length;
        }

        private void UpdateHud(string status)
        {
            ScoreText.Text = $"Rouge {human.Score} - {bot.Score} Bleu";
            CompactScoreText.Text = $"{human.Score} - {bot.Score}";
            StatusText.Text = status;
            CompactStatusText.Text = status;
        }

        private void UpdateCooldowns(Player p, double dt)
        {
            p.DashCooldown = Math.Max(0, p.DashCooldown - dt);
            p.ShotCooldown = Math.Max(0, p.ShotCooldown - dt);
            p.DribbleCooldown = Math.Max(0, p.DribbleCooldown - dt);
        }

        private void UpdateCooldownTexts()
        {
            SetSkillVisual(DashText, DashIcon, CompactDashIcon, human.DashCooldown, "Dash", Brushes.Gold);
            SetSkillVisual(ShotText, ShotIcon, CompactShotIcon, human.ShotCooldown, "Tir", Brushes.OrangeRed);
            SetSkillVisual(DribbleText, DribbleIcon, CompactDribbleIcon, human.DribbleCooldown, "Dribble", Brushes.DeepSkyBlue);
        }

        private void SetSkillVisual(System.Windows.Controls.TextBlock text, System.Windows.Controls.Canvas icon, System.Windows.Controls.Canvas compactIcon, double cooldown, string label, Brush readyBrush)
        {
            bool ready = cooldown <= 0;
            double pulse = 0.75 + 0.25 * Math.Abs(Math.Sin(DateTime.Now.TimeOfDay.TotalSeconds * 5));

            text.Text = ready ? $"{label} : prêt" : $"{label} : {cooldown:F1}s";
            text.Foreground = ready ? readyBrush : Brushes.Orange;

            icon.Opacity = ready ? pulse : 0.35;
            compactIcon.Opacity = ready ? pulse : 0.35;
        }

        private void UpdateVisuals()
        {
            Canvas.SetLeft(humanShape, human.X);
            Canvas.SetTop(humanShape, human.Y);

            Canvas.SetLeft(botShape, bot.X);
            Canvas.SetTop(botShape, bot.Y);

            Canvas.SetLeft(ballShape, ball.X);
            Canvas.SetTop(ballShape, ball.Y);
        }

        private void ToggleHud()
        {
            hudVisible = !hudVisible;
            HudPanel.Visibility = hudVisible ? Visibility.Visible : Visibility.Collapsed;
            CompactHud.Visibility = hudVisible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (introVisible)
            {
                if (e.Key == Key.Enter)
                {
                    introVisible = false;
                    IntroOverlay.Visibility = Visibility.Collapsed;
                    UpdateHud("Match en cours.");
                }
                return;
            }

            if (e.Key == Key.Escape)
            {
                isPaused = !isPaused;
                PauseOverlay.Visibility = isPaused ? Visibility.Visible : Visibility.Collapsed;
                CompactStatusText.Text = isPaused ? "Pause" : StatusText.Text;
                return;
            }

            if (isPaused)
            {
                if (e.Key == Key.C)
                    ToggleHud();

                if (e.Key == Key.R)
                {
                    human.Score = 0;
                    bot.Score = 0;
                    ResetPositions(true);
                    isPaused = false;
                    PauseOverlay.Visibility = Visibility.Collapsed;
                    UpdateHud("Match relancé.");
                }
                return;
            }

            switch (e.Key)
            {
                case Key.Up: keyUp = true; break;
                case Key.Down: keyDown = true; break;
                case Key.Left: keyLeft = true; break;
                case Key.Right: keyRight = true; break;

                case Key.LeftShift:
                case Key.RightShift:
                    shiftPressed = true;
                    break;

                case Key.Space:
                    spacePressed = true;
                    break;

                case Key.LeftCtrl:
                case Key.RightCtrl:
                    ctrlPressed = true;
                    break;

                case Key.C:
                    ToggleHud();
                    break;

                case Key.R:
                    human.Score = 0;
                    bot.Score = 0;
                    ResetPositions(true);
                    UpdateHud("Match relancé.");
                    break;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up: keyUp = false; break;
                case Key.Down: keyDown = false; break;
                case Key.Left: keyLeft = false; break;
                case Key.Right: keyRight = false; break;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            CompositionTarget.Rendering -= GameLoop;
            base.OnClosed(e);
        }
    }

    public class Player
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double VX { get; set; }
        public double VY { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public Brush Color { get; set; } = Brushes.White;
        public string Name { get; set; } = "";
        public int Score { get; set; }

        public double DashCooldown { get; set; }
        public double ShotCooldown { get; set; }
        public double DribbleCooldown { get; set; }
        
        public bool IsDashing { get; set; }
        public double DashTimeLeft { get; set; }
        public double DashDirX { get; set; }
        public double DashDirY { get; set; }
    }

    public class Ball
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double VX { get; set; }
        public double VY { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public double CurveStrength { get; set; }
        public int CurveTicksLeft { get; set; }

        public bool DribbleActive { get; set; }
        public double DribbleElapsed { get; set; }
        public double DribbleDuration { get; set; }
        public double DribbleCenterX { get; set; }
        public double DribbleCenterY { get; set; }
        public double DribbleRadius { get; set; }
        public double DribbleStartAngle { get; set; }
        public double DribbleEndAngle { get; set; }
        public double DribbleForwardBoost { get; set; }
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