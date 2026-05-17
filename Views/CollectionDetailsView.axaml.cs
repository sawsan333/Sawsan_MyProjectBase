using Avalonia.Controls;

namespace MyProjectBase.Views;

// CODE-BEHIND DE COLLECTIONDETAILSVIEW.AXAML
// Page de détails d'une voiture : affiche image + toutes les propriétés
// Page en LECTURE SEULE (pas de modification)

// La voiture affichée est passée au ViewModel via le constructeur :
//   new CollectionDetailsViewModel(voiture, AllerCollection)
// Elle est stockée dans MaVoiture et tous les {Binding MaVoiture.X} s'y réfèrent

// PATTERN MVVM : aucune logique ici — tout dans CollectionDetailsViewModel
public partial class CollectionDetailsView : UserControl
{
    public CollectionDetailsView()
    {
        InitializeComponent(); // Charge CollectionDetailsView.axaml
    }
}