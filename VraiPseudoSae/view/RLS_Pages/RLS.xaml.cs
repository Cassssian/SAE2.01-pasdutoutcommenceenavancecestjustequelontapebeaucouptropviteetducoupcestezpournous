using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IUTGame.WPF;
using VraiPseudoSae.data.AudioPlayer;
using VraiPseudoSae.data.PakManager;
using VraiPseudoSae.Utils.Sprite;

namespace VraiPseudoSae.view.RLS_Pages
{
    public partial class RLS : Window
    {
        private static readonly PakAudioCatalog Catalog = new();
        private readonly JsonPakAudioService audio = new(Catalog);

        private RLSGame?   game;
        private WPFScreen? screen;

        public RLS()
        {
            InitializeComponent();
            Loaded += (_, _) => Focus();

            Catalog.LoadFromPaks(@"C:\Users\Asus\RiderProjects\VraiPseudoSae201\VraiPseudoSae\Assets\Packs");
            audio.Load(@"C:\Users\Asus\RiderProjects\VraiPseudoSae201\VraiPseudoSae\data\RLS_Audio\AudioStructure.json");
            audio.Preload("car_sound/category/first_jump/jump0001",          "first_jump");
            audio.Preload("car_sound/category/second_jump/jump0003",         "second_jump");
            audio.Preload("car_sound/category/second_jump_movement/jump0002","second_jump_movement");

            GoalExplosionAnime.SetDependencies(GameCanvas, audio);
        }

        public void StartGame(bool vsBot)
        {
            MainMenu.Visibility   = Visibility.Collapsed;
            GameCanvas.Visibility = Visibility.Visible;
            ModeText.Text = vsBot ? "Mode: 1J vs BOT" : "Mode: 2 Joueurs";

            screen = new WPFScreen(GameCanvas);
            InjectSprites(screen);

            game = new RLSGame(screen, "Resources/Sprites", "Resources/Sounds", audio, vsBot);
            game.OnHudRefresh = RefreshHud;
            game.OnGoalShown  = ShowGoal;
            game.OnGoalHidden = HideGoal;
            game.OnBackToMenu = BackToMenu;

            game.Run();
            Focus();
        }

        private void InjectSprites(WPFScreen s)
        {
            SpriteInjector.PreRegister(s, "rls_car1.png",  XamlSpriteExporter.RenderToBitmapImage(SpriteCar1Source,  60,  34));
            SpriteInjector.PreRegister(s, "rls_car2.png",  XamlSpriteExporter.RenderToBitmapImage(SpriteCar2Source,  60,  34));
            SpriteInjector.PreRegister(s, "rls_ball.png",  XamlSpriteExporter.RenderToBitmapImage(SpriteBallSource,  30,  30));
            SpriteInjector.PreRegister(s, "rls_goal.png",  XamlSpriteExporter.RenderToBitmapImage(SpriteGoalSource,  26, 160));
            SpriteInjector.PreRegister(s, "rls_floor.png", XamlSpriteExporter.RenderToBitmapImage(SpriteFloorSource, 500, 40));
        }

        // ── HUD + flammes ─────────────────────────────────────────────────

        private void RefreshHud()
        {
            if (game == null) return;

            // Barres de boost
            ScoreP1Text.Text = game.Score1.ToString();
            ScoreP2Text.Text = game.Score2.ToString();
            Boost1Bar.Width  = 150 * (game.Car1.Boost / 100.0);
            Boost2Bar.Width  = 150 * (game.Car2.Boost / 100.0);

            // Flamme voiture 1
            UpdateFlame(Flame1, game.Car1);

            // Flamme voiture 2
            UpdateFlame(Flame2, game.Car2);
        }

        /// <summary>
        /// Positionne et affiche/cache la flamme d'une voiture.
        /// La flamme suit X,Y de la voiture, décalée derrière selon FacingDir.
        /// </summary>
        private static void UpdateFlame(Canvas flame, RLSCar car)
        {
            if (!car.IsBoosting)
            {
                flame.Visibility = Visibility.Collapsed;
                return;
            }

            // On colle la flamme à la position de la voiture.
            // Les polygones dans Flame1/Flame2 sont déjà dessinés en offset par rapport
            // au coin haut-gauche du Canvas (Points négatifs pour Flame1, >60 pour Flame2)
            // donc on pose simplement le Canvas à la même position que la voiture.
            Canvas.SetLeft(flame, car.X);
            Canvas.SetTop(flame,  car.Y + 4);   // légèrement centré verticalement
            flame.Visibility = Visibility.Visible;
        }

        // ── Gestion des buts ──────────────────────────────────────────────

        private void ShowGoal(string message)
        {
            GoalText.Text       = message;
            GoalText.Visibility = Visibility.Visible;
            GoalExplosionAnime.Visibility = Visibility.Visible;

            if (message.Contains("P1")) GoalExplosionAnime.PlayRightGoal();
            else                        GoalExplosionAnime.PlayLeftGoal();
        }

        private void HideGoal()
        {
            GoalText.Visibility           = Visibility.Collapsed;
            GoalExplosionAnime.Visibility = Visibility.Collapsed;
            RefreshHud();
        }

        private void BackToMenu()
        {
            game?.Pause();
            GameCanvas.Visibility         = Visibility.Collapsed;
            MainMenu.Visibility           = Visibility.Visible;
            GoalText.Visibility           = Visibility.Collapsed;
            GoalExplosionAnime.Visibility = Visibility.Collapsed;
            Flame1.Visibility             = Visibility.Collapsed;
            Flame2.Visibility             = Visibility.Collapsed;
        }

        // ── Clavier ───────────────────────────────────────────────────────

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (game == null) return;
            ((IUTGame.IKeyboardInteract)FindController()).KeyDown(e.Key);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (game == null) return;
            ((IUTGame.IKeyboardInteract)FindController()).KeyUp(e.Key);
        }

        private IUTGame.GameItem FindController()
        {
            foreach (var item in game!.GetType().BaseType!
                .GetMethod("ListItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(game, null) as IUTGame.GameItem[] ?? [])
            {
                if (item is RLSController c) return c;
            }
            throw new System.InvalidOperationException("RLSController introuvable.");
        }

        private void StartVsBot_Click(object sender, RoutedEventArgs e)    => StartGame(true);
        private void StartVsPlayer_Click(object sender, RoutedEventArgs e) => StartGame(false);
        private void BackButton_Click(object sender, RoutedEventArgs e)    => BackToMenu();
    }
}
