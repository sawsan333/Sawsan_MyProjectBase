using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MyProjectBase.ViewModels;
using MyProjectBase.Views;

namespace MyProjectBase;

// APP.AXAML.CS — Point d'initialisation de l'application (code-behind de App.axaml)

// RÔLE : connecte la fenêtre principale (MainWindow) à son ViewModel (MainWindowViewModel)
// C'est ici que la chaîne View ↔ ViewModel est assemblée au démarrage

// ORDRE DES OPÉRATIONS :
//   1. Initialize()                    → charge App.axaml (thème + ViewLocator)
//   2. OnFrameworkInitializationCompleted() → crée la fenêtre, récupère TopLevel,
//                                            crée le ViewModel, assigne DataContext
public class App : Application
{
    // Charge et parse le fichier App.axaml
    // Appelé automatiquement par Avalonia avant OnFrameworkInitializationCompleted
    // Initialise le thème FluentTheme et enregistre le ViewLocator comme DataTemplate global
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // Appelé quand le framework Avalonia est complètement prêt
    // C'est ici qu'on crée la fenêtre et qu'on lie View ↔ ViewModel
    public override void OnFrameworkInitializationCompleted()
    {
        // IClassicDesktopStyleApplicationLifetime = mode desktop standard (fenêtre classique)
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Désactive la validation DataAnnotations d'Avalonia
            // Évite les doubles messages d'erreur de validation dans les formulaires
            // (Avalonia a son propre système de validation qui entre en conflit avec DataAnnotations)
            DisableAvaloniaDataAnnotationValidation();

            // ÉTAPE 1 : créer la fenêtre (View) en PREMIER
            // La fenêtre doit exister avant le ViewModel car on a besoin de son TopLevel
            var mainWindow = new MainWindow();

            // ÉTAPE 2 : récupérer le TopLevel depuis la fenêtre
            // TopLevel = la racine de l'arbre visuel Avalonia (la fenêtre elle-même)
            // Nécessaire pour accéder à StorageProvider (dialogues de fichiers dans CsvServices)
            // Le ! = assertion non-null (on sait que mainWindow a un TopLevel)
            var topLevel = TopLevel.GetTopLevel(mainWindow)!;

            // ÉTAPE 3 : créer le ViewModel et l'assigner comme DataContext de la fenêtre
            // DataContext = la source de données liée à la vue (principe fondamental du MVVM)
            // MainWindowViewModel reçoit topLevel pour le passer à CollectionViewModel → CsvServices
            mainWindow.DataContext = new MainWindowViewModel(topLevel);

            // ÉTAPE 4 : déclarer la fenêtre principale (celle qui s'affiche au lancement)
            desktop.MainWindow = mainWindow;
        }

        // Toujours appeler la base 
        base.OnFrameworkInitializationCompleted();
    }

    // Supprime le plugin de validation DataAnnotations du système de binding Avalonia
    // Sans ça, les messages de validation apparaissent en double dans les formulaires
    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Récupère tous les plugins de type DataAnnotationsValidationPlugin dans la liste
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // Les supprime un par un de la liste globale des validateurs
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}