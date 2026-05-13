using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace VraiPseudoSae.view.Pinball
{
    public partial class PinballGame : Window
    {
        // ─── Constantes physiques ──────────────────────────────────────────────
        private const double TableLeft   = 12;
        private const double TableRight  = 568;
        private const double TableTop    = 0;
        private const double TableBottom = 650;
        private const double BallRadius  = 9;
        private const double Gravity     = 0.28;
        private const double Damping     = 0.65;
        private const double FlipperLen  = 120.0;

        // ─── État de la bille ─────────────────────────────────────────────────
        private double ballX, ballY;
        private double ballVX, ballVY;
        private bool   ballInPlay = false;
        private bool   ballOnPlunger = true;
        private double plungerCharge = 0;
        private bool   chargingPlunger = false;

        // ─── Score / Vies ─────────────────────────────────────────────────────
        private int score = 0;
        private int balls = 3;

        // ─── Flippers ─────────────────────────────────────────────────────────
        private bool leftFlipperUp  = false;
        private bool rightFlipperUp = false;

        // Flipper gauche : pivot (155,635), angle repos = -20°, activé = -45°
        private const double LFPivotX = 155, LFPivotY = 635;
        private const double LFAngleRest = -20, LFAngleUp = -45;

        // Flipper droit : pivot (393,635), angle repos = 200°, activé = 225°
        private const double RFPivotX = 393, RFPivotY = 635;
        private const double RFAngleRest = 200, RFAngleUp = 225;

        // ─── Bumpers (cercle, rayon 23) ───────────────────────────────────────
        private readonly List<(double cx, double cy, int pts, Ellipse el)> bumpers = new();

        // ─── Cibles (rectangles) ──────────────────────────────────────────────
        private readonly List<(double x, double y, double w, double h, int pts, Rectangle el, bool hit)> targets = new();
        private List<(double x, double y, double w, double h, int pts, Rectangle el, bool hit)> targetsMutable = new();

        // ─── Timer principal ──────────────────────────────────────────────────
        private readonly DispatcherTimer gameTimer = new();
        private readonly DispatcherTimer bumperFlashTimer = new();

        // ─── Multiplicateur de score ──────────────────────────────────────────
        private int multiplier = 1;

        public PinballGame()
        {
            InitializeComponent();
            SetupGame();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  INITIALISATION
        // ══════════════════════════════════════════════════════════════════════

        private void SetupGame()
        {
            // Bumpers
            bumpers.Add((130, 118, 100, Bumper1));
            bumpers.Add((180,  78, 100, Bumper2));
            bumpers.Add((353,  78, 100, Bumper3));
            bumpers.Add((398, 118, 200, Bumper4));
            bumpers.Add((123, 193, 150, Bumper5));
            bumpers.Add((405, 193, 150, Bumper6));

            // Cibles (x, y, w, h, pts, element)
            targetsMutable = new List<(double, double, double, double, int, Rectangle, bool)>
            {
                (130, 265, 40, 10, 50,  Target1, false),
                (180, 265, 40, 10, 50,  Target2, false),
                (230, 265, 40, 10, 50,  Target3, false),
                (310, 265, 40, 10, 100, Target4, false),
                (360, 265, 40, 10, 100, Target5, false),
            };

            // Bille à la position de départ
            ResetBall();

            // Timer principal 60 fps
            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += GameLoop;

            // Timer flash bumpers
            bumperFlashTimer.Interval = TimeSpan.FromMilliseconds(120);
            bumperFlashTimer.Tick += BumperFlashReset;
        }

        private void ResetBall()
        {
            ballX = 554;
            ballY = 573;
            ballVX = 0;
            ballVY = 0;
            ballOnPlunger = true;
            ballInPlay = false;
            plungerCharge = 0;
            chargingPlunger = false;
            UpdateBallPosition();
        }

        // ══════════════════════════════════════════════════════════════════════
        //  BOUCLE DE JEU
        // ══════════════════════════════════════════════════════════════════════

        private void GameLoop(object? sender, EventArgs e)
        {
            if (!ballInPlay)
            {
                // Charge du ressort
                if (chargingPlunger)
                {
                    plungerCharge = Math.Min(plungerCharge + 0.6, 22);
                    AnimatePlunger();
                }
                return;
            }

            // Physique
            ballVY += Gravity;
            ballX  += ballVX;
            ballY  += ballVY;

            // Collisions murs
            CollideWalls();

            // Collisions flippers
            CollideFlipperLeft();
            CollideFlipperRight();

            // Collisions bumpers
            CollideBumpers();

            // Collisions cibles
            CollideTargets();

            // Bille perdue (bas)
            if (ballY > TableBottom + 20)
            {
                BallLost();
                return;
            }

            UpdateBallPosition();
        }

        // ──────────────────────────────────────────────────────────────────────
        //  COLLISIONS
        // ──────────────────────────────────────────────────────────────────────

        private void CollideWalls()
        {
            // Mur gauche
            if (ballX - BallRadius < TableLeft + 12)
            {
                ballX  = TableLeft + 12 + BallRadius;
                ballVX = Math.Abs(ballVX) * Damping;
            }
            // Mur droit (zone de lancement exclue)
            if (ballX + BallRadius > 535 && ballY < 88)
            {
                ballX  = 535 - BallRadius;
                ballVX = -Math.Abs(ballVX) * Damping;
            }
            // Mur droit normal
            if (ballX + BallRadius > TableRight - 12 && ballY >= 88)
            {
                ballX  = TableRight - 12 - BallRadius;
                ballVX = -Math.Abs(ballVX) * Damping;
            }
            // Mur haut
            if (ballY - BallRadius < TableTop + 12)
            {
                ballY  = TableTop + 12 + BallRadius;
                ballVY = Math.Abs(ballVY) * Damping;
            }

            // Rampe gauche haute (12,240)→(90,340)
            ReflectLineSegment(12, 240, 90, 340, stiffness: 0.7);
            // Rampe droite haute (536,240)→(460,340)
            ReflectLineSegment(536, 240, 460, 340, stiffness: 0.7);
            // Slingshot gauche (12,370)→(100,450)
            ReflectLineSegment(12, 370, 100, 450, stiffness: 0.85, bonus: 80);
            // Slingshot droit (536,370)→(448,450)
            ReflectLineSegment(536, 370, 448, 450, stiffness: 0.85, bonus: 80);
            // Gutter gauche (12,580)→(155,635)
            ReflectLineSegment(12, 580, 155, 635, stiffness: 0.6);
            // Gutter droit (536,580)→(393,635)
            ReflectLineSegment(536, 580, 393, 635, stiffness: 0.6);
        }

        private void ReflectLineSegment(double x1, double y1, double x2, double y2,
                                         double stiffness = 0.7, int bonus = 0)
        {
            // Vecteur de la ligne
            double dx = x2 - x1;
            double dy = y2 - y1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1) return;

            double nx = -dy / len; // normale
            double ny =  dx / len;

            // Projection de la bille sur la ligne
            double bx = ballX - x1;
            double by = ballY - y1;
            double t  = (bx * dx + by * dy) / (len * len);
            if (t < 0 || t > 1) return;

            // Pied de perpendiculaire
            double px = x1 + t * dx;
            double py = y1 + t * dy;
            double dist = (ballX - px) * nx + (ballY - py) * ny;

            if (Math.Abs(dist) < BallRadius)
            {
                // Repousse
                ballX += nx * (BallRadius - dist);
                ballY += ny * (BallRadius - dist);

                // Réflexion
                double dot = ballVX * nx + ballVY * ny;
                ballVX -= (1 + stiffness) * dot * nx;
                ballVY -= (1 + stiffness) * dot * ny;

                if (bonus > 0)
                    AddScore(bonus * multiplier);
            }
        }

        private void CollideFlipperLeft()
        {
            double angle = (leftFlipperUp ? LFAngleUp : LFAngleRest) * Math.PI / 180;
            double tipX  = LFPivotX + FlipperLen * Math.Cos(angle);
            double tipY  = LFPivotY + FlipperLen * Math.Sin(angle);
            CollideCapsule(LFPivotX, LFPivotY, tipX, tipY, 8, leftFlipperUp ? 0.9 : 0.5, isFlipperActive: leftFlipperUp);
        }

        private void CollideFlipperRight()
        {
            double angle = (rightFlipperUp ? RFAngleUp : RFAngleRest) * Math.PI / 180;
            double tipX  = RFPivotX + FlipperLen * Math.Cos(angle);
            double tipY  = RFPivotY + FlipperLen * Math.Sin(angle);
            CollideCapsule(RFPivotX, RFPivotY, tipX, tipY, 8, rightFlipperUp ? 0.9 : 0.5, isFlipperActive: rightFlipperUp);
        }

        private void CollideCapsule(double ax, double ay, double bx, double by,
                                     double radius, double bounce, bool isFlipperActive)
        {
            double dx = bx - ax;
            double dy = by - ay;
            double len2 = dx * dx + dy * dy;
            if (len2 < 0.001) return;

            double t = ((ballX - ax) * dx + (ballY - ay) * dy) / len2;
            t = Math.Max(0, Math.Min(1, t));

            double closestX = ax + t * dx;
            double closestY = ay + t * dy;

            double distX = ballX - closestX;
            double distY = ballY - closestY;
            double dist  = Math.Sqrt(distX * distX + distY * distY);

            double combined = BallRadius + radius;
            if (dist < combined && dist > 0.001)
            {
                double nx = distX / dist;
                double ny = distY / dist;

                // Repousse
                ballX = closestX + nx * combined;
                ballY = closestY + ny * combined;

                // Réflexion
                double dot = ballVX * nx + ballVY * ny;
                if (dot < 0)
                {
                    ballVX -= (1 + bounce) * dot * nx;
                    ballVY -= (1 + bounce) * dot * ny;

                    if (isFlipperActive)
                    {
                        // Boost vers le haut
                        ballVY -= 4.5;
                        if (ax < 300)
                            ballVX += 1.5;
                        else
                            ballVX -= 1.5;
                    }
                }
            }
        }

        private void CollideBumpers()
        {
            foreach (var (cx, cy, pts, el) in bumpers)
            {
                double dist = Math.Sqrt((ballX - cx) * (ballX - cx) + (ballY - cy) * (ballY - cy));
                double combined = BallRadius + 23;
                if (dist < combined && dist > 0.001)
                {
                    double nx = (ballX - cx) / dist;
                    double ny = (ballY - cy) / dist;

                    ballX = cx + nx * (combined + 1);
                    ballY = cy + ny * (combined + 1);

                    double speed = Math.Sqrt(ballVX * ballVX + ballVY * ballVY);
                    ballVX = nx * Math.Max(speed, 7);
                    ballVY = ny * Math.Max(speed, 7);

                    AddScore(pts * multiplier);
                    FlashBumper(el);
                }
            }
        }

        private void CollideTargets()
        {
            for (int i = 0; i < targetsMutable.Count; i++)
            {
                var t = targetsMutable[i];
                if (t.hit) continue;

                double tx = t.x, ty = t.y, tw = t.w, th = t.h;
                if (ballX + BallRadius > tx && ballX - BallRadius < tx + tw &&
                    ballY + BallRadius > ty && ballY - BallRadius < ty + th)
                {
                    // Rebond
                    ballVY = -Math.Abs(ballVY) * 0.7;
                    AddScore(t.pts * multiplier);

                    // Marquer la cible
                    t.el.Fill = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                    t.el.Stroke = new SolidColorBrush(Color.FromRgb(80, 80, 80));
                    targetsMutable[i] = (tx, ty, tw, th, t.pts, t.el, true);

                    // Si toutes les cibles touchées → bonus + reset
                    bool allHit = true;
                    foreach (var tt in targetsMutable) if (!tt.hit) { allHit = false; break; }
                    if (allHit)
                    {
                        multiplier = Math.Min(multiplier + 1, 5);
                        AddScore(5000);
                        ResetTargets();
                    }
                }
            }
        }

        private void ResetTargets()
        {
            for (int i = 0; i < targetsMutable.Count; i++)
            {
                var t = targetsMutable[i];
                if (i < 3)
                {
                    t.el.Fill = new SolidColorBrush(Color.FromRgb(0xF5, 0x7F, 0x17));
                    t.el.Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0x02));
                }
                else
                {
                    t.el.Fill = new SolidColorBrush(Color.FromRgb(0xE5, 0x39, 0x35));
                    t.el.Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B));
                }
                targetsMutable[i] = (t.x, t.y, t.w, t.h, t.pts, t.el, false);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        //  FLIPPERS VISUELS
        // ──────────────────────────────────────────────────────────────────────

        private void UpdateFlippers()
        {
            // Flipper gauche
            double lAngle = leftFlipperUp ? LFAngleUp : LFAngleRest;
            double lRad   = lAngle * Math.PI / 180;
            double ltX    = LFPivotX + FlipperLen * Math.Cos(lRad);
            double ltY    = LFPivotY + FlipperLen * Math.Sin(lRad);
            FlipperLeft.Points = new PointCollection
            {
                new Point(LFPivotX, LFPivotY - 8),
                new Point(ltX, ltY - 3),
                new Point(ltX, ltY + 3),
                new Point(LFPivotX, LFPivotY + 8),
            };

            // Flipper droit
            double rAngle = rightFlipperUp ? RFAngleUp : RFAngleRest;
            double rRad   = rAngle * Math.PI / 180;
            double rtX    = RFPivotX + FlipperLen * Math.Cos(rRad);
            double rtY    = RFPivotY + FlipperLen * Math.Sin(rRad);
            FlipperRight.Points = new PointCollection
            {
                new Point(RFPivotX, RFPivotY - 8),
                new Point(rtX, rtY - 3),
                new Point(rtX, rtY + 3),
                new Point(RFPivotX, RFPivotY + 8),
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        //  BILLE VISUELLE
        // ──────────────────────────────────────────────────────────────────────

        private void UpdateBallPosition()
        {
            Canvas.SetLeft(Ball, ballX - BallRadius);
            Canvas.SetTop(Ball,  ballY - BallRadius);
        }

        // ──────────────────────────────────────────────────────────────────────
        //  RESSORT VISUEL
        // ──────────────────────────────────────────────────────────────────────

        private void AnimatePlunger()
        {
            double topY = 590 + plungerCharge;
            Canvas.SetTop(Plunger, topY);
            Canvas.SetTop(Ball, 573 + plungerCharge / 2);
            ballY = 573 + plungerCharge / 2 + BallRadius;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  LOGIQUE DE JEU
        // ══════════════════════════════════════════════════════════════════════

        private void LaunchBall()
        {
            if (!ballOnPlunger) return;
            double power = plungerCharge * 0.9 + 8;
            ballVX = -1.5;
            ballVY = -power;
            ballOnPlunger = false;
            ballInPlay    = true;
            chargingPlunger = false;
            plungerCharge   = 0;
            Canvas.SetTop(Plunger, 590);
            ReadyOverlay.Visibility = Visibility.Collapsed;
        }

        private void BallLost()
        {
            ballInPlay = false;
            balls--;
            BallsText.Text = balls.ToString();

            if (balls <= 0)
            {
                GameOver();
            }
            else
            {
                ResetBall();
                UpdateBallPosition();
            }
        }

        private void AddScore(int pts)
        {
            score += pts;
            ScoreText.Text = score.ToString("N0").Replace(",", " ");
        }

        private void GameOver()
        {
            gameTimer.Stop();
            FinalScoreText.Text = $"Score final : {score:N0}".Replace(",", " ");
            GameOverOverlay.Visibility = Visibility.Visible;
        }

        // ──────────────────────────────────────────────────────────────────────
        //  EFFETS VISUELS
        // ──────────────────────────────────────────────────────────────────────

        private Ellipse? _flashingBumper;

        private void FlashBumper(Ellipse bumperEl)
        {
            _flashingBumper = bumperEl;
            bumperEl.Fill = new SolidColorBrush(Colors.White);
            bumperEl.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.White,
                BlurRadius = 20,
                ShadowDepth = 0
            };
            bumperFlashTimer.Stop();
            bumperFlashTimer.Start();
        }

        private void BumperFlashReset(object? sender, EventArgs e)
        {
            bumperFlashTimer.Stop();
            if (_flashingBumper == null) return;

            // Restore par nom
            foreach (var (cx, cy, pts, el) in bumpers)
            {
                if (el == _flashingBumper)
                {
                    el.Effect = null;
                    if (pts == 200)
                        el.Fill = new SolidColorBrush(Color.FromRgb(0xC6, 0x28, 0x28));
                    else if (pts == 150)
                        el.Fill = new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x20));
                    else
                        el.Fill = new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0));
                    break;
                }
            }
            _flashingBumper = null;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ENTRÉES CLAVIER
        // ══════════════════════════════════════════════════════════════════════

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Z:
                case Key.LeftShift:
                    leftFlipperUp = true;
                    UpdateFlippers();
                    break;

                case Key.OemQuestion: // touche /
                case Key.RightShift:
                case Key.OemBackslash:
                    rightFlipperUp = true;
                    UpdateFlippers();
                    break;

                case Key.Space:
                    if (ballOnPlunger && !chargingPlunger)
                    {
                        chargingPlunger = true;
                        if (!gameTimer.IsEnabled) gameTimer.Start();
                    }
                    else if (ballOnPlunger)
                    {
                        LaunchBall();
                    }
                    break;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Z:
                case Key.LeftShift:
                    leftFlipperUp = false;
                    UpdateFlippers();
                    break;

                case Key.OemQuestion:
                case Key.RightShift:
                case Key.OemBackslash:
                    rightFlipperUp = false;
                    UpdateFlippers();
                    break;

                case Key.Space:
                    if (chargingPlunger)
                        LaunchBall();
                    break;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  BOUTONS UI
        // ══════════════════════════════════════════════════════════════════════

        private void BtnReplay_Click(object sender, RoutedEventArgs e)
        {
            score = 0;
            balls = 3;
            multiplier = 1;
            ScoreText.Text = "0";
            BallsText.Text = "3";
            ResetTargets();
            ResetBall();
            GameOverOverlay.Visibility = Visibility.Collapsed;
            ReadyOverlay.Visibility    = Visibility.Visible;
        }

        private void BtnQuit_Click(object sender, RoutedEventArgs e)
        {
            gameTimer.Stop();
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            gameTimer.Stop();
            bumperFlashTimer.Stop();
        }
    }
}
