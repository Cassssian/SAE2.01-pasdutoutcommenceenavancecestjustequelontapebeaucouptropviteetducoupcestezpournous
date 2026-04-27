using System;
using System.Collections.Generic;
using System.Windows;
using System.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using VraiPseudoSae.data.AudioPlayer;

namespace VraiPseudoSae.view.RLS_Pages
{
    public partial class RLS : Window
    {
        const double Gravity = 0.6;
        const double GroundY = 520;
        const double FieldWidth = 1000;
        const double Friction = 0.92;
        const double BallFriction = 0.99;
        const double BallBounce = 0.75;

        class Car(string NameCar)
        {
            public double X, Y;
            public double VX, VY;
            public readonly double Width = 60;
            public readonly double Height = 38;
            public bool OnGround;
            public double Boost = 100;
            public Canvas Shape;
            public Shape Flame1, Flame2;
            public bool IsBot;
            public int FacingDir = 1;   // 1 = droite, -1 = gauche
            public int JumpsLeft = 2;   // double saut
            public bool JumpKeyPrev;    // état précédent de la touche saut (edge detection)
            public readonly string name = NameCar;
        }

        Car p1 = new Car("p1");
        Car p2 = new Car("p2");

        double ballX = 485, ballY = 250;
        double ballVX = 0, ballVY = 0;
        const double ballSize = 30;

        int score1 = 0, score2 = 0;
        HashSet<Key> keys = new HashSet<Key>();
        DispatcherTimer timer;
        bool goalPaused = false;
        int goalTimer = 0;
        bool gameRunning = false;
        Random rng = new Random();
        AudioRegistry registry = new AudioRegistry();

        // Pour le bot : edge detection du saut
        bool botJumpPrev = false;

        public RLS()
        {
            InitializeComponent();
            Loaded += (s, e) => Focus();
            registry.Load(@"C:\Users\Asus\RiderProjects\VraiPseudoSae201\VraiPseudoSae\data\RLS_Audio\AudioStructure.json");
        }

        public void StartGame(bool vsBot)
        {
            MainMenu.Visibility = Visibility.Collapsed;
            GameCanvas.Visibility = Visibility.Visible;
            ModeText.Text = vsBot ? "Mode: 1J vs BOT" : "Mode: 2 Joueurs";

            p1.Shape = Car1; p1.Flame1 = Car1Flame; p1.Flame2 = Car1Flame2; p1.IsBot = false;
            p2.Shape = Car2; p2.Flame1 = Car2Flame; p2.Flame2 = Car2Flame2; p2.IsBot = vsBot;

            // Direction de départ : P1 regarde à droite, P2 à gauche
            p1.FacingDir = 1;
            p2.FacingDir = 1;

            score1 = 0; score2 = 0;
            ResetPositions();

            if (timer == null)
            {
                timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                timer.Tick += GameLoop;
            }
            gameRunning = true;
            timer.Start();
            Focus();
        }

        void BackToMenu()
        {
            gameRunning = false;
            timer?.Stop();
            GameCanvas.Visibility = Visibility.Collapsed;
            MainMenu.Visibility = Visibility.Visible;
            GoalText.Visibility = Visibility.Collapsed;
        }

        void ResetPositions()
        {
            p1.X = 150; p1.Y = 482; p1.VX = 0; p1.VY = 0; p1.OnGround = true;
            p1.JumpsLeft = 2; p1.JumpKeyPrev = false;
            p2.X = 790; p2.Y = 482; p2.VX = 0; p2.VY = 0; p2.OnGround = true;
            p2.JumpsLeft = 2; p2.JumpKeyPrev = false;
            ballX = 485; ballY = 250; ballVX = 0; ballVY = 0;
            p1.Boost = 100; p2.Boost = 100;
            botJumpPrev = false;
        }

