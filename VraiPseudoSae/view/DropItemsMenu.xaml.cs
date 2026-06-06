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
    /// Logique d'interaction pour MainDropItems.xaml
    /// </summary>
    public partial class DropItemsMenu : Page
    {
        public DropItemsMenu()
        {
            InitializeComponent();
        }

        public void Jouer(object sender, RoutedEventArgs e)
        {
            DropItems pagejeu = new DropItems();

            // On récupère la Window qui contient cette page
            var parentWindow = Window.GetWindow(this) as MainDropItems;

            // Si on l'a trouvée, on accède à son MainFrame
            if (parentWindow != null)
            {
                parentWindow.MainFrame.Navigate(pagejeu);
            }
        }

        public void Options(object sender, RoutedEventArgs e)
        {
            DropItemsOptions pageopt = new DropItemsOptions();

            // On récupère la Window qui contient cette page
            var parentWindow = Window.GetWindow(this) as MainDropItems;

            // Si on l'a trouvée, on accède à son MainFrame
            if (parentWindow != null)
            {
                parentWindow.MainFrame.Navigate(pageopt);
            }
        }

        public void Quitter(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


    }
}
