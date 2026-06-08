using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using IUTGame.WPF;
using VraiPseudoSae.Utils.SaveManager;
using VraiPseudoSae.view.gameintro;
using VraiPseudoSae.view.hub;

namespace VraiPseudoSae.view
{
    public partial class HomePage : UserControl
    {
        private const double ViewportWidth = 1280;
        private const double ViewportHeight = 720;
        private const double CameraZoom = 5.625;
        private const string ForwardBindingId = "forward";
        private const string BackwardBindingId = "backward";
        private const string LeftBindingId = "left";
        private const string RightBindingId = "right";
        private const string InteractionBindingId = "interaction";

        private readonly Dictionary<string, TextBlock> bindingKeyTexts = new();
        private readonly Dictionary<string, Border> bindingKeyBoxes = new();
        private readonly Dictionary<string, string> bindingValues = new();
        private HubGame? hubGame;
        private Point lastCameraTarget = new(ViewportWidth / 2.0, ViewportHeight / 2.0);
        private ParametresJeuSauvegarde settings = ParametresJeuSauvegardeDepot.ChargerOuDefaut();
        private HubSettingsCategory selectedSettingsCategory = HubSettingsCategory.General;
        private string? listeningBindingId;
        private bool applyingSettingsToUi;
        private bool settingsOverlayVisible;

        public event EventHandler? WizardSurvivalRequested;

        public HomePage()
        {
            InitializeComponent();
            ConfigureSettingsControls();
            ApplySavedSettingsToControls();
            SelectSettingsCategory(HubSettingsCategory.General);
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
                PinZone, 
                FlappyZone,
                settings);

            hubGame.Run();
            CenterCameraOn(hubGame.PlayerCenter);

            FocusGameCanvas();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            hubGame?.StopHubMusic();
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
            double worldWidth = WorldLayer.Width > 0 ? WorldLayer.Width : GameCanvas.Width;
            double worldHeight = WorldLayer.Height > 0 ? WorldLayer.Height : GameCanvas.Height;
            double scaledWorldWidth = worldWidth * CameraZoom;
            double scaledWorldHeight = worldHeight * CameraZoom;

            WorldCameraTranslate.X = GetCameraAxisTranslation(
                worldCenter.X,
                viewportWidth,
                scaledWorldWidth);
            WorldCameraTranslate.Y = GetCameraAxisTranslation(
                worldCenter.Y,
                viewportHeight,
                scaledWorldHeight);
        }

        private static double GetCameraAxisTranslation(double target, double viewportSize, double scaledWorldSize)
        {
            if (scaledWorldSize <= viewportSize)
                return (viewportSize - scaledWorldSize) / 2.0;

            double desired = viewportSize / 2.0 - target * CameraZoom;
            return Math.Clamp(desired, viewportSize - scaledWorldSize, 0);
        }

        public void SetInfoText(string message)
        {
            InfoText.Text = message;
        }

        public void SetPanelKeyHintVisible(bool visible)
        {
            PanelKeyHint.Visibility = visible && !settingsOverlayVisible
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public void OpenGameSettings()
        {
            settingsOverlayVisible = true;
            hubGame?.SetInputLocked(true);
            SetPanelKeyHintVisible(false);
            ApplySavedSettingsToControls();
            HubSettingsOverlay.Visibility = Visibility.Visible;
            HubSettingsOverlay.Focus();
            Keyboard.Focus(HubSettingsOverlay);
        }

        private void CloseGameSettings()
        {
            if (!settingsOverlayVisible)
                return;

            CancelBindingKeyCapture();
            settingsOverlayVisible = false;
            HubSettingsOverlay.Visibility = Visibility.Collapsed;
            hubGame?.SetInputLocked(false);
            FocusGameCanvas();
        }

        private void HomePage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (listeningBindingId is not null)
            {
                CompleteBindingKeyCapture(e.Key);
                e.Handled = true;
                return;
            }

            if (!settingsOverlayVisible)
                return;

            if (e.Key == Key.Escape)
                CloseGameSettings();

            e.Handled = true;
        }

