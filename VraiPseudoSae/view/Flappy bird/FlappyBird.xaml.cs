using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace VraiPseudoSae.view.Flappy_bird
{
    public partial class FlappyBird : Window
    {
        private DispatcherTimer gameTimer = new DispatcherTimer();
        private double gravity = 0.6;
        private double velocity = 0;
        private int score = 0;
        private List<Rectangle> pipes = new List<Rectangle>();
        private Random rand = new Random();
        private bool gameOver = true;
        private int pipeSpeed = 5;

        public FlappyBird()
        {
            InitializeComponent();
            gameTimer.Tick += MainEventTimer;
            gameTimer.Interval = TimeSpan.FromMilliseconds(20);
            GameCanvas.Focus();
        }

        private void StartGame()
        {
            score = 0;
            gameOver = false;
            velocity = 0;
            pipeSpeed = 5;
            Canvas.SetTop(Bird, 250);
            ScoreText.Text = "Score: 0";

            foreach (var pipe in pipes) GameCanvas.Children.Remove(pipe);
            pipes.Clear();

            SpawnPipes(500);
            SpawnPipes(800);

            gameTimer.Start();
        }

        private void MainEventTimer(object sender, EventArgs e)
        {
            velocity += gravity;
            Canvas.SetTop(Bird, Canvas.GetTop(Bird) + velocity);
            BirdRotation.Angle = velocity * 4; // Rotation fluide

            // Hitbox réduite pour être plus juste
            Rect birdHitBox = new Rect(Canvas.GetLeft(Bird) + 5, Canvas.GetTop(Bird) + 5, Bird.Width - 10, Bird.Height - 10);

            if (Canvas.GetTop(Bird) < -20 || Canvas.GetTop(Bird) + Bird.Height > 600)
                EndGame();

            List<Rectangle> pipesToRemove = new List<Rectangle>();

            foreach (var pipe in pipes)
            {
                Canvas.SetLeft(pipe, Canvas.GetLeft(pipe) - pipeSpeed);

                if (Canvas.GetLeft(pipe) < -60)
                {
                    pipesToRemove.Add(pipe);
                    if ((string)pipe.Tag == "Top")
                    {
                        score++;
                        ScoreText.Text = "Score: " + score;
                        if (score % 5 == 0) pipeSpeed++; // Accélération
                    }
                }

                Rect pipeHitBox = new Rect(Canvas.GetLeft(pipe), Canvas.GetTop(pipe), pipe.Width, pipe.Height);
                if (birdHitBox.IntersectsWith(pipeHitBox)) EndGame();
            }

            foreach (var pipe in pipesToRemove)
            {
                pipes.Remove(pipe);
                GameCanvas.Children.Remove(pipe);
            }

            if (pipesToRemove.Count > 0 && (string)pipesToRemove[0].Tag == "Top")
            {
                SpawnPipes(500);
            }
        }

        private void SpawnPipes(double x)
        {
            int gap = 140;
            int topHeight = rand.Next(50, 350);

            Rectangle topPipe = new Rectangle { Width = 60, Height = topHeight, Fill = Brushes.LawnGreen, Stroke = Brushes.DarkGreen, StrokeThickness = 3, Tag = "Top" };
            Canvas.SetLeft(topPipe, x);
            Canvas.SetTop(topPipe, 0);

            Rectangle bottomPipe = new Rectangle { Width = 60, Height = 600 - topHeight - gap, Fill = Brushes.LawnGreen, Stroke = Brushes.DarkGreen, StrokeThickness = 3, Tag = "Bottom" };
            Canvas.SetLeft(bottomPipe, x);
            Canvas.SetTop(bottomPipe, topHeight + gap);

            pipes.Add(topPipe);
            pipes.Add(bottomPipe);
            GameCanvas.Children.Add(topPipe);
            GameCanvas.Children.Add(bottomPipe);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (gameOver) StartGame();
                else velocity = -8;
            }
        }

        private void EndGame()
        {
            gameTimer.Stop();
            gameOver = true;
            ScoreText.Text = $"Game Over! Score: {score}\nESPACE pour rejouer";
        }
    }
}