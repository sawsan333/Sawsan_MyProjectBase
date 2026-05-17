using System.Text.Json.Serialization;
using Avalonia.Media;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyProjectBase.Models;

// Chaque instance correspond à un document dans une collection MongoDB

// STRUCTURE BASE DE DONNÉES :
//   Collection "MesVoitures"      → catalogue global (partagé par tous)
//   Collection "Voitures_{userId}" → copie privée de chaque utilisateur

// ANNOTATIONS MONGODB (MongoDB.Bson) :
//   [BsonId]              → ce champ est le _id du document MongoDB
//   [BsonRepresentation]  → comment le type C# est stocké en DB
//   [BsonIgnore]          → ce champ n'est PAS stocké en DB
//   [BsonIgnoreExtraElements] → si le document MongoDB a des champs inconnus, on les ignore (évite les crashes)

[BsonIgnoreExtraElements]
public class Voiture
{
    // Identifiant unique MongoDB (ObjectId = chaîne hexadécimale de 24 caractères)
    // [BsonId] = c'est le champ _id dans MongoDB (clé primaire)
    // [BsonRepresentation(ObjectId)] = stocké comme ObjectId en DB, manipulé comme string en C#
    // Généré automatiquement par MongoDB à l'insertion, ou manuellement via ObjectId.GenerateNewId()
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = "";

    // Informations descriptives de la voiture
    public string Nom { get; set; } = "";          
    public string Description { get; set; } = "";  
    public string Origine { get; set; } = "";       
    public string Marque { get; set; } = "";        
    public string TypeCarburant { get; set; } = ""; 
    public string PicturePath { get; set; } = "";   

    // Image chargée en mémoire (non stockée en DB)
    // [BsonIgnore] = MongoDB ne sérialise/désérialise pas ce champ
    // [JsonIgnore] = pas sérialisé en JSON non plus (pas dans le CSV)
    // Chargé après récupération depuis MongoDB via ImageHelper.LoadFromResource(PicturePath)
    [BsonIgnore]
    [JsonIgnore]
    public IImage? Image { get; set; } // null si l'image n'a pas encore été chargée

    
    public string Moteur { get; set; } = "";       
    public string Performance { get; set; } = "";  
    public string Consommation { get; set; } = ""; 
    public string Confort { get; set; } = "";      
}