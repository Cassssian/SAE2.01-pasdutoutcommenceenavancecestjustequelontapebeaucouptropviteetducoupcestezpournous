using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VraiPseudoSae.view.intro
{
    public partial class IntroWindow : UserControl
    {
        private const string DecorationAssetBaseUri = "pack://application:,,,/view/intro/elements/";
        private bool transitionStarted;
        public event EventHandler? StartRequested;

        public IntroWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CreateBackgroundStars();
            CreateFloatingDecorations();
            StartIntroAnimation();
            Focus();
            Keyboard.Focus(this);
            _ = AutoStartNextScreenAsync();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                BeginTransitionToHome();
                e.Handled = true;
            }
        }

        private async Task AutoStartNextScreenAsync()
        {
            await Task.Delay(5200);
            BeginTransitionToHome();
        }

        private void CreateBackgroundStars()
        {
            Random random = new(201);

            for (int i = 0; i < 34; i++)
            {
                double size = random.NextDouble() * 2.8 + 1.4;
                Ellipse star = new()
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(Color.FromArgb(210, 129, 255, 202)),
                    Opacity = random.NextDouble() * 0.42 + 0.12,
                    Effect = new BlurEffect { Radius = 0.8 }
                };

                Canvas.SetLeft(star, random.NextDouble() * ActualWidth);
                Canvas.SetTop(star, random.NextDouble() * ActualHeight);
                BackgroundCanvas.Children.Add(star);

                DoubleAnimation twinkle = new()
                {
                    From = star.Opacity,
                    To = Math.Min(0.9, star.Opacity + 0.38),
                    Duration = TimeSpan.FromSeconds(random.NextDouble() * 1.8 + 1.2),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    BeginTime = TimeSpan.FromSeconds(random.NextDouble() * 1.4),
                    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
                };
                star.BeginAnimation(OpacityProperty, twinkle);
            }
        }

        private void CreateFloatingDecorations()
        {
            AddImageDecoration("left_cyan_dot.png", 21, 115, 18, 19, -66, 10, 0, -3, 5, 0, 2.2, 0.54);
            AddImageDecoration("left_green_dots.png", 0, 132, 13, 22, -70, 28, 0, -3, 4, 0, 2.0, 0.7);
            AddImageDecoration("left_triangle.png", 15, 143, 40, 42, -88, 44, 16, 4, -5, 7, 2.8, 0.42);

            AddImageDecoration("top_left_blue_hex.png", 188, 25, 39, 39, -38, -78, -24, -4, -6, 8, 2.6, 0.2);
            AddImageDecoration("center_green_cube.png", 292, 34, 45, 45, 0, -92, 22, 4, -7, 7, 2.9, 0.32);

            AddImageDecoration("top_right_blue_dot.png", 480, 0, 20, 18, 54, -82, 20, 3, -4, 6, 2.4, 0.5);
            AddImageDecoration("top_right_green_hex.png", 503, 19, 31, 33, 72, -72, -18, -4, 5, 6, 2.7, 0.46);
            AddImageDecoration("top_right_green_dot.png", 531, 40, 22, 22, 86, -48, 0, 3, -4, 0, 2.1, 0.62);
            AddImageDecoration("right_green_triangle.png", 548, 48, 31, 41, 96, -36, -16, -4, -5, 7, 2.8, 0.58);
            AddImageDecoration("right_blue_shard_top.png", 565, 78, 14, 24, 88, -18, -12, 2, -5, 6, 2.3, 0.82);
            AddImageDecoration("right_blue_shard_bottom.png", 568, 102, 11, 24, 92, 10, 24, -2, 4, 6, 2.2, 0.96);
            AddImageDecoration("right_bottom_blue_dot.png", 543, 154, 22, 22, 82, 44, 0, -4, 4, 0, 2.1, 0.78);

            AddImageDecoration("bottom_green_hex.png", 174, 185, 38, 38, -54, 70, -24, 5, 4, 8, 3.0, 0.66);
            AddImageDecoration("bottom_green_dots.png", 202, 190, 25, 38, -34, 76, 0, -3, 5, 0, 2.3, 0.84);
        }

        private void AddImageDecoration(
            string fileName,
            double left,
            double top,
            double width,
            double height,
            double entryX,
            double entryY,
            double entryAngle,
            double floatX,
            double floatY,
            double rotateDelta,
            double floatDuration,
            double delay)
        {
            Image image = new()
            {
                Source = new BitmapImage(new Uri($"{DecorationAssetBaseUri}{fileName}", UriKind.Absolute)),
                Width = width,
                Height = height,
                Stretch = Stretch.Fill
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            AddFloatingElement(image, left, top, entryX, entryY, entryAngle, floatX, floatY, rotateDelta, floatDuration, delay);
        }

        private void AddFloatingElement(
            UIElement element,
            double left,
            double top,
            double entryX,
            double entryY,
            double entryAngle,
            double floatX,
            double floatY,
            double rotateDelta,
            double floatDuration,
            double delay,
            double finalAngle = 0)
        {
            ScaleTransform scale = new(0.52, 0.52);
            RotateTransform rotate = new(entryAngle);
            TranslateTransform translate = new(entryX, entryY);
            TransformGroup transformGroup = new();
            transformGroup.Children.Add(scale);
            transformGroup.Children.Add(rotate);
            transformGroup.Children.Add(translate);

            element.Opacity = 0;
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            element.RenderTransform = transformGroup;
            element.Effect = new DropShadowEffect
            {
                Color = Color.FromRgb(98, 232, 255),
                BlurRadius = 10,
                ShadowDepth = 0,
                Opacity = 0.5
            };

            Canvas.SetLeft(element, left);
            Canvas.SetTop(element, top);
            DecorationLayer.Children.Add(element);

            IEasingFunction entryEase = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.45 };
            TimeSpan duration = TimeSpan.FromSeconds(0.82);
            TimeSpan beginTime = TimeSpan.FromSeconds(delay);

            translate.BeginAnimation(TranslateTransform.XProperty, CreateAnimation(entryX, 0, duration, beginTime, entryEase));
            rotate.BeginAnimation(RotateTransform.AngleProperty, CreateAnimation(entryAngle, finalAngle, duration, beginTime, entryEase));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, CreateAnimation(0.52, 1, duration, beginTime, entryEase));
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, CreateAnimation(0.52, 1, duration, beginTime, entryEase));
            element.BeginAnimation(OpacityProperty, CreateAnimation(0, 1, TimeSpan.FromSeconds(0.45), beginTime, null));

            DoubleAnimation entryYAnimation = CreateAnimation(entryY, 0, duration, beginTime, entryEase);
            entryYAnimation.Completed += (_, _) =>
            {
                translate.BeginAnimation(TranslateTransform.XProperty, null);
                translate.BeginAnimation(TranslateTransform.YProperty, null);
                rotate.BeginAnimation(RotateTransform.AngleProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);

                translate.X = 0;
                translate.Y = 0;
                rotate.Angle = finalAngle;
                scale.ScaleX = 1;
                scale.ScaleY = 1;

                StartFloatingLoop(translate, rotate, finalAngle, floatX, floatY, rotateDelta, floatDuration);
            };
            translate.BeginAnimation(TranslateTransform.YProperty, entryYAnimation);
        }

        private static void StartFloatingLoop( 
            TranslateTransform translate,
            RotateTransform rotate,
            double finalAngle,
            double floatX,
            double floatY,
            double rotateDelta,
            double durationSeconds)
        {
            IEasingFunction floatEase = new SineEase { EasingMode = EasingMode.EaseInOut };

            translate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation
            {
                From = 0,
                To = floatX,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = floatEase
            });

            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation
            {
                From = 0,
                To = floatY,
                Duration = TimeSpan.FromSeconds(durationSeconds * 0.86),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = floatEase
            });

            if (rotateDelta > 0)
            {
                rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation
                {
                    From = finalAngle - rotateDelta,
                    To = finalAngle + rotateDelta,
                    Duration = TimeSpan.FromSeconds(durationSeconds * 1.18),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever,
                    EasingFunction = floatEase
                });
            }
        }

        private void StartIntroAnimation()
        {
            IEasingFunction smoothOut = new CubicEase { EasingMode = EasingMode.EaseOut };
            IEasingFunction elasticOut = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.32 };

            BackdropGlow.BeginAnimation(OpacityProperty, CreateAnimation(0, 1, TimeSpan.FromSeconds(1.0), TimeSpan.Zero, smoothOut));
            LogoCanvas.BeginAnimation(OpacityProperty, CreateAnimation(0, 1, TimeSpan.FromSeconds(0.82), TimeSpan.FromSeconds(0.12), smoothOut));
            LogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, CreateAnimation(0.78, 1, TimeSpan.FromSeconds(0.9), TimeSpan.FromSeconds(0.12), elasticOut));
            LogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, CreateAnimation(0.78, 1, TimeSpan.FromSeconds(0.9), TimeSpan.FromSeconds(0.12), elasticOut));
            LogoTranslate.BeginAnimation(TranslateTransform.YProperty, CreateAnimation(26, 0, TimeSpan.FromSeconds(0.9), TimeSpan.FromSeconds(0.12), elasticOut));

            AuthorsPanel.BeginAnimation(OpacityProperty, CreateAnimation(0, 1, TimeSpan.FromSeconds(0.7), TimeSpan.FromSeconds(1.45), smoothOut));
            AuthorsTranslate.BeginAnimation(TranslateTransform.YProperty, CreateAnimation(16, 0, TimeSpan.FromSeconds(0.7), TimeSpan.FromSeconds(1.45), smoothOut));
            PressPromptPanel.BeginAnimation(OpacityProperty, CreateAnimation(0, 1, TimeSpan.FromSeconds(0.65), TimeSpan.FromSeconds(2.05), smoothOut));
            PressPromptTranslate.BeginAnimation(TranslateTransform.YProperty, CreateAnimation(12, 0, TimeSpan.FromSeconds(0.65), TimeSpan.FromSeconds(2.05), smoothOut));

            PressPromptText.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                From = 0.46,
                To = 1,
                BeginTime = TimeSpan.FromSeconds(2.8),
                Duration = TimeSpan.FromSeconds(0.78),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });

            DoubleAnimation shineMove = CreateAnimation(0, 820, TimeSpan.FromSeconds(0.94), TimeSpan.FromSeconds(1.14), new QuadraticEase { EasingMode = EasingMode.EaseInOut });
            LogoShineTranslate.BeginAnimation(TranslateTransform.XProperty, shineMove);

            DoubleAnimationUsingKeyFrames shineOpacity = new()
            {
                BeginTime = TimeSpan.FromSeconds(1.14),
                Duration = TimeSpan.FromSeconds(0.94)
            };
            shineOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(0)));
            shineOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0.62, KeyTime.FromPercent(0.42)));
            shineOpacity.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1)));
            LogoShine.BeginAnimation(OpacityProperty, shineOpacity);
        }

        private void BeginTransitionToHome()
        {
            if (transitionStarted)
            {
                return;
            }

            transitionStarted = true;

            DoubleAnimation fadeAnimation = CreateAnimation(0, 1, TimeSpan.FromSeconds(0.48), TimeSpan.Zero, new QuadraticEase { EasingMode = EasingMode.EaseIn });
            fadeAnimation.Completed += (_, _) => OpenHomePage();
            ExitFade.BeginAnimation(OpacityProperty, fadeAnimation);
        }

        private void OpenHomePage()
        {
            StartRequested?.Invoke(this, EventArgs.Empty);
        }

        private static DoubleAnimation CreateAnimation(
            double from,
            double to,
            TimeSpan duration,
            TimeSpan beginTime,
            IEasingFunction? easingFunction)
        {
            return new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                BeginTime = beginTime,
                FillBehavior = FillBehavior.HoldEnd,
                EasingFunction = easingFunction
            };
        }
    }
}
