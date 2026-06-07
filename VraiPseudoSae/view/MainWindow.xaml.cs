using System.Windows;
using VraiPseudoSae.Utils.GestionnaireSauvegarde;
using VraiPseudoSae.view.gameintro;
using VraiPseudoSae.view.intro;
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
            intro.StartRequested += (_, _) => ShowScreenAfterSplashIntro();
            ScreenHost.Content = intro;
        }

        private void ShowScreenAfterSplashIntro()
        {
            if (ProgressionJeuSauvegardeDepot.IntroductionTerminee())
            {
                ShowHomePage();
                return;
            }

            ShowGameIntroduction();
        }

        private void ShowGameIntroduction()
        {
            ScreenHost.Content = new GameIntroWindow();
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
