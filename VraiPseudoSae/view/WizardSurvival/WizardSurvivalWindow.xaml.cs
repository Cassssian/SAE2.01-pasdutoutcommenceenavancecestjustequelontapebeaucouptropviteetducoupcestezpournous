using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using IUTGame.WPF;
using VraiPseudoSae.Utils.Sprite;
using VraiPseudoSae.view.WizardSurvival.Core;
using VraiPseudoSae.view.WizardSurvival.Rendering;

namespace VraiPseudoSae.view.WizardSurvival;

/// <summary>
/// WPF host for the IUTGame-powered wizard survival mini-game.
/// </summary>
public partial class WizardSurvivalWindow : UserControl
{
    private const double CooldownBarWidth = 206;

    private WizardSurvivalGame? game;
    private WizardEffectsRenderer? effectsRenderer;
    public event EventHandler? ExitRequested;

    public WizardSurvivalWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var screen = new WPFScreen(SpriteCanvas);
        WizardPlayerSpriteSet wizardSprites = InjectSprites(screen);

        game = new WizardSurvivalGame(screen, "Resources/Sprites", "Resources/Sounds", wizardSprites)
        {
            StateChanged = UpdateState,
            HudChanged = UpdateHud,
            CameraChanged = UpdateCamera,
            MapChanged = UpdateMap
        };

        DrawArenaBackground(game.Map);
        effectsRenderer = new WizardEffectsRenderer(EffectLayer);

