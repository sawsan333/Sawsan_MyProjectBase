using Avalonia.Controls;

namespace MyProjectBase.Views;

// CODE-BEHIND DE COLLECTIONVIEW.AXAML
// Page principale (ACCUEIL): affiche la collection de voitures de l'utilisateur connecté

// CONTENU VISIBLE :
//   - Barre du Haut : Import CSV, Export CSV (avec sélection de colonnes), + Ajouter, et Admin (si admin)
//   - Filtres par marque et carburant (ComboBox)
//   - Bouton Déconnecter
//   - Liste des voitures ,avec boutons Voir et Supprimer

// PATTERN MVVM : aucune logique ici — tout dans CollectionViewModel
// x:Name="Collection" dans le XAML permet aux DataTemplate de remonter au ViewModel
//   via : Command="{Binding DataContext.MaCommande, ElementName=Collection}"
public partial class CollectionView : UserControl
{
    public CollectionView()
    {
        InitializeComponent(); // Charge CollectionView.axaml
    }
}