        private void HomePage_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (settingsOverlayVisible)
                e.Handled = true;
        }

        private void ConfigureSettingsControls()
        {
            bindingKeyTexts[ForwardBindingId] = ForwardKeyText;
            bindingKeyTexts[BackwardBindingId] = BackwardKeyText;
            bindingKeyTexts[LeftBindingId] = LeftKeyText;
            bindingKeyTexts[RightBindingId] = RightKeyText;
            bindingKeyTexts[InteractionBindingId] = InteractionKeyText;

            bindingKeyBoxes[ForwardBindingId] = ForwardKeyBox;
            bindingKeyBoxes[BackwardBindingId] = BackwardKeyBox;
            bindingKeyBoxes[LeftBindingId] = LeftKeyBox;
            bindingKeyBoxes[RightBindingId] = RightKeyBox;
            bindingKeyBoxes[InteractionBindingId] = InteractionKeyBox;

            MasterVolumeSlider.ValueChanged += SettingsSlider_ValueChanged;
            MusicVolumeSlider.ValueChanged += SettingsSlider_ValueChanged;
            DialogueVolumeSlider.ValueChanged += SettingsSlider_ValueChanged;
            SfxVolumeSlider.ValueChanged += SettingsSlider_ValueChanged;
            TextSpeedSlider.ValueChanged += SettingsSlider_ValueChanged;
        }

        private void ApplySavedSettingsToControls()
        {
            settings = ParametresJeuSauvegardeDepot.ChargerOuDefaut();
            applyingSettingsToUi = true;

            MasterVolumeSlider.Value = settings.VolumeGeneral;
            MusicVolumeSlider.Value = settings.VolumeMusique;
            DialogueVolumeSlider.Value = settings.VolumeDialogues;
            SfxVolumeSlider.Value = settings.VolumeSfx;
            TextSpeedSlider.Value = settings.VitesseTexte;
            UpdateSettingsValueTexts();
            RefreshBindingDisplays();
            UpdateHudSettingsText();
            UpdatePanelKeyHintText();

            applyingSettingsToUi = false;
            hubGame?.ApplySettings(settings);
        }

        private void SettingsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (applyingSettingsToUi)
                return;

            settings = settings with
            {
                VolumeGeneral = ToPercent(MasterVolumeSlider.Value),
                VolumeMusique = ToPercent(MusicVolumeSlider.Value),
                VolumeDialogues = ToPercent(DialogueVolumeSlider.Value),
                VolumeSfx = ToPercent(SfxVolumeSlider.Value),
                VitesseTexte = ToPercent(TextSpeedSlider.Value)
            };

            UpdateSettingsValueTexts();
            SaveAndApplySettings();
        }

        private void UpdateSettingsValueTexts()
        {
            MasterVolumeValueText.Text = settings.VolumeGeneral + "%";
            MusicVolumeValueText.Text = settings.VolumeMusique + "%";
            DialogueVolumeValueText.Text = settings.VolumeDialogues + "%";
            SfxVolumeValueText.Text = settings.VolumeSfx + "%";
            TextSpeedValueText.Text = settings.VitesseTexte + "%";
        }

        private void SaveAndApplySettings()
        {
            settings = settings.Normaliser();
            ParametresJeuSauvegardeDepot.Sauvegarder(settings);
            hubGame?.ApplySettings(settings);
            UpdateHudSettingsText();
            RefreshBindingDisplays();
            UpdatePanelKeyHintText();
        }

        private void UpdateHudSettingsText()
        {
            MovementHelpText.Text =
                $"Déplacement : {FormatKey(ForwardKey)}/{FormatKey(LeftKey)}/{FormatKey(BackwardKey)}/{FormatKey(RightKey)}";
            InteractionHelpText.Text = $"Interaction : {FormatKey(InteractionKey)}";
        }

        private void UpdatePanelKeyHintText()
        {
            PanelKeyHintText.Text = FormatKey(InteractionKey);
        }

        private void RefreshBindingDisplays()
        {
            SetBindingValue(ForwardBindingId, FormatKey(ForwardKey));
            SetBindingValue(BackwardBindingId, FormatKey(BackwardKey));
            SetBindingValue(LeftBindingId, FormatKey(LeftKey));
            SetBindingValue(RightBindingId, FormatKey(RightKey));
            SetBindingValue(InteractionBindingId, FormatKey(InteractionKey));
        }

        private void SetBindingValue(string bindingId, string value)
        {
            bindingValues[bindingId] = value;

            if (bindingKeyTexts.TryGetValue(bindingId, out TextBlock? text))
                text.Text = value;
        }

        private void SelectSettingsCategory(HubSettingsCategory category)
        {
            selectedSettingsCategory = category;
            GeneralSettingsPanel.Visibility = category == HubSettingsCategory.General ? Visibility.Visible : Visibility.Collapsed;
            ControlsSettingsPanel.Visibility = category == HubSettingsCategory.Controls ? Visibility.Visible : Visibility.Collapsed;
            

            ApplyCategoryButtonState(GeneralCategoryButton, selectedSettingsCategory == HubSettingsCategory.General);
            ApplyCategoryButtonState(ControlsCategoryButton, selectedSettingsCategory == HubSettingsCategory.Controls);
        }

        private static void ApplyCategoryButtonState(Border button, bool selected)
        {
            button.Background = new SolidColorBrush(selected ? Color.FromRgb(45, 32, 12) : Color.FromRgb(17, 20, 28));
            button.BorderBrush = new SolidColorBrush(selected ? Color.FromRgb(255, 155, 47) : Color.FromRgb(79, 91, 105));
            button.Opacity = selected ? 1 : 0.78;

            if (button.Child is TextBlock text)
                text.Foreground = new SolidColorBrush(selected ? Colors.White : Color.FromRgb(230, 240, 247));
        }

        private void GeneralCategoryButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SelectSettingsCategory(HubSettingsCategory.General);
            e.Handled = true;
        }

        private void ControlsCategoryButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SelectSettingsCategory(HubSettingsCategory.Controls);
            e.Handled = true;
        }
        private void MazeCategoryButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SelectSettingsCategory(HubSettingsCategory.Labyrinthe);
            e.Handled = true;
        }

        private void ForwardChangeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            BeginBindingKeyCapture(ForwardBindingId);
            e.Handled = true;
        }

        private void BackwardChangeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            BeginBindingKeyCapture(BackwardBindingId);
            e.Handled = true;
        }

        private void LeftChangeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            BeginBindingKeyCapture(LeftBindingId);
            e.Handled = true;
        }

        private void RightChangeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            BeginBindingKeyCapture(RightBindingId);
            e.Handled = true;
        }

        private void InteractionChangeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            BeginBindingKeyCapture(InteractionBindingId);
            e.Handled = true;
        }

        private void ForwardResetButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ResetBindingKey(ForwardBindingId);
            e.Handled = true;
        }

        private void BackwardResetButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ResetBindingKey(BackwardBindingId);
            e.Handled = true;
        }

        private void LeftResetButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ResetBindingKey(LeftBindingId);
            e.Handled = true;
        }

        private void RightResetButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ResetBindingKey(RightBindingId);
            e.Handled = true;
        }

        private void InteractionResetButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ResetBindingKey(InteractionBindingId);
            e.Handled = true;
        }

        private void HubSettingsCloseButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CloseGameSettings();
            e.Handled = true;
        }

        private void BeginBindingKeyCapture(string bindingId)
        {
            if (listeningBindingId is not null)
            {
                if (bindingValues.TryGetValue(listeningBindingId, out string? previousValue) &&
                    bindingKeyTexts.TryGetValue(listeningBindingId, out TextBlock? previousText))
                {
                    previousText.Text = previousValue;
                }

                ApplyBindingBoxState(listeningBindingId, listening: false);
            }

            listeningBindingId = bindingId;

            if (bindingKeyTexts.TryGetValue(bindingId, out TextBlock? text))
                text.Text = "Appuyez sur une touche";

            ApplyBindingBoxState(bindingId, listening: true);
            HubSettingsOverlay.Focus();
            Keyboard.Focus(HubSettingsOverlay);
        }

        private void CompleteBindingKeyCapture(Key key)
        {
            if (listeningBindingId is null)
                return;

            string bindingId = listeningBindingId;
            SetBindingKey(bindingId, key);
            ApplyBindingBoxState(bindingId, listening: false);
            listeningBindingId = null;
        }

        private void ResetBindingKey(string bindingId)
        {
            if (listeningBindingId == bindingId)
                listeningBindingId = null;

            SetBindingKey(bindingId, GetDefaultBindingKey(bindingId));
            ApplyBindingBoxState(bindingId, listening: false);
        }

        private void CancelBindingKeyCapture()
        {
            if (listeningBindingId is null)
                return;

            if (bindingValues.TryGetValue(listeningBindingId, out string? previousValue) &&
                bindingKeyTexts.TryGetValue(listeningBindingId, out TextBlock? previousText))
            {
                previousText.Text = previousValue;
            }

            ApplyBindingBoxState(listeningBindingId, listening: false);
            listeningBindingId = null;
        }

        private void SetBindingKey(string bindingId, Key key)
        {
            settings = bindingId switch
            {
                ForwardBindingId => settings with { ToucheAvancer = key.ToString() },
                BackwardBindingId => settings with { ToucheReculer = key.ToString() },
                LeftBindingId => settings with { ToucheGauche = key.ToString() },
                RightBindingId => settings with { ToucheDroite = key.ToString() },
                InteractionBindingId => settings with { ToucheInteraction = key.ToString() },
                _ => settings
            };

            SetBindingValue(bindingId, FormatKey(key));
            SaveAndApplySettings();
        }

        private void ApplyBindingBoxState(string bindingId, bool listening)
        {
            if (!bindingKeyBoxes.TryGetValue(bindingId, out Border? keyBox))
                return;

            keyBox.Background = new SolidColorBrush(listening ? Colors.White : Colors.Black);
            keyBox.BorderBrush = new SolidColorBrush(Colors.White);

            if (bindingKeyTexts.TryGetValue(bindingId, out TextBlock? text))
                text.Foreground = new SolidColorBrush(listening ? Colors.Black : Colors.White);
        }

        private Key GetDefaultBindingKey(string bindingId)
        {
            ParametresJeuSauvegarde defaults = ParametresJeuSauvegarde.ParDefaut;

            return bindingId switch
            {
                ForwardBindingId => ParseSavedKey(defaults.ToucheAvancer, Key.Z),
                BackwardBindingId => ParseSavedKey(defaults.ToucheReculer, Key.S),
                LeftBindingId => ParseSavedKey(defaults.ToucheGauche, Key.Q),
                RightBindingId => ParseSavedKey(defaults.ToucheDroite, Key.D),
                InteractionBindingId => ParseSavedKey(defaults.ToucheInteraction, Key.E),
                _ => Key.None
            };
        }

        private Key ForwardKey => ParseSavedKey(settings.ToucheAvancer, Key.Z);

        private Key BackwardKey => ParseSavedKey(settings.ToucheReculer, Key.S);

        private Key LeftKey => ParseSavedKey(settings.ToucheGauche, Key.Q);

        private Key RightKey => ParseSavedKey(settings.ToucheDroite, Key.D);

        private Key InteractionKey => ParseSavedKey(settings.ToucheInteraction, Key.E);

        private static int ToPercent(double value)
        {
            return (int)Math.Round(Math.Clamp(value, 0, 100));
        }

        public static string FormatKey(Key key)
        {
            if (key == Key.None)
                return string.Empty;

            if (key == Key.Return)
                return "Entrée";

            if (key == Key.Escape)
                return "Échap";

            if (key == Key.Space)
                return "Espace";

            if (key >= Key.D0 && key <= Key.D9)
                return ((int)(key - Key.D0)).ToString();

            if (key >= Key.NumPad0 && key <= Key.NumPad9)
                return "Num " + (int)(key - Key.NumPad0);

            return key.ToString().ToUpperInvariant();
        }

        public static Key ParseSavedKey(string savedKey, Key fallback)
        {
            return Enum.TryParse(savedKey, ignoreCase: true, out Key key) && key != Key.None
                ? key
                : fallback;
        }

        public void OpenWizardSurvival()
        {
            WizardSurvivalRequested?.Invoke(this, EventArgs.Empty);
        }

        private enum HubSettingsCategory
        {
            General,
            Controls,
            Labyrinthe,
            FootCar,
            Football,
            Sorcier,
            FlappyBird,
            Dropper
        }
    }
}
