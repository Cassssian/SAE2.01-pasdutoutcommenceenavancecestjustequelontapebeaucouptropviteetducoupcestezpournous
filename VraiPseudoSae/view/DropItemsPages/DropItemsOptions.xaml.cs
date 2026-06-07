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


        public DropItemsOptions(DropItemsMenu menu)
        {
            InitializeComponent();

            this.menu = menu;
        }

        public void Retour_Menu(object sender, RoutedEventArgs e)
        {
            // On récupère la Window qui contient cette page
            var parentWindow = Window.GetWindow(this) as MainDropItems;

            // Si on l'a trouvée, on accède à son MainFrame
            if (parentWindow != null)
            {
                parentWindow.MainFrame.Navigate(menu);
            }
        }
    }
}