        void GameLoop(object sender, EventArgs e)
        {
            if (goalPaused)
            {
                goalTimer--;
                if (goalTimer <= 0)
                {
                    goalPaused = false;
                    GoalText.Visibility = Visibility.Collapsed;
                    ResetPositions();
                }
                return;
            }

            HandleP1Input();
            if (p2.IsBot) HandleBotInput(); else HandleP2Input();

            UpdateCar(p1);
            UpdateCar(p2);
            UpdateBall();
            CheckCarBallCollision(p1);
            CheckCarBallCollision(p2);
            CheckCarCarCollision();
            CheckCrossbarCollision();
            CheckGoal();
            Render();
        }

        void ProcessCarInput(Car c, bool left, bool right, bool jump, bool boost)
        {
            if (left) { c.VX -= 0.8; c.FacingDir = c.name == "p1" ? -1: 1; }
            if (right) { c.VX += 0.8; c.FacingDir = c.name == "p1" ? 1 : -1; }

            // Saut (edge detection) : compte chaque appui, max 2 en l'air
            if (jump && !c.JumpKeyPrev)
            {
                if (c.OnGround)
                {
                    c.VY = -12;
                    c.OnGround = false;
                    c.JumpsLeft = 1; // il lui reste 1 double saut
                    //registry.Play("car_sound/category/second_jump_mouvement/jump0020");
                }
                else if (c.JumpsLeft > 0)
                {
                    c.VY = -11; // 2e saut légèrement plus faible
                    c.JumpsLeft--;
                }
            }
            c.JumpKeyPrev = jump;

            // Vol : saut + boost en l'air
            if (jump && boost && c.Boost > 0 && !c.OnGround)
            {
                c.VY -= 0.9;
                c.Boost -= 1.5;
            }
            else if (boost && c.Boost > 0)
            {
                double dir = c.VX >= 0 ? 1 : -1;
                if (Math.Abs(c.VX) < 0.1) dir = c.FacingDir;
                c.VX += dir * 1.2;
                c.Boost -= 1.5;
            }

            if (c.Boost < 0) c.Boost = 0;
            c.Boost = Math.Min(100, c.Boost + 0.2);

            bool flameOn = boost && c.Boost > 0;
            c.Flame1.Visibility = flameOn ? Visibility.Visible : Visibility.Collapsed;
            c.Flame2.Visibility = flameOn ? Visibility.Visible : Visibility.Collapsed;
        }

        void HandleP1Input()
        {
            ProcessCarInput(p1,
                left: keys.Contains(Key.Q),
                right: keys.Contains(Key.D),
                jump: keys.Contains(Key.Z),
                boost: keys.Contains(Key.Space));
        }

        void HandleP2Input()
        {
            ProcessCarInput(p2,
                left: keys.Contains(Key.K),
                right: keys.Contains(Key.M),
                jump: keys.Contains(Key.O),
                boost: keys.Contains(Key.Enter));
        }

