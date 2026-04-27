using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Controls;
using VraiPseudoSae.view.RLS_Pages;
using VraiPseudoSae.data.PakManager;

namespace VraiPseudoSae.view
{
    public partial class HomePage : Window
    {
        private readonly DispatcherTimer gameTimer = new DispatcherTimer();

        private bool goUp, goDown, goLeft, goRight;
        private double playerX = 610;
        private double playerY = 520;

        private const double BaseSpeed = 4.0;

        public HomePage()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            gameTimer.Tick += GameLoop;
            gameTimer.Start();
            Focus();
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            double speed = BaseSpeed;

            if (goUp) playerY -= speed;
            if (goDown) playerY += speed;
            if (goLeft) playerX -= speed;
            if (goRight) playerX += speed;

            playerX = Math.Max(0, Math.Min(1220, playerX));
            playerY = Math.Max(250, Math.Min(610, playerY));

            Canvas.SetLeft(Player, playerX);
            Canvas.SetTop(Player, playerY);

            ApplyFake3DScale();
            UpdateInteractionText();
        }

        private void ApplyFake3DScale()
        {
            double minScale = 0.72;
            double maxScale = 1.18;

            double t = (playerY - 250) / (610 - 250);
            double scale = minScale + (maxScale - minScale) * t;

            Player.RenderTransformOrigin = new Point(0.5, 1);
            Player.RenderTransform = new System.Windows.Media.ScaleTransform(scale, scale);

            Panel.SetZIndex(Player, (int)playerY);
            Panel.SetZIndex(FootballZone, (int)Canvas.GetTop(FootballZone));
            Panel.SetZIndex(MazeZone, (int)Canvas.GetTop(MazeZone));
        }

        private void UpdateInteractionText()
        {
            Point playerCenter = new Point(playerX + 30, playerY + 45);
            Point footballCenter = new Point(Canvas.GetLeft(FootballZone) + 85, Canvas.GetTop(FootballZone) + 30);
            Point mazeCenter = new Point(Canvas.GetLeft(MazeZone) + 85, Canvas.GetTop(MazeZone) + 30);
            Point rlsCenter = new Point(Canvas.GetLeft(RLSZone) + 85, Canvas.GetTop(RLSZone) + 30);

            double footballDist = Distance(playerCenter, footballCenter);
            double mazeDist = Distance(playerCenter, mazeCenter);
            double rlsDist = Distance(playerCenter, rlsCenter);

            if (footballDist < 90)
                InfoText.Text = "Appuie sur E pour lancer le mini-jeu FOOT";
            else if (mazeDist < 90)
                InfoText.Text = "Appuie sur E pour lancer le mini-jeu LABYRINTHE";
            else if (rlsDist < 90)
                InfoText.Text = "Appuie sur E pour lancer le mini-jeu RLS";
            else
                InfoText.Text = "Va vers une zone";
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z || e.Key == Key.W || e.Key == Key.Up) goUp = true;
            if (e.Key == Key.S || e.Key == Key.Down) goDown = true;
            if (e.Key == Key.Q || e.Key == Key.A || e.Key == Key.Left) goLeft = true;
            if (e.Key == Key.D || e.Key == Key.Right) goRight = true;

            if (e.Key == Key.E)
            {
                TryLaunchMiniGame();
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z || e.Key == Key.W || e.Key == Key.Up) goUp = false;
            if (e.Key == Key.S || e.Key == Key.Down) goDown = false;
            if (e.Key == Key.Q || e.Key == Key.A || e.Key == Key.Left) goLeft = false;
            if (e.Key == Key.D || e.Key == Key.Right) goRight = false;
        }

        private void TryLaunchMiniGame()
        {
            Point playerCenter = new Point(playerX + 30, playerY + 45);
            Point footballCenter = new Point(Canvas.GetLeft(FootballZone) + 85, Canvas.GetTop(FootballZone) + 30);
            Point mazeCenter = new Point(Canvas.GetLeft(MazeZone) + 85, Canvas.GetTop(MazeZone) + 30);
            Point rlsCenter = new Point(Canvas.GetLeft(RLSZone) + 85, Canvas.GetTop(RLSZone) + 30);

            if (Distance(playerCenter, footballCenter) < 90)
            {
                LaunchFootballGame();
                return;
            }

            if (Distance(playerCenter, mazeCenter) < 90)
            {
                LaunchMazeGame();
                return;
            }
            
            if (Distance(playerCenter, rlsCenter) < 90)
            {
                LaunchRls();
                return;
            }
        }

        private void LaunchFootballGame()
        {
            FootGame display = new FootGame();
            display.Show();
        }

        private void LaunchMazeGame()
        {
            Maze display = new Maze();
            display.Show();
        }

        private void LaunchRls()
        {
            RLS display = new RLS();
            display.Show();
        }

        private double Distance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
