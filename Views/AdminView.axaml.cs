using Avalonia.Controls;

namespace MyProjectBase.Views;

// CODE-BEHIND DE ADMINVIEW.AXAML
// Page d'admin 

// MÉCANISMES CLÉS (dans AdminView.axaml) :
//   - x:Name="AdminPage" → permet aux DataTemplate de remonter aux commandes du ViewModel
//   - IsVisible={Binding !ConfirmationVisible} → cache la liste pendant la confirmation
//   - IsVisible={Binding IsEditing} → affiche le panneau d'édition pour la ligne active
//   - ElementName=AdminPage → remonte au DataContext principal (AdminViewModel) depuis le DataTemplate

// PATTERN MVVM : aucune logique ici — tout dans AdminViewModel
public partial class AdminView : UserControl
{
    public AdminView()
    {
        InitializeComponent(); // Charge AdminView.axaml
    }
}