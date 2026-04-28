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
        public static void ExportIfMissing(FrameworkElement element, string outputPath, int pixelWidth, int pixelHeight)
        {
            if (File.Exists(outputPath))
                return;

            Export(element, outputPath, pixelWidth, pixelHeight);
        }

        public static void ExportOverwrite(FrameworkElement element, string outputPath, int pixelWidth, int pixelHeight)
        {
            Export(element, outputPath, pixelWidth, pixelHeight);
        }

        private static void Export(FrameworkElement element, string outputPath, int pixelWidth, int pixelHeight)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            FrameworkElement visualToRender = CloneElement(element, pixelWidth, pixelHeight);
            visualToRender.Measure(new Size(pixelWidth, pixelHeight));
            visualToRender.Arrange(new Rect(0, 0, pixelWidth, pixelHeight));
            visualToRender.UpdateLayout();

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                pixelWidth,
                pixelHeight,
                96,
                96,
                PixelFormats.Pbgra32);

            renderBitmap.Render(visualToRender);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            encoder.Save(fileStream);
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