using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using IUTGame.WPF;
using VraiPseudoSae.Utils.AudioPlayer;
using VraiPseudoSae.Utils.SaveManager;
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
        private const double StarInteractionRadius = 10;
        private const double GameTileInteractionRadius = 14;
        private const int CharacterExpressionColumn = 2;
        private const int CharacterExpressionRow = 3;
        private const int NormalTextDelayMilliseconds = 34;
        private const int RainbowTextDelayMilliseconds = 24;
        private const int DialogueEndPauseMilliseconds = 2600;
        private const string ForwardBindingId = "forward";
        private const string BackwardBindingId = "backward";
        private const string LeftBindingId = "left";
        private const string RightBindingId = "right";
        private const string InteractionBindingId = "interaction";

        private readonly Point panelCenter = new(64, 53);
        private readonly Point starCenter = new(29, 34);
        private readonly Point gameTileCenter = new(100, 98);
        private readonly DispatcherTimer keyPulseTimer = new();
        private readonly DispatcherTimer rainbowTimer = new();
        private readonly DispatcherTimer movementTimer = new();
        private readonly DispatcherTimer caseColorTimer = new();
        private readonly List<DialogueEffectGlyph> dialogueEffectGlyphs = new();
        private readonly Dictionary<string, TextBlock> bindingKeyTexts = new();
        private readonly Dictionary<string, Border> bindingKeyBoxes = new();
        private readonly Dictionary<string, string> bindingValues = new();
        private readonly ScaleTransform introStarScale = new(1, 1);
        private readonly RotateTransform introStarRotate = new();
        private readonly TranslateTransform introStarFloat = new();
        private readonly Random random = new();

        private EllipseGeometry revealHole = null!;
        private GameIntroGame? introGame;
        private TaskCompletionSource<GameIntroLanguage>? languageChoiceCompletion;
        private TaskCompletionSource<bool>? dialogueInputCompletion;
        private DateTime animationStartUtc;
        private ParametresJeuSauvegarde settings = ParametresJeuSauvegardeDepot.ChargerOuDefaut();
        private GameIntroLanguage selectedLanguage = GameIntroLanguage.French;
        private GameIntroLanguage highlightedLanguage = GameIntroLanguage.French;
        private SettingsIntroCategory selectedSettingsCategory = SettingsIntroCategory.General;
        private string? listeningBindingId;
        private IntroInteractionStage interactionStage = IntroInteractionStage.Panel;
        private bool waitingForSpace = true;
        private bool choosingLanguage;
        private bool dialogueActive;
        private bool dialogueTyping;
        private bool dialogueRevealRequested;
        private bool dialogueAdvanceRequested;
        private bool inputsLocked = true;
        private bool movementEnabled;
        private bool settingsIntroStarted;
        private bool settingsOverlayVisible;
        private bool settingsTutorialPlaying;
        private bool applyingSettingsToUi;
        private bool postSettingsSceneStarted;
        private bool starApproachDialogueShown;
        private bool tileApproachSceneStarted;
        private int starCount;
        private int caseColorFrame;
        private bool moveUp;
        private bool moveDown;
        private bool moveLeft;
        private bool moveRight;
        private readonly JsonPakAudioService audio = new();

        public GameIntroWindow()
        {
            InitializeComponent();
            ConfigureStarTransforms();
            ConfigureRevealOverlay();
            ConfigureTimers();
            ConfigureCharacterPortrait();
            ConfigureSettingsControls();
            ApplySavedSettingsToControls();
            audio.LoadFromPath("Assets/woosh.mp3", "woosh");
            audio.LoadFromPath("Assets/bip.wav", "bip");
        }

        private Point PlayerCenter => introGame?.PlayerCenter ?? new Point(104, 84);

        private void ConfigureStarTransforms()
        {
            TransformGroup starTransform = new();
            starTransform.Children.Add(introStarScale);
            starTransform.Children.Add(introStarRotate);
            starTransform.Children.Add(introStarFloat);
            IntroStar.RenderTransform = starTransform;
        }

        private void ConfigureCharacterPortrait()
        {
            DialogueSpeakerPortraitImage.Source =
                GameIntroSpriteSheetFactory.CreateExpressionPortrait(CharacterExpressionColumn, CharacterExpressionRow);
        }

        private void StartIntroGame()
        {
            if (introGame is not null)
            {
                introGame.Resume();
                return;
            }

            var screen = new WPFScreen(IntroSpriteCanvas);
            GameIntroPlayerSpriteSet sprites = GameIntroSpriteSheetFactory.Register(screen);
            introGame = new GameIntroGame(screen, sprites);
            introGame.Run();
        }

        private void GameIntroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StartIntroGame();
            animationStartUtc = DateTime.UtcNow;
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
            caseColorTimer.Stop();
            introGame?.Pause();
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

            if (TryHandleDialogueSkip(e))
            {
                e.Handled = true;
                return;
            }

            if (settingsOverlayVisible)
            {
                if (e.Key == Key.Escape)
                    CloseSettingsOverlayTemporarily();

                e.Handled = true;
                return;
            }

            if (movementEnabled && e.Key == InteractionKey)
            {
                if (TryHandleIntroInteraction())
                {
                    e.Handled = true;
                    return;
                }
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

        private bool TryHandleDialogueSkip(KeyEventArgs e)
        {
            if (!dialogueActive || e.Key != Key.Space)
                return false;

            if (!e.IsRepeat)
            {
                if (dialogueTyping && !dialogueRevealRequested)
                    dialogueRevealRequested = true;
                else
                    dialogueAdvanceRequested = true;

                dialogueInputCompletion?.TrySetResult(true);
            }

            return true;
        }

        private bool TryHandleIntroInteraction()
        {
            if (interactionStage == IntroInteractionStage.Panel && IsPlayerInPanelInteractionRange())
            {
                if (!settingsIntroStarted)
                    _ = StartSettingsIntroSceneAsync();
                else
                    OpenSettingsOverlay();

                return true;
            }

            if (interactionStage == IntroInteractionStage.StarReady && IsPlayerInStarInteractionRange())
            {
                _ = StartStarAbsorbSceneAsync();
                return true;
            }

            if (interactionStage == IntroInteractionStage.GameTileReady && IsPlayerInGameTileInteractionRange())
            {
                _ = StartGameTileInteractionSceneAsync();
                return true;
            }

            return false;
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

            DialogueSpeakerPortrait.Visibility = Visibility.Visible;
            foreach (IReadOnlyList<DialogueSegment> block in GameIntroScript.OpeningBlocks(selectedLanguage))
                await ShowDialogueAsync(block);

            await Task.Delay(900);
            DialoguePanel.Visibility = Visibility.Collapsed;
            DialogueSpeakerPortrait.Visibility = Visibility.Collapsed;
            PlaySfx("woosh");
            await FocusCameraAsync(panelCenter, OverviewScale, 900, centerTarget: false);
            inputsLocked = true;
            await Task.Delay(4200);
            PlaySfx("woosh");
            await FocusCameraAsync(PlayerCenter, FocusScale, 950);
            DialogueSpeakerPortrait.Visibility = Visibility.Visible;
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
            DialogueSpeakerPortrait.Visibility = Visibility.Collapsed;
            await EnsurePlayerCloseToPanelAsync();
            PlaySfx("woosh");
            await FocusCameraAsync(panelCenter, OverviewScale, 900, centerTarget: false);
            ShowPanelKeyHint();
            movementEnabled = true;
            inputsLocked = false;
            movementTimer.Start();
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
            UpdateSettingsInteractionAvailability();
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
            UpdateSettingsInteractionAvailability();

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

            if (!postSettingsSceneStarted)
            {
                _ = StartPostSettingsSceneAsync();
                return;
            }

            inputsLocked = false;
            movementEnabled = true;
            movementTimer.Start();
            UpdateInteractionKeyHintVisibility();
            FocusIntroInput();
        }

        private void UpdateSettingsCloseAvailability()
        {
            SettingsCloseButton.Opacity = settingsTutorialPlaying ? 0.35 : 1;
            SettingsTemporaryCloseText.Opacity = settingsTutorialPlaying ? 0.35 : 0.72;
        }

        private void UpdateSettingsInteractionAvailability()
        {
            bool enabled = !settingsTutorialPlaying;

            GeneralCategoryButton.IsHitTestVisible = enabled;
            MainMenuCategoryButton.IsHitTestVisible = enabled;
            SettingsDetailsPanel.IsHitTestVisible = enabled;
            SettingsCloseButton.IsHitTestVisible = enabled;

            ApplySettingsCategoryButtonStates();
            SettingsDetailsPanel.Opacity = enabled ? 1 : 0.72;
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

        private void ApplySettingsIntroText()
        {
            SettingsIntroUiText ui = GameIntroScript.SettingsUi(selectedLanguage);

            SettingsCategoriesTitleText.Text = ui.CategoriesTitle;
            GeneralCategoryText.Text = ui.GeneralCategory;
            MainMenuCategoryText.Text = ui.MainMenuCategory;

            GeneralSettingsTitleText.Text = ui.GeneralTitle;
            MasterVolumeText.Text = ui.MasterVolume;
            MusicVolumeText.Text = ui.MusicVolume;
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
            selectedSettingsCategory = category;
            GeneralSettingsPanel.Visibility = category == SettingsIntroCategory.General ? Visibility.Visible : Visibility.Collapsed;
            MainMenuSettingsPanel.Visibility = category == SettingsIntroCategory.Controls ? Visibility.Visible : Visibility.Collapsed;

            ApplySettingsCategoryButtonStates();
        }

        private void ApplySettingsCategoryButtonStates()
        {
            ApplyCategoryButtonState(GeneralCategoryButton, selectedSettingsCategory == SettingsIntroCategory.General);
            ApplyCategoryButtonState(MainMenuCategoryButton, selectedSettingsCategory == SettingsIntroCategory.Controls);

            if (settingsTutorialPlaying)
            {
                GeneralCategoryButton.Opacity = 0.62;
                MainMenuCategoryButton.Opacity = 0.62;
            }
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
            if (IgnoreSettingsInputDuringTutorial(e))
                return;

            SelectSettingsCategory(SettingsIntroCategory.General);
            e.Handled = true;
        }

        private void MainMenuCategoryButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IgnoreSettingsInputDuringTutorial(e))
                return;

            SelectSettingsCategory(SettingsIntroCategory.Controls);
            e.Handled = true;
        }

        private void ForwardChangeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IgnoreSettingsInputDuringTutorial(e))
                return;

            BeginBindingKeyCapture(ForwardBindingId);
            e.Handled = true;
        }

        private void BackwardChangeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IgnoreSettingsInputDuringTutorial(e))
                return;

            BeginBindingKeyCapture(BackwardBindingId);
            e.Handled = true;
        }

        private void LeftChangeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IgnoreSettingsInputDuringTutorial(e))
                return;

            BeginBindingKeyCapture(LeftBindingId);
            e.Handled = true;
        }

        private void RightChangeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IgnoreSettingsInputDuringTutorial(e))
                return;

            BeginBindingKeyCapture(RightBindingId);
            e.Handled = true;
        }

        private void InteractionChangeButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IgnoreSettingsInputDuringTutorial(e))
                return;

            BeginBindingKeyCapture(InteractionBindingId);
            e.Handled = true;
        }

        private void ForwardResetButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IgnoreSettingsInputDuringTutorial(e))
                return;

            ResetBindingKey(ForwardBindingId);
            e.Handled = true;
        }

        private void BackwardResetButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IgnoreSettingsInputDuringTutorial(e))
                return;

            ResetBindingKey(BackwardBindingId);
            e.Handled = true;
        }

        private void LeftResetButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IgnoreSettingsInputDuringTutorial(e))
                return;

            ResetBindingKey(LeftBindingId);
            e.Handled = true;
        }

        private void RightResetButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IgnoreSettingsInputDuringTutorial(e))
                return;

            ResetBindingKey(RightBindingId);
            e.Handled = true;
        }

        private void InteractionResetButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IgnoreSettingsInputDuringTutorial(e))
                return;

            ResetBindingKey(InteractionBindingId);
            e.Handled = true;
        }

        private void SettingsCloseButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IgnoreSettingsInputDuringTutorial(e))
                return;

            CloseSettingsOverlayTemporarily();
            e.Handled = true;
        }

        private bool IgnoreSettingsInputDuringTutorial(MouseButtonEventArgs e)
        {
            if (!settingsTutorialPlaying)
                return false;

            e.Handled = true;
            FocusIntroInput();
            return true;
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
            MusicVolumeSlider.Value = settings.VolumeMusique;
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
            if (applyingSettingsToUi || settingsTutorialPlaying)
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
            SaveCurrentSettings();
        }

        private void UpdateSettingsValueTexts()
        {
            MasterVolumeValueText.Text = settings.VolumeGeneral + "%";
            MusicVolumeValueText.Text = settings.VolumeMusique + "%";
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
                SettingsIntroHighlightTarget.MusicVolume => MusicVolumeSettingRow,
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
            introGame?.StopPlayerMovement();
        }

        private bool IsPlayerInPanelInteractionRange()
        {
            return Distance(PlayerCenter, panelCenter) <= PanelInteractionRadius;
        }

        private bool IsPlayerInStarInteractionRange()
        {
            return Distance(PlayerCenter, starCenter) <= StarInteractionRadius;
        }

        private bool IsPlayerInGameTileInteractionRange()
        {
            return Distance(PlayerCenter, gameTileCenter) <= GameTileInteractionRadius;
        }

        private void UpdateInteractionKeyHintVisibility()
        {
            if (settingsOverlayVisible || !movementEnabled)
            {
                HidePanelKeyHint();
                return;
            }

            if (interactionStage == IntroInteractionStage.Panel && IsPlayerInPanelInteractionRange())
            {
                ShowInteractionKeyHint(panelCenter, -23);
                return;
            }

            if (interactionStage == IntroInteractionStage.StarReady && IsPlayerInStarInteractionRange())
            {
                ShowInteractionKeyHint(starCenter, -18);
                return;
            }

            if (interactionStage == IntroInteractionStage.GameTileReady && IsPlayerInGameTileInteractionRange())
            {
                ShowInteractionKeyHint(gameTileCenter, -16);
                return;
            }

            HidePanelKeyHint();
        }

        private void UpdatePanelKeyHintVisibility()
        {
            UpdateInteractionKeyHintVisibility();
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
            double targetLeft = Clamp(targetCenter.X - GameIntroPlayer.SpriteWidth / 2.0, 0, MapSize - GameIntroPlayer.SpriteWidth);
            double targetTop = Clamp(targetCenter.Y - GameIntroPlayer.SpriteHeight / 2.0, 0, MapSize - GameIntroPlayer.SpriteHeight);
            double distance = Distance(
                startCenter,
                new Point(targetLeft + GameIntroPlayer.SpriteWidth / 2.0, targetTop + GameIntroPlayer.SpriteHeight / 2.0));
            int durationMilliseconds = (int)Clamp(distance / PlayerSpeed * 1000, 550, 1450);

            return introGame?.MovePlayerToAsync(targetLeft, targetTop, durationMilliseconds) ?? Task.CompletedTask;
        }

        private async Task StartPostSettingsSceneAsync()
        {
            postSettingsSceneStarted = true;
            interactionStage = IntroInteractionStage.PostSettingsScene;
            LockPlayerControls();
            await Task.Delay(260);

            introGame?.FacePlayerDown();
            PlaySfx("woosh");
            await FocusCameraAsync(PlayerCenter, FocusScale, 750);
            DialogueSpeakerPortrait.Visibility = Visibility.Visible;
            await ShowDialogueAsync(GameIntroScript.PostSettingsTourDone(selectedLanguage), 0);
            HideDialogue();

            PlaySfx("woosh");
            Task shakeTask = ShakeCameraAsync(5600, 9.5);
            await FocusCameraAsync(panelCenter, OverviewScale, 420, centerTarget: false);
            Task runTask = introGame?.RunPlayerCirclesAsync(new Point(64, 82), 18, 5, 5200) ?? Task.CompletedTask;
            DialogueSpeakerPortrait.Visibility = Visibility.Visible;
            Task shoutTask = ShowDialogueAsync(GameIntroScript.CenterCabinetShake(selectedLanguage));

            await Task.Delay(3200);
            await DropStarAndCreateTileAsync();
            await Task.WhenAll(shakeTask, runTask, shoutTask);
            introGame?.StopPlayerMovement();
            HideDialogue();

            StartStarIdleAnimation();
            PlaySfx("woosh");
            await FocusCameraAsync(starCenter, FocusScale, 700);
            DialogueSpeakerPortrait.Visibility = Visibility.Visible;
            await ShowDialogueAsync(GameIntroScript.StarAndTileDiscovery(selectedLanguage));

            PlaySfx("woosh");
            await FocusCameraAsync(gameTileCenter, FocusScale, 650);
            await ShowDialogueAsync(GameIntroScript.TileDiscovery(selectedLanguage));
            HideDialogue();

            PlaySfx("woosh");
            await FocusCameraAsync(panelCenter, OverviewScale, 850, centerTarget: false);
            UnlockPlayerControls(IntroInteractionStage.WaitingForStarApproach);
            ProgressionJeuSauvegardeDepot.MarquerIntroductionTerminee();
        }

        private async Task StartStarApproachSceneAsync()
        {
            starApproachDialogueShown = true;
            LockPlayerControls();
            PlaySfx("woosh");
            await FocusCameraAsync(PlayerCenter, FocusScale, 520);
            DialogueSpeakerPortrait.Visibility = Visibility.Visible;
            await ShowDialogueAsync(GameIntroScript.StarApproach(selectedLanguage));
            HideDialogue();
            await FocusCameraAsync(panelCenter, OverviewScale, 520, centerTarget: false);
            UnlockPlayerControls(IntroInteractionStage.StarReady);
        }

        private async Task StartStarAbsorbSceneAsync()
        {
            interactionStage = IntroInteractionStage.StarAbsorbScene;
            LockPlayerControls();
            HidePanelKeyHint();
            StartStarIdleAnimation(430);
            await AnimateStarRiseAsync();

            DialogueSpeakerPortrait.Visibility = Visibility.Visible;
            await ShowDialogueAsync(GameIntroScript.StarInteractionPanic(selectedLanguage));
            HideDialogue();

            await AnimateStarIntoPlayerAsync();
            SetStarCount(1);
            ShowStarHud();

            DialogueSpeakerPortrait.Visibility = Visibility.Visible;
            await ShowDialogueAsync(GameIntroScript.StarAbsorbedPanic(selectedLanguage));
            await ShowDialogueAsync(GameIntroScript.Ellipsis());
            await ShowDialogueAsync(GameIntroScript.WeirdAttraction(selectedLanguage));

            PlaySfx("woosh");
            await FocusCameraAsync(gameTileCenter, FocusScale, 700);
            await Task.Delay(950);
            PlaySfx("woosh");
            await FocusCameraAsync(panelCenter, OverviewScale, 760, centerTarget: false);

            HighlightStarHud();
            await ShowDialogueAsync(GameIntroScript.StarHudExplanation(selectedLanguage));
            HideStarHudHighlight();
            HideDialogue();

            UnlockPlayerControls(IntroInteractionStage.WaitingForGameTileApproach);
        }

        private async Task StartGameTileApproachSceneAsync()
        {
            tileApproachSceneStarted = true;
            LockPlayerControls();
            HidePanelKeyHint();
            PlaySfx("woosh");
            await FocusCameraAsync(PlayerCenter, FocusScale, 650);
            DialogueSpeakerPortrait.Visibility = Visibility.Visible;
            await ShowDialogueAsync(GameIntroScript.BrokenTileBeforeAttack(selectedLanguage));
            HideDialogue();

            bool impactShakeDone = false;
            await (introGame?.PlayPlayerAttackAsync(gameTileCenter, frame =>
            {
                if (frame == 3 && !impactShakeDone)
                {
                    impactShakeDone = true;
                    _ = ShakeCameraAsync(260, 5.2);
                }
            }) ?? Task.CompletedTask);

            for (int i = 0; i < 3; i++)
                await ShowDialogueAsync(GameIntroScript.Ellipsis(), DialogueEndPauseMilliseconds, false);

            Task rantShake = ShakeCameraAsync(5200, 4.8);
            DialogueSpeakerPortrait.Visibility = Visibility.Visible;
            foreach (IReadOnlyList<DialogueSegment> page in GameIntroScript.AttackPainRantPages(selectedLanguage))
                await ShowDialogueAsync(page);
            await rantShake;
            await ShowDialogueAsync(GameIntroScript.Disclaimer(selectedLanguage));
            await ShowDialogueAsync(GameIntroScript.WhatNow(selectedLanguage));
            await ShowDialogueAsync(GameIntroScript.Ellipsis());
            await ShowDialogueAsync(GameIntroScript.RememberStar(selectedLanguage));
            HideDialogue();

            PlaySfx("woosh");
            await FocusCameraAsync(panelCenter, OverviewScale, 650, centerTarget: false);
            UnlockPlayerControls(IntroInteractionStage.GameTileReady);
        }

        private async Task StartGameTileInteractionSceneAsync()
        {
            interactionStage = IntroInteractionStage.GameTileInteractionScene;
            LockPlayerControls();
            HidePanelKeyHint();

            await (introGame?.PlayPlayerInteractAsync(gameTileCenter) ?? Task.CompletedTask);
            await AnimateStarOutOfPlayerToTileAsync();

            DialogueSpeakerPortrait.Visibility = Visibility.Visible;
            await ShowDialogueAsync(GameIntroScript.StarLeavesPanic(selectedLanguage));
            HideDialogue();

            await AnimateStarIntoTileAsync();
            SetStarCount(0);
            StartCaseColorCycle(120);

            DialogueSpeakerPortrait.Visibility = Visibility.Visible;
            await ShowDialogueAsync(GameIntroScript.TileColorPanic(selectedLanguage));
            HideDialogue();

            StartCaseColorCycle(36);
            await Task.Delay(5000);
            ShowFlashbang();
            await Task.Delay(1800);
            DialogueSpeakerPortrait.Visibility = Visibility.Visible;
            await ShowDialogueAsync(GameIntroScript.FlashBlindPanic(selectedLanguage));
            HideDialogue();

            HideFlashbang();
            StopCaseColorCycle(Color.FromRgb(58, 225, 92));
            await Task.Delay(1800);
            DialogueSpeakerPortrait.Visibility = Visibility.Visible;
            await ShowDialogueAsync(GameIntroScript.Ellipsis());
            await Task.Delay(1000);
            await ShowDialogueAsync(GameIntroScript.RepairedApology(selectedLanguage));
            HideDialogue();
            interactionStage = IntroInteractionStage.Done;
            inputsLocked = true;
            movementEnabled = false;
        }

        private void LockPlayerControls()
        {
            inputsLocked = true;
            movementEnabled = false;
            movementTimer.Stop();
            ResetMovementInput();
            HidePanelKeyHint();
        }

        private void UnlockPlayerControls(IntroInteractionStage nextStage)
        {
            interactionStage = nextStage;
            inputsLocked = false;
            movementEnabled = true;
            movementTimer.Start();
            UpdateInteractionKeyHintVisibility();
            FocusIntroInput();
        }

        private void HideDialogue()
        {
            DialoguePanel.Visibility = Visibility.Collapsed;
            DialogueSpeakerPortrait.Visibility = Visibility.Collapsed;
            dialogueEffectGlyphs.Clear();
        }

        private async Task DropStarAndCreateTileAsync()
        {
            IntroStar.Visibility = Visibility.Visible;
            IntroStar.Opacity = 1;
            introStarScale.ScaleX = 1.05;
            introStarScale.ScaleY = 1.05;
            SetStarCenter(new Point(starCenter.X, -9));
            StartStarIdleAnimation(520);

            await AnimateCanvasTopAsync(IntroStar, starCenter.Y - IntroStar.Height / 2.0, 980, new BackEase
            {
                EasingMode = EasingMode.EaseOut,
                Amplitude = 0.35
            });

            SetStarCenter(starCenter);
            CreateGroundParticles(starCenter);
            PlaySfx("bip");
            ShowGameCaseTile();
            await ShakeCameraAsync(360, 4.6);
        }

        private void StartStarIdleAnimation(int spinDurationMilliseconds = 1050)
        {
            introStarRotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation
            {
                From = introStarRotate.Angle,
                To = introStarRotate.Angle + 360,
                Duration = TimeSpan.FromMilliseconds(spinDurationMilliseconds),
                RepeatBehavior = RepeatBehavior.Forever
            });

            introStarFloat.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation
            {
                From = -1.4,
                To = 1.6,
                Duration = TimeSpan.FromMilliseconds(620 + random.Next(0, 190)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });

            introStarFloat.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation
            {
                From = -0.6,
                To = 0.7,
                Duration = TimeSpan.FromMilliseconds(840 + random.Next(0, 240)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });
        }

        private async Task AnimateStarRiseAsync()
        {
            await AnimateStarManualAsync(900, progress =>
            {
                double eased = EaseInOut(progress);
                SetStarCenter(new Point(starCenter.X, Lerp(starCenter.Y, starCenter.Y - 13, eased)));
                introStarScale.ScaleX = 1 + eased * 0.18;
                introStarScale.ScaleY = 1 + eased * 0.18;
                introStarRotate.Angle += 18 + progress * 22;
            });
        }

        private async Task AnimateStarIntoPlayerAsync()
        {
            Point start = new(starCenter.X, starCenter.Y - 13);
            Point target = PlayerCenter;

            await AnimateStarManualAsync(2100, progress =>
            {
                double eased = EaseInOut(progress);
                double orbit = Math.Sin(progress * Math.PI * 6.0) * (9 * (1 - progress));
                Point center = new(
                    Lerp(start.X, target.X, eased) + Math.Cos(progress * Math.PI * 8.0) * orbit,
                    Lerp(start.Y, target.Y, eased) + Math.Sin(progress * Math.PI * 8.0) * orbit);

                SetStarCenter(center);
                double scale = Math.Max(0.05, 1.15 - progress * 1.05);
                introStarScale.ScaleX = scale;
                introStarScale.ScaleY = scale;
                introStarRotate.Angle += 25 + progress * 40;
            });

            IntroStar.Visibility = Visibility.Collapsed;
        }

        private async Task AnimateStarOutOfPlayerToTileAsync()
        {
            IntroStar.Visibility = Visibility.Visible;
            IntroStar.Opacity = 1;
            Point start = PlayerCenter;
            Point target = new(gameTileCenter.X, gameTileCenter.Y - 12);

            await AnimateStarManualAsync(1200, progress =>
            {
                double eased = EaseInOut(progress);
                SetStarCenter(new Point(
                    Lerp(start.X, target.X, eased),
                    Lerp(start.Y, target.Y - Math.Sin(progress * Math.PI) * 10, eased)));
                double scale = Lerp(0.1, 1.05, eased);
                introStarScale.ScaleX = scale;
                introStarScale.ScaleY = scale;
                introStarRotate.Angle += 22 + progress * 24;
            });

            StartStarIdleAnimation(420);
        }

        private async Task AnimateStarIntoTileAsync()
        {
            Point start = new(gameTileCenter.X, gameTileCenter.Y - 12);

            await AnimateStarManualAsync(1900, progress =>
            {
                double eased = EaseInOut(progress);
                double radius = 7 * (1 - progress);
                Point center = new(
                    Lerp(start.X, gameTileCenter.X, eased) + Math.Cos(progress * Math.PI * 10.0) * radius,
                    Lerp(start.Y, gameTileCenter.Y, eased) + Math.Sin(progress * Math.PI * 10.0) * radius);
                SetStarCenter(center);
                double scale = Math.Max(0.04, 1.05 - progress);
                introStarScale.ScaleX = scale;
                introStarScale.ScaleY = scale;
                introStarRotate.Angle += 28 + progress * 58;
            });

            IntroStar.Visibility = Visibility.Collapsed;
        }

        private async Task AnimateStarManualAsync(int durationMilliseconds, Action<double> update)
        {
            introStarFloat.BeginAnimation(TranslateTransform.XProperty, null);
            introStarFloat.BeginAnimation(TranslateTransform.YProperty, null);
            introStarRotate.BeginAnimation(RotateTransform.AngleProperty, null);
            introStarFloat.X = 0;
            introStarFloat.Y = 0;

            Stopwatch stopwatch = Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < durationMilliseconds)
            {
                double progress = Clamp(stopwatch.ElapsedMilliseconds / (double)durationMilliseconds, 0, 1);
                update(progress);
                await Task.Delay(16);
            }

            update(1);
        }

        private Task AnimateCanvasTopAsync(FrameworkElement element, double to, int durationMilliseconds, IEasingFunction? easing = null)
        {
            TaskCompletionSource completion = new();
            DoubleAnimation animation = new()
            {
                From = Canvas.GetTop(element),
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMilliseconds),
                EasingFunction = easing,
                FillBehavior = FillBehavior.HoldEnd
            };

            animation.Completed += (_, _) =>
            {
                element.BeginAnimation(Canvas.TopProperty, null);
                Canvas.SetTop(element, to);
                completion.TrySetResult();
            };

            element.BeginAnimation(Canvas.TopProperty, animation);
            return completion.Task;
        }

        private async Task ShakeCameraAsync(int durationMilliseconds, double intensity)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < durationMilliseconds)
            {
                double progress = stopwatch.ElapsedMilliseconds / (double)durationMilliseconds;
                double currentIntensity = intensity * (1 - progress * 0.45);
                CameraShakeTranslate.X = (random.NextDouble() * 2 - 1) * currentIntensity;
                CameraShakeTranslate.Y = (random.NextDouble() * 2 - 1) * currentIntensity;
                await Task.Delay(34);
            }

            CameraShakeTranslate.X = 0;
            CameraShakeTranslate.Y = 0;
        }

        private void SetStarCenter(Point center)
        {
            Canvas.SetLeft(IntroStar, center.X - IntroStar.Width / 2.0);
            Canvas.SetTop(IntroStar, center.Y - IntroStar.Height / 2.0);
        }

        private void ShowGameCaseTile()
        {
            Canvas.SetLeft(GameCaseTile, gameTileCenter.X - GameCaseTile.Width / 2.0);
            Canvas.SetTop(GameCaseTile, gameTileCenter.Y - GameCaseTile.Height / 2.0);
            GameCaseTile.Visibility = Visibility.Visible;
            GameCaseTile.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(260)
            });
        }

        private void CreateGroundParticles(Point origin)
        {
            Color[] colors =
            [
                Color.FromRgb(62, 48, 35),
                Color.FromRgb(82, 64, 43),
                Color.FromRgb(38, 37, 32)
            ];

            for (int i = 0; i < 18; i++)
            {
                double size = 0.8 + random.NextDouble() * 1.25;
                Ellipse particle = new()
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(colors[random.Next(colors.Length)]),
                    Opacity = 0.95
                };

                IntroEffectsCanvas.Children.Add(particle);
                Canvas.SetLeft(particle, origin.X - size / 2.0);
                Canvas.SetTop(particle, origin.Y - size / 2.0);

                double xTarget = origin.X + random.NextDouble() * 14 - 7;
                double yApex = origin.Y - 4 - random.NextDouble() * 7;
                double yTarget = origin.Y + 2 + random.NextDouble() * 2;
                int duration = 520 + random.Next(0, 260);

                DoubleAnimation xAnimation = new()
                {
                    To = xTarget,
                    Duration = TimeSpan.FromMilliseconds(duration),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                DoubleAnimationUsingKeyFrames yAnimation = new();
                yAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(yApex, KeyTime.FromPercent(0.42), new CubicEase { EasingMode = EasingMode.EaseOut }));
                yAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(yTarget, KeyTime.FromPercent(1), new CubicEase { EasingMode = EasingMode.EaseIn }));
                yAnimation.Duration = TimeSpan.FromMilliseconds(duration);

                DoubleAnimation opacityAnimation = new()
                {
                    To = 0,
                    BeginTime = TimeSpan.FromMilliseconds(duration * 0.55),
                    Duration = TimeSpan.FromMilliseconds(duration * 0.45)
                };

                opacityAnimation.Completed += (_, _) => IntroEffectsCanvas.Children.Remove(particle);

                particle.BeginAnimation(Canvas.LeftProperty, xAnimation);
                particle.BeginAnimation(Canvas.TopProperty, yAnimation);
                particle.BeginAnimation(OpacityProperty, opacityAnimation);
            }
        }

        private void ShowStarHud()
        {
            StarHud.Visibility = Visibility.Visible;
            StarHud.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(280)
            });
        }

        private void SetStarCount(int value)
        {
            starCount = Math.Max(0, value);
            StarHudCountText.Text = starCount.ToString();
        }

        private void HighlightStarHud()
        {
            StarHud.UpdateLayout();
            Point topLeft = StarHud.TranslatePoint(new Point(-8, -8), Root);
            Canvas.SetLeft(StarHudHighlight, topLeft.X);
            Canvas.SetTop(StarHudHighlight, topLeft.Y);
            StarHudHighlight.Width = StarHud.ActualWidth + 16;
            StarHudHighlight.Height = StarHud.ActualHeight + 16;
            StarHudHighlight.Visibility = Visibility.Visible;
            StarHudHighlight.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(180)
            });
        }

        private void HideStarHudHighlight()
        {
            StarHudHighlight.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(180)
            });
            StarHudHighlight.Visibility = Visibility.Collapsed;
        }

        private void StartCaseColorCycle(int intervalMilliseconds)
        {
            caseColorTimer.Interval = TimeSpan.FromMilliseconds(intervalMilliseconds);
            caseColorTimer.Start();
        }

        private void TickCaseColorCycle()
        {
            caseColorFrame++;
            Color color = ColorFromHsv((caseColorFrame * 28) % 360, 0.9, 1);
            GameCaseTile.Background = new SolidColorBrush(color);
            GameCaseTile.BorderBrush = new SolidColorBrush(ColorFromHsv((caseColorFrame * 28 + 120) % 360, 0.9, 0.55));
        }

        private void StopCaseColorCycle(Color finalColor)
        {
            caseColorTimer.Stop();
            GameCaseTile.Background = new SolidColorBrush(finalColor);
            GameCaseTile.BorderBrush = new SolidColorBrush(Color.FromRgb(20, 92, 35));
        }

        private void ShowFlashbang()
        {
            FlashbangOverlay.Visibility = Visibility.Visible;
            FlashbangOverlay.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(120)
            });
        }

        private void HideFlashbang()
        {
            FlashbangOverlay.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(520)
            });
            FlashbangOverlay.Visibility = Visibility.Collapsed;
        }

        private static double Lerp(double start, double end, double progress) => start + (end - start) * progress;

        private static double EaseInOut(double progress)
        {
            progress = Clamp(progress, 0, 1);
            return progress < 0.5
                ? 2 * progress * progress
                : 1 - Math.Pow(-2 * progress + 2, 2) / 2;
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
            rainbowTimer.Tick += (_, _) => UpdateDialogueEffectGlyphs();

            movementTimer.Interval = TimeSpan.FromMilliseconds(16);
            movementTimer.Tick += (_, _) => UpdatePlayerMovement();

            caseColorTimer.Interval = TimeSpan.FromMilliseconds(150);
            caseColorTimer.Tick += (_, _) => TickCaseColorCycle();
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

        private async Task ShowDialogueAsync(
            IEnumerable<DialogueSegment> segments,
            int endPauseMilliseconds = DialogueEndPauseMilliseconds, bool withPunctuationWait = true)
        {
            List<DialogueSegment> segmentList = segments.ToList();
            DialogueTextWrap.Children.Clear();
            dialogueEffectGlyphs.Clear();
            DialoguePanel.Visibility = Visibility.Visible;
            dialogueActive = true;
            dialogueTyping = true;
            dialogueRevealRequested = false;
            dialogueAdvanceRequested = false;

            try
            {
                for (int segmentIndex = 0; segmentIndex < segmentList.Count; segmentIndex++)
                {
                    DialogueSegment segment = segmentList[segmentIndex];
                    List<DialogueWordToken> tokens = SplitDialogueWords(segment.Text).ToList();

                    for (int tokenIndex = 0; tokenIndex < tokens.Count; tokenIndex++)
                    {
                        DialogueWordToken token = tokens[tokenIndex];
                        StackPanel wordPanel = CreateWordPanel(token.LeadingSpaces, token.TrailingSpaces);
                        DialogueTextWrap.Children.Add(wordPanel);

                        for (int i = 0; i < token.Word.Length; i++)
                        {
                            bool instantReveal = dialogueRevealRequested || dialogueAdvanceRequested;
                            char character = token.Word[i];
                            AddDialogueCharacter(wordPanel, character, segment.Style, !instantReveal);

                            bool isLastCharacter =
                                segmentIndex == segmentList.Count - 1 &&
                                tokenIndex == tokens.Count - 1 &&
                                i == token.Word.Length - 1;

                            if (((!isLastCharacter || endPauseMilliseconds > 0) && !instantReveal) && withPunctuationWait)
                            {
                                int delay = GetDialogueCharacterDelay(segment.Style, token.Word, i);
                                await WaitDialogueDelayAsync(delay, revealSkipsDelay: true);
                            }
                        }
                    }
                }

                dialogueTyping = false;
                dialogueRevealRequested = true;

                if (endPauseMilliseconds > 0)
                    await WaitDialogueDelayAsync(endPauseMilliseconds, revealSkipsDelay: false);
            }
            finally
            {
                dialogueInputCompletion?.TrySetResult(true);
                dialogueInputCompletion = null;
                dialogueActive = false;
                dialogueTyping = false;
                dialogueRevealRequested = false;
                dialogueAdvanceRequested = false;
            }
        }

        private async Task WaitDialogueDelayAsync(int delayMilliseconds, bool revealSkipsDelay)
        {
            if (delayMilliseconds <= 0 || dialogueAdvanceRequested)
                return;

            if (revealSkipsDelay && dialogueRevealRequested)
                return;

            TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            dialogueInputCompletion = completion;
            Task delayTask = Task.Delay(delayMilliseconds);
            await Task.WhenAny(delayTask, completion.Task);

            if (ReferenceEquals(dialogueInputCompletion, completion))
                dialogueInputCompletion = null;
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

        private void AddDialogueCharacter(
            Panel wordPanel,
            char character,
            DialogueTextStyle style,
            bool playTick = true)
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
            else if (style == DialogueTextStyle.StarWord)
            {
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(246, 188, 59));
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.TextDecorations = CreateLowerUnderline(Color.FromRgb(246, 188, 59));
                RegisterDialogueEffect(textBlock, style);
            }
            else if (style == DialogueTextStyle.TileWord)
            {
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(104, 255, 132));
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.TextDecorations = CreateLowerUnderline(Color.FromRgb(104, 255, 132));
            }
            else if (style == DialogueTextStyle.AccessWord)
            {
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 155, 47));
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.TextDecorations = CreateLowerUnderline(Color.FromRgb(255, 155, 47));
            }
            else if (style == DialogueTextStyle.StarCount)
            {
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(246, 188, 59));
                textBlock.FontWeight = FontWeights.Bold;
                RegisterDialogueEffect(textBlock, style);
            }
            else if (style == DialogueTextStyle.Italic)
            {
                textBlock.FontStyle = FontStyles.Italic;
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(210, 218, 230));
            }
            else if (style == DialogueTextStyle.Shake)
            {
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 96, 96));
                textBlock.FontWeight = FontWeights.Bold;
                RegisterDialogueEffect(textBlock, style);
            }
            else if (style == DialogueTextStyle.Rainbow)
            {
                textBlock.FontWeight = FontWeights.Bold;
                textBlock.Foreground = new SolidColorBrush(Colors.White);
                RegisterDialogueEffect(textBlock, style);
            }

            wordPanel.Children.Add(textBlock);

            if (playTick)
                PlayDialogueTick();
        }

        private void RegisterDialogueEffect(TextBlock textBlock, DialogueTextStyle style)
        {
            TranslateTransform translate = new();
            textBlock.RenderTransform = translate;
            dialogueEffectGlyphs.Add(new DialogueEffectGlyph(textBlock, translate, dialogueEffectGlyphs.Count, style));

            if (!rainbowTimer.IsEnabled)
                rainbowTimer.Start();
        }

        private void UpdateDialogueEffectGlyphs()
        {
            double time = (DateTime.UtcNow - animationStartUtc).TotalSeconds;

            foreach (DialogueEffectGlyph glyph in dialogueEffectGlyphs)
            {
                if (glyph.Style == DialogueTextStyle.Rainbow)
                {
                    double hue = (time * 115 + glyph.Index * 17) % 360;
                    glyph.Text.Foreground = new SolidColorBrush(ColorFromHsv(hue, 0.92, 1));
                    glyph.Translate.Y = Math.Sin(time * 5.4 + glyph.Index * 0.52) * 4.5;
                }
                else if (glyph.Style == DialogueTextStyle.StarWord || glyph.Style == DialogueTextStyle.StarCount)
                {
                    double value = 0.82 + Math.Sin(time * 5.2 + glyph.Index * 0.36) * 0.18;
                    glyph.Text.Foreground = new SolidColorBrush(Color.FromRgb(
                        246,
                        (byte)Math.Round(178 + value * 30),
                        59));
                    glyph.Translate.Y = Math.Sin(time * 5.8 + glyph.Index * 0.44) * 3.4;
                }
                else if (glyph.Style == DialogueTextStyle.Shake)
                {
                    glyph.Translate.X = Math.Sin(time * 17.5 + glyph.Index * 1.7) * 2.8;
                    glyph.Translate.Y = Math.Sin(time * 23.0 + glyph.Index * 0.9) * 0.7;
                }
            }
        }

        private static TextDecorationCollection CreateLowerUnderline(Color color)
        {
            TextDecoration underline = new()
            {
                Location = TextDecorationLocation.Underline,
                Pen = new Pen(new SolidColorBrush(color), 1.8),
                PenOffset = 10,
                PenOffsetUnit = TextDecorationUnit.Pixel
            };

            return [underline];
        }

        private void ShowPanelKeyHint()
        {
            ShowInteractionKeyHint(panelCenter, -23);
        }

        private void ShowInteractionKeyHint(Point center, double topOffset)
        {
            ApplyPanelKeyHintText();

            Canvas.SetLeft(PanelKeyHint, center.X - PanelKeyHint.Width / 2.0);
            Canvas.SetTop(PanelKeyHint, center.Y + topOffset);

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
            {
                introGame?.StopPlayerMovement();
                UpdateInteractionKeyHintVisibility();
                CheckIntroProximityTriggers();
                return;
            }

            introGame?.SetPlayerInput(dx, dy);
            UpdateInteractionKeyHintVisibility();
            CheckIntroProximityTriggers();
        }

        private void CheckIntroProximityTriggers()
        {
            if (interactionStage == IntroInteractionStage.WaitingForStarApproach &&
                !starApproachDialogueShown &&
                IsPlayerInStarInteractionRange())
            {
                _ = StartStarApproachSceneAsync();
            }
            else if (interactionStage == IntroInteractionStage.WaitingForGameTileApproach &&
                     !tileApproachSceneStarted &&
                     IsPlayerInGameTileInteractionRange())
            {
                _ = StartGameTileApproachSceneAsync();
            }
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

        private sealed record DialogueEffectGlyph(
            TextBlock Text,
            TranslateTransform Translate,
            int Index,
            DialogueTextStyle Style);

        private enum IntroInteractionStage
        {
            Panel,
            PostSettingsScene,
            WaitingForStarApproach,
            StarReady,
            StarAbsorbScene,
            WaitingForGameTileApproach,
            GameTileReady,
            GameTileInteractionScene,
            Done
        }
    }
}

