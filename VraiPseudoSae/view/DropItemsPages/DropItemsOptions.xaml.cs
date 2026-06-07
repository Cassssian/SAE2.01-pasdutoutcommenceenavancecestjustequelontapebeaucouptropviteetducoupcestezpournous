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

namespace VraiPseudoSae.view
{
    /// <summary>
    /// Logique d'interaction pour DropItemsOptions.xaml
    /// </summary>
    public partial class DropItemsOptions : Page
    {
        /// <summary>
        /// On sauvegarde le menu qui est là.
        /// </summary>
        private DropItemsMenu menu;

        private DropItemsMapsEnum decor;


        public DropItemsOptions(DropItemsMenu menu)
        {
            InitializeComponent();

            this.menu = menu;

            switch (menu.Settings.Decor)
            {
                case (DropItemsMapsEnum.JOUR):
                    ButtonNuit.IsChecked = false;
                    ButtonJour.IsChecked = true;
                    break;
                case (DropItemsMapsEnum.AUBE):
                    ButtonNuit.IsChecked = false;
                    ButtonAube.IsChecked = true;
                    break;
                case (DropItemsMapsEnum.NUIT):
                    break;
            }
        }

        public void Retour_Menu(object sender, RoutedEventArgs e)
        {
            menu.Settings = new DropItemsSettings(decor);

            // On récupère la Window qui contient cette page
            var parentWindow = Window.GetWindow(this) as MainDropItems;

            // Si on l'a trouvée, on accède à son MainFrame
            if (parentWindow != null)
            {
                parentWindow.MainFrame.Navigate(menu);
            }
        }

        private void DecorNuitChoisi(object sender, RoutedEventArgs e)
        {
            decor = DropItemsMapsEnum.NUIT;
        }

        private void DecorAubeChoisi(object sender, RoutedEventArgs e)
        {
            decor = DropItemsMapsEnum.AUBE;
        }

        private void DecorJourChoisi(object sender, RoutedEventArgs e)
        {
            decor = DropItemsMapsEnum.JOUR;
        }
    }
}
