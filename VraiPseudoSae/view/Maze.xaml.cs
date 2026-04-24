using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Threading;

namespace VraiPseudoSae.view
{
    public partial class Maze : Window
    {
        private const int CellSize = 25;
        private const int Rows = 21;
        private const int Cols = 31;
        private const int LightRadius = 4;

        private readonly Random random = new Random();
        private readonly DispatcherTimer moveTimer = new DispatcherTimer();

        private int[,] grid = new int[Rows, Cols];
        private int playerX = 1;
        private int playerY = 1;
        private (int x, int y) exitCell = (Cols - 2, Rows - 2);

        private double pourcentage = 0.15;

        private bool moveUp;
        private bool moveDown;
        private bool moveLeft;
        private bool moveRight;

        public Maze()
        {
            InitializeComponent();

            moveTimer.Interval = TimeSpan.FromMilliseconds(100);
            moveTimer.Tick += MoveTimer_Tick;
            moveTimer.Start();

            InitializeMaze();
            DrawScene();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MazeCanvas.Focus();
            Keyboard.Focus(MazeCanvas);
        }

        private void InitializeMaze()
        {
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    grid[y, x] = 1;
                }
            }

            GenerateMaze(1, 1);

            playerX = 1;
            playerY = 1;

            exitCell = FindFarthestOpenCellFrom(playerX, playerY);
            grid[exitCell.y, exitCell.x] = 0;
        }

        private void GenerateMaze(int x, int y)
        {
            grid[y, x] = 0;

            var directions = new List<(int dx, int dy)>
            {
                (0, -1),
                (0, 1),
                (-1, 0),
                (1, 0)
            };

            directions = directions.OrderBy(_ => random.Next()).ToList();

            foreach (var (dx, dy) in directions)
            {
                int nx = x + 2 * dx;
                int ny = y + 2 * dy;

                if (nx > 0 && nx < Cols - 1 && ny > 0 && ny < Rows - 1)
                {
                    if (random.NextDouble() < pourcentage)
                    {
                        grid[y + dy, x + dx] = 0;
                        if (grid[ny, nx] == 1)
                            grid[ny, nx] = 0;
                    }
                }
            }

            foreach (var (dx, dy) in directions)
            {
                int nx = x + 2 * dx;
                int ny = y + 2 * dy;

                if (nx > 0 && nx < Cols - 1 && ny > 0 && ny < Rows - 1 && grid[ny, nx] == 1)
                {
                    grid[y + dy, x + dx] = 0;
                    GenerateMaze(nx, ny);
                }
            }
        }

        private bool[,] ComputeLightMap(int px, int py)
        {
            bool[,] lightMap = new bool[Rows, Cols];
            Queue<(int x, int y)> queue = new Queue<(int x, int y)>();

            queue.Enqueue((px, py));
            lightMap[py, px] = true;

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();

                foreach (var (dx, dy) in new (int, int)[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < Cols && ny >= 0 && ny < Rows)
                    {
                        if (!lightMap[ny, nx] && grid[ny, nx] == 0)
                        {
                            int dist2 = (nx - px) * (nx - px) + (ny - py) * (ny - py);
                            if (dist2 <= LightRadius * LightRadius)
                            {
                                lightMap[ny, nx] = true;
                                queue.Enqueue((nx, ny));
                            }
                        }
                    }
                }
            }

            lightMap[exitCell.y, exitCell.x] = true;
            return lightMap;
        }

        private void DrawScene()
        {
            bool[,] lightMap = ComputeLightMap(playerX, playerY);

            MazeCanvas.Children.Clear();

            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    if (!lightMap[y, x])
                        continue;

                    Rectangle rect = new Rectangle
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Stroke = Brushes.DimGray,
                        StrokeThickness = 1
                    };

                    if ((x, y) == exitCell)
                    {
                        rect.Fill = Brushes.LimeGreen;
                    }
                    else if (grid[y, x] == 1)
                    {
                        rect.Fill = Brushes.WhiteSmoke;
                    }
                    else
                    {
                        rect.Fill = Brushes.MediumPurple;
                    }

                    Canvas.SetLeft(rect, x * CellSize);
                    Canvas.SetTop(rect, y * CellSize);

                    MazeCanvas.Children.Add(rect);
                }
            }

            DrawPlayer();

            InfoText.Text = $"Joueur : ({playerX}, {playerY}) | Sortie : ({exitCell.x}, {exitCell.y}) | R = recommencer";
        }

        private void DrawPlayer()
        {
            Ellipse player = new Ellipse
            {
                Width = CellSize - 8,
                Height = CellSize - 8,
                Fill = Brushes.Red
            };

            Canvas.SetLeft(player, playerX * CellSize + 4);
            Canvas.SetTop(player, playerY * CellSize + 4);

            MazeCanvas.Children.Add(player);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    moveUp = true;
                    break;
                case Key.Down:
                    moveDown = true;
                    break;
                case Key.Left:
                    moveLeft = true;
                    break;
                case Key.Right:
                    moveRight = true;
                    break;
                case Key.R:
                    InitializeMaze();
                    DrawScene();
                    break;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    moveUp = false;
                    break;
                case Key.Down:
                    moveDown = false;
                    break;
                case Key.Left:
                    moveLeft = false;
                    break;
                case Key.Right:
                    moveRight = false;
                    break;
            }
        }

        private void MoveTimer_Tick(object? sender, EventArgs e)
        {
            int newX = playerX;
            int newY = playerY;

            if (moveUp)
                newY--;
            else if (moveDown)
                newY++;
            else if (moveLeft)
                newX--;
            else if (moveRight)
                newX++;

            if (CanMoveTo(newX, newY))
            {
                playerX = newX;
                playerY = newY;
                DrawScene();

                if ((playerX, playerY) == exitCell)
                {
                    moveUp = moveDown = moveLeft = moveRight = false;
                    MessageBox.Show("Bravo, tu as trouvé la sortie !");
                    InitializeMaze();
                    DrawScene();
                }
            }
        }

        private bool CanMoveTo(int x, int y)
        {
            if (x < 0 || x >= Cols || y < 0 || y >= Rows)
                return false;

            return grid[y, x] == 0;
        }

        private (int x, int y) FindFarthestOpenCellFrom(int startX, int startY)
        {
            Queue<(int x, int y)> queue = new Queue<(int x, int y)>();
            bool[,] visited = new bool[Rows, Cols];
            int[,] distance = new int[Rows, Cols];

            queue.Enqueue((startX, startY));
            visited[startY, startX] = true;

            (int x, int y) farthest = (startX, startY);

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();

                if (distance[y, x] > distance[farthest.y, farthest.x])
                    farthest = (x, y);

                foreach (var (dx, dy) in new (int, int)[] { (0, -1), (0, 1), (-1, 0), (1, 0) })
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < Cols && ny >= 0 && ny < Rows)
                    {
                        if (!visited[ny, nx] && grid[ny, nx] == 0)
                        {
                            visited[ny, nx] = true;
                            distance[ny, nx] = distance[y, x] + 1;
                            queue.Enqueue((nx, ny));
                        }
                    }
                }
            }

            return farthest;
        }
    }
} 