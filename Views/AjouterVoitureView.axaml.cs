using Avalonia;
using Avalonia.Controls;

namespace MyProjectBase.Views;

// CODE-BEHIND DE AJOUTERVOITUREVIEW.AXAML
// Page pour ajouter une voiture à la collection de l'utilisateur

// FLUX :
//   1. AjouterVoitureViewModel charge le catalogue MongoDB (collection "MesVoitures")
//   2. L'utilisateur sélectionne une voiture dans la ListBox
//   3. Clic "Ajouter" → crée une copie avec nouvel Id → insère dans "Voitures_{userId}" → retour
//   4. Clic "Annuler"  → retour sans modification

// PATTERN MVVM : aucune logique ici — tout dans AjouterVoitureViewModel
public partial class AjouterVoitureView : UserControl
{
    public AjouterVoitureView()
    {
        InitializeComponent(); // Charge AjouterVoitureView.axaml
    }
}