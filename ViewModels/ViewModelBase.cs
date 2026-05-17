using System;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MyProjectBase.Services;

namespace MyProjectBase.ViewModels;

// CLASSE DE BASE DE TOUS LES VIEWMODELS
// Tous les ViewModels du projet héritent de cette classe (pattern MVVM)

// RESPONSABILITÉS :
//   1. ObservableObject (CommunityToolkit) → implémente INotifyPropertyChanged
//      Permet au data binding Avalonia de détecter les changements de propriétés
//   2. IDisposable → libération des ressources quand on change de page
//      Appelé par MainWindowViewModel.OnCurrentPageChanging (oldValue?.Dispose())
//   3. CancellationTokenSource → annulation des tâches async en cours lors du changement de page
//   4. ScannerManager → accès optionnel au port série (scanner/Arduino)
//
// abstract = on ne peut pas instancier ViewModelBase directement (que ses sous-classes)
public abstract class ViewModelBase : ObservableObject, IDisposable
{
    // CancellationTokenSource : permet d'annuler les opérations async (ex: chargement en cours)
    // Quand on quitte une page (Dispose appelé) → _cts.Cancel() arrête les tâches en background
    // À utiliser dans les ViewModels enfants : _cts.Token passé aux méthodes async
    private readonly CancellationTokenSource _cts = new();

    // Scanner USB (port série) — nullable car pas toujours disponible (scanner non branché)
    // public pour que MainWindowViewModel puisse l'assigner après connexion réussie
    // Les ViewModels enfants peuvent y accéder pour lire les données du scanner
    public ScannerManager? MyScanner;

    // Dispose = nettoyage des ressources
    // Appelé automatiquement par MainWindowViewModel quand on change de page :
    //   partial void OnCurrentPageChanging(...) => oldValue?.Dispose();
    public void Dispose()
    {
        MyScanner?.ClosePort(); // ?. = appel conditionnel : ferme le port seulement si ouvert
        _cts.Cancel();          // Annule toutes les tâches async en cours dans ce ViewModel
        _cts.Dispose();         // Libère la mémoire (handle système) du CancellationTokenSource
    }
}