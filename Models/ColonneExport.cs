using CommunityToolkit.Mvvm.ComponentModel;

namespace MyProjectBase.Models;

// Représente une colonne sélectionnable pour l'export CSV

// UTILISATION :
//   CollectionViewModel.ColonnesExport = ObservableCollection<ColonneExport>
//   Dans CollectionView.axaml → Flyout avec une CheckBox par ColonneExport
//   L'utilisateur coche/décoche → EstSelectionnee change → binding two-way
//   Lors de l'export : on filtre les colonnes où EstSelectionnee = true

// Hérite de ObservableObject (CommunityToolkit) pour que le data binding fonctionne
// sur EstSelectionnee → quand on coche une case, l'UI se met à jour automatiquement
public partial class ColonneExport : ObservableObject
{
    // Texte affiché dans la CheckBox de l'UI (peut être différent du nom de propriété)
    public string Label { get; set; }
    
    // CsvServices.SaveDataAsync utilise ce nom pour retrouver la propriété via GetProperties()
    // Doit correspondre exactement à un nom de propriété de Voiture
    public string NomPropriete { get; set; }
    
    // Indique si cette colonne est sélectionnée pour l'export
    // [ObservableProperty] génère la propriété publique EstSelectionnee
    //   avec INotifyPropertyChanged → la CheckBox dans l'UI reflète automatiquement l'état
    // Two-way binding : CheckBox.IsChecked ↔ EstSelectionnee
    [ObservableProperty]
    private bool _estSelectionnee = true; // Toutes les colonnes cochées par défaut

    // Constructeur : crée une colonne avec son label, son nom de propriété, et son état initial
    // selectionneeParDefaut = true → la colonne sera cochée au démarrage (comportement par défaut)
    public ColonneExport(string label, string nomPropriete, bool selectionneeParDefaut = true)
    {
        Label = label;
        NomPropriete = nomPropriete;
        _estSelectionnee = selectionneeParDefaut; // Initialise directement le champ (pas la propriété)
    }
}