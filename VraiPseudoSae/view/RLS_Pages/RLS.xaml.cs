using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IUTGame.WPF;
using VraiPseudoSae.data.GoalExplosion;
using VraiPseudoSae.data.PakManager;
using VraiPseudoSae.Utils.AudioPlayer;
using VraiPseudoSae.Utils.Sprite;
using VraiPseudoSae.view.RLS_Pages.GoalExplosions;

namespace VraiPseudoSae.view.RLS_Pages
{
    public partial class RLS : Window
    {
        private static readonly PakAudioCatalog Catalog = new();
        private readonly JsonPakAudioService audio = new(Catalog);
        private GoalExplosionBase? explosionBaseP1;
        private GoalExplosionBase? explosionBaseP2;
        private readonly GoalExplosionFlashController? goalFlashController;
        private readonly Color p1BaseColor = (Color)ColorConverter.ConvertFromString("#FFE94560");
        private readonly Color p1AccentColor = (Color)ColorConverter.ConvertFromString("#FFB83048");

        private readonly Color p2BaseColor = (Color)ColorConverter.ConvertFromString("#FF0F9D58");
        private readonly Color p2AccentColor = (Color)ColorConverter.ConvertFromString("#FF0A6E3E");

        private RLSGame?   game;
        private WPFScreen? screen;

        public RLS()
        {
            InitializeComponent();
            Loaded += (_, _) => Focus();

            string executionDir = AppDomain.CurrentDomain.BaseDirectory;

            string projectRoot = Path.GetFullPath(Path.Combine(executionDir, "..", "..", ".."));

            string packsPath = Path.Combine(projectRoot, "Assets", "Packs");
            string audioJsonPath = Path.Combine(projectRoot, "data", "RLS_Audio", "AudioStructure.json");

            Catalog.LoadFromPaks(packsPath);
            audio.Load(audioJsonPath);
            audio.Preload("car_sound/category/first_jump/jump0001",          "first_jump");
            audio.Preload("car_sound/category/second_jump/jump0003",         "second_jump");
            audio.Preload("car_sound/category/second_jump_movement/jump0002","second_jump_movement");
         
            goalFlashController = new GoalExplosionFlashController(
                GoalFlashWhite,
                GoalFlashPowder,
                GoalFlashPowderFill,
                GoalFlashPowderMaskBrush
            );
        }

        public void StartGame(bool vsBot)
        {
            UpdateGoalFlashPalettes();
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
            
            EnsureExplosionLoaded(ref explosionBaseP1, GoalExplosionType.AnimeSmoke);
            EnsureExplosionLoaded(ref explosionBaseP2, GoalExplosionType.B89);

            game.Run();
            Focus();
        }
        
        private void UpdateGoalFlashPalettes()
        {
            goalFlashController?.SetCarPalettes(
                new GoalExplosionFlashController.CarPalette(p1BaseColor, p1AccentColor),
                new GoalExplosionFlashController.CarPalette(p2BaseColor, p2AccentColor)
            );
        }

        private void InjectSprites(WPFScreen s)
        {
            SpriteInjector.PreRegister(s, "rls_car1.png",  XamlSpriteExporter.RenderToBitmapImage(SpriteCar1Source,  60,  34));
            SpriteInjector.PreRegister(s, "rls_car2.png",  XamlSpriteExporter.RenderToBitmapImage(SpriteCar2Source,  60,  34));
            SpriteInjector.PreRegister(s, "rls_ball.png",  XamlSpriteExporter.RenderToBitmapImage(SpriteBallSource,  30,  30));
            SpriteInjector.PreRegister(s, "rls_goal.png",  XamlSpriteExporter.RenderToBitmapImage(SpriteGoalSource,  26, 160));
            SpriteInjector.PreRegister(s, "rls_floor.png", XamlSpriteExporter.RenderToBitmapImage(SpriteFloorSource, 500, 40));
        }

