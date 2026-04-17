using INF1009.Services;
using System.Diagnostics;

namespace INF1009;

// Point d'entrée de la simulation réseau (couches Transport et Réseau)
internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("════════════════════════════════════════════════");
        Console.WriteLine("    Simulation du service de réseau — INF1009   ");
        Console.WriteLine("════════════════════════════════════════════════");
        Console.WriteLine();

        var chrono = Stopwatch.StartNew();

        try
        {
            // Résolution des chemins 
            string projectRoot = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

            string dataFolder = Path.Combine(projectRoot, "Data");

            string slecPath = Path.Combine(dataFolder, "Slec.txt");
            string secrPath = Path.Combine(dataFolder, "Secr.txt");
            string lecrPath = Path.Combine(dataFolder, "Lecr.txt");
            string llecPath = Path.Combine(dataFolder, "Llec.txt");

            EnsureDataFolderExists(dataFolder);

            Console.WriteLine("[1/3] Fichiers de données :");
            Console.WriteLine($"      Entrée  (S_lec) : {slecPath}");
            Console.WriteLine($"      Sortie  (S_ecr) : {secrPath}");
            Console.WriteLine($"      Entrée  (L_ecr) : {lecrPath}");
            Console.WriteLine($"      Sortie  (L_lec) : {llecPath}");
            Console.WriteLine();

            if (!File.Exists(slecPath))
            {
                Console.WriteLine($"[ERREUR] Fichier introuvable : {slecPath}");
                return;
            }

            // Instanciation des couches 
            Console.WriteLine("[2/3] Initialisation des entités...");
            var fileService          = new FileService(slecPath, secrPath, lecrPath, llecPath);
            var segmentationService  = new SegmentationService();
            var linkServiceSimulator = new LinkServiceSimulator(fileService);
            var channel              = new PrimitiveChannel();
            var networkEntity        = new NetworkEntity(linkServiceSimulator, segmentationService, channel);
            var transportEntity      = new TransportEntity(fileService, channel);

            fileService.ClearOutputFiles();

            // Lancement de la simulation dans deux threads séparés 
            Console.WriteLine("[3/3] Exécution de la simulation...");

            var erThread = new Thread(networkEntity.RunLoop) { Name = "ER" };
            var etThread = new Thread(transportEntity.Run)   { Name = "ET" };

            erThread.Start();
            etThread.Start();

            etThread.Join();
            erThread.Join();

            chrono.Stop();
            Console.WriteLine();
            Console.WriteLine($"  Simulation terminée avec succès en {chrono.ElapsedMilliseconds} ms.");
        }
        catch (Exception ex)
        {
            chrono.Stop();
            Console.WriteLine();
            Console.WriteLine("[ERREUR] Une exception est survenue pendant l'exécution :");
            Console.WriteLine($"  Type   : {ex.GetType().Name}");
            Console.WriteLine($"  Détail : {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("══════════════════════════════════════════════════");
        Console.WriteLine("  Fin du programme.");
        Console.WriteLine("══════════════════════════════════════════════════");
    }

    // Crée le dossier Data s'il n'existe pas encore
    private static void EnsureDataFolderExists(string dataFolder)
    {
        if (!Directory.Exists(dataFolder))
        {
            Directory.CreateDirectory(dataFolder);
            Console.WriteLine($"  Dossier créé : {dataFolder}");
        }
    }
}