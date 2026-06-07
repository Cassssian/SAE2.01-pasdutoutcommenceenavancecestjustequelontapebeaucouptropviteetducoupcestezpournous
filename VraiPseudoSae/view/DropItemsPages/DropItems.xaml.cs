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
using VraiPseudoSae.view.DropItemsPages;
using VraiPseudoSae.view.DropItemsPages.Decors;

namespace VraiPseudoSae.view
{
    /// <summary>
    /// Logique d'interaction pour DropItems.xaml
    /// </summary>
    public partial class DropItems : Page
    {
        private DropItemsSettings settings;

        public DropItems(DropItemsSettings settings)
        {
            InitializeComponent();

            this.settings = settings;

            AppliquerDecor(settings.Decor);

        }

        private void AppliquerDecor(DropItemsMapsEnum typeDecor)
        {
            switch (typeDecor)
            {
                case DropItemsMapsEnum.NUIT:
                    ConteneurDecor.Content = new DecorNuit();
                    break;
                case DropItemsMapsEnum.JOUR:
                    ConteneurDecor.Content = new DecorJour();
                    break;
                case DropItemsMapsEnum.AUBE:
                    // ConteneurDecor.Content = new DecorAube();
                    break;
                default:
                    ConteneurDecor.Content = new DecorNuit(); // Sécurité
                    break;
            }
        }
    }
}
