// Connexion et lecture du scanner USB/COM

// DONNÉES REÇUES :
//   Le scanner envoie soit du JSON { ... } soit du texte brut (code-barre)
//   Les données arrivent par fragments → buffering dans _buffer
//   Quand un message complet est détecté → mis dans SerialBuffer (file FIFO)
//   Les ViewModels s'abonnent à SerialBuffer.Changed pour être notifiés
using System;
using System.Collections;
using System.IO;
using System.IO.Ports;
using System.Linq;

namespace MyProjectBase.Services;

// partial = la classe peut être divisée en plusieurs fichiers .cs (convention Avalonia/CommunityToolkit)
public partial class ScannerManager
{
    private SerialPort? _mySerialPort; // L'objet de connexion au port COM (null = port fermé)
    private string? _portDetected;    // Nom du port détecté 
    private string _buffer = "";      // Buffer d'accumulation des données reçues par fragments
    
    // File FIFO des messages complets reçus du scanner
    // public readonly = accessible depuis les ViewModels, mais non remplaçable
    public readonly QueueBuffer SerialBuffer = new();

    // OUVERTURE DU PORT — Détecte automatiquement le port et ouvre la connexion
    // Appelé dans MainWindowViewModel (constructeur) au démarrage de l'application
    public void OpenPort()
    {
        // Si un port était déjà ouvert, on le ferme proprement avant d'en ouvrir un nouveau
        if (_mySerialPort != null)
        {
            try
            {
                if (_mySerialPort.IsOpen) _mySerialPort.Close();
                _mySerialPort.Dispose(); // Libère le handle système
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Failed to close the port."); 
            }
            finally
            {
                _mySerialPort = null; // Toujours remettre à null (même en cas d'exception)
            }
        }

        // DÉTECTION WINDOWS 
        // WMI = Windows Management Instrumentation (API système Windows)
        // Win32_PnPEntity = liste tous les périphériques Plug and Play
        // On cherche ceux qui ont "(COM" dans leur nom (= ports COM dans le gestionnaire de périphériques)
        if (OperatingSystem.IsWindows())
        {
#if WINDOWS // Directive de compilation : ce code n'est compilé que pour Windows
            var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");

            foreach (System.Management.ManagementBaseObject baseObj in searcher.Get())
            {
                string id  = baseObj["PNPDeviceID"]?.ToString() ?? ""; // Ex: "USB\VID_xxxx&PID_A4A7\..."
                string nom = baseObj["Name"]?.ToString() ?? "";          // Ex: "Scanner M900D (COM3)"

                // PID_A4A7 = Product ID USB du scanner M900D (identifiant hardware)
                if (id.Contains("PID_A4A7"))
                {
                    // Extrait "COM3" depuis "Scanner M900D (COM3)"
                    // LastIndexOf("COM") → position de "COM" dans la chaîne
                    // LastIndexOf(")")   → position du ")" de fermeture
                    int debut = nom.LastIndexOf("COM", StringComparison.Ordinal);
                    int fin   = nom.LastIndexOf(")",   StringComparison.Ordinal);
                    if (debut != -1 && fin != -1)
                    {
                        _portDetected = nom.Substring(debut, fin - debut); // Ex: "COM3"
                        break; // Trouvé → on arrête la boucle
                    }
                }
            }
#endif
        }
        // DÉTECTION LINUX 
        // /dev/serial/by-id/ = dossier Linux contenant des liens symboliques nommés
        //   par l'identifiant du fabricant (plus fiable que /dev/ttyACM0 qui peut changer)
        else if (OperatingSystem.IsLinux())
        {
            const string byId = "/dev/serial/by-id";

            if (Directory.Exists(byId))
            {
                foreach (var device in Directory.GetFiles(byId))
                {
                    // "20080411" = partie de l'identifiant USB du M900D sous Linux
                    if (device.Contains("20080411", StringComparison.OrdinalIgnoreCase))
                    {
                        // GetFullPath résout le lien symbolique → retourne le vrai chemin (/dev/ttyACM0)
                        _portDetected = Path.GetFullPath(device);
                        break;
                    }
                }
            }

            // Si by-id n'a rien trouvé → essaie /dev/ttyACM0 (port Arduino par défaut sous Linux)
            if (_portDetected == null && File.Exists("/dev/ttyACM0"))
                _portDetected = "/dev/ttyACM0";
        }

        // FALLBACK UNIVERSEL 
        // Si aucune détection spécifique n'a fonctionné → essaie les ports dans l'ordre
        // FirstOrDefault(File.Exists) = retourne le premier port dont le fichier existe
        if (_portDetected == null)
        {
            string[] fallbacks = ["/dev/ttyACM0", "/dev/ttyUSB0", "/dev/ttyACM1"];
            _portDetected = fallbacks.FirstOrDefault(File.Exists); // null si aucun trouvé
        }

        // CONFIGURATION ET OUVERTURE 
        // Paramètres identiques pour M900D et Arduino UNO (imposés par le cahier des charges)
        _mySerialPort = new SerialPort
        {
            BaudRate     = 9600,           // Vitesse 
            PortName     = _portDetected,  // Port détecté ci-dessus
            Parity       = Parity.None,    // Pas de bit de parité (vérification d'erreur désactivée)
            DataBits     = 8,              // 8 bits de données par trame (standard)
            StopBits     = StopBits.One,   // 1 bit de stop
            ReadTimeout  = 10000,          // Timeout lecture 
            WriteTimeout = 10000           // Timeout écriture 
        };

        // Abonne DataHandler à l'événement DataReceived
        // DataHandler sera appelé automatiquement à chaque arrivée de données sur le port
        _mySerialPort.DataReceived += DataHandler;
        _mySerialPort.Open(); // Lance la communication (ouvre le port COM)
    }

