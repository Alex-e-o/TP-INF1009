//using INF1009.Models;

namespace INF1009.Services;

// Centralise tout accès aux fichiers de simulation.
// Slec.txt — jeu d'essai lu par ET
// Secr.txt — résultats écrits par ET
// Lecr.txt — paquets émis par ER vers la couche liaison
// Llec.txt — réponses reçues de la couche liaison (simulées)
public class FileService
{
    private readonly string _slecPath;
    private readonly string _secrPath;
    private readonly string _lecrPath;
    private readonly string _llecPath;

    // Verrou pour éviter les conflits d'écriture si ET et ER écrivent en même temps
    private readonly object _writeLock = new();

    public FileService(string slecPath, string secrPath,
                       string lecrPath,  string llecPath)
    {
        _slecPath = slecPath;
        _secrPath = secrPath;
        _lecrPath = lecrPath;
        _llecPath = llecPath;
    }

    // Vide les fichiers de sortie avant de démarrer une nouvelle simulation
    public void ClearOutputFiles()
    {
        File.WriteAllText(_secrPath, string.Empty);
        File.WriteAllText(_lecrPath, string.Empty);
        File.WriteAllText(_llecPath, string.Empty);
    }

    // Retourne les lignes utiles de Slec.txt (ignore les commentaires et lignes vides)
    public IEnumerable<string> ReadRequests()
    {
        if (!File.Exists(_slecPath))
            throw new FileNotFoundException($"Fichier jeu d'essai introuvable : {_slecPath}");

        return File.ReadLines(_slecPath)
                   .Select(l => l.Trim())
                   .Where(l => l.Length > 0 && !l.StartsWith('#'));
    }

    // Écrit une ligne de résultat dans Secr.txt
    public void WriteResult(int endpointId, int src, int dst, string result)
    {
        var line = $"[IDENT {endpointId,3}] src={src,-3} dst={dst,-3} → {result}";
        AppendLine(_secrPath, line);
    }

    // Écrit un paquet émis par ER dans Lecr.txt
    public void WriteEmittedPacket(int connNum, string label, string binaryPayload)
    {
        var line = $"[CONN {connNum,3}] EMIT  {label,-30}: {binaryPayload}";
        AppendLine(_lecrPath, line);
    }

    // Écrit la réponse reçue (ou l'absence de réponse) dans Llec.txt
    public void WriteReceivedResponse(int connNum, string label,
                                       string? binaryPayload = null)
    {
        var line = binaryPayload is not null
            ? $"[CONN {connNum,3}] RECV  {label,-30}: {binaryPayload}"
            : $"[CONN {connNum,3}] RECV  {label}";
        AppendLine(_llecPath, line);
    }

    private void AppendLine(string path, string line)
    {
        lock (_writeLock)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }
}
