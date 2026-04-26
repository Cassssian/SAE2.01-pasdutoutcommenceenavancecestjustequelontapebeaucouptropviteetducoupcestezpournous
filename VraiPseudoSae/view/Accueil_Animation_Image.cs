using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace VraiPseudoSae.view
{
    public class ImagePiece
    {
        public Image Element { get; set; }
        public TranslateTransform Transform { get; set; }
        public double DestX { get; set; }
        public double DestY { get; set; }
        public double Vitesse { get; set; } = 0.1; // Coefficient de fluidité (Lerp)
    }
}
