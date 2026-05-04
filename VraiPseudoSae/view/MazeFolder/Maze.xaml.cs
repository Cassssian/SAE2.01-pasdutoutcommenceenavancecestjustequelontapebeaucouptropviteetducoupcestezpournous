using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
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
        private readonly DispatcherTimer particleTimer = new DispatcherTimer();

        private int[,] grid = new int[Rows, Cols];
        private (int x, int y) exitCell = (Cols - 2, Rows - 2);

        private double pourcentage = 0.5;

        private bool moveUp;
        private bool moveDown;
        private bool moveLeft;
        private bool moveRight;

        private double Timer;
        private Player player;

        private Particles startParticles;
        private Particles endParticles;

        internal class Player
        {
            public int x, y;
            public int teleportTimer = 0;
            public bool canTeleport = true;

            public Player(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public void DrawPlayer(Canvas mazeCanvas)
            {
                Ellipse player = new Ellipse
                {
                    Width = CellSize - 8,
                    Height = CellSize - 8,
                    Fill = Brushes.Red
                };

                Canvas.SetLeft(player, x * CellSize + 4);
                Canvas.SetTop(player, y * CellSize + 4);

                mazeCanvas.Children.Add(player);
            }

            public void Reset()
            {
                x = 1;
                y = 1;
                teleportTimer = 0;
                canTeleport = true;
            }
        }

        public Maze()
        {
            InitializeComponent();

            moveTimer.Interval = TimeSpan.FromMilliseconds(100);
            moveTimer.Tick += MoveTimer_Tick;
            moveTimer.Start();

            particleTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            particleTimer.Tick += ParticleTimer_Tick;
            particleTimer.Start();

            Timer = HoldTimer(pourcentage);

            player = new Player(1, 1);

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
                for (int x = 0; x < Cols; x++)
                    grid[y, x] = 1;

            GenerateMaze(1, 1);

            exitCell = FindFarthestOpenCellFrom(player.x, player.y);
            grid[exitCell.y, exitCell.x] = 0;
        }

        private void GenerateMaze(int x, int y)
        {
            grid[y, x] = 0;

            var directions = new List<(int dx, int dy)>
            {
                (0, -1), (0, 1), (-1, 0), (1, 0)
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
            bool[,] lightMap = ComputeLightMap(player.x, player.y);

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
                        rect.Fill = Brushes.LimeGreen;
                    else if (grid[y, x] == 1)
                        rect.Fill = Brushes.WhiteSmoke;
                    else
                        rect.Fill = Brushes.MediumPurple;

                    Canvas.SetLeft(rect, x * CellSize);
                    Canvas.SetTop(rect, y * CellSize);
                    MazeCanvas.Children.Add(rect);
                }
            }

            player.DrawPlayer(MazeCanvas);

            InfoText.Text = $"Joueur : ({player.x}, {player.y}) | Sortie : ({exitCell.x}, {exitCell.y}) | R = recommencer | Pourcentage {pourcentage}";
        }

        // Timer dédié aux particules (~60 FPS) — indépendant du mouvement
        private void ParticleTimer_Tick(object? sender, EventArgs e)
        {
            if (startParticles is null && endParticles is null)
                return;

            // On redessine la scène pour avoir un fond propre, puis on pose les particules par-dessus
            DrawScene();

            if (startParticles is not null)
            {
                startParticles.Update();
                startParticles.Draw(MazeCanvas);
                if (startParticles.IsFinished)
                    startParticles = null;
            }

            if (endParticles is not null)
            {
                endParticles.Update();
                endParticles.Draw(MazeCanvas);
                if (endParticles.IsFinished)
                    endParticles = null;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:    moveUp = true;    break;
                case Key.Down:  moveDown = true;  break;
                case Key.Left:  moveLeft = true;  break;
                case Key.Right: moveRight = true; break;
                case Key.R:
                    player.Reset();
                    InitializeMaze();
                    DrawScene();
                    break;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:    moveUp = false;    break;
                case Key.Down:  moveDown = false;  break;
                case Key.Left:  moveLeft = false;  break;
                case Key.Right: moveRight = false; break;
                case Key.Space:
                    if (player.canTeleport) Teleport();
                    break;
            }
        }

        private void MoveTimer_Tick(object? sender, EventArgs e)
        {
            // Cooldown de téléportation
            if (player.teleportTimer > 0)
            {
                player.teleportTimer--;
                if (player.teleportTimer <= 0)
                {
                    player.teleportTimer = 0;
                    player.canTeleport = true;
                }
            }

            int newX = player.x;
            int newY = player.y;

            if (moveUp)         newY--;
            else if (moveDown)  newY++;
            else if (moveLeft)  newX--;
            else if (moveRight) newX++;

            bool moved = false;

            if (CanMoveTo(newX, newY))
            {
                player.x = newX;
                player.y = newY;
                moved = true;
            }

            // Ne redessine que si pas de particules actives (sinon ParticleTimer_Tick gère le rendu)
            if (startParticles is null && endParticles is null)
                DrawScene();

            if (moved && (player.x, player.y) == exitCell)
            {
                moveUp = moveDown = moveLeft = moveRight = false;
                MessageBox.Show("Bravo, tu as trouvé la sortie !");
                pourcentage = Math.Max(pourcentage - 0.075, 0.001);
                Timer = HoldTimer(pourcentage);
                player.Reset();
                InitializeMaze();
                DrawScene();
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

        private double HoldTimer(double pourcentageTimer)
        {
            double temp = 30;
            if (pourcentageTimer >= 0.5)      temp *= 50;
            else if (pourcentageTimer >= 0.4) temp *= 45;
            else if (pourcentageTimer >= 0.3) temp *= 60;
            else if (pourcentageTimer >= 0.2) temp *= 75;
            else if (pourcentageTimer >= 0.1) temp *= 80;
            else if (pourcentageTimer == 1)   temp *= 100;
            return temp;
        }

        public static IEnumerable<IEnumerable<T>> Product<T>(
            IEnumerable<IEnumerable<T>> iterables,
            int repeat = 1)
        {
            var pools = Enumerable.Repeat(iterables, repeat)
                .SelectMany(x => x)
                .Select(x => x.ToList())
                .ToList();

            IEnumerable<IEnumerable<T>> result = new List<List<T>> { new List<T>() };

            foreach (var pool in pools)
            {
                result = result.SelectMany(
                    x => pool,
                    (x, y) =>
                    {
                        var newList = new List<T>(x);
                        newList.Add(y);
                        return newList;
                    }
                ).ToList();
            }

            foreach (var prod in result)
                yield return prod;
        }

        private void Teleport()
        {
            if (!player.canTeleport)
                return;

            int currentX = player.x;
            int currentY = player.y;

            var values = new[] { -2, -1, 0, 1, 2 };

            var directions = Product(
                new[] { values.Cast<int>(), values.Cast<int>() }
            ).ToList();

            var validTeleports = new List<(int x, int y)>();

            foreach (var pair in directions)
            {
                var enumerator = pair.GetEnumerator();
                enumerator.MoveNext();
                int dx = enumerator.Current;
                enumerator.MoveNext();
                int dy = enumerator.Current;

                int nextX = currentX + dx;
                int nextY = currentY + dy;

                if (nextX > 0 && nextX < Cols - 1 &&
                    nextY > 0 && nextY < Rows - 1 &&
                    grid[nextY, nextX] == 1)
                {
                    int beyondX = nextX + dx;
                    int beyondY = nextY + dy;

                    if (beyondX > 0 && beyondX < Cols - 1 &&
                        beyondY > 0 && beyondY < Rows - 1 &&
                        grid[beyondY, beyondX] == 0 &&
                        !(nextX == exitCell.x && nextY == exitCell.y) &&
                        !(nextX == player.x && nextY == player.y))
                    {
                        validTeleports.Add((beyondX, beyondY));
                    }
                }
            }

            if (validTeleports.Count == 0)
                return;

            var chosenCoord = validTeleports[random.Next(validTeleports.Count)];
            int new_x = chosenCoord.x;
            int new_y = chosenCoord.y;

            int startPxX = currentX * CellSize + CellSize / 2;
            int startPxY = currentY * CellSize + CellSize / 2;
            int endPxX   = new_x   * CellSize + CellSize / 2;
            int endPxY   = new_y   * CellSize + CellSize / 2;

            startParticles = new Particles(startPxX, startPxY, Colors.CornflowerBlue);
            endParticles   = new Particles(endPxX,   endPxY,   Colors.Teal);

            player.x = new_x;
            player.y = new_y;

            player.canTeleport = false;
            player.teleportTimer = 20;
        }
    }
}
