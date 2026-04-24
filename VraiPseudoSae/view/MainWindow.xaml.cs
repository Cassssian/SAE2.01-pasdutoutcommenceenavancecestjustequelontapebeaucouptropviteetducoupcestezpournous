using System.Windows;

namespace VraiPseudoSae.view;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void BtnMazeClick(object sender, RoutedEventArgs e)
    {
        Maze display = new Maze();
        display.Show();
    }

    private void BtnFootClick(object sender, RoutedEventArgs e)
    {
        FootGame display = new FootGame();
        display.Show();
    }
}