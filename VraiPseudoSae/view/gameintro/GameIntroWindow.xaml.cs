using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VraiPseudoSae.Utils.GestionnaireSauvegarde;
using VraiPseudoSae.Utils.AudioPlayer;
using Keyboard = System.Windows.Input.Keyboard;

namespace VraiPseudoSae.view.gameintro
{
    public partial class GameIntroWindow : UserControl
    {
        private const double ViewportWidth = 1280;
        private const double ViewportHeight = 720;
        private const double MapSize = 128;
        private const double OverviewScale = 5.625;
        private const double FocusScale = 14;
        private const double PlayerSpeed = 30;
        private const double PanelInteractionRadius = 32;
        private const double PanelAutoMoveTargetRadius = 23;
        private const int NormalTextDelayMilliseconds = 34;
        private const int RainbowTextDelayMilliseconds = 24;
        private const int DialogueEndPauseMilliseconds = 2600;
        private const string ForwardBindingId = "forward";
        private const string BackwardBindingId = "backward";
        private const string LeftBindingId = "left";
        private const string RightBindingId = "right";
        private const string InteractionBindingId = "interaction";

        private readonly Point panelCenter = new(64, 53);
        private readonly DispatcherTimer keyPulseTimer = new();
        private readonly DispatcherTimer rainbowTimer = new();
        private readonly DispatcherTimer movementTimer = new();
        private readonly List<RainbowGlyph> rainbowGlyphs = new();
        private readonly Dictionary<string, TextBlock> bindingKeyTexts = new();
        private readonly Dictionary<string, Border> bindingKeyBoxes = new();
        private readonly Dictionary<string, string> bindingValues = new();

        private EllipseGeometry revealHole = null!;
        private TaskCompletionSource<GameIntroLanguage>? languageChoiceCompletion;
        private DateTime animationStartUtc;
        private DateTime lastMovementTickUtc;
        private ParametresJeuSauvegarde settings = ParametresJeuSauvegardeDepot.ChargerOuDefaut();
        private GameIntroLanguage selectedLanguage = GameIntroLanguage.French;
        private GameIntroLanguage highlightedLanguage = GameIntroLanguage.French;
        private string? listeningBindingId;
        private bool waitingForSpace = true;
        private bool choosingLanguage;
        private bool inputsLocked = true;
        private bool movementEnabled;
        private bool settingsIntroStarted;
        private bool settingsOverlayVisible;
        private bool settingsTutorialPlaying;
        private bool applyingSettingsToUi;
        private bool moveUp;
        private bool moveDown;
        private bool moveLeft;
        private bool moveRight;
        private readonly JsonPakAudioService audio = new();

        public GameIntroWindow()
        {
            InitializeComponent();
            ConfigureRevealOverlay();
            ConfigureTimers();
            ConfigureSettingsControls();
            ApplySavedSettingsToControls();
            audio.LoadFromPath("Assets/woosh.mp3", "woosh");
            audio.LoadFromPath("Assets/bip.wav", "bip");
        }

        private Point PlayerCenter => new(
            Canvas.GetLeft(PlayerSquare) + PlayerSquare.Width / 2.0,
            Canvas.GetTop(PlayerSquare) + PlayerSquare.Height / 2.0);

        private void GameIntroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            animationStartUtc = DateTime.UtcNow;
            lastMovementTickUtc = DateTime.UtcNow;
            SpaceKeyText.Text = GameIntroScript.SpaceKey(selectedLanguage);
            SetCamera(panelCenter, OverviewScale, false);
            StartSpaceKeyAnimation();
            FocusIntroInput();
        }

