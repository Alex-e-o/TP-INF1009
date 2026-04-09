using INF1009.Models;

namespace INF1009.Services;

public class TransportEntity
{
    private readonly FileService _fileService;
    private readonly NetworkEntity _networkEntity;

    private readonly Dictionary<int, ConnectionContext> _connections = new();
    private int _nextEndpointId = 1;

    public TransportEntity(FileService fileService, NetworkEntity networkEntity)
    {
        _fileService = fileService;
        _networkEntity = networkEntity;
    }

    public void ProcessAllRequests()
    {
        List<string> lines = _fileService.ReadAllInputLines();

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            ProcessInputLine(line);
        }
    }

    private void ProcessInputLine(string line)
    {
        string[] parts = line.Split(';');

        if (parts.Length < 1)
            return;

        string command = parts[0].Trim().ToUpperInvariant();

        switch (command)
        {
            case "CONNECT":
                ProcessConnect(parts);
                break;

            case "DATA":
                ProcessData(parts);
                break;

            case "DISCONNECT":
                ProcessDisconnect(parts);
                break;

            default:
                _fileService.WriteTransportResult($"Commande inconnue : {line}");
                break;
        }
    }

    private void ProcessConnect(string[] parts)
    {
        if (parts.Length < 3)
            return;

        int endpointId = _nextEndpointId++;
        byte source = byte.Parse(parts[1]);
        byte destination = byte.Parse(parts[2]);

        var primitive = new Primitive
        {
            Type = PrimitiveType.NConnectReq,
            EndpointId = endpointId,
            SourceAddress = source,
            DestinationAddress = destination
        };

        Primitive response = _networkEntity.HandlePrimitive(primitive);

        var context = new ConnectionContext
        {
            EndpointId = endpointId,
            SourceAddress = source,
            DestinationAddress = destination,
            State = response.Type == PrimitiveType.NConnectConf
                ? ConnectionState.Established
                : ConnectionState.Refused
        };

        _connections[endpointId] = context;

        _fileService.WriteTransportResult(
            $"CONNECT endpoint={endpointId} src={source} dst={destination} => {response.Type} ({response.Reason})");
    }

    private void ProcessData(string[] parts)
    {
        if (parts.Length < 3)
            return;

        int endpointId = int.Parse(parts[1]);
        string data = parts[2];

        if (!_connections.TryGetValue(endpointId, out ConnectionContext? context))
        {
            _fileService.WriteTransportResult($"DATA impossible : endpoint {endpointId} introuvable");
            return;
        }

        var primitive = new Primitive
        {
            Type = PrimitiveType.NDataReq,
            EndpointId = endpointId,
            SourceAddress = context.SourceAddress,
            DestinationAddress = context.DestinationAddress,
            UserData = data
        };

        Primitive response = _networkEntity.HandlePrimitive(primitive);

        _fileService.WriteTransportResult(
            $"DATA endpoint={endpointId} => {response.Type} ({response.Reason})");
    }

    private void ProcessDisconnect(string[] parts)
    {
        if (parts.Length < 2)
            return;

        int endpointId = int.Parse(parts[1]);

        if (!_connections.TryGetValue(endpointId, out ConnectionContext? context))
        {
            _fileService.WriteTransportResult($"DISCONNECT impossible : endpoint {endpointId} introuvable");
            return;
        }

        var primitive = new Primitive
        {
            Type = PrimitiveType.NDisconnectReq,
            EndpointId = endpointId,
            SourceAddress = context.SourceAddress,
            DestinationAddress = context.DestinationAddress
        };

        Primitive response = _networkEntity.HandlePrimitive(primitive);

        _connections.Remove(endpointId);

        _fileService.WriteTransportResult(
            $"DISCONNECT endpoint={endpointId} => {response.Type} ({response.Reason})");
    }
}