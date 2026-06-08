using System;
using System.Collections.Generic;
using System.IO;
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
        private double gravity = 0.48;
        private double velocity = 0;
        private int score = 0;
        private int highScore = 0;
        private List<Rectangle> pipes = new List<Rectangle>();
        private Random rand = new Random();
        private bool isPlaying = false;
        private bool isGameOver = false;
        private double pipeSpeed = 4.5;

        private string savePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "flappy_highscore.txt");

        public FlappyBird()
        {
            InitializeComponent();
            gameTimer.Tick += GameLoop;
            gameTimer.Interval = TimeSpan.FromMilliseconds(16);
            LoadHighScore();
            GameCanvas.Focus();
        }

        private void LoadHighScore()
        {
            try
            {
                if (System.IO.File.Exists(savePath))
                {
                    string content = File.ReadAllText(savePath);
                    if (int.TryParse(content, out int savedScore))
                    {
                        highScore = savedScore;
                    }
                }
            }
            catch { }
            BestScoreText.Text = "BEST: " + highScore;
        }

        private void SaveHighScore()
        {
            try
            {
                System.IO.File.WriteAllText(savePath, highScore.ToString());
            }
            catch { }
        }

        private void StartGame()
        {
            score = 0;
            pipeSpeed = 4.5;
            velocity = -7;
            isPlaying = true;
            isGameOver = false;

            ScoreText.Text = "SCORE: 0";
            MessageText.Visibility = Visibility.Collapsed;
            Canvas.SetTop(Bird, 220);
            BirdRotation.Angle = 0;

            foreach (var pipe in pipes)
            {
                GameCanvas.Children.Remove(pipe);
            }
            pipes.Clear();

            SpawnPipePair(450);
            SpawnPipePair(720);

            gameTimer.Start();
        }

        private void GameLoop(object sender, EventArgs e)
        {
            velocity += gravity;
            Canvas.SetTop(Bird, Canvas.GetTop(Bird) + velocity);

            // Remplacement de Math.Clamp par une méthode compatible toutes versions
            double currentAngle = velocity * 4;
            BirdRotation.Angle = Math.Max(-30, Math.Min(currentAngle, 70));

            Rect birdBox = new Rect(Canvas.GetLeft(Bird) + 4, Canvas.GetTop(Bird) + 4, Bird.Width - 8, Bird.Height - 8);

            if (Canvas.GetTop(Bird) < -40 || Canvas.GetTop(Bird) > GameCanvas.ActualHeight)
            {
                TriggerGameOver();
                return;
            }

            List<Rectangle> toRemove = new List<Rectangle>();

            for (int i = 0; i < pipes.Count; i++)
            {
                Rectangle pipe = pipes[i];
                Canvas.SetLeft(pipe, Canvas.GetLeft(pipe) - pipeSpeed);

                Rect pipeBox = new Rect(Canvas.GetLeft(pipe), Canvas.GetTop(pipe), pipe.Width, pipe.Height);
                if (birdBox.IntersectsWith(pipeBox))
                {
                    TriggerGameOver();
                    return;
                }

                if (Canvas.GetLeft(pipe) < -70)
                {
                    toRemove.Add(pipe);
                    if ((string)pipe.Tag == "Top")
                    {
                        score++;
                        ScoreText.Text = "SCORE: " + score;

                        if (score % 4 == 0 && pipeSpeed < 8)
                        {
                            pipeSpeed += 0.5;
                        }
                    }
                }
            }

            foreach (var pipe in toRemove)
            {
                pipes.Remove(pipe);
                GameCanvas.Children.Remove(pipe);
            }

            if (toRemove.Count > 0 && (string)toRemove[0].Tag == "Top")
            {
                SpawnPipePair(500);
            }
        }

        private void SpawnPipePair(double xLocation)
        {
            double gap = 130;
            double minHeight = 40;
            double maxHeight = GameCanvas.ActualHeight - gap - 60;

            double topHeight = rand.Next((int)minHeight, (int)maxHeight);
            double bottomHeight = GameCanvas.ActualHeight - topHeight - gap;

            Brush pipeBrush = (Brush)FindResource("PipeBrush");

            Rectangle topPipe = new Rectangle
            {
                Width = 65,
                Height = topHeight,
                Fill = pipeBrush,
                Stroke = Brushes.Black,
                StrokeThickness = 2.5,
                Tag = "Top"
            };
            Canvas.SetLeft(topPipe, xLocation);
            Canvas.SetTop(topPipe, 0);

            Rectangle bottomPipe = new Rectangle
            {
                Width = 65,
                Height = bottomHeight,
                Fill = pipeBrush,
                Stroke = Brushes.Black,
                StrokeThickness = 2.5,
                Tag = "Bottom"
            };
            Canvas.SetLeft(bottomPipe, xLocation);
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
                if (isGameOver || !isPlaying)
                {
                    StartGame();
                }
                else
                {
                    velocity = -6.4;
                }
            }
        }

        private void TriggerGameOver()
        {
            gameTimer.Stop();
            isPlaying = false;
            using (gameTimer as IDisposable) { }
            isGameOver = true;

            if (score > highScore)
            {
                highScore = score;
                BestScoreText.Text = "BEST: " + highScore;
                SaveHighScore();
                MessageText.Text = $"NOUVEAU RECORD !\nSCORE : {score}\n\n[ ESPACE ]";
            }
            else
            {
                MessageText.Text = $"GAME OVER\nSCORE : {score}\n\n[ ESPACE ]";
            }

            MessageText.Visibility = Visibility.Visible;
        }
    }
}