        void HandleBotInput()
        {
            double botCX = p2.X + p2.Width / 2;
            double ballCX = ballX + ballSize / 2;
            double ballCY = ballY + ballSize / 2;

            // Prédiction simple de la position de la balle (20 frames dans le futur)
            double predictedBallX = ballCX + ballVX * 15;
            double predictedBallY = ballCY + ballVY * 15 + 0.5 * Gravity * 15 * 15;
            
            // Limites du terrain pour la prédiction
            predictedBallX = Math.Max(20, Math.Min(FieldWidth - 20, predictedBallX));
            predictedBallY = Math.Max(0, Math.Min(GroundY, predictedBallY));

            double targetX;
            // Mode Kickoff
            if (Math.Abs(ballVX) < 0.1 && Math.Abs(ballCX - 500) < 10)
            {
                targetX = ballCX + 35; // Fonce sur la balle
            }
            else if (ballCX < 400) // Défense pure : la balle est chez l'adversaire, mais on veut rester prêt
            {
                targetX = ballCX + 100;
                if (targetX > 850) targetX = 850;
            }
            else if (ballCX > 700) // Danger : la balle est proche de notre but
            {
                targetX = ballCX + 20; // On essaie de la dégager d'urgence
            }
            else // Milieu de terrain / Attaque
            {
                // On essaie de rester à droite de la balle
                if (botCX < ballCX - 10) // On est passé à gauche de la balle (DANGER d'owngoal)
                {
                    targetX = ballCX + 80; // On essaie de se replacer à droite en priorité
                }
                else
                {
                    targetX = predictedBallX + 30;
                }
            }

            // Sécurité : ne jamais aller trop loin à gauche si la balle est à droite
            if (ballCX > botCX + 100 && targetX < botCX) targetX = botCX + 10; 

            double diff = targetX - botCX;
            bool left = diff < -15;
            bool right = diff > 15;

            // Saut et Boost
            bool jump = false;
            bool boost = false;

            double distToBall = Math.Sqrt(Math.Pow(ballCX - botCX, 2) + Math.Pow(ballCY - (p2.Y + p2.Height / 2), 2));

            // Saut si la balle est haute et proche
            if (p2.OnGround)
            {
                if (distToBall < 120 && ballCY < p2.Y - 20)
                {
                    jump = true;
                }
            }
            else if (p2.JumpsLeft > 0)
            {
                // Double saut pour atteindre une balle très haute
                if (ballCY < p2.Y - 50 && distToBall < 100)
                {
                    jump = true;
                }
            }

            // Boost pour aller plus vite vers la cible ou pour s'envoler
            if (Math.Abs(diff) > 100 && p2.Boost > 10)
            {
                boost = true;
            }
            
            if (!p2.OnGround && ballCY < p2.Y && distToBall < 150 && p2.Boost > 0)
            {
                boost = true;
            }

            ProcessCarInput(p2, left, right, jump, boost);
        }

        void UpdateCar(Car c)
        {
            c.VY += Gravity;
            c.X += c.VX;
            c.Y += c.VY;
            c.VX *= Friction;

            if (c.Y + c.Height >= GroundY)
            {
                c.Y = GroundY - c.Height;
                c.VY = 0;
                if (!c.OnGround) c.JumpsLeft = 2; // recharge le double saut à l'atterrissage
                c.OnGround = true;
            }
            else c.OnGround = false;

            if (c.X < 0) { c.X = 0; c.VX = 0; }
            if (c.X + c.Width > FieldWidth) { c.X = FieldWidth - c.Width; c.VX = 0; }
            if (c.Y < 0) { c.Y = 0; c.VY = 0; }

            c.VX = Math.Max(-15, Math.Min(15, c.VX));
            c.VY = Math.Max(-18, Math.Min(20, c.VY));
        }

        void UpdateBall()
        {
            ballVY += Gravity * 0.5;
            ballX += ballVX;
            ballY += ballVY;
            ballVX *= BallFriction;

            if (ballY + ballSize >= GroundY)
            {
                ballY = GroundY - ballSize;
                ballVY = -ballVY * BallBounce;
                ballVX *= 0.95;
                if (Math.Abs(ballVY) < 1) ballVY = 0;
            }

            if (ballX < 0)
            {
                if (ballY + ballSize < 370 || ballY > 520)
                { ballX = 0; ballVX = -ballVX * BallBounce; }
            }
            if (ballX + ballSize > FieldWidth)
            {
                if (ballY + ballSize < 370 || ballY > 520)
                { ballX = FieldWidth - ballSize; ballVX = -ballVX * BallBounce; }
            }

            if (ballY < 0) { ballY = 0; ballVY = -ballVY * BallBounce; }
        }

        void CheckCrossbarCollision()
        {
            CheckBallRectCollision(0, 365, 26, 8);
            CheckBallRectCollision(974, 365, 26, 8);
        }

