using System;
using System.Collections.Generic;
using System.Text;

namespace VraiPseudoSae.view.Accueil_Pages
{
    internal class ImageAnim
    {
        private string uri;

        private int coordx;
        public int Coordx
        {
            get {  return coordx; }
            set { coordx = value; }
        }


        private int coordy;
        public int Coordy
        {
            get { return coordy; }
            set { coordy = value; }
        }


        private int finalx;
        public int Finalx
        {
            get { return  finalx; }
            set {  finalx = value; }
        }

        private int finaly;
        public int Finaly
        {
            get
            {
                return finaly;
            }
            set { finaly = value; }
        }




        public ImageAnim(string uri, int coordx, int coordy, int finalx, int finaly)
        {
            // URL de l'image
            this.uri = uri;

            // Coordonnées initiales
            this.coordx = coordx;
            this.coordy = coordy;

            // Coordonnées finales
            this.finalx = finalx;
            this.finaly = finaly;
        }


        public void TranslateImage()
        {

        }

        public void Draw()
        {

        }
    }
}
