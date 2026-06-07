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
    /// Logique d'interaction pour DropItemsMenu.xaml
    /// </summary>
    public partial class MainDropItems : Window
    {
        private DropItemsMenu menu_jeu;

        public MainDropItems()
        {
            InitializeComponent();

            menu_jeu = new DropItemsMenu();

            MainFrame.Navigate(menu_jeu);
        }

        private void MainDropItems_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // On vérifie si la page actuellement affichée est bien le jeu (DropItems)
                if (MainFrame.Content is DropItems)
                {
                    // On récupère la Window qui contient cette page
                    var parentWindow = Window.GetWindow(this) as MainDropItems;

                    // Si on l'a trouvée, on accède à son MainFrame
                    if (parentWindow != null)
                    {
                        parentWindow.MainFrame.Navigate(menu_jeu);
                    }
                }
            }
        }
    }
}