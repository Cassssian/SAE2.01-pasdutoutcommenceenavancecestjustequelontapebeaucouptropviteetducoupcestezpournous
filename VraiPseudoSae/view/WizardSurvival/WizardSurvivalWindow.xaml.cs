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
public partial class WizardSurvivalWindow : Window
{
    private const double CooldownBarWidth = 120;

    private WizardSurvivalGame? game;
    private WizardEffectsRenderer? effectsRenderer;

    public WizardSurvivalWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var screen = new WPFScreen(SpriteCanvas);
        InjectSprites(screen);

        game = new WizardSurvivalGame(screen, "Resources/Sprites", "Resources/Sounds")
        {
            StateChanged = UpdateState,
            HudChanged = UpdateHud,
            CameraChanged = UpdateCamera
        };

        DrawArenaBackground(game.Map);
        effectsRenderer = new WizardEffectsRenderer(EffectLayer);

        game.Run();
        FocusGame();
    }

    private void Window_Closed(object? sender, EventArgs e) => game?.Pause();

    private void InjectSprites(WPFScreen screen)
    {
        SpriteInjector.PreRegister(screen, "wizard_player.png", XamlSpriteExporter.RenderToBitmapImage(SpritePlayerSource, 34, 44));
        SpriteInjector.PreRegister(screen, "wizard_zombie.png", XamlSpriteExporter.RenderToBitmapImage(SpriteZombieSource, 34, 38));
        SpriteInjector.PreRegister(screen, "wizard_zombie_evolved.png", XamlSpriteExporter.RenderToBitmapImage(SpriteZombieEvolvedSource, 34, 38));
        SpriteInjector.PreRegister(screen, "wizard_fireball.png", XamlSpriteExporter.RenderToBitmapImage(SpriteFireballSource, 18, 18));
    }

    private void DrawArenaBackground(ICollisionMap map)
    {
        WorldLayer.Children.Clear();

        Rectangle floor = new()
        {
            Width = map.Width,
            Height = map.Height,
            Fill = new SolidColorBrush(Color.FromRgb(32, 50, 45))
        };
        WorldLayer.Children.Add(floor);

        for (int x = 0; x < map.Width; x += 80)
        {
            Line line = new()
            {
                X1 = x,
                X2 = x,
                Y1 = 0,
                Y2 = map.Height,
                Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                StrokeThickness = 1
            };
            WorldLayer.Children.Add(line);
        }

        for (int y = 0; y < map.Height; y += 80)
        {
            Line line = new()
            {
                X1 = 0,
                X2 = map.Width,
                Y1 = y,
                Y2 = y,
                Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                StrokeThickness = 1
            };
            WorldLayer.Children.Add(line);
        }

        foreach (ArenaObstacle obstacle in map.Obstacles)
            DrawObstacle(obstacle);
    }

    private void DrawObstacle(ArenaObstacle obstacle)
    {
        Brush fill = obstacle.Kind switch
        {
            "tree" => new SolidColorBrush(Color.FromRgb(36, 92, 56)),
            "ruin" => new SolidColorBrush(Color.FromRgb(92, 86, 96)),
            _ => new SolidColorBrush(Color.FromRgb(80, 89, 98))
        };

        Rectangle rect = new()
        {
            Width = obstacle.Bounds.Width,
            Height = obstacle.Bounds.Height,
            Fill = fill,
            Stroke = new SolidColorBrush(Color.FromRgb(30, 36, 44)),
            StrokeThickness = 2,
            RadiusX = 4,
            RadiusY = 4
        };
        Canvas.SetLeft(rect, obstacle.Bounds.Left);
        Canvas.SetTop(rect, obstacle.Bounds.Top);
        WorldLayer.Children.Add(rect);
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
        });
    }

    private void UpdateCamera(Camera2D camera)
    {
        Dispatcher.Invoke(() =>
        {
            WorldTransform.X = -camera.X;
            WorldTransform.Y = -camera.Y;
            if (game is not null)
                effectsRenderer?.Render(game);
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

    private void QuitButton_Click(object sender, RoutedEventArgs e) => Close();

    private void FocusGame()
    {
        SpriteCanvas.Focus();
        Keyboard.Focus(SpriteCanvas);
    }

    private static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));
}
