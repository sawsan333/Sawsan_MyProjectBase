using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MyProjectBase.Models;

namespace MyProjectBase.Services;

// Toutes les opérations base de données passent par cette classe
// Pattern "Service" : on isole la logique d'accès aux données ici, hors des ViewModels
// Une seule instance est créée dans MainWindowViewModel et partagée entre toutes les pages

// STRUCTURE DE LA BASE "VoituresDB" :
//   Collection "Users"             → tous les comptes (LoginAsync, RegisterAsync, GetAllUsersAsync)
//   Collection "MesVoitures"       → catalogue global de voitures disponibles (GetCatalogueAsync)
//   Collection "Voitures_{userId}" → collection PRIVÉE de chaque utilisateur
//                                    Créée automatiquement par MongoDB au premier InsertOne

// ASYNC/AWAIT : toutes les méthodes sont async car les opérations réseau (MongoDB distant)
//              ne doivent jamais bloquer le thread UI
public class MongoServices
{
    // Référence à la base de données MongoDB
    private readonly IMongoDatabase _database;

    // Connexion au serveur MongoDB distant
    public MongoServices()
    {
        var client = new MongoClient("mongodb://Meeeee:IAmTheBest@185.157.245.38:443");
        _database = client.GetDatabase("VoituresDB"); // Sélectionne la base de données
    }

    // Retourne la collection privée d'un utilisateur (créée automatiquement si inexistante)
    // userId = Username de l'utilisateur
    // $"Voitures_{userId}" = interpolation de chaîne (syntaxe C# moderne)
    private IMongoCollection<Voiture> GetUserCollection(string userId)
    {
        return _database.GetCollection<Voiture>($"Voitures_{userId}");
    }

    // Propriété raccourci vers la collection des utilisateurs
    // => = expression body : évalué à chaque accès
    private IMongoCollection<User> Users =>
        _database.GetCollection<User>("Users");

    // Hachage SHA-256 du mot de passe avant stockage
    // SHA-256 = algorithme de hachage unidirectionnel (impossible de retrouver le mot de passe original)
    // On stocke le haché → lors du login on hache la saisie et on compare les hachés
    // Convert.ToBase64String = convertit les bytes en chaîne Base64 (stockable en DB)
    private string Hash(string input)
    {
        using var sha = SHA256.Create(); // using = libère les ressources automatiquement après
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }

    // AUTHENTIFICATION 
    // Connexion : cherche un utilisateur dont le username OU l'email correspond et dont le mot de passe hashé correspond
    // Retourne null si aucun utilisateur trouvé (identifiants incorrects)
    public async Task<User?> LoginAsync(string username, string password)
    {
        var hashed = Hash(password); // Hache le mot de passe saisi avant de comparer
        return await Users.Find(u =>
            (u.Username == username || u.Email == username) && // Login par username OU email
            u.Password == hashed                              // Compare les hachés (jamais les mots de passe en clair)
        ).FirstOrDefaultAsync(); // Retourne le premier résultat, ou null si aucun
    }

    // Inscription : crée un nouveau compte en vérifiant l'unicité de l'email
    // Le mot de passe est hashé ici avant insertion (User.Password est modifié)
    public async Task RegisterAsync(User user)
    {
        user.Password = Hash(user.Password); // Hash avant stockage — OBLIGATOIRE
        
        // Vérifie si un compte avec cet email existe déjà (AnyAsync = plus efficace que Count > 0)
        var existe = await Users.Find(u => u.Email == user.Email).AnyAsync();
        if (existe)
            throw new Exception("Cet email est déjà utilisé."); // Attrapée dans LoginViewModel.Register
        
        await Users.InsertOneAsync(user); // MongoDB assigne automatiquement l'Id (ObjectId)
    }

    // GESTION DES UTILISATEURS (admin uniquement) 

    // Récupère tous les utilisateurs pour la page d'admin
    // _ => true = filtre "tout le monde" (équivalent de SELECT * FROM Users)
    public async Task<List<User>> GetAllUsersAsync()
        => await Users.Find(_ => true).ToListAsync();

    // Met à jour un utilisateur existant
    // ReplaceOne = équivalent UPDATE ... WHERE Id = userId
    public async Task UpdateUserAsync(User user)
        => await Users.ReplaceOneAsync(u => u.Id == user.Id, user);

    // Supprime un utilisateur par son Id MongoDB
    // DeleteOne = équivalent DELETE FROM Users WHERE Id = userId
    public async Task DeleteUserAsync(string userId)
        => await Users.DeleteOneAsync(u => u.Id == userId);

    // VOITURES (collection privée de l'utilisateur)

    // Récupère toutes les voitures de la collection privée de l'utilisateur
    public async Task<List<Voiture>> GetVoituresAsync(string userId)
        => await GetUserCollection(userId).Find(_ => true).ToListAsync();

    // Ajoute une voiture dans la collection privée
    // InsertOneAsync = INSERT INTO Voitures_{userId} VALUES (v)
    public async Task AddVoitureAsync(string userId, Voiture v)
    {
        await GetUserCollection(userId).InsertOneAsync(v);
    }

    // Supprime une voiture par son Id
    // v.Id = Id MongoDB de la voiture (ObjectId 24 chars)
    public async Task DeleteVoitureAsync(string userId, string id)
    {
        await GetUserCollection(userId)
            .DeleteOneAsync(v => v.Id == id);
    }

    // Met à jour une voiture existante
    // x.Id == v.Id = filtre pour trouver le bon document (clé primaire)
    public async Task UpdateVoitureAsync(string userId, Voiture v)
    {
        await GetUserCollection(userId)
            .ReplaceOneAsync(x => x.Id == v.Id, v);
    }

    //  Collection "MesVoitures"
    // Récupère le catalogue partagé de voitures disponibles à l'ajout
    // "MesVoitures" = collection globale 
    // C'est depuis ce catalogue que les utilisateurs choisissent quoi ajouter à leur collection
    public async Task<List<Voiture>> GetCatalogueAsync()
    {
        var collection = _database.GetCollection<Voiture>("MesVoitures");
        return await collection.Find(_ => true).ToListAsync();
    }
}