        void CheckBallRectCollision(double rx, double ry, double rw, double rh)
        {
            double bcx = ballX + ballSize / 2;
            double bcy = ballY + ballSize / 2;
            double closestX = Math.Max(rx, Math.Min(bcx, rx + rw));
            double closestY = Math.Max(ry, Math.Min(bcy, ry + rh));
            double dx = bcx - closestX;
            double dy = bcy - closestY;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < ballSize / 2 && dist > 0)
            {
                double nx = dx / dist;
                double ny = dy / dist;
                double overlap = ballSize / 2 - dist;
                ballX += nx * overlap;
                ballY += ny * overlap;
                double dot = ballVX * nx + ballVY * ny;
                ballVX = (ballVX - 2 * dot * nx) * BallBounce;
                ballVY = (ballVY - 2 * dot * ny) * BallBounce;
            }
        }

        void CheckCarBallCollision(Car c)
        {
            double bcx = ballX + ballSize / 2;
            double bcy = ballY + ballSize / 2;
            double closestX = Math.Max(c.X, Math.Min(bcx, c.X + c.Width));
            double closestY = Math.Max(c.Y, Math.Min(bcy, c.Y + c.Height));
            double dx = bcx - closestX;
            double dy = bcy - closestY;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            if (dist < ballSize / 2)
            {
                if (dist == 0) { dx = 1; dy = 0; dist = 1; }
                double nx = dx / dist;
                double ny = dy / dist;
                double overlap = ballSize / 2 - dist;
                ballX += nx * overlap;
                ballY += ny * overlap;

                double impact = 8 + Math.Sqrt(c.VX * c.VX + c.VY * c.VY) * 0.6;
                ballVX = nx * impact + c.VX * 0.5;
                ballVY = ny * impact + c.VY * 0.5 - 2;
            }
        }

        void CheckCarCarCollision()
        {
            if (p1.X < p2.X + p2.Width && p1.X + p1.Width > p2.X &&
                p1.Y < p2.Y + p2.Height && p1.Y + p1.Height > p2.Y)
            {
                double tmpVX = p1.VX;
                p1.VX = p2.VX * 0.8;
                p2.VX = tmpVX * 0.8;
                if (p1.X < p2.X) { p1.X -= 2; p2.X += 2; }
                else { p1.X += 2; p2.X -= 2; }
            }
        }

        void CheckGoal()
        {
            if (ballX <= 20 && ballY > 373 && ballY < 520)
            { score2++; ShowGoal("GOAL P2!"); }
            else if (ballX + ballSize >= 980 && ballY > 373 && ballY < 520)
            { score1++; ShowGoal("GOAL P1!"); }
        }

        void ShowGoal(string msg)
        {
            GoalText.Text = msg;
            GoalText.Visibility = Visibility.Visible;
            goalPaused = true;
            goalTimer = 90;
        }

        void Render()
        {
            Canvas.SetLeft(p1.Shape, p1.X);
            Canvas.SetTop(p1.Shape, p1.Y);
            Canvas.SetLeft(p2.Shape, p2.X);
            Canvas.SetTop(p2.Shape, p2.Y);
            Canvas.SetLeft(Ball, ballX);
            Canvas.SetTop(Ball, ballY);

            // FLIP horizontal selon la direction
            ApplyFlip(p1);
            ApplyFlip(p2);

            ScoreText.Text = $"{score1} - {score2}";
            Boost1Bar.Width = 150 * (p1.Boost / 100.0);
            Boost2Bar.Width = 150 * (p2.Boost / 100.0);
        }

        void ApplyFlip(Car c)
        {
            // On applique un ScaleTransform centré sur la largeur de la voiture
            var scale = new ScaleTransform(c.FacingDir, 1, c.Width / 2, c.Height / 2);
            c.Shape.RenderTransform = scale;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            keys.Add(e.Key);
            if (e.Key == Key.R && gameRunning) { ResetPositions(); score1 = 0; score2 = 0; }
            if (e.Key == Key.Escape && gameRunning) BackToMenu();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e) => keys.Remove(e.Key);
    }
}
