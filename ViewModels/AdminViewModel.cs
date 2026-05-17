using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyProjectBase.Models;
using MyProjectBase.Services;

namespace MyProjectBase.ViewModels;

// PATTERN D'ÉDITION  :
//   Chaque ligne de la liste est un UserRowViewModel (User + IsEditing)
//   IsEditing=false → carte lecture seule (boutons Modifier/Supprimer)
//   IsEditing=true  → panneau d'édition avec TextBox + ComboBox
//   _rowEnEdition   → référence à la ligne en cours d'édition (une seule à la fois)

// PATTERN CONFIRMATION SUPPRESSION :
//   DemanderSuppression() → mémorise UserASupprimer + ConfirmationVisible=true → dialog apparaît
//   ConfirmerSuppression() → supprime vraiment en DB
//   AnnulerSuppression()  → ferme le dialog sans supprimer
public partial class AdminViewModel : ViewModelBase
{
    private readonly MongoServices _mongoServices = null!; // null! = jamais null en pratique (sauf constructeur vide)
    private readonly Action _retour = null!;               // Callback → AllerCollection

    // Liste de toutes les lignes utilisateurs affichées
    // ObservableCollection → l'UI se met à jour automatiquement
    [ObservableProperty] private ObservableCollection<UserRowViewModel> _utilisateurs = new();
    
    // Message de confirmation affiché après une action
    [ObservableProperty] private string? _message;

    // SUPPRESSION 
    // Utilisateur ciblé par la suppression (null si pas de suppression en cours)
    [ObservableProperty] private User? _userASupprimer;
    
    // true = affiche le dialog de confirmation (et masque la liste)
    [ObservableProperty] private bool _confirmationVisible;

    // ÉDITION 
    // Référence à la ligne actuellement en mode édition (une seule à la fois)
    private UserRowViewModel? _rowEnEdition;
    
    // Champs temporaires d'édition — liés aux TextBox/ComboBox dans AdminView.axaml
    // via ElementName=AdminPage (remonte depuis le DataTemplate)
    [ObservableProperty] private string _editUsername = "";
    [ObservableProperty] private string _editEmail = "";
    [ObservableProperty] private string _editRole = "user"; // Valeur par défaut dans le ComboBox

    // Options du ComboBox de rôle — liées via ItemsSource="{Binding DataContext.Roles, ElementName=AdminPage}"
    public List<string> Roles { get; } = new() { "user", "admin" };

    // Constructeur vide : UNIQUEMENT pour le designer Avalonia (preview AdminView.axaml)
    public AdminViewModel() { }

    // Constructeur principal
    public AdminViewModel(MongoServices mongoServices, Action retour)
    {
        _mongoServices = mongoServices;
        _retour = retour;
        _ = ChargerUtilisateurs(); // Charge la liste au démarrage
    }

    // Récupère tous les utilisateurs depuis MongoDB et les convertit en UserRowViewModel
    // UserRowViewModel = wrapper qui ajoute IsEditing (logique UI) au User (données)
    private async Task ChargerUtilisateurs()
    {
        var users = await _mongoServices.GetAllUsersAsync();
        // ConvertAll = Map : transforme chaque User en UserRowViewModel
        Utilisateurs = new ObservableCollection<UserRowViewModel>(
            users.ConvertAll(u => new UserRowViewModel(u))
        );
    }

    // COMMANDES SUPPRESSION

    // Étape 1 : mémorise le user à supprimer et affiche le dialog de confirmation
    // CommandParameter dans AdminView.axaml = {Binding User} (l'objet User de la ligne)
    [RelayCommand]
    private void DemanderSuppression(User? user)
    {
        if (user == null) return;
        UserASupprimer = user;       // Mémorise qui supprimer
        ConfirmationVisible = true;  // Affiche le dialog 
        Message = null;              // Efface le message précédent
    }

    // Étape 2 : suppression effective + recharge + fermeture du dialog
    [RelayCommand]
    private async Task ConfirmerSuppression()
    {
        if (UserASupprimer?.Id == null) return; // ?.Id = safe navigation si UserASupprimer est null
        await _mongoServices.DeleteUserAsync(UserASupprimer.Id); // Supprime en MongoDB
        await ChargerUtilisateurs(); // Recharge la liste (mise à jour de l'UI)
        Message = $" ✓ Utilisateur '{UserASupprimer.Username}' supprimé.";
        UserASupprimer = null;          // Reset
        ConfirmationVisible = false;    // Ferme le dialog
    }

    // Annuler la suppression : ferme le dialog sans modifier la DB
    [RelayCommand]
    private void AnnulerSuppression()
    {
        UserASupprimer = null;
        ConfirmationVisible = false;
    }

    // COMMANDES ÉDITION

    // Ouvre le panneau d'édition pour un utilisateur
    // Ferme l'édition précédente si une autre ligne était en mode édition
    [RelayCommand]
    private void OuvrirEdition(User? user)
    {
        if (user == null) return;

        // Si une autre ligne était en édition, la fermer d'abord
        if (_rowEnEdition != null)
            _rowEnEdition.IsEditing = false; // IsEditing → binding → panneau masqué

        // Cherche la ligne correspondante dans la liste observable
        foreach (var row in Utilisateurs)
        {
            if (row.User.Id == user.Id)
            {
                _rowEnEdition = row;
                // Copie les valeurs actuelles dans les champs temporaires (EditUsername, etc.)
                EditUsername = user.Username;
                EditEmail = user.Email ?? ""; // ?? "" = si Email est null → chaîne vide
                EditRole = user.Role;
                row.IsEditing = true; // Affiche le panneau d'édition pour cette ligne
                break;
            }
        }

        Message = null;
    }

    // Sauvegarde les modifications en MongoDB
    [RelayCommand]
    private async Task SauvegarderEdition()
    {
        if (_rowEnEdition == null) return;

        // Validation : nom d'utilisateur obligatoire
        if (string.IsNullOrWhiteSpace(EditUsername))
        {
            Message = " Le nom d'utilisateur ne peut pas être vide.";
            return;
        }

        // Applique les nouvelles valeurs sur l'objet User de la ligne
        _rowEnEdition.User.Username = EditUsername;
        _rowEnEdition.User.Email = EditEmail;
        _rowEnEdition.User.Role = EditRole;

        await _mongoServices.UpdateUserAsync(_rowEnEdition.User); // Replace un en MongoDB
        await ChargerUtilisateurs(); // Recharge toute la liste pour afficher les nouvelles valeurs

        Message = "Utilisateur mis à jour.";
        _rowEnEdition = null; // Reset : plus aucune ligne en édition
    }

    // Annule l'édition : ferme le panneau sans sauvegarder
    [RelayCommand]
    private void AnnulerEdition()
    {
        if (_rowEnEdition != null)
            _rowEnEdition.IsEditing = false; // Masque le panneau d'édition
        _rowEnEdition = null;
    }

    // Retour vers CollectionView
    [RelayCommand]
    private void Retour() => _retour.Invoke();
}