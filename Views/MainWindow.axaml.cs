using Avalonia.Controls;

namespace MyProjectBase.Views;

// CODE-BEHIND DE MAINWINDOW.AXAML
// La fenêtre principale de l'application (la seule fenêtre réelle)
//
// PATTERN MVVM : cette classe ne contient AUCUNE logique métier — tout est dans MainWindowViewModel
// Le DataContext est assigné dans App.axaml.cs :
//   mainWindow.DataContext = new MainWindowViewModel(topLevel);
//
// Le ContentControl dans MainWindow.axaml est lié à CurrentPage :
//   quand CurrentPage change → ViewLocator résout la View → l'affiche dans la fenêtre
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent(); // Charge et initialise le XAML (MainWindow.axaml)
    }
}