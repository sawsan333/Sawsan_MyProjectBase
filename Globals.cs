using System.Collections.ObjectModel;
using MyProjectBase.Models;

namespace MyProjectBase;

// GLOBALS.CS — Variables globales partagées dans toute l'application

// static = une seule instance, accessible partout sans instanciation : MyGlobals.MesVoitures
// Utile pour partager des données entre ViewModels sans les passer en paramètre partout

// POURQUOI UNE CLASSE GLOBALE ?
//   CollectionViewModel charge les voitures depuis MongoDB et les stocke dans MesVoitures
//   D'autres ViewModels (ex: CollectionDetailsViewModel via GoToDetailsFromChild) ont besoin
//   d'y accéder → plutôt que de les repasser en paramètre, on les stocke globalement
public static class MyGlobals
{
    // Liste de toutes les voitures de l'utilisateur connecté (chargées depuis MongoDB)
    
    // ObservableCollection (pas List) : notifie automatiquement l'UI quand elle change
    // → Si on ajoute/supprime une voiture ici, tous les composants UI liés se rafraîchissent
    // C'est le principe du "data binding" du pattern MVVM
    
    // CYCLE DE VIE :
    //   LoadFromMongoAsync()  → Clear() + Add() pour chaque voiture de la DB
    //   SupprimerVoiture()    → Remove(voiture) après suppression en DB
    //   LoadFromCsv()         → Clear() + Add() après import CSV
    //   AppliquerFiltres()    → lit cette liste et crée une sous-liste filtrée (MesVoituresObservable)
    public static ObservableCollection<Voiture> MesVoitures = new();
    
    // Utilisateur actuellement connecté (null = personne connecté)
    // Assigné dans MainWindowViewModel.OnLoginSuccess() après login réussi
    // Remis à null dans MainWindowViewModel.Logout()
    // Pourrait être utilisé dans de futures fonctionnalités pour vérifier les droits d'accès
    public static User? CurrentUser { get; set; }
}