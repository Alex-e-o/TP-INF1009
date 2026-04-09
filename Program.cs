using INF1009.Services;

namespace INF1009;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("=== Début de la simulation du service de réseau ===");

        try
        {
            string projectRoot = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, @"..\..\.."));

            string dataFolder = Path.Combine(projectRoot, "Data");

            string slecPath = Path.Combine(dataFolder, "Slec.txt");
            string secrPath = Path.Combine(dataFolder, "Secr.txt");
            string lecrPath = Path.Combine(dataFolder, "Lecr.txt");
            string llecPath = Path.Combine(dataFolder, "Llec.txt");

            EnsureDataFolderExists(dataFolder);

            var fileService = new FileService(slecPath, secrPath, lecrPath, llecPath);
            var segmentationService = new SegmentationService();
            var linkServiceSimulator = new LinkServiceSimulator(fileService);
            var networkEntity = new NetworkEntity(linkServiceSimulator, segmentationService);
            var transportEntity = new TransportEntity(fileService, networkEntity);

            fileService.ClearOutputFiles();

            if (!File.Exists(slecPath))
            {
                Console.WriteLine("Le fichier Slec.txt est introuvable.");
                Console.WriteLine($"Chemin attendu : {slecPath}");
                return;
            }

            transportEntity.ProcessAllRequests();

            Console.WriteLine("Simulation terminée avec succès.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Une erreur est survenue pendant l'exécution du programme.");
            Console.WriteLine($"Détail : {ex.Message}");
        }

        Console.WriteLine("=== Fin du programme ===");
    }

    private static void EnsureDataFolderExists(string dataFolder)
    {
        if (!Directory.Exists(dataFolder))
        {
            Directory.CreateDirectory(dataFolder);
            Console.WriteLine($"Dossier créé : {dataFolder}");
        }
    }
}