using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace VraiPseudoSae.view.RLS_Pages;

public partial class Menu : UserControl
{
    public Menu()
    {
        InitializeComponent();
    }

    private RLS? ParentWindow => Window.GetWindow(this) as RLS;
    
    private void Btn2P_Click(object sender, RoutedEventArgs e) => ParentWindow?.StartGame(false);
    private void Btn1P_Click(object sender, RoutedEventArgs e) => ParentWindow?.StartGame(true);
    private void CustomCar(object sender, RoutedEventArgs e) => throw new NotImplementedException();
}