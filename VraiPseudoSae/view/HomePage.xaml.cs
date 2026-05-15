using System.Windows;
using IUTGame.WPF;
using VraiPseudoSae.view.hub;
using VraiPseudoSae.Utils.Sprite;

namespace VraiPseudoSae.view
{
    public partial class HomePage : Window
    {
        private HubGame hubGame = null!;

        public HomePage()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var screen = new WPFScreen(GameCanvas);

            // 1. Rendre le sprite du joueur depuis le XAML en mémoire
            var playerBitmap = XamlSpriteExporter.RenderToBitmapImage(PlayerSpriteSource, 60, 90);

            // 2. L'injecter dans le SpriteStore de la DLL sous le nom exact attendu par HubPlayer
            SpriteInjector.PreRegister(screen, "player_hub.png", playerBitmap);

            // 3. Créer et lancer le jeu (le LoadSprite("player_hub.png") trouvera le bitmap injecté)
            string spritesResourcePath = "Resources/Sprites";
            string soundsResourcePath = "Resources/Sounds";

            hubGame = new HubGame(
                screen,
                spritesResourcePath,
                soundsResourcePath,
                this,
                FootballZone,
                MazeZone,
                RLSZone,
                PinZone);

            hubGame.Run();

            GameCanvas.Focus();
            Focus();
        }

        public void SetInfoText(string message)
        {
            InfoText.Text = message;
        }
    }
}
