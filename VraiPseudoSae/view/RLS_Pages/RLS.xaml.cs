using System.Windows;
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

        private RLSGame? game;
        private WPFScreen? screen;

        public RLS()
        {
            InitializeComponent();
            Loaded += (_, _) => Focus();

            Catalog.LoadFromPaks(@"C:\Users\Asus\RiderProjects\VraiPseudoSae201\VraiPseudoSae\Assets\Packs");
            audio.Load(@"C:\Users\Asus\RiderProjects\VraiPseudoSae201\VraiPseudoSae\data\RLS_Audio\AudioStructure.json");
            audio.Preload("car_sound/category/first_jump/jump0001", "first_jump");
            audio.Preload("car_sound/category/second_jump/jump0003", "second_jump");
            audio.Preload("car_sound/category/second_jump_movement/jump0002", "second_jump_movement");

            GoalExplosionAnime.SetDependencies(GameCanvas, audio);
        }

        public void StartGame(bool vsBot)
        {
            MainMenu.Visibility = Visibility.Collapsed;
            GameCanvas.Visibility = Visibility.Visible;
            ModeText.Text = vsBot ? "Mode: 1J vs BOT" : "Mode: 2 Joueurs";

            screen = new WPFScreen(GameCanvas);

            InjectSprites(screen);

            game = new RLSGame(
                screen,
                "Resources/Sprites",
                "Resources/Sounds",
                audio,
                vsBot);

            game.OnHudRefresh = RefreshHud;
            game.OnGoalShown = ShowGoal;
            game.OnGoalHidden = HideGoal;
            game.OnBackToMenu = BackToMenu;

            game.Run();
            Focus();
        }

        private void InjectSprites(WPFScreen currentScreen)
        {
            var bmpCar1   = XamlSpriteExporter.RenderToBitmapImage(SpriteCar1Source, 60, 34);
            var bmpCar2   = XamlSpriteExporter.RenderToBitmapImage(SpriteCar2Source, 60, 34);
            var bmpBall   = XamlSpriteExporter.RenderToBitmapImage(SpriteBallSource, 30, 30);
            var bmpGoal   = XamlSpriteExporter.RenderToBitmapImage(SpriteGoalSource, 26, 160);
            var bmpFloor  = XamlSpriteExporter.RenderToBitmapImage(SpriteFloorSource, 500, 40);

            SpriteInjector.PreRegister(currentScreen, "rls_car1.png",  bmpCar1);
            SpriteInjector.PreRegister(currentScreen, "rls_car2.png",  bmpCar2);
            SpriteInjector.PreRegister(currentScreen, "rls_ball.png",  bmpBall);
            SpriteInjector.PreRegister(currentScreen, "rls_goal.png",  bmpGoal);
            SpriteInjector.PreRegister(currentScreen, "rls_floor.png", bmpFloor);
        }

        private void RefreshHud()
        {
            if (game == null)
                return;

            ScoreP1Text.Text = game.Score1.ToString();
            ScoreP2Text.Text = game.Score2.ToString();

            Boost1Bar.Width = 150 * (game.Car1.Boost / 100.0);
            Boost2Bar.Width = 150 * (game.Car2.Boost / 100.0);
        }

        private void ShowGoal(string message)
        {
            GoalText.Text = message;
            GoalText.Visibility = Visibility.Visible;
            GoalExplosionAnime.Visibility = Visibility.Visible;

            if (message.Contains("P1"))
                GoalExplosionAnime.PlayRightGoal();
            else
                GoalExplosionAnime.PlayLeftGoal();
        }

        private void HideGoal()
        {
            GoalText.Visibility = Visibility.Collapsed;
            GoalExplosionAnime.Visibility = Visibility.Collapsed;
            RefreshHud();
        }

        private void BackToMenu()
        {
            game?.Pause();
            GameCanvas.Visibility = Visibility.Collapsed;
            MainMenu.Visibility = Visibility.Visible;
            GoalText.Visibility = Visibility.Collapsed;
            GoalExplosionAnime.Visibility = Visibility.Collapsed;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (game == null)
                return;

            ((IUTGame.IKeyboardInteract)FindController()).KeyDown(e.Key);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (game == null)
                return;

            ((IUTGame.IKeyboardInteract)FindController()).KeyUp(e.Key);
        }

        private IUTGame.GameItem FindController()
        {
            foreach (var item in game!.GetType().BaseType!
                         .GetMethod("ListItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                         .Invoke(game, null) as IUTGame.GameItem[] ?? [])
            {
                if (item is RLSController controller)
                    return controller;
            }
            throw new System.InvalidOperationException("RLSController introuvable dans le jeu.");
        }

        private void StartVsBot_Click(object sender, RoutedEventArgs e)  => StartGame(true);
        private void StartVsPlayer_Click(object sender, RoutedEventArgs e) => StartGame(false);
        private void BackButton_Click(object sender, RoutedEventArgs e)   => BackToMenu();
    }
}