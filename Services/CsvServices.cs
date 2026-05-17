// Import et export de fichiers CSV

// TECHNIQUE CLÉE : 
//   typeof(Voiture).GetProperties() = liste toutes les propriétés de la classe Voiture
//   property.SetValue(obj, value)   = assigne une valeur à une propriété par son nom
//   property.GetValue(item)         = lit la valeur d'une propriété
//   → Permet de mapper dynamiquement les colonnes CSV ↔ propriétés, sans hard-coder chaque champ
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MongoDB.Bson;
using MyProjectBase.Models;

namespace MyProjectBase.Services;

public class CsvServices
{
    // TopLevel = la fenêtre racine Avalonia, nécessaire pour ouvrir les dialogues système de fichiers
    // Injecté via le constructeur depuis CollectionViewModel (qui le reçoit de MainWindowViewModel)
    private readonly TopLevel _topLevel;

    public CsvServices(TopLevel topLevel) 
    {
        _topLevel = topLevel; 
    }

    // IMPORT CSV — ouvre un explorateur de fichiers et charge les voitures depuis le fichier choisi
    // Retourne une liste vide si l'utilisateur annule ou si le fichier est vide
    public async Task<List<Voiture>> LoadDataAsync()
    {
        var list = new List<Voiture>();

        // StorageProvider.OpenFilePickerAsync = ouvre le dialogue système "Ouvrir un fichier"
        // AllowMultiple=false = un seul fichier à la fois
        var files = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionnez un fichier CSV",
            AllowMultiple = false
        });

        // Si l'utilisateur ferme sans choisir → files est vide → on retourne la liste vide
        if (files.Count <= 0) return list;
        
        // Ouverture du fichier choisi en lecture
        // await using = ferme et libère le stream automatiquement même en cas d'exception
        await using var stream = await files[0].OpenReadAsync();
        using var reader = new StreamReader(stream, Encoding.UTF8); // UTF-8 pour les accents
        var lines = new List<string?>();

        // Lit le fichier ligne par ligne (async pour ne pas bloquer l'UI)
        while (!reader.EndOfStream) lines.Add(await reader.ReadLineAsync());

        if (lines.Count == 0) return list; // Fichier vide

        // LIGNE 0 = en-têtes (noms des colonnes)
        // Ex: "Nom;Marque;TypeCarburant;Origine;Moteur"
        var headers = lines[0].Split(';');
        
        // Récupère toutes les propriétés de la classe Voiture via réflexion
        // Permet de mapper n'importe quelle colonne CSV → propriété Voiture automatiquement
        var properties = typeof(Voiture).GetProperties();

        // Parcourt les lignes de données (i=1 pour sauter l'en-tête)
        for (var i = 1; i < lines.Count; i++)
        {
            var obj = new Voiture(); // Crée une nouvelle voiture vide pour cette ligne
            var values = lines[i]?.Split(';'); // Sépare les valeurs par ";"

            if (values != null)
            {
                // Pour chaque colonne (j) : trouve la propriété correspondante et assigne la valeur
                for (var j = 0; j < headers.Length && j < values.Length; j++)
                {
                    // Cherche la propriété par nom (OrdinalIgnoreCase = insensible à la casse)
                    // Ex: header "nom" → trouve propriété "Nom" dans Voiture
                    var property = properties.FirstOrDefault(p =>
                        p.Name.Equals(headers[j], StringComparison.OrdinalIgnoreCase));
                    
                    // Ignore si la propriété n'existe pas ou si la valeur est vide
                    if (property == null || string.IsNullOrWhiteSpace(values[j])) continue;

                    try
                    {
                        // Cas spécial : ObjectId MongoDB (pas convertible via Convert.ChangeType)
                        if (property.PropertyType == typeof(ObjectId))
                        {
                            var objectIdValue = new ObjectId(values[j]); // Parse la chaîne hex en ObjectId
                            property.SetValue(obj, objectIdValue);
                        }
                        else
                        {
                            // Conversion générique string → type de la propriété (string, int, bool, etc.)
                            var value = Convert.ChangeType(values[j], property.PropertyType);
                            property.SetValue(obj, value); // Assigne la valeur via réflexion
                        }
                    }
                    catch (Exception ex)
                    {
                        // Si la conversion échoue (valeur invalide), on remonte l'exception
                        throw new InvalidOperationException(ex.Message);
                    }
                }
            }

            list.Add(obj); // Ajoute la voiture construite à la liste résultat
        }
        return list;
    }

    // EXPORT CSV — ouvre un dialogue "Enregistrer sous" et sauvegarde les colonnes sélectionnées
    // colonnesSelectionnees = noms des propriétés Voiture à inclure (filtrées par les CheckBox)
    public async Task SaveDataAsync(List<Voiture> data, IEnumerable<string> colonnesSelectionnees)
    {
        // Récupère toutes les propriétés de Voiture pour les filtrer ensuite
        var allProperties = typeof(Voiture).GetProperties();

        // Filtre : garde uniquement les propriétés dont le nom est dans colonnesSelectionnees
        // Select → trouve la PropertyInfo correspondante
        // Where(p != null) → élimine les noms invalides
        var properties = colonnesSelectionnees
            .Select(nom => allProperties.FirstOrDefault(p =>
                p.Name.Equals(nom, StringComparison.OrdinalIgnoreCase)))
            .Where(p => p != null)
            .ToList();

        if (properties.Count == 0) return; // Rien à exporter si aucune colonne sélectionnée

        // Construction du contenu CSV en mémoire avec StringBuilder
        // StringBuilder est plus performant que la concaténation string + string pour les grandes listes
        var csv = new StringBuilder();
        
        // LIGNE 1 : les en-têtes
        // string.Join(";", ...) = joint les éléments avec ";" (format CSV français)
        csv.AppendLine(string.Join(";", properties.Select(p => p!.Name)));

        // LIGNES SUIVANTES : une ligne par voiture
        foreach (var item in data)
        {
            // Pour chaque propriété sélectionnée : récupère la valeur via réflexion
            // GetValue(item) = lit la valeur de la propriété sur l'objet item
            // ?. et ?? string.Empty = si la valeur est null → chaîne vide
            var values = properties.Select(p => p!.GetValue(item)?.ToString() ?? string.Empty);
            csv.AppendLine(string.Join(";", values));
        }

        // Ouvre le dialogue système "Enregistrer sous"
        var file = await _topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Enregistrer le fichier CSV",
            SuggestedFileName = "voitures.csv", // Nom proposé par défaut
            FileTypeChoices = new[]
            {
                // Filtre de type de fichier : seuls les .csv sont affichés
                new FilePickerFileType("Fichier CSV") { Patterns = new[] { "*.csv" } }
            }
        });

        // null si l'utilisateur a annulé → on ne fait rien
        if (file != null)
        {
            // Ouvre le fichier en écriture et écrit le contenu CSV
            // await using = ferme et libère le stream automatiquement
            await using var stream = await file.OpenWriteAsync();
            using var writer = new StreamWriter(stream, Encoding.UTF8); // UTF-8 pour les accents
            await writer.WriteAsync(csv.ToString());
        }
    }
}