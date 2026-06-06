using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using IUTGame.WPF;
using VraiPseudoSae.view.hub;
using VraiPseudoSae.Utils.Sprite;

namespace VraiPseudoSae.view
{
    public partial class HomePage : UserControl
    {
        private HubGame? hubGame;
        public event EventHandler? WizardSurvivalRequested;

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

            FocusGameCanvas();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            hubGame?.Pause();
        }

        private void FocusGameCanvas()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                GameCanvas.Focus();
                Keyboard.Focus(GameCanvas);
            }), DispatcherPriority.Input);
        }

        public void SetInfoText(string message)
        {
            InfoText.Text = message;
        }

        public void OpenWizardSurvival()
        {
            WizardSurvivalRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
