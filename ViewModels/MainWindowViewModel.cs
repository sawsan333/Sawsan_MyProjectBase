using System;
using System.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyProjectBase.Models;
using MyProjectBase.Services;

namespace MyProjectBase.ViewModels;

// VIEWMODEL PRINCIPAL
// Gère QUELLE page est affichée et COMMENT on navigue entre elles

// FLUX DE NAVIGATION :
//   Démarrage      → LoginView
//   Login réussi   → CollectionView  (userId + role transmis)
//   Clic Admin     → AdminView       (admin uniquement)
//   Clic + Ajouter → AjouterVoitureView
//   Clic Voir      → CollectionDetailsView
//   Logout / Retour → LoginView / CollectionView

// CurrentPage change → ViewLocator résout la View → affichée dans MainWindow.axaml (ContentControl)
public partial class MainWindowViewModel : ViewModelBase
{
    // [ObservableProperty] génère CurrentPage avec INotifyPropertyChanged
    // Le ContentControl dans MainWindow.axaml est lié à cette propriété :
    //   <ContentControl Content="{Binding CurrentPage}"/>
    // Quand CurrentPage change → Avalonia appelle ViewLocator.Build(CurrentPage) → nouvelle View
    [ObservableProperty] private ViewModelBase _currentPage = null!;

    private readonly TopLevel _topLevel;           // Fenêtre racine (pour les dialogues fichiers CSV)
    private readonly MongoServices _mongoServices; // Service DB unique, partagé entre toutes les pages
    private User? _currentUser;                    // Utilisateur connecté (null = déconnecté)

    // Constructeur : reçoit le TopLevel depuis App.axaml.cs
    public MainWindowViewModel(TopLevel topLevel)
    {
        _topLevel = topLevel;
        _mongoServices = new MongoServices(); // Une seule instance DB pour toute l'app

        // Tentative de connexion au scanner USB
        // try/catch : le scanner n'est pas toujours branché
        try
        {
            MyScanner = new ScannerManager();
            MyScanner.OpenPort(); // Détecte le port USB et ouvre la connexion série
        }
        catch (Exception e)
        {
            Console.WriteLine($"Scanner non disponible : {e.Message}"); // Log, pas d'exception
        }

        // Page de login
        // OnLoginSuccess est le callback appelé quand le login réussit
        CurrentPage = new LoginViewModel(_mongoServices, OnLoginSuccess);
    }

    // Appelé par LoginViewModel quand l'authentification réussit
    // Reçoit le User complet depuis MongoDB
    private void OnLoginSuccess(User user)
    {
        _currentUser = user; // Mémorise l'utilisateur pour toute la session
        AllerCollection();
    }

    // Navigue vers la page principale (collection de voitures)
    // Transmet toutes les informations nécessaires au CollectionViewModel
    private void AllerCollection()
    {
        if (_currentUser == null) return;
        CurrentPage = new CollectionViewModel(
            GoToDetailsFromChildCommand,      // Commande pour naviguer vers les détails
            _topLevel,                        // Pour les dialogues de fichiers CSV
            _mongoServices,
            _currentUser.Username,            // Identifiant de la collection MongoDB
            _currentUser.Role,               // "admin" ou "user"
            goToAdmin: _currentUser.Role == "admin" ? AllerAdmin : null, // null = bouton Admin masqué
            goToAjout: AllerAjoutVoiture,
            logout: Logout
        );
    }

    // Navigue vers la page d'admin
    // AllerCollection est le callback de retour
    private void AllerAdmin()
    {
        CurrentPage = new AdminViewModel(_mongoServices, AllerCollection);
    }

    // Navigue vers la page d'ajout de voiture
    // retour: AllerCollection → après ajout ou annulation, revient à la collection
    private void AllerAjoutVoiture()
    {
        if (_currentUser == null) return;
        CurrentPage = new AjouterVoitureViewModel(
            _mongoServices,
            _currentUser.Username, // Nécessaire pour insérer dans "Voitures_{username}"
            retour: AllerCollection
        );
    }

    // Dispose l'ancien ViewModel : ferme le port série, annule les tâches async
    // oldValue?.Dispose() = appel conditionnel (ne fait rien si oldValue est null)
    partial void OnCurrentPageChanging(ViewModelBase? oldValue, ViewModelBase? newValue)
        => oldValue?.Dispose();

    // Navigue vers les détails d'une voiture
    // [RelayCommand] génère GoToDetailsFromChildCommand (passé comme paramètre à CollectionViewModel)
    // Reçoit l'Id MongoDB de la voiture → la retrouve dans MyGlobals.MesVoitures
    [RelayCommand]
    private void GoToDetailsFromChild(string voitureId)
    {
        // First() : cherche la voiture par Id
        var voiture = MyGlobals.MesVoitures.First(v => v.Id == voitureId);
        CurrentPage = new CollectionDetailsViewModel(voiture, AllerCollection);
    }
    
    // Déconnexion : reset l'utilisateur + retour au login
    // [RelayCommand] génère LogoutCommand → passé au CollectionViewModel comme callback
    [RelayCommand]
    private void Logout()
    {
        _currentUser = null; // Plus d'utilisateur connecté
        CurrentPage = new LoginViewModel(_mongoServices, OnLoginSuccess);
    }
}