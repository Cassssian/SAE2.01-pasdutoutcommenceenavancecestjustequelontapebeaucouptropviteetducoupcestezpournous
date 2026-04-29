using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace VraiPseudoSae.view
{
    /// <summary>
    /// Logique d'interaction pour Accueil.xaml
    /// </summary>
    public partial class Accueil : Page
    {
        // COMPOSANTS DE LA CLASSE
        private List<ImagePiece> _pieces = new List<ImagePiece>();


        // FONCTIONS
        public Accueil()
        {
            InitializeComponent();

            CompositionTarget.Rendering += GameLoop;
        }


        private void CreerMorceau(Image image, double xFinal, double yFinal)
        {
            var transform = new TranslateTransform(-200, Random.Shared.Next(0, 500)); // Part de la gauche
            image.RenderTransform = transform;

            ImagePiece debris = new ImagePiece(image, transform, xFinal, yFinal, 0.3);

            _pieces.Add(debris);

            MainCanvas.Children.Add(image);
        }



        private void GameLoop(object? sender, EventArgs e)
        {
            foreach (ImagePiece piece in _pieces)
            {
                // Calcul de la distance restante
                double diffX = piece.DestX - piece.Transform.X;
                double diffY = piece.DestY - piece.Transform.Y;

                // Si on est assez proche, on stoppe pour éviter les micro-vibrations
                if (Math.Abs(diffX) < 0.5 && Math.Abs(diffY) < 0.5)
                {
                    piece.Transform.X = piece.DestX;
                    piece.Transform.Y = piece.DestY;
                    continue;
                }

                // Déplacement fluide (Lerp)
                // On bouge de 10% (0.1) de la distance restante à chaque frame
                piece.Transform.X += diffX * piece.Vitesse;
                piece.Transform.Y += diffY * piece.Vitesse;
            }
        }
    }

    public class ImagePiece
    {
        private Image element;
        public Image Element
        {
            get { return element; }
            set { element = value; }
        }

        private TranslateTransform transform;
        public TranslateTransform Transform
        {
            get => transform;
            set { transform = value; }
        }

        private double destx;
        public double DestX
        {
            get { return destx; }
            set { destx = value; }
        }

        private double desty;
        public double DestY
        {
            get { return desty; }
            set { desty = value; }
        }

        private double vitesse;
        public double Vitesse { get { return vitesse; } set { vitesse = value; } } // Coefficient de vitesse


        public ImagePiece(Image element, TranslateTransform transform, double destX, double destY, double vitesse)
        {
            this.element = element ?? throw new ArgumentNullException(nameof(element));
            this.transform = transform ?? throw new ArgumentNullException(nameof(transform));
            this.destx = destX;
            this.desty = destY;
            this.vitesse = vitesse;
        }

    }
}
