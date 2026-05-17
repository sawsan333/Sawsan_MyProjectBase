using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyProjectBase.Models;
using MyProjectBase.Services;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using MyProjectBase.Helpers;

namespace MyProjectBase.ViewModels;

// Affiche les voitures de l'utilisateur avec filtres, import/export CSV, ajout, suppression

// DONNÉES :
//   MyGlobals.MesVoitures     = source de vérité (toutes les voitures chargées)
//   MesVoituresObservable     = sous-ensemble filtré (affiché dans la ListBox)
//   AppliquerFiltres()        = recrée MesVoituresObservable depuis MesVoitures + filtres actifs

// NAVIGATION :
//   Les Actions goToAdmin, goToAjout, logout sont des callbacks fournis par MainWindowViewModel
//   FromParentCommand = GoToDetailsFromChildCommand de MainWindowViewModel (passé en paramètre)
public partial class CollectionViewModel : ViewModelBase
{
    private readonly CsvServices? _csvServices;     // Import/Export CSV
    private readonly MongoServices? _mongoServices; // CRUD MongoDB
    private readonly string _userId = "";           // "Voitures_{_userId}" = collection MongoDB privée
    private readonly string _role = "user";         
    private readonly Action? _goToAdmin;            // Callback → AdminView (null si pas admin)
    private readonly Action? _goToAjout;            // Callback → AjouterVoitureView
    private readonly Action? _logout;               // Callback → déconnexion → LoginView

    // Constructeur vide : UNIQUEMENT pour le designer Avalonia (preview XAML)
    public CollectionViewModel() { }

    // Constructeur principal : reçoit tous les services et callbacks de MainWindowViewModel
    public CollectionViewModel(
        IRelayCommand<string> fromParentCommand, // Commande pour naviguer vers les détails
        TopLevel topLevel,
        MongoServices mongoServices,
        string userId,
        string role,
        Action? goToAdmin = null,
        Action? goToAjout = null,
        Action? logout = null)
    {
        FromParentCommand = fromParentCommand; // Stocke la commande "aller aux détails"
        _csvServices = new CsvServices(topLevel);
        _mongoServices = mongoServices;
        _userId = userId;
        _role = role;
        _goToAdmin = goToAdmin;
        _goToAjout = goToAjout;
        _logout = logout;
        
        MesVoituresObservable = new ObservableCollection<Voiture>();
        _ = LoadFromMongoAsync(); // Charge la collection au démarrage
        // _ = discard : ignore le Task retourné (pas de await dans un constructeur)
    }

    // COMMANDES DE NAVIGATION 
    
    [RelayCommand]
    private void AllerAdmin() => _goToAdmin?.Invoke(); // ?. = appel seulement si non null

    [RelayCommand]
    private void OuvrirAjoutVoiture() => _goToAjout?.Invoke();
    
    [RelayCommand]
    private void Logout() => _logout?.Invoke();

    // Liée dans CollectionView.axaml :
    //   Command="{Binding DataContext.FromParentCommand, ElementName=Collection}"
    //   CommandParameter="{Binding Id}"  ← Id de la voiture cliquée
    public IRelayCommand<string>? FromParentCommand { get; }
    
    
    public bool IsAdmin => _role == "admin";

    // FILTRES 
    // Listes des options de filtres (affichées dans les ComboBox)
    public List<string> Marques { get; } = new()
    {
        "Tout",
        "Mercedes", "BMW", "Audi", "Lamborghini",
        "Porsche", "Ferrari", "Tesla", "Range Rover", "Bugatti"
    };

    public List<string> Carburants { get; } = new()
    {
        "Tout",
        "Essence", "Diesel", "Électrique"
    };

    // Valeur sélectionnée dans les ComboBox de filtre
    // Les méthodes partielles sont appelées automatiquement par [ObservableProperty] au changement
    [ObservableProperty] private string? _marqueSelectionnee;
    [ObservableProperty] private string? _carburantSelectionne;

    // Ces méthodes partielles sont générées et appelées quand la propriété change
    // Dès que l'utilisateur change un filtre dans la ComboBox → la liste se rafraîchit
    partial void OnMarqueSelectionneeChanged(string? value) => AppliquerFiltres();
    partial void OnCarburantSelectionneChanged(string? value) => AppliquerFiltres();

    // Liste affichée dans la ListBox (sous-ensemble filtré de MyGlobals.MesVoitures)
    [ObservableProperty] private ObservableCollection<Voiture> _mesVoituresObservable = new();
    
    // Voiture actuellement sélectionnée dans la liste
    [ObservableProperty] private Voiture? _voitureSelectionnee;

