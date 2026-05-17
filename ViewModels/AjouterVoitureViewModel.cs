using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MongoDB.Bson;
using MyProjectBase.Helpers;
using MyProjectBase.Models;
using MyProjectBase.Services;

namespace MyProjectBase.ViewModels;

// FONCTIONNEMENT :
//   1. Charge le catalogue global depuis MongoDB 
//   2. L'utilisateur sélectionne une voiture dans la ListBox → VoitureSelectionnee se met à jour
//   3. Clic "Ajouter" → crée une COPIE de la voiture (nouvel Id) → insère dans "Voitures_{userId}"
//   4. Clic "Annuler" → retour sans modification
public partial class AjouterVoitureViewModel : ViewModelBase
{
    private readonly MongoServices _mongoServices;
    private readonly string _userId;  // Identifiant de la collection privée de l'utilisateur
    private readonly Action _retour;  // Callback retour → MainWindowViewModel.AllerCollection

    // Catalogue = voitures disponibles à l'ajout (collection globale "MesVoitures")
    // ObservableCollection : l'UI se met à jour automatiquement quand on y ajoute des voitures
    public ObservableCollection<Voiture> Catalogue { get; set; }

    // Voiture sélectionnée dans la ListBox
    // Liée via SelectedItem="{Binding VoitureSelectionnee}" dans AjouterVoitureView.axaml
    [ObservableProperty] private Voiture? _voitureSelectionnee;

    // Message d'erreur affiché en rouge dans la View
    [ObservableProperty] private string? _errorMessage;
    
    // Indicateur de chargement → désactive le bouton "Ajouter" pendant l'opération MongoDB
    [ObservableProperty] private bool _isLoading;

    // Constructeur : initialise et lance le chargement du catalogue en arrière-plan
    public AjouterVoitureViewModel(MongoServices mongoServices, string userId, Action retour)
    {
        _mongoServices = mongoServices;
        _userId = userId;
        _retour = retour;
        Catalogue = new ObservableCollection<Voiture>();

        // Task.Run : exécute ChargerCatalogue sur un thread background
        // Nécessaire car le constructeur ne peut pas être async
        // Alternative : _ = Task.Run(...) ou utiliser un pattern "Init()"
        Task.Run(async () => await ChargerCatalogue());
    }

    // Charge le catalogue global depuis MongoDB et tente de charger les images
    private async Task ChargerCatalogue()
    {
        var voitures = await _mongoServices.GetCatalogueAsync(); // Requête MongoDB 
        
        foreach (var v in voitures)
        {
            // Chargement de l'image depuis les Assets  (format "avares://...")
            if (!string.IsNullOrEmpty(v.PicturePath))
            {
                try
                {
                    v.Image = ImageHelper.LoadFromResource(new Uri(v.PicturePath));
                }
                catch { } // Image absente ou chemin invalide → on ignore (v.Image reste null)
            }

            Catalogue.Add(v); // Ajout à l'ObservableCollection → la ListBox se met à jour
        }
    }

    // COMMANDE D'AJOUT 
    // [RelayCommand] génère AjouterCommand lié au bouton "Ajouter" dans la View
    [RelayCommand]
    private async Task Ajouter()
    {
        // Validation 
        if (VoitureSelectionnee == null)
        {
            ErrorMessage = "Veuillez sélectionner une voiture.";
            return;
        }
        
        try
        {
            // Crée une COPIE de la voiture du catalogue avec un NOUVEL Id MongoDB
            // On ne réutilise pas l'Id du catalogue car chaque user a sa propre copie indépendante
            var v = new Voiture
            {
                Id = ObjectId.GenerateNewId().ToString(), // Génère un Id MongoDB unique
                Nom = VoitureSelectionnee.Nom,
                Description = VoitureSelectionnee.Description,
                Origine = VoitureSelectionnee.Origine,
                Marque = VoitureSelectionnee.Marque,
                TypeCarburant = VoitureSelectionnee.TypeCarburant,
                Moteur = VoitureSelectionnee.Moteur,
                Performance = VoitureSelectionnee.Performance,
                Consommation = VoitureSelectionnee.Consommation,
                Confort = VoitureSelectionnee.Confort,
                PicturePath = VoitureSelectionnee.PicturePath 
            };

            // Insère la copie dans la collection privée "Voitures_{_userId}" de l'utilisateur
            await _mongoServices.AddVoitureAsync(_userId, v);

            _retour.Invoke(); // Retour à CollectionView (qui rechargera depuis MongoDB)
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur : {ex.Message}"; // Affiche l'erreur à l'utilisateur
        }
    }

    // Annuler : retour immédiat sans modification
    // [RelayCommand] génère AnnulerCommand lié aux boutons "Annuler" et "← Retour" dans la View
    [RelayCommand]
    private void Annuler() => _retour.Invoke();
}