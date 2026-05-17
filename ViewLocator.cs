using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MyProjectBase.ViewModels;

namespace MyProjectBase;

/// ViewLocator — Mécanisme de résolution automatique View ↔ ViewModel (cœur du pattern MVVM)

/// PRINCIPE :
///   Quand Avalonia doit afficher un ViewModel ,
///   il cherche un DataTemplate qui "match" parmi les DataTemplates globaux (App.axaml)
///   → ViewLocator est enregistré comme DataTemplate global dans App.axaml
///   → Match() retourne true pour tout ViewModelBase
///   → Build() transforme le nom du ViewModel en nom de View par remplacement de texte,
///     puis instancie la View par réflexion
///
/// CONVENTION DE NOMMAGE (à respecter pour ajouter une nouvelle page) :
///   ViewModel : MyProjectBase.ViewModels.XxxViewModel
///   View      : MyProjectBase.Views.XxxView
///   Si le nom ne suit pas cette convention → ViewLocator affiche "Not Found: ..."

[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
// [RequiresUnreferencedCode] = avertissement pour la publication AOT (Ahead-of-Time compilation)
// La réflexion peut être supprimée lors du trimming → ce warning documente le risque
public class ViewLocator : IDataTemplate
{
    // Build = construit et retourne la View correspondant au ViewModel reçu
    // Appelé par Avalonia quand un ViewModel doit être affiché dans un ContentControl
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        // Transformation du nom complet du type ViewModel → View par remplacement de chaîne
        // FullName = nom complet avec namespace : "MyProjectBase.ViewModels.LoginViewModel"
        // Replace("ViewModel", "View") → "MyProjectBase.Views.LoginView"
        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        
        // Type.GetType(name) = recherche le type C# par son nom complet (via réflexion)
        // Retourne null si le type n'existe pas (View non créée, nom incorrect, etc.)
        var type = Type.GetType(name);

        if (type != null)
        {
            // Activator.CreateInstance = instancie la View en appelant son constructeur vide
            // Le ! = assertion non-null (on sait que la View a un constructeur sans paramètre)
            return (Control)Activator.CreateInstance(type)!;
        }

        // Si la View n'est pas trouvée → affiche un message d'erreur dans l'UI
        // Utile en développement pour détecter rapidement les erreurs de nommage
        return new TextBlock { Text = "Not Found: " + name };
    }

    // Match = filtre pour quels objets ce DataTemplate s'applique
    // Retourne true uniquement pour les objets qui héritent de ViewModelBase
    // → Ce DataTemplate ne s'applique PAS aux strings, nombres, etc. dans d'autres contextes
    public bool Match(object? data)
    {
        return data is ViewModelBase; // Pattern matching C# : vérifie le type à la volée
    }
}