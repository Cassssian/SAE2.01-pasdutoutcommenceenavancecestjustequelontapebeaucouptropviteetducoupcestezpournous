using System.Windows;
using VraiPseudoSae.view.WizardSurvival;

namespace VraiPseudoSae.view
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ShowIntro();
        }

        private void ShowIntro()
        {
            IntroWindow intro = new();
            intro.StartRequested += (_, _) => ShowHomePage();
            ScreenHost.Content = intro;
        }

        private void ShowHomePage()
        {
            HomePage homePage = new();
            homePage.WizardSurvivalRequested += (_, _) => ShowWizardSurvival();
            ScreenHost.Content = homePage;
        }

        private void ShowWizardSurvival()
        {
            WizardSurvivalWindow wizardSurvival = new();
            wizardSurvival.ExitRequested += (_, _) => ShowHomePage();
            ScreenHost.Content = wizardSurvival;
        }
    }
}
