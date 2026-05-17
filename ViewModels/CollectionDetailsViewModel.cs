using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyProjectBase.Models;

namespace MyProjectBase.ViewModels;

// VIEWMODEL DÉTAILS VOITURE — Affiche toutes les infos d'une voiture sélectionnée
// Page en LECTURE SEULE (pas de modification ni de suppression)

// La voiture est passée directement au constructeur (depuis MyGlobals.MesVoitures)
// Tous les {Binding MaVoiture.X} dans CollectionDetailsView.axaml y font référence
public partial class CollectionDetailsViewModel : ViewModelBase
{
    // La voiture affichée — [ObservableProperty] pour que le data binding fonctionne
    // Tous les champs sont liés dans le XAML
    [ObservableProperty] private Voiture _maVoiture;

    // Callback pour retourner à la liste (fourni par MainWindowViewModel.AllerCollection)
    private readonly Action _retour;

    // Constructeur : reçoit la voiture et le callback de retour
    // La voiture est trouvée dans MyGlobals.MesVoitures par son Id dans GoToDetailsFromChild
    public CollectionDetailsViewModel(Voiture voiture, Action retour)
    {
        _retour = retour;
        MaVoiture = voiture; // Assigne → le binding affiche les données dans la View
    }

    // Commande liée au bouton "← Retour" dans CollectionDetailsView.axaml
    // [RelayCommand] génère RetourAccueilCommand (IRelayCommand)
    // → appelle _retour() → MainWindowViewModel.AllerCollection() → CollectionView s'affiche
    [RelayCommand]
    private void RetourAccueil()
    {
        _retour?.Invoke(); // ?. = appel sécurisé (ne fait rien si _retour est null)
    }
}