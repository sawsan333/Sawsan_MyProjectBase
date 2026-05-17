using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyProjectBase.Models;
using MyProjectBase.Services;

namespace MyProjectBase.ViewModels;

// VIEWMODEL LOGIN (connexion + inscription)

// 2 FONCTIONNALITÉS :
//   Login()    → vérifie les identifiants en MongoDB → callback onLoginSuccess si OK
//   Register() → valide les champs → crée le compte en MongoDB (rôle "user" par défaut)
public partial class LoginViewModel : ViewModelBase
{
    private readonly MongoServices _mongoServices;    // Accès MongoDB
    private readonly Action<User> _onLoginSuccess;    // Callback → MainWindowViewModel.OnLoginSuccess

    // CHAMPS CONNEXION 
    // [ObservableProperty] génère la propriété publique + INotifyPropertyChanged
    // Liés aux TextBox dans Loginview.axaml : Text="{Binding LoginUsername}", etc.
    [ObservableProperty] private string _loginUsername = "";
    [ObservableProperty] private string _loginPassword = "";
    [ObservableProperty] private string? _loginErrorMessage;

    // CHAMPS INSCRIPTION
    [ObservableProperty] private string _registerUsername = "";
    [ObservableProperty] private string _registerEmail = "";
    [ObservableProperty] private string _registerPassword = "";
    [ObservableProperty] private string _registerPasswordConfirm = ""; // Doit correspondre à _registerPassword
    [ObservableProperty] private string? _registerErrorMessage;

    // Indicateur chargement : true pendant un appel MongoDB → désactive les boutons, affiche "Connexion..."
    [ObservableProperty] private bool _isLoading;

    public LoginViewModel(MongoServices mongoServices, Action<User> onLoginSuccess)
    {
        _mongoServices = mongoServices;
        _onLoginSuccess = onLoginSuccess;
    }

    // CONNEXION
    // [RelayCommand] génère LoginCommand (IAsyncRelayCommand)
    // Lié au bouton "Se connecter" : Command="{Binding LoginCommand}"
    [RelayCommand]
    private async Task Login()
    {
        LoginErrorMessage = null; // Efface le message d'erreur précédent

        // Validation locale (avant d'appeler MongoDB)
        if (string.IsNullOrWhiteSpace(LoginUsername) || string.IsNullOrWhiteSpace(LoginPassword))
        {
            LoginErrorMessage = "Veuillez remplir tous les champs.";
            return;
        }

        IsLoading = true;
        try
        {
            // Appel MongoDB : cherche un user avec ce username (ou email) + mot de passe hashé SHA-256
            var user = await _mongoServices.LoginAsync(LoginUsername, LoginPassword);
            if (user == null)
                LoginErrorMessage = "Identifiants incorrects.";
            else
                _onLoginSuccess(user); // Succès → MainWindowViewModel navigue vers CollectionView
        }
        catch (Exception ex)
        {
            LoginErrorMessage = $"Erreur : {ex.Message}"; // Erreur réseau, serveur inaccessible, etc.
        }
        finally { IsLoading = false; } // Toujours remettre IsLoading à false (même en cas d'erreur)
    }

    // INSCRIPTION
    // [RelayCommand] génère RegisterCommand
    // Lié au bouton "Créer un compte" : Command="{Binding RegisterCommand}"
    [RelayCommand]
    private async Task Register()
    {
        RegisterErrorMessage = null;

        // Validation 1 : champs obligatoires
        if (string.IsNullOrWhiteSpace(RegisterUsername) || string.IsNullOrWhiteSpace(RegisterEmail))
        {
            RegisterErrorMessage = "Nom d'utilisateur et email obligatoires.";
            return;
        }

        // Validation 2 : format email 
        if (!EmailValide(RegisterEmail))
        {
            RegisterErrorMessage = "Email invalide (ex: utilisateur@gmail.com)";
            return;
        }

        // Validation 3 : longueur minimale du mot de passe
        if (RegisterPassword.Length < 6)
        {
            RegisterErrorMessage = "Le mot de passe doit contenir au minimum 6 caractères.";
            return;
        }

        // Validation 4 : confirmation du mot de passe
        if (RegisterPassword != RegisterPasswordConfirm)
        {
            RegisterErrorMessage = "Les mots de passe ne correspondent pas.";
            return;
        }

        IsLoading = true;
        try
        {
            // Le rôle est toujours "user" à la création — l'admin peut le changer ensuite via AdminView
            // MongoServices.RegisterAsync hashera le mot de passe en SHA-256 avant de l'insérer
            await _mongoServices.RegisterAsync(new User
            {
                Username = RegisterUsername,
                Email = RegisterEmail,
                Password = RegisterPassword,
                Role = "user",
            });
            RegisterErrorMessage = "Compte créé ! Connectez-vous.";
            RegisterUsername = "";
            RegisterEmail = "";
            RegisterPassword = "";
            RegisterPasswordConfirm = "";
        }
        catch (Exception ex)
        {
            RegisterErrorMessage = ex.Message.Contains("DuplicateKey")
                ? " Ce nom d'utilisateur existe déjà."
                : $"Erreur : {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    // Valide le format email avec une Regex
    // Règle : au moins un char (non @ ni espace) + @ + idem + . + idem
    // ^[^@\s]+  = début + un ou plusieurs chars qui ne sont pas @ ni espace
    // @         = le caractère @
    // [^@\s]+   = domaine
    // \.        = le point (échappé)
    // [^@\s]+$  = extension (.com, .be, etc.) + fin
    private bool EmailValide(string email)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
        );
    }
}