        private void EnsureExplosionLoaded(ref GoalExplosionBase explosionPlayer, GoalExplosionType id)
        {

            switch (id)
            {
                case GoalExplosionType.Anime:
                    GoalExplosion_Anime goalExplosionAnime = new GoalExplosion_Anime(GameCanvas, audio);
                    explosionPlayer = goalExplosionAnime;
                    break;

                case GoalExplosionType.AnimeSmoke:
                    GoalExplosion_AnimeSmoke goalExplosionAnimeSmoke = new GoalExplosion_AnimeSmoke(GameCanvas, audio);
                    explosionPlayer = goalExplosionAnimeSmoke;
                    break;

                // TODO: autres cases
                case GoalExplosionType.B89:
                    GoalExplosion_B89 goalExplosionB89 = new GoalExplosion_B89(GameCanvas, audio);
                    explosionPlayer = goalExplosionB89;
                    break;
                case GoalExplosionType.Badaboom:
                    break;
                case GoalExplosionType.Ballistic:
                    break;
                case GoalExplosionType.Baseball:
                    break;
                case GoalExplosionType.Batman:
                    break;
                case GoalExplosionType.Bats:
                    break;
                case GoalExplosionType.BeachBalls:
                    break;
                case GoalExplosionType.Blade_T01:
                    break;
                case GoalExplosionType.Blade_T02:
                    break;
                case GoalExplosionType.BPMS:
                    break;
                case GoalExplosionType.Break:
                    break;
                case GoalExplosionType.Bubbles:
                    break;
                case GoalExplosionType.Butterflies:
                    break;
                case GoalExplosionType.ChinaDragon:
                    break;
                case GoalExplosionType.Confetti:
                    break;
                case GoalExplosionType.DarkEnergy_T01:
                    break;
                case GoalExplosionType.DarkEnergy_T02:
                    break;
                case GoalExplosionType.DarkEnergy_T03:
                    break;
                case GoalExplosionType.Digiglobe:
                    break;
                case GoalExplosionType.Dirt:
                    break;
                case GoalExplosionType.Dragon:
                    break;
                case GoalExplosionType.DynamicEnergy:
                    break;
                case GoalExplosionType.Electric:
                    break;
                case GoalExplosionType.Fingerguns:
                    break;
                case GoalExplosionType.Finish:
                    break;
                case GoalExplosionType.Fireworks:
                    break;
                case GoalExplosionType.Fish:
                    break;
                case GoalExplosionType.Fruit:
                    break;
                case GoalExplosionType.GeoTech:
                    break;
                case GoalExplosionType.Ghost:
                    break;
                case GoalExplosionType.Gravity:
                    break;
                case GoalExplosionType.HeartHands:
                    break;
                case GoalExplosionType.HorseHindLegs:
                    break;
                case GoalExplosionType.Hypnotik:
                    break;
                case GoalExplosionType.IceShards:
                    break;
                case GoalExplosionType.IllustratedBursts:
                    break;
                case GoalExplosionType.IonCannon:
                    break;
                case GoalExplosionType.Lanterns:
                    break;
                case GoalExplosionType.Leaves:
                    break;
                case GoalExplosionType.LoopDeLoop:
                    break;
                case GoalExplosionType.Meteors:
                    break;
                case GoalExplosionType.MicDrop:
                    break;
                case GoalExplosionType.Missiles:
                    break;
                case GoalExplosionType.MisterMonsoon:
                    break;
                case GoalExplosionType.Neurons:
                    break;
                case GoalExplosionType.NewDefault:
                    break;
                case GoalExplosionType.Nuhai:
                    break;
                case GoalExplosionType.Nuke:
                    break;
                case GoalExplosionType.October:
                    break;
                case GoalExplosionType.Polygon:
                    break;
                case GoalExplosionType.Popcorn:
                    break;
                case GoalExplosionType.Portal:
                    break;
                case GoalExplosionType.Presents:
                    break;
                case GoalExplosionType.Quartz:
                    break;
                case GoalExplosionType.Ring_T01:
                    break;
                case GoalExplosionType.Ring_T02:
                    break;
                case GoalExplosionType.Ring_T03:
                    break;
                case GoalExplosionType.RLCS_01:
                    break;
                case GoalExplosionType.SARBPC:
                    break;
                case GoalExplosionType.Season10_T1:
                    break;
                case GoalExplosionType.Season10_T2:
                    break;
                case GoalExplosionType.Season10_T3:
                    break;
                case GoalExplosionType.SideSwipe:
                    break;
                case GoalExplosionType.SimplePoof:
                    break;
                case GoalExplosionType.SingleAtoms:
                    break;
                case GoalExplosionType.Skull:
                    break;
                case GoalExplosionType.Solar:
                    break;
                case GoalExplosionType.SpaceSticker:
                    break;
                case GoalExplosionType.Sphenergy:
                    break;
                case GoalExplosionType.Splash:
                    break;
                case GoalExplosionType.Stage:
                    break;
                case GoalExplosionType.Starbeam:
                    break;
                case GoalExplosionType.Techy:
                    break;
                case GoalExplosionType.Tmblr:
                    break;
                case GoalExplosionType.Toon:
                    break;
                case GoalExplosionType.Trex:
                    break;
                case GoalExplosionType.TW:
                    break;
                case GoalExplosionType.Voxel:
                    break;
                case GoalExplosionType.WallBreak_T01:
                    break;
                case GoalExplosionType.WallBreak_T02:
                    break;
                case GoalExplosionType.WarpSpeed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }

            if (!GoalExplosionLayer.Children.Contains(explosionPlayer))
            {
                GoalExplosionLayer.Children.Add(explosionPlayer);
            }
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
        /// La flamme suit X, Y de la voiture, décalée derrière selon FacingDir.
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

            if (message.Contains("P1"))
            {
                goalFlashController?.PlayP1GoalFlash();
                explosionBaseP1.Visibility = Visibility.Visible;
                explosionBaseP1.PlayRightGoal();
            }
            else
            {
                goalFlashController?.PlayP2GoalFlash();
                explosionBaseP2.Visibility = Visibility.Visible;
                explosionBaseP2.PlayLeftGoal();
            }
        }

        private void HideGoal()
        {
            GoalText.Visibility           = Visibility.Collapsed;
            explosionBaseP1.Visibility = Visibility.Collapsed;
            explosionBaseP2.Visibility = Visibility.Collapsed;
            RefreshHud();
        }

        private void BackToMenu()
        {
            game?.Pause();
            GameCanvas.Visibility         = Visibility.Collapsed;
            MainMenu.Visibility           = Visibility.Visible;
            GoalText.Visibility           = Visibility.Collapsed;
            explosionBaseP1.Visibility    = Visibility.Collapsed;
            explosionBaseP2.Visibility    = Visibility.Collapsed;
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
            throw new InvalidOperationException("RLSController introuvable.");
        }

        private void StartVsBot_Click(object sender, RoutedEventArgs e)    => StartGame(true);
        private void StartVsPlayer_Click(object sender, RoutedEventArgs e) => StartGame(false);
        private void BackButton_Click(object sender, RoutedEventArgs e)    => BackToMenu();
    }
}
