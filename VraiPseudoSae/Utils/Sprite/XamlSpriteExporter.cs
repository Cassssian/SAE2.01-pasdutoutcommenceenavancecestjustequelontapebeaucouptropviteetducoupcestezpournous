using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VraiPseudoSae.Utils.Sprite
{
    public static class XamlSpriteExporter
    {
        /// <summary>
        /// Rend un Canvas XAML en BitmapImage en mémoire (sans passer par le disque).
        /// </summary>
        public static BitmapImage RenderToBitmapImage(FrameworkElement element, int pixelWidth, int pixelHeight)
        {
            FrameworkElement clone = CloneElement(element, pixelWidth, pixelHeight);
            clone.Measure(new Size(pixelWidth, pixelHeight));
            clone.Arrange(new Rect(0, 0, pixelWidth, pixelHeight));
            clone.UpdateLayout();

            RenderTargetBitmap rtb = new RenderTargetBitmap(
                pixelWidth, pixelHeight,
                96, 96,
                PixelFormats.Pbgra32);
            rtb.Render(clone);

            // Conversion RenderTargetBitmap → BitmapImage (nécessaire pour la DLL)
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using MemoryStream ms = new MemoryStream();
            encoder.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // Obligatoire pour utilisation cross-thread

            return bitmapImage;
        }

        private static FrameworkElement CloneElement(FrameworkElement source, int width, int height)
        {
            Canvas container = new Canvas
            {
                Width = width,
                Height = height,
                Background = Brushes.Transparent
            };

            if (source is Canvas canvas)
            {
                foreach (UIElement child in canvas.Children)
                {
                    UIElement clone = CloneChild(child);
                    double left = Canvas.GetLeft(child);
                    double top = Canvas.GetTop(child);
                    double right = Canvas.GetRight(child);
                    double bottom = Canvas.GetBottom(child);

                    if (!double.IsNaN(left)) Canvas.SetLeft(clone, left);
                    if (!double.IsNaN(top)) Canvas.SetTop(clone, top);
                    if (!double.IsNaN(right)) Canvas.SetRight(clone, right);
                    if (!double.IsNaN(bottom)) Canvas.SetBottom(clone, bottom);

                    container.Children.Add(clone);
                }
            }
            else
            {
                throw new InvalidOperationException("Le sprite source doit être un Canvas.");
            }

            return container;
        }

        private static UIElement CloneChild(UIElement child)
        {
            string xaml = System.Windows.Markup.XamlWriter.Save(child);
            using StringReader stringReader = new StringReader(xaml);
            using System.Xml.XmlReader xmlReader = System.Xml.XmlReader.Create(stringReader);
            return (UIElement)System.Windows.Markup.XamlReader.Load(xmlReader);
        }
    }
}