        private void GameIntroWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            keyPulseTimer.Stop();
            rainbowTimer.Stop();
            movementTimer.Stop();
        }

        private void GameIntroWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (listeningBindingId is not null)
            {
                CompleteBindingKeyCapture(e.Key);
                e.Handled = true;
                return;
            }

            if (choosingLanguage)
            {
                HandleLanguageChoiceKey(e.Key);
                e.Handled = true;
                return;
            }

            if (waitingForSpace && e.Key == Key.Space)
            {
                waitingForSpace = false;
                e.Handled = true;
                _ = StartIntroSequenceAsync();
                return;
            }

            if (settingsOverlayVisible)
            {
                if (e.Key == Key.Escape)
                    CloseSettingsOverlayTemporarily();

                e.Handled = true;
                return;
            }

            if (movementEnabled && e.Key == InteractionKey && IsPlayerInPanelInteractionRange())
            {
                e.Handled = true;

                if (!settingsIntroStarted)
                    _ = StartSettingsIntroSceneAsync();
                else
                    OpenSettingsOverlay();

                return;
            }

            if (inputsLocked || !movementEnabled)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == ForwardKey)
                moveUp = true;
            if (e.Key == BackwardKey)
                moveDown = true;
            if (e.Key == LeftKey)
                moveLeft = true;
            if (e.Key == RightKey)
                moveRight = true;

            e.Handled = true;
        }

        private void GameIntroWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == ForwardKey)
                moveUp = false;
            if (e.Key == BackwardKey)
                moveDown = false;
            if (e.Key == LeftKey)
                moveLeft = false;
            if (e.Key == RightKey)
                moveRight = false;
        }

        private async Task StartIntroSequenceAsync()
        {
            keyPulseTimer.Stop();
            await PlayKeyPressAnimationAsync(SpaceKeyPrompt, SpaceKeyText);
            SpaceKeyPrompt.Visibility = Visibility.Collapsed;
            selectedLanguage = await ShowLanguageChoiceAsync();
            await PlayRevealAsync();

            await Task.Delay(1800);
            PlaySfx("woosh");
            await FocusCameraAsync(PlayerCenter, FocusScale, 1100);

            foreach (IReadOnlyList<DialogueSegment> block in GameIntroScript.OpeningBlocks(selectedLanguage))
                await ShowDialogueAsync(block);

            await Task.Delay(900);
            DialoguePanel.Visibility = Visibility.Collapsed;
            PlaySfx("woosh");
            await FocusCameraAsync(panelCenter, OverviewScale, 900, centerTarget: false);
            inputsLocked = true;
            await Task.Delay(4200);
            PlaySfx("woosh");
            await FocusCameraAsync(PlayerCenter, FocusScale, 950);
            await ShowDialogueAsync(GameIntroScript.PrankExplanation(selectedLanguage));

            await Task.Delay(550);
            PlaySfx("woosh");
            await FocusCameraAsync(panelCenter, FocusScale, 700);
            await Task.Delay(1000);
            PlaySfx("woosh");
            await FocusCameraAsync(PlayerCenter, FocusScale, 700);

            await ShowDialogueAsync(GameIntroScript.MagicPanelReveal(selectedLanguage));
            await Task.Delay(2100);
            await ShowDialogueAsync(GameIntroScript.InteractionHint(selectedLanguage));

            await Task.Delay(600);
            DialoguePanel.Visibility = Visibility.Collapsed;
            await EnsurePlayerCloseToPanelAsync();
            PlaySfx("woosh");
            await FocusCameraAsync(panelCenter, OverviewScale, 900, centerTarget: false);
            ShowPanelKeyHint();
            movementEnabled = true;
            inputsLocked = false;
            movementTimer.Start();
            //ProgressionJeuSauvegardeDepot.MarquerIntroductionTerminee();
            FocusIntroInput();
        }

        private async Task StartSettingsIntroSceneAsync()
        {
            if (settingsIntroStarted && settingsOverlayVisible)
                return;

            settingsIntroStarted = true;
            settingsTutorialPlaying = true;
            OpenSettingsOverlay();
            SelectSettingsCategory(SettingsIntroCategory.General);

            DialogueSpeakerPortrait.Visibility = Visibility.Visible;
            await Task.Delay(420);

            foreach (SettingsIntroStep step in GameIntroScript.SettingsTutorial(selectedLanguage))
            {
                SelectSettingsCategory(step.Category);
                await Task.Delay(90);
                ShowSettingsHighlight(step.HighlightTarget);
                await ShowDialogueAsync(step.Segments);
                await Task.Delay(320);
            }

            HideSettingsHighlight();
            settingsTutorialPlaying = false;
            UpdateSettingsCloseAvailability();
            DialoguePanel.Visibility = Visibility.Collapsed;
            DialogueSpeakerPortrait.Visibility = Visibility.Collapsed;
            FocusIntroInput();
        }

        private void OpenSettingsOverlay()
        {
            settingsOverlayVisible = true;
            inputsLocked = true;
            movementEnabled = false;
            movementTimer.Stop();
            ResetMovementInput();
            HidePanelKeyHint();
            ApplySettingsIntroText();
            UpdateSettingsCloseAvailability();

            SettingsIntroOverlay.Opacity = 0;
            SettingsIntroOverlay.Visibility = Visibility.Visible;
            SettingsIntroOverlay.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(260),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

            FocusIntroInput();
        }

        private void CloseSettingsOverlayTemporarily()
        {
            if (!settingsOverlayVisible || settingsTutorialPlaying)
                return;

            CancelBindingKeyCapture();
            HideSettingsHighlight();
            DialoguePanel.Visibility = Visibility.Collapsed;
            DialogueSpeakerPortrait.Visibility = Visibility.Collapsed;
            SettingsIntroOverlay.Visibility = Visibility.Collapsed;
            settingsOverlayVisible = false;
            inputsLocked = false;
            movementEnabled = true;
            lastMovementTickUtc = DateTime.UtcNow;
            movementTimer.Start();
            ShowPanelKeyHint();
            FocusIntroInput();
        }

        private void UpdateSettingsCloseAvailability()
        {
            SettingsCloseButton.Opacity = settingsTutorialPlaying ? 0.35 : 1;
            SettingsTemporaryCloseText.Opacity = settingsTutorialPlaying ? 0.35 : 0.72;
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
            DialogueVolumeSlider.ValueChanged += SettingsSlider_ValueChanged;
            SfxVolumeSlider.ValueChanged += SettingsSlider_ValueChanged;
            TextSpeedSlider.ValueChanged += SettingsSlider_ValueChanged;
        }

        private void ApplySettingsIntroText()
        {
            SettingsIntroUiText ui = GameIntroScript.SettingsUi(selectedLanguage);

            SettingsCategoriesTitleText.Text = ui.CategoriesTitle;
            GeneralCategoryText.Text = ui.GeneralCategory;
            MainMenuCategoryText.Text = ui.MainMenuCategory;

            GeneralSettingsTitleText.Text = ui.GeneralTitle;
            MasterVolumeText.Text = ui.MasterVolume;
            DialogueVolumeText.Text = ui.DialogueVolume;
            SfxVolumeText.Text = ui.SfxVolume;
            TextSpeedText.Text = ui.TextSpeed;
            UpdateSettingsValueTexts();

            MainMenuSettingsTitleText.Text = ui.MainMenuTitle;
            ActionHeaderText.Text = ui.ActionHeader;
            KeyHeaderText.Text = ui.KeyHeader;
            ForwardActionText.Text = ui.ForwardAction;
            BackwardActionText.Text = ui.BackwardAction;
            LeftActionText.Text = ui.LeftAction;
            RightActionText.Text = ui.RightAction;
            InteractionActionText.Text = ui.InteractionAction;
            RefreshBindingDisplays();

            ForwardChangeButtonText.Text = ui.ChangeKey;
            BackwardChangeButtonText.Text = ui.ChangeKey;
            LeftChangeButtonText.Text = ui.ChangeKey;
            RightChangeButtonText.Text = ui.ChangeKey;
            InteractionChangeButtonText.Text = ui.ChangeKey;
            SettingsTemporaryCloseText.Text = ui.CloseTemporary;
        }

        private void SelectSettingsCategory(SettingsIntroCategory category)
        {
            GeneralSettingsPanel.Visibility = category == SettingsIntroCategory.General ? Visibility.Visible : Visibility.Collapsed;
            MainMenuSettingsPanel.Visibility = category == SettingsIntroCategory.Controls ? Visibility.Visible : Visibility.Collapsed;

            ApplyCategoryButtonState(GeneralCategoryButton, category == SettingsIntroCategory.General);
            ApplyCategoryButtonState(MainMenuCategoryButton, category == SettingsIntroCategory.Controls);
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
            SelectSettingsCategory(SettingsIntroCategory.General);
            e.Handled = true;
        }

        private void MainMenuCategoryButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SelectSettingsCategory(SettingsIntroCategory.Controls);
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

        private void SettingsCloseButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CloseSettingsOverlayTemporarily();
            e.Handled = true;
        }

        private void BeginBindingKeyCapture(string bindingId)
        {
            if (listeningBindingId is not null)
            {
                if (bindingValues.TryGetValue(listeningBindingId, out string? previousValue) &&
                    bindingKeyTexts.TryGetValue(listeningBindingId, out TextBlock? previousText) &&
                    previousText is not null)
                {
                    previousText.Text = previousValue;
                }

                ApplyBindingBoxState(listeningBindingId, listening: false);
            }

            listeningBindingId = bindingId;
            SettingsIntroUiText ui = GameIntroScript.SettingsUi(selectedLanguage);

            if (bindingKeyTexts.TryGetValue(bindingId, out TextBlock? text) && text is not null)
                text.Text = ui.PressKey;

            ApplyBindingBoxState(bindingId, listening: true);
            FocusIntroInput();
        }

        private void CompleteBindingKeyCapture(Key key)
        {
            if (listeningBindingId is null)
                return;

            string bindingId = listeningBindingId;
            SetBindingKey(bindingId, key, save: true);
            ApplyBindingBoxState(bindingId, listening: false);
            listeningBindingId = null;
        }

        private void ResetBindingKey(string bindingId)
        {
            if (listeningBindingId == bindingId)
                listeningBindingId = null;

            SetBindingKey(bindingId, GetDefaultBindingKey(bindingId), save: true);
            ApplyBindingBoxState(bindingId, listening: false);
        }

        private void CancelBindingKeyCapture()
        {
            if (listeningBindingId is null)
                return;

            if (bindingValues.TryGetValue(listeningBindingId, out string? previousValue) &&
                bindingKeyTexts.TryGetValue(listeningBindingId, out TextBlock? previousText) &&
                previousText is not null)
            {
                previousText.Text = previousValue;
            }

            ApplyBindingBoxState(listeningBindingId, listening: false);
            listeningBindingId = null;
        }

        private void SetBindingKey(string bindingId, Key key, bool save)
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
            ApplyPanelKeyHintText();

            if (save)
                SaveCurrentSettings();
        }

        private void SetBindingValue(string bindingId, string value)
        {
            bindingValues[bindingId] = value;

            if (bindingKeyTexts.TryGetValue(bindingId, out TextBlock? text) && text is not null)
                text.Text = value;
        }

        private void ApplyBindingBoxState(string bindingId, bool listening)
        {
            if (!bindingKeyBoxes.TryGetValue(bindingId, out Border? keyBox) || keyBox is null)
                return;

            keyBox.Background = new SolidColorBrush(listening ? Colors.White : Colors.Black);
            keyBox.BorderBrush = new SolidColorBrush(Colors.White);

            if (bindingKeyTexts.TryGetValue(bindingId, out TextBlock? text) && text is not null)
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

        private string FormatKey(Key key)
        {
            if (key == Key.None)
                return string.Empty;

            if (key == Key.Return)
                return selectedLanguage == GameIntroLanguage.French ? "Entrée" : "Enter";

            if (key == Key.Escape)
                return selectedLanguage == GameIntroLanguage.French ? "Échap" : "Esc";

            if (key == Key.Space)
                return selectedLanguage == GameIntroLanguage.French ? "Espace" : "Space";

            if (key >= Key.D0 && key <= Key.D9)
                return ((int)(key - Key.D0)).ToString();

            if (key >= Key.NumPad0 && key <= Key.NumPad9)
                return "Num " + (int)(key - Key.NumPad0);

            return key.ToString().ToUpperInvariant();
        }

        private void ApplySavedSettingsToControls()
        {
            applyingSettingsToUi = true;

            MasterVolumeSlider.Value = settings.VolumeGeneral;
            DialogueVolumeSlider.Value = settings.VolumeDialogues;
            SfxVolumeSlider.Value = settings.VolumeSfx;
            TextSpeedSlider.Value = settings.VitesseTexte;
            UpdateSettingsValueTexts();
            RefreshBindingDisplays();
            ApplyPanelKeyHintText();

            applyingSettingsToUi = false;
        }

        private void SettingsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (applyingSettingsToUi)
                return;

            settings = settings with
            {
                VolumeGeneral = ToPercent(MasterVolumeSlider.Value),
                VolumeDialogues = ToPercent(DialogueVolumeSlider.Value),
                VolumeSfx = ToPercent(SfxVolumeSlider.Value),
                VitesseTexte = ToPercent(TextSpeedSlider.Value)
            };

            UpdateSettingsValueTexts();
            SaveCurrentSettings();
        }

        private void UpdateSettingsValueTexts()
        {
            MasterVolumeValueText.Text = settings.VolumeGeneral + "%";
            DialogueVolumeValueText.Text = settings.VolumeDialogues + "%";
            SfxVolumeValueText.Text = settings.VolumeSfx + "%";
            TextSpeedValueText.Text = settings.VitesseTexte + "%";
        }

        private void RefreshBindingDisplays()
        {
            SetBindingValue(ForwardBindingId, FormatKey(ForwardKey));
            SetBindingValue(BackwardBindingId, FormatKey(BackwardKey));
            SetBindingValue(LeftBindingId, FormatKey(LeftKey));
            SetBindingValue(RightBindingId, FormatKey(RightKey));
            SetBindingValue(InteractionBindingId, FormatKey(InteractionKey));
        }

        private void ApplyPanelKeyHintText()
        {
            PanelKeyHintText.Text = FormatKey(InteractionKey);
        }

        private void SaveCurrentSettings()
        {
            settings = settings.Normaliser();
            ParametresJeuSauvegardeDepot.Sauvegarder(settings);
        }

        private void PlaySfx(string alias)
        {
            audio.Play(alias, EffectiveSfxVolume);
        }

        private void PlayDialogueTick()
        {
            audio.Play("bip", EffectiveDialogueVolume);
        }

        private float EffectiveSfxVolume =>
            (float)(settings.VolumeGeneral / 100.0 * settings.VolumeSfx / 100.0);

        private float EffectiveDialogueVolume =>
            (float)(settings.VolumeGeneral / 100.0 * settings.VolumeDialogues / 100.0);

        private Key ForwardKey => ParseSavedKey(settings.ToucheAvancer, Key.Z);

        private Key BackwardKey => ParseSavedKey(settings.ToucheReculer, Key.S);

        private Key LeftKey => ParseSavedKey(settings.ToucheGauche, Key.Q);

        private Key RightKey => ParseSavedKey(settings.ToucheDroite, Key.D);

        private Key InteractionKey => ParseSavedKey(settings.ToucheInteraction, Key.E);

        private static int ToPercent(double value)
        {
            return (int)Math.Round(Clamp(value, 0, 100));
        }

        private static Key ParseSavedKey(string savedKey, Key fallback)
        {
            return Enum.TryParse(savedKey, ignoreCase: true, out Key key) && key != Key.None
                ? key
                : fallback;
        }

        private void ShowSettingsHighlight(SettingsIntroHighlightTarget target)
        {
            FrameworkElement? element = GetSettingsHighlightElement(target);

            if (element is null)
            {
                HideSettingsHighlight();
                return;
            }

            SettingsIntroOverlay.UpdateLayout();
            Rect bounds = element.TransformToAncestor(SettingsIntroOverlay).TransformBounds(
                new Rect(0, 0, element.ActualWidth, element.ActualHeight));
            bounds.Inflate(8, 8);

            SettingsTutorialHighlight.Width = bounds.Width;
            SettingsTutorialHighlight.Height = bounds.Height;
            Canvas.SetLeft(SettingsTutorialHighlight, bounds.Left);
            Canvas.SetTop(SettingsTutorialHighlight, bounds.Top);

            SettingsTutorialHighlight.Visibility = Visibility.Visible;
            SettingsTutorialHighlight.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        private void HideSettingsHighlight()
        {
            SettingsTutorialHighlight.BeginAnimation(OpacityProperty, null);
            SettingsTutorialHighlight.Opacity = 0;
            SettingsTutorialHighlight.Visibility = Visibility.Collapsed;
        }

        private FrameworkElement? GetSettingsHighlightElement(SettingsIntroHighlightTarget target)
        {
            return target switch
            {
                SettingsIntroHighlightTarget.Interface => SettingsWindowFrame,
                SettingsIntroHighlightTarget.CategoryPanel => SettingsCategoryPanel,
                SettingsIntroHighlightTarget.GeneralCategory => GeneralCategoryButton,
                SettingsIntroHighlightTarget.GeneralSettings => GeneralSettingsContent,
                SettingsIntroHighlightTarget.MasterVolume => MasterVolumeSettingRow,
                SettingsIntroHighlightTarget.DialogueVolume => DialogueVolumeSettingRow,
                SettingsIntroHighlightTarget.SfxVolume => SfxVolumeSettingRow,
                SettingsIntroHighlightTarget.TextSpeed => TextSpeedSettingRow,
                SettingsIntroHighlightTarget.MainMenuCategory => MainMenuCategoryButton,
                SettingsIntroHighlightTarget.KeyBindings => MainMenuBindingsList,
                SettingsIntroHighlightTarget.KeyBox => InteractionKeyBox,
                SettingsIntroHighlightTarget.ChangeButton => InteractionChangeButton,
                SettingsIntroHighlightTarget.ResetButton => InteractionResetButton,
                _ => null
            };
        }

        private void HidePanelKeyHint()
        {
            PanelHintTranslate.BeginAnimation(TranslateTransform.YProperty, null);
            PanelHintRotate.BeginAnimation(RotateTransform.AngleProperty, null);
            PanelKeyHint.Visibility = Visibility.Collapsed;
        }

        private void ResetMovementInput()
        {
            moveUp = false;
            moveDown = false;
            moveLeft = false;
            moveRight = false;
        }

        private bool IsPlayerInPanelInteractionRange()
        {
            return Distance(PlayerCenter, panelCenter) <= PanelInteractionRadius;
        }

        private void UpdatePanelKeyHintVisibility()
        {
            if (settingsOverlayVisible || !movementEnabled || !IsPlayerInPanelInteractionRange())
            {
                HidePanelKeyHint();
                return;
            }

            ShowPanelKeyHint();
        }

        private Task EnsurePlayerCloseToPanelAsync()
        {
            if (IsPlayerInPanelInteractionRange())
                return Task.CompletedTask;

            Point startCenter = PlayerCenter;
            Vector direction = startCenter - panelCenter;

            if (direction.LengthSquared < 0.001)
                direction = new Vector(0, 1);

            direction.Normalize();
            Point targetCenter = panelCenter + direction * PanelAutoMoveTargetRadius;
            double targetLeft = Clamp(targetCenter.X - PlayerSquare.Width / 2.0, 0, MapSize - PlayerSquare.Width);
            double targetTop = Clamp(targetCenter.Y - PlayerSquare.Height / 2.0, 0, MapSize - PlayerSquare.Height);
            double distance = Distance(startCenter, new Point(targetLeft + PlayerSquare.Width / 2.0, targetTop + PlayerSquare.Height / 2.0));
            int durationMilliseconds = (int)Clamp(distance / PlayerSpeed * 1000, 550, 1450);

            TaskCompletionSource completion = new();
            DoubleAnimation leftAnimation = new()
            {
                From = Canvas.GetLeft(PlayerSquare),
                To = targetLeft,
                Duration = TimeSpan.FromMilliseconds(durationMilliseconds),
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
                FillBehavior = FillBehavior.HoldEnd
            };
            DoubleAnimation topAnimation = leftAnimation.Clone();
            topAnimation.From = Canvas.GetTop(PlayerSquare);
            topAnimation.To = targetTop;

            topAnimation.Completed += (_, _) =>
            {
                PlayerSquare.BeginAnimation(Canvas.LeftProperty, null);
                PlayerSquare.BeginAnimation(Canvas.TopProperty, null);
                Canvas.SetLeft(PlayerSquare, targetLeft);
                Canvas.SetTop(PlayerSquare, targetTop);
                completion.TrySetResult();
            };

            PlayerSquare.BeginAnimation(Canvas.LeftProperty, leftAnimation);
            PlayerSquare.BeginAnimation(Canvas.TopProperty, topAnimation);

            return completion.Task;
        }

        private Task<GameIntroLanguage> ShowLanguageChoiceAsync()
        {
            languageChoiceCompletion = new TaskCompletionSource<GameIntroLanguage>();
            choosingLanguage = true;
            highlightedLanguage = GameIntroLanguage.French;
            ApplyLanguageChoiceText();
            UpdateLanguageChoiceHighlight();
            LanguageChoiceOverlay.Visibility = Visibility.Visible;
            FocusIntroInput();
            return languageChoiceCompletion.Task;
        }

        private void ApplyLanguageChoiceText()
        {
            LanguageChoiceTitle.Text = GameIntroScript.LanguageChoiceTitle(highlightedLanguage);
            LanguageChoiceHelpText.Text = GameIntroScript.LanguageChoiceHelp(highlightedLanguage);
            FrenchLanguageText.Text = GameIntroScript.FrenchLabel(highlightedLanguage);
            EnglishLanguageText.Text = GameIntroScript.EnglishLabel(highlightedLanguage);
        }

        private void HandleLanguageChoiceKey(Key key)
        {
            if (key == Key.Left || key == Key.Q || key == Key.A)
            {
                highlightedLanguage = GameIntroLanguage.French;
                UpdateLanguageChoiceHighlight();
                return;
            }

            if (key == Key.Right || key == Key.D)
            {
                highlightedLanguage = GameIntroLanguage.English;
                UpdateLanguageChoiceHighlight();
                return;
            }

            if (key == Key.Enter || key == Key.Space)
                CompleteLanguageChoice(highlightedLanguage);
        }

        private void CompleteLanguageChoice(GameIntroLanguage language)
        {
            if (!choosingLanguage)
                return;

            selectedLanguage = language;
            choosingLanguage = false;
            LanguageChoiceOverlay.Visibility = Visibility.Collapsed;
            languageChoiceCompletion?.TrySetResult(language);
            languageChoiceCompletion = null;
        }

        private void UpdateLanguageChoiceHighlight()
        {
            ApplyLanguageChoiceText();
            FrenchLanguageButton.Opacity = highlightedLanguage == GameIntroLanguage.French ? 1 : 0.56;
            EnglishLanguageButton.Opacity = highlightedLanguage == GameIntroLanguage.English ? 1 : 0.56;
        }

        private void FrenchLanguageButton_MouseEnter(object sender, MouseEventArgs e)
        {
            highlightedLanguage = GameIntroLanguage.French;
            UpdateLanguageChoiceHighlight();
        }

        private void EnglishLanguageButton_MouseEnter(object sender, MouseEventArgs e)
        {
            highlightedLanguage = GameIntroLanguage.English;
            UpdateLanguageChoiceHighlight();
        }

        private void FrenchLanguageButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CompleteLanguageChoice(GameIntroLanguage.French);
            e.Handled = true;
        }

        private void EnglishLanguageButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CompleteLanguageChoice(GameIntroLanguage.English);
            e.Handled = true;
        }

        private void ConfigureRevealOverlay()
        {
            revealHole = new EllipseGeometry(new Point(ViewportWidth / 2.0, ViewportHeight / 2.0), 0, 0);

            GeometryGroup geometry = new() { FillRule = FillRule.EvenOdd };
            geometry.Children.Add(new RectangleGeometry(new Rect(0, 0, ViewportWidth, ViewportHeight)));
            geometry.Children.Add(revealHole);
            RevealOverlay.Data = geometry;
        }

        private void ConfigureTimers()
        {
            keyPulseTimer.Interval = TimeSpan.FromSeconds(1.65);
            keyPulseTimer.Tick += (_, _) => _ = PlayKeyPressAnimationAsync(SpaceKeyPrompt, SpaceKeyText);

            rainbowTimer.Interval = TimeSpan.FromMilliseconds(33);
            rainbowTimer.Tick += (_, _) => UpdateRainbowGlyphs();

            movementTimer.Interval = TimeSpan.FromMilliseconds(16);
            movementTimer.Tick += (_, _) => UpdatePlayerMovement();
        }

        private void StartSpaceKeyAnimation()
        {
            SpaceKeyTranslate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation
            {
                From = 0,
                To = -22,
                Duration = TimeSpan.FromSeconds(0.78),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });

            SpaceKeyRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation
            {
                From = -4,
                To = 5,
                Duration = TimeSpan.FromSeconds(0.92),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });

            SpaceKeySkew.BeginAnimation(SkewTransform.AngleXProperty, new DoubleAnimation
            {
                From = -3.5,
                To = 3.5,
                Duration = TimeSpan.FromSeconds(0.64),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });

            keyPulseTimer.Start();
        }

        private async Task PlayKeyPressAnimationAsync(Border keyBox, TextBlock keyText)
        {
            AnimateKeyColors(keyBox, keyText, inverted: true, 85);
            await Task.Delay(95);
            AnimateKeyColors(keyBox, keyText, inverted: false, 85);
            await Task.Delay(95);
            AnimateKeyColors(keyBox, keyText, inverted: true, 85);
            await Task.Delay(95);
            AnimateKeyColors(keyBox, keyText, inverted: false, 120);
        }

        private static void AnimateKeyColors(Border keyBox, TextBlock keyText, bool inverted, int milliseconds)
        {
            SolidColorBrush background = EnsureBrush(keyBox.Background, inverted ? Colors.White : Colors.Black);
            SolidColorBrush foreground = EnsureBrush(keyText.Foreground, inverted ? Colors.Black : Colors.White);

            keyBox.Background = background;
            keyText.Foreground = foreground;

            background.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation
            {
                To = inverted ? Colors.White : Colors.Black,
                Duration = TimeSpan.FromMilliseconds(milliseconds)
            });

            foreground.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation
            {
                To = inverted ? Colors.Black : Colors.White,
                Duration = TimeSpan.FromMilliseconds(milliseconds)
            });
        }

        private static SolidColorBrush EnsureBrush(Brush brush, Color fallback)
        {
            if (brush is SolidColorBrush solid && !solid.IsFrozen)
                return solid;

            return new SolidColorBrush(fallback);
        }

        private Task PlayRevealAsync()
        {
            TaskCompletionSource completion = new();
            DoubleAnimation radiusAnimation = new()
            {
                From = 0,
                To = 1500,
                Duration = TimeSpan.FromSeconds(1.35),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd
            };

            radiusAnimation.Completed += (_, _) =>
            {
                RevealOverlay.Visibility = Visibility.Collapsed;
                completion.TrySetResult();
            };

            revealHole.BeginAnimation(EllipseGeometry.RadiusXProperty, radiusAnimation);
            revealHole.BeginAnimation(EllipseGeometry.RadiusYProperty, radiusAnimation.Clone());

            return completion.Task;
        }

        private Task FocusCameraAsync(Point target, double scale, int durationMilliseconds, bool centerTarget = true)
        {
            TaskCompletionSource completion = new();
            DoubleAnimation scaleAnimation = CreateCameraAnimation(CameraScale.ScaleX, scale, durationMilliseconds);
            double targetX = centerTarget ? GetCenteredCameraX(target, scale) : GetBoundedCameraX(target, scale);
            double targetY = centerTarget ? GetCenteredCameraY(target, scale) : GetBoundedCameraY(target, scale);
            DoubleAnimation translateXAnimation = CreateCameraAnimation(CameraTranslate.X, targetX, durationMilliseconds);
            DoubleAnimation translateYAnimation = CreateCameraAnimation(CameraTranslate.Y, targetY, durationMilliseconds);

            translateYAnimation.Completed += (_, _) => completion.TrySetResult();

            CameraScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            CameraScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation.Clone());
            CameraTranslate.BeginAnimation(TranslateTransform.XProperty, translateXAnimation);
            CameraTranslate.BeginAnimation(TranslateTransform.YProperty, translateYAnimation);

            return completion.Task;
        }

        private void SetCamera(Point target, double scale, bool animate)
        {
            if (animate)
            {
                _ = FocusCameraAsync(target, scale, 600);
                return;
            }

            CameraScale.ScaleX = scale;
            CameraScale.ScaleY = scale;
            CameraTranslate.X = GetBoundedCameraX(target, scale);
            CameraTranslate.Y = GetBoundedCameraY(target, scale);
        }

        private static DoubleAnimation CreateCameraAnimation(double from, double to, int durationMilliseconds)
        {
            return new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMilliseconds),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
                FillBehavior = FillBehavior.HoldEnd
            };
        }

        private static double GetCenteredCameraX(Point target, double scale)
        {
            return ViewportWidth / 2.0 - target.X * scale;
        }

        private static double GetCenteredCameraY(Point target, double scale)
        {
            return ViewportHeight / 2.0 - target.Y * scale;
        }

        private static double GetBoundedCameraX(Point target, double scale)
        {
            double worldWidth = MapSize * scale;

            if (worldWidth <= ViewportWidth)
                return (ViewportWidth - worldWidth) / 2.0;

            double desired = GetCenteredCameraX(target, scale);
            return Clamp(desired, ViewportWidth - worldWidth, 0);
        }

        private static double GetBoundedCameraY(Point target, double scale)
        {
            double worldHeight = MapSize * scale;

            if (worldHeight <= ViewportHeight)
                return (ViewportHeight - worldHeight) / 2.0;

            double desired = GetCenteredCameraY(target, scale);
            return Clamp(desired, ViewportHeight - worldHeight, 0);
        }

        private async Task ShowDialogueAsync(IEnumerable<DialogueSegment> segments)
        {
            DialogueTextWrap.Children.Clear();
            rainbowGlyphs.Clear();
            DialoguePanel.Visibility = Visibility.Visible;

            foreach (DialogueSegment segment in segments)
            {
                foreach (DialogueWordToken token in SplitDialogueWords(segment.Text))
                {
                    StackPanel wordPanel = CreateWordPanel(token.LeadingSpaces, token.TrailingSpaces);
                    DialogueTextWrap.Children.Add(wordPanel);

                    for (int i = 0; i < token.Word.Length; i++)
                    {
                        char character = token.Word[i];
                        AddDialogueCharacter(wordPanel, character, segment.Style);
                        await Task.Delay(GetDialogueCharacterDelay(segment.Style, token.Word, i));
                    }
                }
            }

            await Task.Delay(DialogueEndPauseMilliseconds);
        }

        private int GetDialogueCharacterDelay(DialogueTextStyle style, string word, int index)
        {
            int baseDelay = style == DialogueTextStyle.Rainbow
                ? RainbowTextDelayMilliseconds
                : NormalTextDelayMilliseconds;

            char character = word[index];

            if (IsRepeatedPunctuationBeforeLastCharacter(word, index))
                return ApplyTextSpeed(baseDelay);

            if (character == ',' || character == ';' || character == ':')
                return ApplyTextSpeed(baseDelay + 180);

            if (character == '.' || character == '!' || character == '?')
                return ApplyTextSpeed(baseDelay + 420);

            return ApplyTextSpeed(baseDelay);
        }

        private int ApplyTextSpeed(int delayMilliseconds)
        {
            double factor = 1.65 - settings.VitesseTexte * 0.012;
            return Math.Max(8, (int)Math.Round(delayMilliseconds * factor));
        }

        private static bool IsRepeatedPunctuationBeforeLastCharacter(string word, int index)
        {
            char character = word[index];

            if (!IsPunctuationWithPause(character))
                return false;

            return index + 1 < word.Length && word[index + 1] == character;
        }

        private static bool IsPunctuationWithPause(char character)
        {
            return character == ',' ||
                   character == ';' ||
                   character == ':' ||
                   character == '.' ||
                   character == '!' ||
                   character == '?';
        }

        private static IEnumerable<DialogueWordToken> SplitDialogueWords(string text)
        {
            int index = 0;

            while (index < text.Length)
            {
                int leadingSpaces = 0;

                while (index < text.Length && char.IsWhiteSpace(text[index]))
                {
                    leadingSpaces++;
                    index++;
                }

                int wordStart = index;

                while (index < text.Length && !char.IsWhiteSpace(text[index]))
                    index++;

                if (wordStart == index)
                    yield break;

                string word = text[wordStart..index];
                int trailingSpaces = 0;

                while (index < text.Length && char.IsWhiteSpace(text[index]))
                {
                    trailingSpaces++;
                    index++;
                }

                yield return new DialogueWordToken(word, leadingSpaces, trailingSpaces);
            }
        }

        private static StackPanel CreateWordPanel(int leadingSpaces, int trailingSpaces)
        {
            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(leadingSpaces * 8, 0, Math.Max(1, trailingSpaces) * 8, 8)
            };
        }

        private void AddDialogueCharacter(Panel wordPanel, char character, DialogueTextStyle style)
        {
            TextBlock textBlock = new()
            {
                Text = character.ToString(),
                FontFamily = (FontFamily)FindResource("PixelFont"),
                FontSize = 27,
                FontWeight = FontWeights.Normal,
                Foreground = Brushes.White,
                LineHeight = 38,
                Margin = new Thickness(0, 0, 1, 0)
            };

            TextOptions.SetTextRenderingMode(textBlock, TextRenderingMode.Aliased);
            TextOptions.SetTextFormattingMode(textBlock, TextFormattingMode.Display);

            if (style == DialogueTextStyle.PanelWord)
            {
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 219, 90));
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.TextDecorations = CreateLowerUnderline(Color.FromRgb(255, 219, 90));
            }
            else if (style == DialogueTextStyle.ControlsWord)
            {
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(122, 242, 255));
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.TextDecorations = CreateLowerUnderline(Color.FromRgb(122, 242, 255));
            }
            else if (style == DialogueTextStyle.Rainbow)
            {
                TranslateTransform translate = new();
                textBlock.RenderTransform = translate;
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.Foreground = new SolidColorBrush(Colors.White);
                rainbowGlyphs.Add(new RainbowGlyph(textBlock, translate, rainbowGlyphs.Count));

                if (!rainbowTimer.IsEnabled)
                    rainbowTimer.Start();
            }

            wordPanel.Children.Add(textBlock);
            PlayDialogueTick();
        }

        private void UpdateRainbowGlyphs()
        {
            double time = (DateTime.UtcNow - animationStartUtc).TotalSeconds;

            foreach (RainbowGlyph glyph in rainbowGlyphs)
            {
                double hue = (time * 115 + glyph.Index * 17) % 360;
                glyph.Text.Foreground = new SolidColorBrush(ColorFromHsv(hue, 0.92, 1));
                glyph.Translate.Y = Math.Sin(time * 5.4 + glyph.Index * 0.52) * 4.5;
            }
        }

        private static TextDecorationCollection CreateLowerUnderline(Color color)
        {
            TextDecoration underline = new()
            {
                Location = TextDecorationLocation.Underline,
                Pen = new Pen(new SolidColorBrush(color), 1.8),
                PenOffset = 6,
                PenOffsetUnit = TextDecorationUnit.Pixel
            };

            return new TextDecorationCollection { underline };
        }

        private void ShowPanelKeyHint()
        {
            ApplyPanelKeyHintText();

            if (!IsPlayerInPanelInteractionRange())
            {
                HidePanelKeyHint();
                return;
            }

            if (PanelKeyHint.Visibility == Visibility.Visible)
                return;

            PanelKeyHint.Visibility = Visibility.Visible;
            PanelHintTranslate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation
            {
                From = 0,
                To = -3.4,
                Duration = TimeSpan.FromSeconds(0.62),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });

            PanelHintRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation
            {
                From = -5,
                To = 5,
                Duration = TimeSpan.FromSeconds(0.88),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });
        }

        private void UpdatePlayerMovement()
        {
            DateTime now = DateTime.UtcNow;
            double deltaSeconds = Math.Min(0.05, (now - lastMovementTickUtc).TotalSeconds);
            lastMovementTickUtc = now;

            double dx = 0;
            double dy = 0;

            if (moveLeft)
                dx -= 1;
            if (moveRight)
                dx += 1;
            if (moveUp)
                dy -= 1;
            if (moveDown)
                dy += 1;

            if (dx == 0 && dy == 0)
                return;

            double length = Math.Sqrt(dx * dx + dy * dy);
            dx /= length;
            dy /= length;

            double left = Canvas.GetLeft(PlayerSquare) + dx * PlayerSpeed * deltaSeconds;
            double top = Canvas.GetTop(PlayerSquare) + dy * PlayerSpeed * deltaSeconds;

            Canvas.SetLeft(PlayerSquare, Clamp(left, 0, MapSize - PlayerSquare.Width));
            Canvas.SetTop(PlayerSquare, Clamp(top, 0, MapSize - PlayerSquare.Height));
            UpdatePanelKeyHintVisibility();
        }

        private void FocusIntroInput()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Focus();
                Keyboard.Focus(this);
            }), DispatcherPriority.Input);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }

        private static double Distance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static Color ColorFromHsv(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value *= 255;
            byte v = Convert.ToByte(value);
            byte p = Convert.ToByte(value * (1 - saturation));
            byte q = Convert.ToByte(value * (1 - f * saturation));
            byte t = Convert.ToByte(value * (1 - (1 - f) * saturation));

            return hi switch
            {
                0 => Color.FromRgb(v, t, p),
                1 => Color.FromRgb(q, v, p),
                2 => Color.FromRgb(p, v, t),
                3 => Color.FromRgb(p, q, v),
                4 => Color.FromRgb(t, p, v),
                _ => Color.FromRgb(v, p, q)
            };
        }

        private sealed record DialogueWordToken(string Word, int LeadingSpaces, int TrailingSpaces);

        private sealed record RainbowGlyph(TextBlock Text, TranslateTransform Translate, int Index);
    }
}
