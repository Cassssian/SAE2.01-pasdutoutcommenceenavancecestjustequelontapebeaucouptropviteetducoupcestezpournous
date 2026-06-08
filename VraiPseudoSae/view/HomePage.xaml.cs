using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using IUTGame.WPF;
using VraiPseudoSae.view.gameintro;
using VraiPseudoSae.view.hub;

namespace VraiPseudoSae.view
{
    public partial class HomePage : UserControl
    {
        private const double ViewportWidth = 1280;
        private const double ViewportHeight = 720;
        private const double CameraZoom = 5.625;

        private HubGame? hubGame;
        private Point lastCameraTarget = new(ViewportWidth / 2.0, ViewportHeight / 2.0);

        public event EventHandler? WizardSurvivalRequested;

        public HomePage()
        {
            InitializeComponent();
            SizeChanged += HomePage_SizeChanged;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigureCamera();
            var screen = new WPFScreen(GameCanvas);
            GameIntroPlayerSpriteSet playerSprites = GameIntroSpriteSheetFactory.Register(screen);

            string spritesResourcePath = "Resources/Sprites";
            string soundsResourcePath = "Resources/Sounds";

            hubGame = new HubGame(
                screen,
                spritesResourcePath,
                soundsResourcePath,
                playerSprites,
                this,
                FootballZone,
                MazeZone,
                RLSZone,
                PinZone);

            hubGame.Run();
            CenterCameraOn(hubGame.PlayerCenter);

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

        private void HomePage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CenterCameraOn(lastCameraTarget);
        }

        private void ConfigureCamera()
        {
            WorldCameraScale.ScaleX = CameraZoom;
            WorldCameraScale.ScaleY = CameraZoom;
            CenterCameraOn(lastCameraTarget);
        }

        public void CenterCameraOn(Point worldCenter)
        {
            lastCameraTarget = worldCenter;

            double viewportWidth = CameraViewport.ActualWidth > 0 ? CameraViewport.ActualWidth : ViewportWidth;
            double viewportHeight = CameraViewport.ActualHeight > 0 ? CameraViewport.ActualHeight : ViewportHeight;

            WorldCameraTranslate.X = GetCameraAxisTranslation(
                worldCenter.X,
                viewportWidth);
            WorldCameraTranslate.Y = GetCameraAxisTranslation(
                worldCenter.Y,
                viewportHeight);
        }

        private static double GetCameraAxisTranslation(double target, double viewportSize)
        {
            return viewportSize / 2.0 - target * CameraZoom;
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
