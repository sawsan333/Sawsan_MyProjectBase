using CommunityToolkit.Mvvm.ComponentModel;
using MyProjectBase.Models;

namespace MyProjectBase.ViewModels;

// VIEWMODEL LIGNE UTILISATEUR — Wrapper de User pour l'affichage dans AdminView
// POURQUOI CE WRAPPER ?
//   Dans AdminView.axaml, chaque ligne peut être en mode "lecture" ou "édition"
//   Le modèle User (en base) n'a pas cette notion (IsEditing = logique purement UI)
//   Ce ViewModel ajoute IsEditing au User pour gérer l'affichage inline

//   IsEditing=false (défaut) → carte lecture seule (Username + Email + Badge rôle + boutons)
//   IsEditing=true           → panneau d'édition (TextBox + ComboBox + Sauvegarder/Annuler)
//
// Hérite de ObservableObject (pas ViewModelBase) : c'est une ligne dans une liste,
// pas une page complète → pas besoin de Dispose() ni du scanner
public partial class UserRowViewModel : ObservableObject
{
    // L'objet User sous-jacent (données réelles : Id, Username, Email, Role, Password)
    // public pour que AdminView.axaml puisse accéder à User.Username, User.Email, etc.
    //   via {Binding User.Username}, {Binding User.Email}, etc.
    public User User { get; }

    // IsEditing = true → affiche le panneau d'édition dans AdminView.axaml
    // [ObservableProperty] génère IsEditing avec INotifyPropertyChanged
    // Lié à IsVisible du Border d'édition : IsVisible="{Binding IsEditing}"
    // Modifié par AdminViewModel.OuvrirEdition() et AnnulerEdition()
    [ObservableProperty]
    private bool _isEditing;
    // Toutes les lignes commencent en lecture (IsEditing=false par défaut)

    // Constructeur : reçoit le User à wrapper
    public UserRowViewModel(User user)
    {
        User = user;
    }
}