using Avalonia;
using System;

namespace MyProjectBase;

// PROGRAM.CS — Point d'entrée de l'application (première ligne de code exécutée)

// sealed = cette classe ne peut pas être héritée (bonne pratique pour les classes utilitaires)
sealed class Program
{
    // [STAThread] = Single Thread Apartment
    // OBLIGATOIRE pour toutes les applications Windows avec interface graphique
    // Garantit que le thread principal utilise le modèle COM STA (requis par certaines APIs Windows)
    // Sans ce tag, l'UI peut avoir des comportements erratiques ou des crashes sur Windows
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
    // StartWithClassicDesktopLifetime = lance l'app en mode fenêtre desktop classique
    // Crée la boucle d'événements (event loop) qui maintient l'app en vie
    // L'app reste active jusqu'à ce que la fenêtre principale soit fermée

    // Configure le builder Avalonia avec les options du projet
    // NE PAS SUPPRIMER : aussi utilisé par le designer visuel de Rider/Visual Studio
    // Le designer appelle cette méthode pour initialiser Avalonia sans lancer l'app complète
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()  // Utilise notre classe App (App.axaml + App.axaml.cs)
            .UsePlatformDetect()        // Détecte automatiquement l'OS (Windows/Linux/Mac) et charge le bon backend
            .WithInterFont()            // Charge la police Inter (police par défaut du thème FluentTheme)
            .LogToTrace();              // Active les logs de debug dans la console / trace système
}