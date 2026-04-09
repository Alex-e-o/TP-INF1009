using INF1009.Models;

namespace INF1009.Services;

public class FileService
{
    public string SlecPath { get; }
    public string SecrPath { get; }
    public string LecrPath { get; }
    public string LlecPath { get; }

    public FileService(string slecPath, string secrPath, string lecrPath, string llecPath)
    {
        SlecPath = slecPath;
        SecrPath = secrPath;
        LecrPath = lecrPath;
        LlecPath = llecPath;
    }

    public List<string> ReadAllInputLines()
    {
        if (!File.Exists(SlecPath))
            return new List<string>();

        return File.ReadAllLines(SlecPath).ToList();
    }

    public void WriteTransportResult(string line)
    {
        File.AppendAllText(SecrPath, line + Environment.NewLine);
    }

    public void WriteLinkOutput(Packet packet)
    {
        File.AppendAllText(LecrPath, packet + Environment.NewLine);
    }

    public void WriteLinkInput(int connectionNumber, Packet? packet)
    {
        string line = packet is null
            ? $"Conn={connectionNumber} | No response"
            : $"Conn={connectionNumber} | {packet}";

        File.AppendAllText(LlecPath, line + Environment.NewLine);
    }

    public void ClearOutputFiles()
    {
        File.WriteAllText(SecrPath, string.Empty);
        File.WriteAllText(LecrPath, string.Empty);
        File.WriteAllText(LlecPath, string.Empty);
    }
}