    // FERMETURE DU PORT — Ferme proprement la connexion
    // Appelé dans ViewModelBase.Dispose() quand on change de page ou ferme l'app
    public void ClosePort()
    {
        // Pattern matching C# : "is not { IsOpen: true }" = vrai si _mySerialPort est null OU non ouvert
        // Si le port n'est pas ouvert → rien à fermer
        if (_mySerialPort is not { IsOpen: true }) return;

        try
        {
            _mySerialPort.Close();   // Ferme la connexion série
            _mySerialPort.Dispose(); // Libère les ressources système (handle OS)
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error closing port: {ex.Message}", ex);
        }
        finally
        {
            _mySerialPort = null; // Reset dans tous les cas (même en cas d'exception)
        }
    }

    // GESTIONNAIRE DE DONNÉES — Appelé automatiquement à chaque réception de données USB
    // Les données arrivent souvent par FRAGMENTS (ex: "{\"mar" puis "que\":\"BMW\"}") → buffering
    // On accumule dans _buffer jusqu'à avoir un message complet
    private void DataHandler(object sender, EventArgs arg)
    {
        SerialPort sp = (SerialPort)sender; // Cast de l'objet sender en SerialPort
        
        // ReadExisting() = lit TOUT ce qui est disponible dans le buffer interne du port série
        // On concatène au buffer existant (les données arrivent en morceaux)
        _buffer += sp.ReadExisting();

        // Cherche les délimiteurs d'un objet JSON : { et }
        int start = _buffer.IndexOf('{');     // Position du premier {
        int end   = _buffer.LastIndexOf('}'); // Position du dernier }

        if (start != -1 && end != -1 && end > start)
        {
            // JSON complet détecté → extrait le JSON et le met dans la file
            string jsonComplete = _buffer.Substring(start, end - start + 1); // Ex: {"marque":"BMW"}
            _buffer = ""; // Vide le buffer
            SerialBuffer.Enqueue(jsonComplete); // → déclenche l'événement Changed
        }
        else if (!_buffer.Contains('{') &&
                 (_buffer.EndsWith("\n") || _buffer.EndsWith("\r") || _buffer.Length > 30))
        {
            // Pas de JSON détecté → c'est un message texte simple (ex: code-barre EAN/QR)
            // Condition : pas de { ET (fin de ligne OU buffer trop long → message probablement complet)
            var toSend = _buffer.Trim(); // Enlève les espaces/retours à la ligne
            _buffer = "";
            if (!string.IsNullOrEmpty(toSend))
                SerialBuffer.Enqueue(toSend); // Met le texte dans la file
        }
        // Sinon : données incomplètes → on attend plus de données (buffer conservé)
    }

    // QUEUEBUFFER — File FIFO (First In First Out) avec notification de changement
    // Hérite de Queue (System.Collections) pour le stockage FIFO des messages
    // Ajoute un événement Changed pour notifier les abonnés à chaque nouveau message
    
    // UTILISATION :
    //   SerialBuffer.Changed += (s, e) => { var msg = (string)SerialBuffer.Dequeue(); ... }
    //   Les ViewModels s'abonnent pour réagir en temps réel aux scans du scanner
    public sealed partial class QueueBuffer : Queue
    {
        // Événement déclenché à chaque Enqueue (nouveau message reçu)
        // Les ViewModels s'y abonnent pour traiter les scans
        // EventHandler? = nullable (pas d'abonnés = l'événement n'est pas déclenché)
        public event EventHandler? Changed;
        
        // Override de Enqueue : stocke le message PUIS notifie les abonnés
        public override void Enqueue(object? obj)
        {
            base.Enqueue(obj); // Comportement standard (stockage dans la file)
            Changed?.Invoke(this, EventArgs.Empty); // ?. = appel conditionnel (si des abonnés existent)
        }
    }
}