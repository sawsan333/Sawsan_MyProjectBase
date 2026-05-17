using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyProjectBase.Helpers;
using MyProjectBase.Models;
using MyProjectBase.Services;

namespace MyProjectBase.ViewModels;

public partial class ModifierVoitureViewModel : ViewModelBase
{
    private readonly MongoServices _mongoServices;
    private readonly string _userId;
    private readonly Action _retour;
    private readonly string _voitureId; // On garde l'Id original
    private readonly string? _picturePath;

    // Champs éditables
    [ObservableProperty] private string _nom = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _origine = "";
    [ObservableProperty] private string _marque = "";
    [ObservableProperty] private string _typeCarburant = "";
    [ObservableProperty] private string _moteur = "";
    [ObservableProperty] private string _performance = "";
    [ObservableProperty] private string _consommation = "";
    [ObservableProperty] private string _confort = "";
    [ObservableProperty] private string? _errorMessage;
   

    public ModifierVoitureViewModel(Voiture voiture, MongoServices mongoServices,
                                     string userId, Action retour)
    {
        _mongoServices = mongoServices;
        _userId = userId;
        _retour = retour;
        _voitureId = voiture.Id;
        _picturePath = voiture.PicturePath;

        // Pré-remplit les champs avec les valeurs actuelles
        Nom = voiture.Nom;
        Description = voiture.Description;
        Origine = voiture.Origine;
        Marque = voiture.Marque;
        TypeCarburant = voiture.TypeCarburant;
        Moteur = voiture.Moteur;
        Performance = voiture.Performance;
        Consommation = voiture.Consommation;
        Confort = voiture.Confort;
    }

    [RelayCommand]
    private async Task Sauvegarder()
    {
        try
        {
            var voitureModifiee = new Voiture
            {
                Id = _voitureId,
                Nom = Nom,
                Description = Description,
                Origine = Origine,
                Marque = Marque,
                TypeCarburant = TypeCarburant,
                Moteur = Moteur,
                Performance = Performance,
                Consommation = Consommation,
                Confort = Confort,
                PicturePath = _picturePath
                
                
               
                
            };

            // UpdateVoitureAsync est déjà dans MongoServices !
            await _mongoServices.UpdateVoitureAsync(_userId, voitureModifiee);
            _retour.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erreur : {ex.Message}";
        }
    }
    public List<string> Carburants { get; } = new()
    {
        "Essence",
        "Diesel",
        "Électrique"
    };
    
    public List<string> Marques { get; } = new()
    {
        "Mercedes", "BMW", "Audi", "Lamborghini",
        "Porsche", "Ferrari", "Tesla", "Range Rover", "Bugatti"
    };

    [RelayCommand]
    private void Annuler() => _retour.Invoke();
}