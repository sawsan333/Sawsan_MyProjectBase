using Avalonia.Controls;

namespace MyProjectBase.Views;

// CODE-BEHIND DE LOGINVIEW.AXAML
// Page d'accueil : formulaire de connexion (gauche) + formulaire d'inscription (droite)

// PATTERN MVVM : cette classe ne contient AUCUNE logique
// Toute la validation, les appels MongoDB, les messages d'erreur → LoginViewModel
// Les champs TextBox sont liés via {Binding LoginUsername}, {Binding RegisterEmail}, etc.
// Les boutons sont liés via {Binding LoginCommand}, {Binding RegisterCommand}
public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent(); // Charge Loginview.axaml
    }
}