    // COLONNES EXPORT CSV
    // Chaque ColonneExport a : Label (texte UI) + NomPropriete (nom exact dans Voiture) + EstSelectionnee
    public ObservableCollection<ColonneExport> ColonnesExport { get; } = new()
    {
        new ColonneExport("Nom",          "Nom"),
        new ColonneExport("Description",  "Description"),
        new ColonneExport("Origine",      "Origine"),
        new ColonneExport("Marque",       "Marque"),
        new ColonneExport("Carburant",    "TypeCarburant"),
        new ColonneExport("Moteur",       "Moteur"),
        new ColonneExport("Performance",  "Performance"),
        new ColonneExport("Consommation", "Consommation"),
        new ColonneExport("Confort",      "Confort"),
        new ColonneExport("Image (path)", "PicturePath", selectionneeParDefaut: true),
    };

    // COMMANDES CSV 

    // Import : ouvre un explorateur de fichiers → parse le CSV → remplace MyGlobals.MesVoitures
    [RelayCommand]
    private async Task LoadFromCsv()
    {
        if (_csvServices == null) return;
        var voitures = await _csvServices.LoadDataAsync(); // Ouvre le dialogue + parse
        if (voitures.Count == 0) return; // Annulé ou fichier vide

        MyGlobals.MesVoitures.Clear();
        foreach (var v in voitures)
        {
            TryLoadImage(v);              // Tente de charger l'image depuis les Assets
            MyGlobals.MesVoitures.Add(v);
        }
        AppliquerFiltres(); // Rafraîchit la liste affichée
    }

    // Chargement depuis MongoDB : récupère la collection privée "Voitures_{_userId}"
    // public = appelable depuis d'autres contextes (ex: après un ajout)
    public async Task LoadFromMongoAsync()
    {
        if (_mongoServices == null) return;
        var voitures = await _mongoServices.GetVoituresAsync(_userId); // Requête MongoDB async

        // Dispatcher.UIThread.InvokeAsync : nécessaire car on est sur un thread background
        // Sans ça, modifier une ObservableCollection depuis un thread non-UI → exception Avalonia
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            MyGlobals.MesVoitures.Clear();
            foreach (var v in voitures)
            {
                TryLoadImage(v);
                MyGlobals.MesVoitures.Add(v);
            }
            // Reset de l'ObservableCollection → déclenche le data binding → l'UI se met à jour
            MesVoituresObservable = new ObservableCollection<Voiture>(MyGlobals.MesVoitures);
        });
    }

    // Export : ouvre un dialogue "Enregistrer sous" → écrit les colonnes cochées en CSV
    [RelayCommand]
    private async Task ExporterCsv()
    {
        if (_csvServices == null) return;
        // Récupère uniquement les NomPropriete des colonnes cochées (EstSelectionnee = true)
        var cols = ColonnesExport.Where(c => c.EstSelectionnee).Select(c => c.NomPropriete);
        await _csvServices.SaveDataAsync(MyGlobals.MesVoitures.ToList(), cols);
    }

    // Suppression : enlève de MongoDB + de la liste locale + rafraîchit les filtres
    // CommandParameter dans la View = la Voiture entière ({Binding})
    [RelayCommand]
    private async Task SupprimerVoiture(Voiture? voiture)
    {
        if (voiture == null || _mongoServices == null || voiture.Id == null) return;
        await _mongoServices.DeleteVoitureAsync(_userId, voiture.Id); // Supprime en DB
        MyGlobals.MesVoitures.Remove(voiture); // Supprime de la liste globale
        AppliquerFiltres(); // Met à jour MesVoituresObservable
    }

    // FILTRAGE

    // Recalcule MesVoituresObservable en appliquant les filtres actifs sur MyGlobals.MesVoitures
    // Appelée automatiquement quand un filtre change (OnMarqueSelectionneeChanged, etc.)
    private void AppliquerFiltres()
    {
        var res = MyGlobals.MesVoitures.AsEnumerable(); // Commence avec toutes les voitures

        // Filtre marque (si actif = pas null et pas "Tout")
        if (!string.IsNullOrEmpty(_marqueSelectionnee) && _marqueSelectionnee != "Tout")
            res = res.Where(v => v.Marque == _marqueSelectionnee);

        // Filtre carburant (chaînable avec le filtre marque)
        if (!string.IsNullOrEmpty(_carburantSelectionne) && _carburantSelectionne != "Tout")
            res = res.Where(v => v.TypeCarburant == _carburantSelectionne);

        // Remplace l'observable → le data binding met à jour la ListBox automatiquement
        MesVoituresObservable = new ObservableCollection<Voiture>(res);
    }

    // Tente de charger l'image d'une voiture depuis les ressources embarquées (Assets/)
    // static = pas besoin d'instance pour appeler
    // Le catch vide = si l'image n'existe pas, on l'ignore silencieusement (v.Image reste null)
    private static void TryLoadImage(Voiture v)
    {
        if (!string.IsNullOrEmpty(v.PicturePath))
            try { v.Image = ImageHelper.LoadFromResource(new Uri(v.PicturePath)); } catch { }
    }
}