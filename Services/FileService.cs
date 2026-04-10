//using INF1009.Models;

namespace INF1009.Services;

/* 
 Centralise tout accès aux fichiers de simulation.
 Fichiers gérés :
   Slec.txt — jeu d'essai, lu par ET (une communication par ligne)
   Secr.txt — résultats écrits par ET
   Lecr.txt — paquets émis par ER vers la couche liaison
   Llec.txt — réponses reçues de la couche liaison (simulées)
*/
public class FileService
{
    private readonly string _slecPath;
    private readonly string _secrPath;
    private readonly string _lecrPath;
    private readonly string _llecPath;

    // Verrou partagé pour les écritures concurrentes (ET et ER tournent en parallèle).
    private readonly object _writeLock = new();

    public FileService(string slecPath, string secrPath,
                       string lecrPath,  string llecPath)
    {
        _slecPath = slecPath;
        _secrPath = secrPath;
        _lecrPath = lecrPath;
        _llecPath = llecPath;
    }
    
    // Initialisation
    
    /* <summary>
    /// Vide les trois fichiers de sortie au démarrage de la simulation.
    /// Appeler depuis Program.cs avant de lancer ET et ER.
    */
    public void ClearOutputFiles()
    {
        File.WriteAllText(_secrPath, string.Empty);
        File.WriteAllText(_lecrPath, string.Empty);
        File.WriteAllText(_llecPath, string.Empty);
    }

   
    // Lecture — Slec.txt
    
    /*
     Lit toutes les demandes du jeu d'essai.
     Chaque ligne non vide représente un message à transmettre.
     Les lignes vides et commentaires (# …) sont ignorés.
    */
    public IEnumerable<string> ReadRequests()
    {
        if (!File.Exists(_slecPath))
            throw new FileNotFoundException($"Fichier jeu d'essai introuvable : {_slecPath}");

        return File.ReadLines(_slecPath)
                   .Select(l => l.Trim())
                   .Where(l => l.Length > 0 && !l.StartsWith('#'));
    }

    
    // Écriture — Secr.txt (résultats ET)
    
    // Enregistre un résultat de connexion dans Secr.txt.
    
    public void WriteResult(int endpointId, int src, int dst, string result)
    {
        var line = $"[IDENT {endpointId,3}] src={src,-3} dst={dst,-3} → {result}";
        AppendLine(_secrPath, line);
    }
    
    // Écriture — Lecr.txt (paquets émis vers la liaison)
    
    // Enregistre un paquet émis par ER vers la couche liaison.
    
    public void WriteEmittedPacket(int connNum, string label, string binaryPayload)
    {
        var line = $"[CONN {connNum,3}] EMIT  {label,-30}: {binaryPayload}";
        AppendLine(_lecrPath, line);
    }
    
    // Écriture — Llec.txt (réponses reçues de la liaison)
    
    // Enregistre une réponse reçue (ou une absence de réponse) de la liaison.
    public void WriteReceivedResponse(int connNum, string label,
                                       string? binaryPayload = null)
    {
        var line = binaryPayload is not null
            ? $"[CONN {connNum,3}] RECV  {label,-30}: {binaryPayload}"
            : $"[CONN {connNum,3}] RECV  {label}";
        AppendLine(_llecPath, line);
    }
    
    // Helpers privés

    private void AppendLine(string path, string line)
    {
        lock (_writeLock)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }
}