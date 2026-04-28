using System;
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
            SpritePaths.EnsureDirectories();

            XamlSpriteExporter.ExportIfMissing(
                PlayerSpriteSource,
                SpritePaths.PlayerHubPng,
                60,
                90);

            var screen = new WPFScreen(GameCanvas);
            
            if (!System.IO.File.Exists(SpritePaths.PlayerHubPng))
            {
                MessageBox.Show("Sprite introuvable : " + SpritePaths.PlayerHubPng);
                return;
            }

            hubGame = new HubGame(
                screen,
                "Resources/Sprites",
                "Resources/Sounds",
                this,
                FootballZone,
                MazeZone,
                RLSZone);

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