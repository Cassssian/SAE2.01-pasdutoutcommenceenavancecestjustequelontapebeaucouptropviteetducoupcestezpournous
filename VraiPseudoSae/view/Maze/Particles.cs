using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;

namespace VraiPseudoSae.view;

public class Particle
{
    public double x, y;
    public double vx, vy;
    public int lifetime;
    public int maxLifetime;
    public Color color;
}

public class Particles
{
    private Color color;
    private int duration;
    private int count;
    private List<Particle> particlesList = new List<Particle>();
    private int timer;
    private readonly Random rng = new Random();
    private Color[] palette = new Color[12];

    public bool IsFinished => particlesList.Count == 0;

    public Particles(int x, int y, Color color, int duration = 30, int count = 40)
    {
        this.color = color;
        this.duration = duration;
        this.count = count;

        GeneratePalette();

        for (int i = 0; i < count; i++)
        {
            double angle = rng.NextDouble() * Math.PI * 2;
            double speed = 0.5 + rng.NextDouble() * 3.5;
            double vx = speed * Math.Cos(angle);
            double vy = speed * Math.Sin(angle);
            int lifetime = rng.Next(20, 50);

            particlesList.Add(new Particle
            {
                x = x, y = y,
                vx = vx, vy = vy,
                lifetime = lifetime,
                maxLifetime = lifetime,
                color = palette[rng.Next(palette.Length)]
            });
        }
    }

    // 12 teintes : de sombre (0.4x) à clair (1.5x) sur la couleur de base
    private void GeneratePalette()
    {
        for (int i = 0; i < 12; i++)
        {
            float factor = 0.4f + (i / 11f) * 1.1f;
            byte r = (byte)Math.Clamp(color.R * factor, 0, 255);
            byte g = (byte)Math.Clamp(color.G * factor, 0, 255);
            byte b = (byte)Math.Clamp(color.B * factor, 0, 255);
            palette[i] = Color.FromRgb(r, g, b);
        }
    }

    // Appelé à chaque tick : déplace les particules et réduit leur durée de vie
    public void Update()
    {
        if (timer < duration)
            timer++;

        foreach (var p in particlesList)
        {
            p.x += p.vx;
            p.y += p.vy;
            p.lifetime--;
        }

        particlesList.RemoveAll(p => p.lifetime <= 0);
    }

    // Appelé après DrawScene() pour dessiner les particules sur le canvas
    // Recrée les rectangles à chaque frame car le canvas est vidé par DrawScene
    public void Draw(Canvas canvas)
    {
        foreach (var p in particlesList)
        {
            double alpha = (double)p.lifetime / p.maxLifetime;

            var rect = new Rectangle
            {
                Width = 4,
                Height = 4,
                Fill = new SolidColorBrush(p.color),
                Opacity = alpha
            };

            Canvas.SetLeft(rect, p.x - 2);
            Canvas.SetTop(rect, p.y - 2);
            canvas.Children.Add(rect);
        }
    }
}
