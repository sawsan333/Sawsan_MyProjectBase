using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyProjectBase.Models;

// SÉCURITÉ : le mot de passe est TOUJOURS hashé (SHA-256) avant stockage
//            On ne stocke jamais le mot de passe en clair — voir MongoServices.Hash()
public class User
{
    // Identifiant unique MongoDB
    // ? = nullable : null avant que MongoDB l'assigne lors de l'InsertOne
    // Généré automatiquement par MongoDB à la création du compte
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    // Nom d'utilisateur : sert aussi d'identifiant de collection MongoDB
    // "Voitures_{Username}" = nom de la collection privée de cet utilisateur
    public string Username { get; set; } = "";
    
    // Email : peut aussi servir de login (LoginAsync accepte username OU email)
    // Doit être unique (vérifié dans MongoServices.RegisterAsync)
    public string Email { get; set; } = "";
    
    // Mot de passe HASHÉ en SHA-256
    // Stocké en base64 après hachage : Convert.ToBase64String(sha256.ComputeHash(...))
    // Lors du login : on hache la saisie et on compare les hachés
    public string Password { get; set; } = "";
    
    // Rôle qui détermine les droits d'accès :
    //   "user"  → collection privée uniquement, pas accès à la page Admin
    //   "admin" → collection dédiée + accès à la page d'admin (créer/modifier/supprimer users)
    // Défaut "user" à la création (l'admin peut le modifier ensuite via AdminView)
    public string Role { get; set; } = "user";
}