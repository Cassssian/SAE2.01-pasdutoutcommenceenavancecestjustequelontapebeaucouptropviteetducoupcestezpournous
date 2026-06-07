using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.VisualBasic.Devices;
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
        private const int NormalTextDelayMilliseconds = 34;
        private const int RainbowTextDelayMilliseconds = 24;
        private const int DialogueEndPauseMilliseconds = 2600;

        private readonly Point panelCenter = new(64, 53);
        private readonly DispatcherTimer keyPulseTimer = new();
        private readonly DispatcherTimer rainbowTimer = new();
        private readonly DispatcherTimer movementTimer = new();
        private readonly List<RainbowGlyph> rainbowGlyphs = new();

        private EllipseGeometry revealHole = null!;
        private TaskCompletionSource<GameIntroLanguage>? languageChoiceCompletion;
        private DateTime animationStartUtc;
        private DateTime lastMovementTickUtc;
        private GameIntroLanguage selectedLanguage = GameIntroLanguage.French;
        private GameIntroLanguage highlightedLanguage = GameIntroLanguage.French;
        private bool waitingForSpace = true;
        private bool choosingLanguage;
        private bool inputsLocked = true;
        private bool movementEnabled;
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

            if (inputsLocked || !movementEnabled)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Z || e.Key == Key.W || e.Key == Key.Up)
                moveUp = true;
            if (e.Key == Key.S || e.Key == Key.Down)
                moveDown = true;
            if (e.Key == Key.Q || e.Key == Key.A || e.Key == Key.Left)
                moveLeft = true;
            if (e.Key == Key.D || e.Key == Key.Right)
                moveRight = true;

            e.Handled = true;
        }

        private void GameIntroWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z || e.Key == Key.W || e.Key == Key.Up)
                moveUp = false;
            if (e.Key == Key.S || e.Key == Key.Down)
                moveDown = false;
            if (e.Key == Key.Q || e.Key == Key.A || e.Key == Key.Left)
                moveLeft = false;
            if (e.Key == Key.D || e.Key == Key.Right)
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
            audio.Play("woosh");
            await FocusCameraAsync(PlayerCenter, FocusScale, 1100);

            foreach (IReadOnlyList<DialogueSegment> block in GameIntroScript.OpeningBlocks(selectedLanguage))
                await ShowDialogueAsync(block);

            await Task.Delay(900);
            DialoguePanel.Visibility = Visibility.Collapsed;
            audio.Play("woosh");
            await FocusCameraAsync(panelCenter, OverviewScale, 900, centerTarget: false);
            inputsLocked = true;
            await Task.Delay(4200);
            audio.Play("woosh");
            await FocusCameraAsync(PlayerCenter, FocusScale, 950);
            await ShowDialogueAsync(GameIntroScript.PrankExplanation(selectedLanguage));

            await Task.Delay(550);
            audio.Play("woosh");
            await FocusCameraAsync(panelCenter, FocusScale, 700);
            await Task.Delay(1000);
            audio.Play("woosh");
            await FocusCameraAsync(PlayerCenter, FocusScale, 700);

            await ShowDialogueAsync(GameIntroScript.MagicPanelReveal(selectedLanguage));
            await Task.Delay(2100);
            await ShowDialogueAsync(GameIntroScript.InteractionHint(selectedLanguage));

            await Task.Delay(600);
            DialoguePanel.Visibility = Visibility.Collapsed;
            audio.Play("woosh");
            await FocusCameraAsync(panelCenter, OverviewScale, 900, centerTarget: false);
            ShowPanelKeyHint();
            movementEnabled = true;
            inputsLocked = false;
            movementTimer.Start();
            //ProgressionJeuSauvegardeDepot.MarquerIntroductionTerminee();
            FocusIntroInput();
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

        private static int GetDialogueCharacterDelay(DialogueTextStyle style, string word, int index)
        {
            int baseDelay = style == DialogueTextStyle.Rainbow
                ? RainbowTextDelayMilliseconds
                : NormalTextDelayMilliseconds;

            char character = word[index];

            if (IsRepeatedPunctuationBeforeLastCharacter(word, index))
                return baseDelay;

            if (character == ',' || character == ';' || character == ':')
                return baseDelay + 180;

            if (character == '.' || character == '!' || character == '?')
                return baseDelay + 420;

            return baseDelay;
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
            audio.Play("bip");
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