        game.Run();
        FocusGame();
    }

    private void Window_Unloaded(object sender, RoutedEventArgs e) => game?.Pause();

    private WizardPlayerSpriteSet InjectSprites(WPFScreen screen)
    {
        WizardPlayerSpriteSet wizardSprites = WizardSpriteSheetFactory.Register(screen);
        SpriteInjector.PreRegister(screen, "wizard_zombie_right.png", PixelSpriteFactory.Zombie(evolved: false));
        SpriteInjector.PreRegister(screen, "wizard_zombie_left.png", PixelSpriteFactory.Zombie(evolved: false, mirrored: true));
        SpriteInjector.PreRegister(screen, "wizard_zombie_evolved_right.png", PixelSpriteFactory.Zombie(evolved: true));
        SpriteInjector.PreRegister(screen, "wizard_zombie_evolved_left.png", PixelSpriteFactory.Zombie(evolved: true, mirrored: true));
        SpriteInjector.PreRegister(screen, "wizard_fireball_right.png", PixelSpriteFactory.Fireball());
        SpriteInjector.PreRegister(screen, "wizard_fireball_left.png", PixelSpriteFactory.Fireball(mirrored: true));
        return wizardSprites;
    }

    private void DrawArenaBackground(ICollisionMap map)
    {
        WorldLayer.Children.Clear();

        Rectangle floor = new()
        {
            Width = map.Width,
            Height = map.Height,
            Fill = new SolidColorBrush(Color.FromRgb(43, 23, 66))
        };
        RenderOptions.SetEdgeMode(floor, EdgeMode.Aliased);
        WorldLayer.Children.Add(floor);

        for (int y = 0; y < map.Height; y += 32)
        {
            for (int x = 0; x < map.Width; x += 32)
            {
                if (((x + y) / 32) % 2 == 0)
                    continue;

                Rectangle tile = new()
                {
                    Width = 32,
                    Height = 32,
                    Fill = new SolidColorBrush(Color.FromRgb(49, 31, 76)),
                    Opacity = 0.55
                };
                Canvas.SetLeft(tile, x);
                Canvas.SetTop(tile, y);
                WorldLayer.Children.Add(tile);
            }
        }

        for (int x = 0; x < map.Width; x += 64)
        {
            Rectangle line = new()
            {
                Width = 2,
                Height = map.Height,
                Fill = new SolidColorBrush(Color.FromArgb(45, 31, 52, 67))
            };
            Canvas.SetLeft(line, x);
            WorldLayer.Children.Add(line);
        }

        foreach (ArenaObstacle obstacle in map.Obstacles)
            DrawObstacle(obstacle);
    }

    private void DrawObstacle(ArenaObstacle obstacle)
    {
        switch (obstacle.Kind)
        {
            case "manor":
                DrawManor(obstacle.Bounds, tall: true);
                break;
            case "manor_wing":
                DrawManor(obstacle.Bounds, tall: false);
                break;
            case "broken_house":
                DrawBrokenHouse(obstacle.Bounds);
                break;
            case "collapsed_roof":
                DrawCollapsedRoof(obstacle.Bounds);
                break;
            case "cracked_house":
                DrawCrackedHouse(obstacle.Bounds);
                break;
            case "rotten_garden":
                DrawRottenGarden(obstacle.Bounds);
                break;
            case "dead_tree":
                DrawDeadTree(obstacle.Bounds);
                break;
            case "well":
                DrawWell(obstacle.Bounds);
                break;
            case "ruin":
                DrawRuin(obstacle.Bounds);
                break;
            default:
                DrawStonePile(obstacle.Bounds);
                break;
        }
    }

    private void DrawManor(Rect bounds, bool tall)
    {
        double roofY = bounds.Top - (tall ? 28 : 20);
        AddBlock(bounds.Left + 8, bounds.Top + 8, bounds.Width - 16, bounds.Height - 8, Rgb(42, 52, 72), Rgb(15, 24, 34), 4);
        AddJaggedRoof(bounds.Left - 6, roofY, bounds.Width + 12, tall ? 42 : 32, Rgb(76, 42, 62), brokenSlots: tall ? 5 : 3);

        AddBlock(bounds.Left + 18, bounds.Top + 20, 24, 24, Rgb(229, 236, 190), Rgb(20, 30, 40), 3);
        AddBlock(bounds.Left + 62, bounds.Top + 18, 24, 24, Rgb(98, 154, 162), Rgb(20, 30, 40), 3);
        AddBlock(bounds.Left + bounds.Width - 46, bounds.Top + 18, 24, 24, Rgb(229, 236, 190), Rgb(20, 30, 40), 3);
        AddBrokenWindow(bounds.Left + 20, bounds.Top + 52, 26, 22);
        AddBlock(bounds.Left + bounds.Width / 2 - 16, bounds.Top + bounds.Height - 36, 32, 32, Rgb(37, 25, 40), Rgb(12, 18, 28), 3);

        AddCrack(bounds.Left + bounds.Width - 58, bounds.Top + 38, new[] { (0, 0), (-8, 8), (2, 16), (-10, 28), (-4, 38) });
        AddCrack(bounds.Left + 70, bounds.Top + 48, new[] { (0, 0), (9, 7), (3, 17), (13, 28) });
        AddDebris(bounds.Left + bounds.Width - 48, bounds.Bottom - 12, 7);
    }

    private void DrawBrokenHouse(Rect bounds)
    {
        AddBlock(bounds.Left + 10, bounds.Top + 18, bounds.Width - 20, bounds.Height - 18, Rgb(53, 70, 78), Rgb(17, 28, 35), 4);
        AddJaggedRoof(bounds.Left + 2, bounds.Top - 12, bounds.Width - 4, 38, Rgb(91, 54, 55), brokenSlots: 4);
        AddBlock(bounds.Left + 18, bounds.Top + 34, 22, 22, Rgb(229, 236, 190), Rgb(20, 30, 40), 3);
        AddBrokenWindow(bounds.Left + bounds.Width - 44, bounds.Top + 32, 24, 24);
        AddBlock(bounds.Left + 58, bounds.Top + bounds.Height - 30, 26, 30, Rgb(38, 26, 35), Rgb(15, 22, 30), 3);
        AddCrack(bounds.Left + 88, bounds.Top + 28, new[] { (0, 0), (-7, 8), (-1, 16), (-12, 25), (-8, 36) });
        AddDebris(bounds.Left + 16, bounds.Bottom - 10, 8);
    }

    private void DrawCollapsedRoof(Rect bounds)
    {
        AddBlock(bounds.Left + 8, bounds.Top + 20, bounds.Width - 16, bounds.Height - 20, Rgb(55, 62, 75), Rgb(15, 23, 32), 4);
        AddBlock(bounds.Left + 4, bounds.Top + 8, bounds.Width * 0.45, 18, Rgb(92, 48, 58), Rgb(25, 20, 28), 3);
        AddBlock(bounds.Left + bounds.Width * 0.56, bounds.Top + 2, bounds.Width * 0.36, 16, Rgb(92, 48, 58), Rgb(25, 20, 28), 3);
        AddBlock(bounds.Left + bounds.Width * 0.44, bounds.Top + 4, 18, 36, Rgb(25, 20, 28));
        AddBlock(bounds.Left + 24, bounds.Top + 38, 24, 22, Rgb(98, 154, 162), Rgb(20, 30, 40), 3);
        AddBrokenWindow(bounds.Left + bounds.Width - 58, bounds.Top + 34, 28, 24);
        AddCrack(bounds.Left + bounds.Width / 2 + 8, bounds.Top + 42, new[] { (0, 0), (8, 9), (1, 17), (11, 29), (5, 38) });
        AddDebris(bounds.Left + bounds.Width / 2 - 18, bounds.Bottom - 8, 10);
    }

    private void DrawCrackedHouse(Rect bounds)
    {
        AddBlock(bounds.Left + 8, bounds.Top + 14, bounds.Width - 16, bounds.Height - 14, Rgb(62, 72, 76), Rgb(15, 24, 34), 4);
        AddJaggedRoof(bounds.Left, bounds.Top - 18, bounds.Width, 36, Rgb(71, 39, 66), brokenSlots: 4);
        AddBrokenWindow(bounds.Left + 20, bounds.Top + 30, 24, 24);
        AddBlock(bounds.Left + 70, bounds.Top + 28, 24, 24, Rgb(229, 236, 190), Rgb(20, 30, 40), 3);
        AddBlock(bounds.Right - 42, bounds.Top + 42, 22, 30, Rgb(37, 25, 40), Rgb(12, 18, 28), 3);
        AddCrack(bounds.Left + 52, bounds.Top + 18, new[] { (0, 0), (-10, 8), (-2, 15), (-14, 28), (-7, 42), (-17, 55) });
        AddCrack(bounds.Right - 50, bounds.Top + 22, new[] { (0, 0), (8, 10), (1, 18), (12, 30) });
        AddDebris(bounds.Left + 35, bounds.Bottom - 10, 6);
    }

    private void DrawRuin(Rect bounds)
    {
        AddBlock(bounds.Left + 8, bounds.Top + 26, bounds.Width - 16, bounds.Height - 26, Rgb(87, 103, 109), Rgb(16, 25, 31), 4);
        AddBlock(bounds.Left + 20, bounds.Top + 10, 34, 28, Rgb(87, 103, 109), Rgb(16, 25, 31), 4);
        AddBlock(bounds.Right - 58, bounds.Top + 2, 38, 36, Rgb(87, 103, 109), Rgb(16, 25, 31), 4);
        AddBrokenWindow(bounds.Left + 26, bounds.Top + 40, 26, 22);
        AddBrokenWindow(bounds.Right - 60, bounds.Top + 38, 26, 22);
        AddCrack(bounds.Left + 72, bounds.Top + 34, new[] { (0, 0), (8, 10), (1, 20), (12, 32) });
        AddDebris(bounds.Left + 8, bounds.Bottom - 8, 10);
    }

    private void DrawRottenGarden(Rect bounds)
    {
        AddBlock(bounds.Left, bounds.Top, bounds.Width, bounds.Height, Rgb(44, 61, 45), Rgb(18, 28, 22), 3);
        for (int x = 12; x < bounds.Width - 12; x += 22)
        {
            AddBlock(bounds.Left + x, bounds.Top + 8, 5, bounds.Height - 16, Rgb(76, 55, 42));
            AddBlock(bounds.Left + x + 7, bounds.Top + 16, 7, 5, Rgb(64, 93, 58));
            AddBlock(bounds.Left + x - 4, bounds.Top + 36, 8, 4, Rgb(83, 64, 45));
        }

        for (int x = 0; x < bounds.Width; x += 18)
        {
            AddBlock(bounds.Left + x, bounds.Top - 4, 10, 12, Rgb(86, 62, 45), Rgb(20, 24, 22), 2);
            AddBlock(bounds.Left + x, bounds.Bottom - 8, 10, 12, Rgb(86, 62, 45), Rgb(20, 24, 22), 2);
        }

        AddBlock(bounds.Left + 12, bounds.Top + bounds.Height - 20, bounds.Width - 24, 5, Rgb(63, 44, 34));
        AddBlock(bounds.Left + bounds.Width * 0.62, bounds.Top + 18, 18, 10, Rgb(32, 38, 30));
        AddBlock(bounds.Left + bounds.Width * 0.66, bounds.Top + 10, 5, 28, Rgb(92, 65, 46));
        AddDebris(bounds.Right - 46, bounds.Top + 14, 8);
    }

    private void DrawDeadTree(Rect bounds)
    {
        AddBlock(bounds.Left + bounds.Width / 2 - 10, bounds.Top + 28, 20, bounds.Height - 26, Rgb(72, 49, 43), Rgb(23, 20, 24), 3);
        AddBlock(bounds.Left + bounds.Width / 2 - 28, bounds.Top + 54, 56, 12, Rgb(72, 49, 43), Rgb(23, 20, 24), 3);
        AddBlock(bounds.Left + bounds.Width / 2 + 12, bounds.Top + 26, 46, 10, Rgb(72, 49, 43), Rgb(23, 20, 24), 3);
        AddBlock(bounds.Left + bounds.Width / 2 - 54, bounds.Top + 34, 44, 10, Rgb(72, 49, 43), Rgb(23, 20, 24), 3);
        AddBlock(bounds.Left + bounds.Width / 2 - 4, bounds.Top + 8, 12, 38, Rgb(72, 49, 43), Rgb(23, 20, 24), 3);
        AddBlock(bounds.Left + bounds.Width / 2 - 14, bounds.Top + 18, 8, 8, Rgb(151, 217, 215));
        AddDebris(bounds.Left + 16, bounds.Bottom - 10, 7);
    }

    private void DrawWell(Rect bounds)
    {
        AddBlock(bounds.Left + 14, bounds.Top + 24, bounds.Width - 28, bounds.Height - 24, Rgb(71, 80, 86), Rgb(18, 27, 33), 4);
        AddBlock(bounds.Left + 22, bounds.Top + 34, bounds.Width - 44, 20, Rgb(20, 25, 34), Rgb(8, 12, 18), 3);
        AddBlock(bounds.Left + 8, bounds.Top + 10, 10, bounds.Height - 12, Rgb(70, 49, 43), Rgb(21, 18, 20), 2);
        AddBlock(bounds.Right - 18, bounds.Top + 10, 10, bounds.Height - 12, Rgb(70, 49, 43), Rgb(21, 18, 20), 2);
        AddJaggedRoof(bounds.Left + 8, bounds.Top - 6, bounds.Width - 16, 30, Rgb(78, 42, 58), brokenSlots: 2);
        AddBlock(bounds.Left + 38, bounds.Top + 4, 18, 8, Rgb(37, 25, 40));
    }

    private void DrawStonePile(Rect bounds)
    {
        AddBlock(bounds.Left, bounds.Top + 14, bounds.Width, bounds.Height - 14, Rgb(67, 79, 86), Rgb(18, 27, 33), 4);
        AddBlock(bounds.Left + 14, bounds.Top + 4, bounds.Width * 0.42, 24, Rgb(82, 94, 102), Rgb(18, 27, 33), 4);
        AddBlock(bounds.Right - 54, bounds.Top + 2, 42, 28, Rgb(82, 94, 102), Rgb(18, 27, 33), 4);
        AddCrack(bounds.Left + bounds.Width * 0.55, bounds.Top + 20, new[] { (0, 0), (-8, 8), (2, 17), (-8, 28) });
        AddDebris(bounds.Left + 20, bounds.Bottom - 8, 8);
    }

    private void AddJaggedRoof(double x, double y, double width, double height, Brush brush, int brokenSlots)
    {
        Polygon roof = new()
        {
            Fill = brush,
            Stroke = Rgb(18, 22, 30),
            StrokeThickness = 3,
            Points = new PointCollection
            {
                new(x, y + height),
                new(x + width * 0.5, y),
                new(x + width, y + height),
                new(x + width - 10, y + height),
                new(x + width * 0.5, y + 10),
                new(x + 10, y + height)
            }
        };
        RenderOptions.SetEdgeMode(roof, EdgeMode.Aliased);
        WorldLayer.Children.Add(roof);

        for (int i = 0; i < brokenSlots; i++)
        {
            double holeX = x + 18 + i * (width - 36) / Math.Max(1, brokenSlots);
            AddBlock(holeX, y + height - 12 - (i % 2) * 8, 14, 12, Rgb(43, 23, 66));
        }
    }

    private void AddBrokenWindow(double x, double y, double width, double height)
    {
        AddBlock(x, y, width, height, Rgb(21, 28, 38), Rgb(9, 13, 20), 3);
        AddBlock(x + 3, y + 3, width * 0.35, height * 0.35, Rgb(229, 236, 190));
        AddBlock(x + width * 0.55, y + 4, width * 0.28, height * 0.28, Rgb(98, 154, 162));
        AddBlock(x + width * 0.46, y + 2, 4, height - 4, Rgb(13, 18, 28));
        AddBlock(x + 2, y + height * 0.5, width - 4, 4, Rgb(13, 18, 28));
        AddBlock(x + width - 7, y + height - 7, 6, 6, Rgb(43, 23, 66));
    }

    private void AddCrack(double x, double y, IEnumerable<(int X, int Y)> points)
    {
        (int X, int Y)? previous = null;
        foreach ((int X, int Y) point in points)
        {
            if (previous is not null)
                AddPixelLine(x + previous.Value.X, y + previous.Value.Y, x + point.X, y + point.Y, Rgb(15, 20, 28), 4);

            AddBlock(x + point.X - 2, y + point.Y - 2, 4, 4, Rgb(15, 20, 28));
            previous = point;
        }
    }

    private void AddDebris(double x, double y, int count)
    {
        for (int i = 0; i < count; i++)
        {
            double dx = (i * 17) % 64;
            double dy = ((i * 11) % 18) - 9;
            double size = 5 + (i % 3) * 2;
            AddBlock(x + dx, y + dy, size, size, i % 2 == 0 ? Rgb(82, 94, 102) : Rgb(60, 45, 50));
        }
    }

    private void AddPixelLine(double x1, double y1, double x2, double y2, Brush brush, double size)
    {
        double dx = x2 - x1;
        double dy = y2 - y1;
        int steps = Math.Max(1, (int)(Math.Sqrt(dx * dx + dy * dy) / size));
        for (int i = 0; i <= steps; i++)
        {
            double t = i / (double)steps;
            AddBlock(x1 + dx * t, y1 + dy * t, size, size, brush);
        }
    }

    private void AddBlock(double x, double y, double width, double height, Brush fill) =>
        AddBlock(x, y, width, height, fill, null, 0);

    private void AddBlock(double x, double y, double width, double height, Brush fill, Brush? stroke, double strokeThickness)
    {
        Rectangle rect = new()
        {
            Width = Math.Max(0, width),
            Height = Math.Max(0, height),
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = strokeThickness,
            SnapsToDevicePixels = true
        };
        RenderOptions.SetEdgeMode(rect, EdgeMode.Aliased);
        Canvas.SetLeft(rect, Math.Round(x));
        Canvas.SetTop(rect, Math.Round(y));
        WorldLayer.Children.Add(rect);
    }

    private void AddScreenBlock(Canvas layer, double x, double y, double width, double height, Brush fill, double opacity)
    {
        Rectangle rect = new()
        {
            Width = Math.Max(0, width),
            Height = Math.Max(0, height),
            Fill = fill,
            Opacity = opacity,
            SnapsToDevicePixels = true
        };
        RenderOptions.SetEdgeMode(rect, EdgeMode.Aliased);
        Canvas.SetLeft(rect, Math.Round(x));
        Canvas.SetTop(rect, Math.Round(y));
        layer.Children.Add(rect);
    }

    private static SolidColorBrush Rgb(byte r, byte g, byte b) => new(Color.FromRgb(r, g, b));

    private void RenderLakes(ICollisionMap map, Camera2D camera)
    {
        LakeLayer.Children.Clear();
        foreach (ArenaLake lake in map.Lakes)
            DrawLake(lake, camera);
    }

    private void DrawLake(ArenaLake lake, Camera2D camera)
    {
        double left = lake.Bounds.Left - camera.X;
        double top = lake.Bounds.Top - camera.Y;
        double width = lake.Bounds.Width;
        double height = lake.Bounds.Height;

        for (int row = 0; row < height; row += 6)
        {
            double normalizedY = ((row + 3) / height - 0.5) * 2;
            double rowWidth = width * Math.Sqrt(Math.Max(0, 1 - normalizedY * normalizedY));
            double rowLeft = left + (width - rowWidth) / 2;
            AddScreenBlock(LakeLayer, rowLeft, top + row, rowWidth, 6, PickLakeBrush(lake, row), 0.9);
        }

        AddScreenBlock(LakeLayer, left + width * 0.18, top + 2, width * 0.64, 4, Rgb(12, 18, 28), 0.75);
        AddScreenBlock(LakeLayer, left + width * 0.14, top + height - 7, width * 0.68, 5, Rgb(12, 18, 28), 0.65);

        int sparkles = lake.IsReady ? 7 : 11;
        for (int i = 0; i < sparkles; i++)
        {
            double phase = lake.Phase * (lake.IsReady ? 2.5 : 7.5) + i * 1.73;
            if (Math.Sin(phase) <= -0.15)
                continue;

            double px = left + width * (0.18 + ((i * 23) % 61) / 100.0);
            double py = top + height * (0.22 + ((i * 17) % 53) / 100.0);
            AddScreenBlock(LakeLayer, px, py, 4, 4, PickLakeSparkle(lake), 0.9);
        }
    }

    private Brush PickLakeBrush(ArenaLake lake, int stripe)
    {
        double pulse = 0.5 + 0.5 * Math.Sin(lake.Phase * 7 + stripe);
        return lake switch
        {
            BuffLake when lake.IsReady => stripe % 2 == 0 ? Rgb(28, 146, 91) : Rgb(61, 217, 139),
            BuffLake => pulse > 0.55 ? Rgb(37, 167, 146) : Rgb(27, 91, 132),
            NerfLake when lake.IsReady => stripe % 2 == 0 ? Rgb(143, 37, 48) : Rgb(231, 79, 74),
            NerfLake => pulse > 0.55 ? Rgb(177, 50, 76) : Rgb(89, 38, 72),
            _ => stripe % 2 == 0 ? Rgb(30, 84, 139) : Rgb(48, 128, 171)
        };
    }

    private Brush PickLakeSparkle(ArenaLake lake) =>
        lake switch
        {
            BuffLake when lake.IsReady => Rgb(151, 255, 184),
            BuffLake => Rgb(126, 255, 224),
            NerfLake when lake.IsReady => Rgb(255, 177, 128),
            NerfLake => Rgb(255, 104, 128),
            _ => Rgb(160, 230, 255)
        };

    private void UpdateMap(ICollisionMap map)
    {
        Dispatcher.Invoke(() =>
        {
            DrawArenaBackground(map);
            if (game is not null)
                RenderLakes(map, game.Camera);
        });
    }

    private void UpdateState(WizardGameState state)
    {
        Dispatcher.Invoke(() =>
        {
            MenuOverlay.Visibility = state == WizardGameState.Menu ? Visibility.Visible : Visibility.Collapsed;
            PauseOverlay.Visibility = state == WizardGameState.Paused ? Visibility.Visible : Visibility.Collapsed;
            UpgradeOverlay.Visibility = state == WizardGameState.Upgrade ? Visibility.Visible : Visibility.Collapsed;
            GameOverOverlay.Visibility = state == WizardGameState.GameOver ? Visibility.Visible : Visibility.Collapsed;
            HudLayer.Visibility = state == WizardGameState.Menu ? Visibility.Collapsed : Visibility.Visible;
            StateHintText.Text = state switch
            {
                WizardGameState.Playing => "A: ameliorations | ESC: pause",
                WizardGameState.Dying => "Le sorcier s'effondre...",
                WizardGameState.Paused => "Pause",
                WizardGameState.Upgrade => "ESC ou X: retour",
                WizardGameState.GameOver => "R: recommencer",
                _ => "Entree: jouer"
            };
            FocusGame();
        });
    }

    private void UpdateHud(WizardHudSnapshot hud)
    {
        Dispatcher.Invoke(() =>
        {
            ScoreText.Text = $"Score: {hud.Score}";
            LivesText.Text = $"Vie: {hud.Lives}/{hud.MaxLives}";
            KillsText.Text = $"Zombies: {hud.ZombiesKilled}";
            FinalScoreText.Text = $"Score final: {hud.FinalScore}";
            FireballCooldownFill.Width = CooldownBarWidth * Clamp01(hud.FireballProgress);
            CelestialCooldownFill.Width = CooldownBarWidth * Clamp01(hud.CelestialProgress);
            ShieldCooldownFill.Width = CooldownBarWidth * Clamp01(hud.ShieldProgress);
            LaserCooldownFill.Width = CooldownBarWidth * Clamp01(hud.LaserProgress);
            LakeStatusText.Text = hud.LakeStatus switch
            {
                LakeStatusKind.Buffed => $"Lac: BOOST {Math.Ceiling(hud.LakeStatusRemaining)}s",
                LakeStatusKind.Nerfed => $"Lac: MALUS {Math.Ceiling(hud.LakeStatusRemaining)}s",
                _ => "Lac: --"
            };
            LakeStatusText.Foreground = hud.LakeStatus switch
            {
                LakeStatusKind.Buffed => Rgb(101, 255, 164),
                LakeStatusKind.Nerfed => Rgb(255, 112, 112),
                _ => Rgb(199, 211, 230)
            };
        });
    }

    private void UpdateCamera(Camera2D camera)
    {
        Dispatcher.Invoke(() =>
        {
            WorldTransform.X = -camera.X;
            WorldTransform.Y = -camera.Y;
            if (game is not null)
            {
                RenderLakes(game.Map, camera);
                effectsRenderer?.Render(game);
            }
        });
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        game?.StartNewGame();
        FocusGame();
    }

    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        game?.StartNewGame();
        FocusGame();
    }

    private void ResumeButton_Click(object sender, RoutedEventArgs e)
    {
        game?.TogglePause();
        FocusGame();
    }

    private void BackToMenuButton_Click(object sender, RoutedEventArgs e)
    {
        game?.ReturnToMenu();
        FocusGame();
    }

    private void CloseUpgradeButton_Click(object sender, RoutedEventArgs e)
    {
        game?.CloseUpgradePanel();
        FocusGame();
    }

    private void UpgradeButton_Click(object sender, RoutedEventArgs e)
    {
        if (game is null || sender is not Button { Tag: string tag })
            return;

        bool parsed = Enum.TryParse(tag, out UpgradeKind upgrade);
        bool applied = parsed && game.ApplyUpgrade(upgrade);
        UpgradeStatusText.Text = applied
            ? "Amelioration appliquee."
            : "Score insuffisant.";
        FocusGame();
    }

    private void QuitButton_Click(object sender, RoutedEventArgs e)
    {
        game?.Pause();
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void FocusGame()
    {
        SpriteCanvas.Focus();
        Keyboard.Focus(SpriteCanvas);
    }

    private static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));
}
