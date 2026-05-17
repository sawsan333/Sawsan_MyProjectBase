using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace MyProjectBase.Helpers
{
    // Chargement d'images depuis différentes sources
    
    // static = méthodes utilitaires appelables sans instanciation : ImageHelper.LoadFromResource(...)
    // Utilisé dans les ViewModels pour charger les images des voitures après récupération depuis MongoDB
    
    // 2 MÉTHODES :
    //   LoadFromResource → charge depuis les Assets embarqués dans l'application (format "avares://...")
    //   LoadFromWeb      → charge depuis une URL HTTP/HTTPS (non utilisé actuellement mais disponible)
    public static class ImageHelper
    {
        // Charge une image depuis les ressources embarquées dans le binaire de l'application
        
        // resourceUri format : "avares://MyProjectBase/Assets/bmw.png"
        //   "avares://"    = préfixe Avalonia pour les ressources embarquées
        //   "MyProjectBase" = nom du projet 
        //   "Assets/bmw.png" = chemin dans le dossier Assets
        
        // Les fichiers dans Assets/ sont déclarés comme ressources dans MyProjectBase.csproj
        // avec <AvaloniaResource Include="Assets/**" />
        
        // AssetLoader.Open(uri) = ouvre un Stream vers la ressource embarquée
        // new Bitmap(stream) = crée un Bitmap Avalonia depuis le stream
        public static Bitmap LoadFromResource(Uri resourceUri)
        {
            return new Bitmap(AssetLoader.Open(resourceUri));
        }

        // Charge une image depuis une URL web (HTTP/HTTPS)
        // async : le téléchargement réseau ne doit pas bloquer l'UI
        // Bitmap? : retourne null en cas d'erreur (le ? indique que la valeur peut être null)
        
        public static async Task<Bitmap?> LoadFromWeb(Uri url)
        {
            // using var = libère le HttpClient automatiquement après l'utilisation
            // HttpClient ne devrait normalement pas être instancié à chaque appel
            using var httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode(); 
                
                // Télécharge le contenu binaire de l'image puis le convertit en Bitmap
                var data = await response.Content.ReadAsByteArrayAsync();
                return new Bitmap(new MemoryStream(data)); // MemoryStream = flux en mémoire depuis les bytes
            }
            catch (HttpRequestException ex)
            {
                // En cas d'erreur réseau (timeout, 404, DNS, etc.) : log et retourne null
                // L'appelant doit gérer le cas null (ex: afficher une image par défaut)
                Console.WriteLine($"An error occurred while downloading image '{url}' : {ex.Message}");
                return null;
            }
        }
    }
}