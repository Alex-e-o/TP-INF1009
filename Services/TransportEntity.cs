using System.Text;
using INF1009.Models;

namespace INF1009.Services;

public class TransportEntity
{
    private readonly FileService _fileService;
    private readonly NetworkEntity _networkEntity;
    private readonly Random _random;

    private readonly Dictionary<int, ConnectionContext> _connections = new();
    private int _nextEndpointId = 1;

    public TransportEntity(FileService fileService, NetworkEntity networkEntity)
    {
        _fileService = fileService;
        _networkEntity = networkEntity;
        _random = new Random();
    }
    
    public void Run()
    {
        foreach (string message in ReadMessages())
        {
            ProcessCommunication(message);
        }
    }

    private void ProcessCommunication(string message)
    {
        int endpointId = AllocateEndpointId();
        (int src, int dst) = GenerateDistinctAddresses();

        var context = new ConnectionContext
        {
            EndpointId = endpointId,
            SourceAddress = src,
            DestinationAddress = dst,
            State = ConnectionState.WaitingForConfirmation
        };

        _connections[endpointId] = context;

        Primitive connectReq = Primitive.ConnectReq(
            endpointId: endpointId,
            src: src,
            dst: dst);

        Primitive? connectResponse = _networkEntity.HandlePrimitive(connectReq);

        if (connectResponse is null)
        {
            context.State = ConnectionState.Closed;
            _fileService.WriteResult(endpointId, src, dst, "Erreur : aucune réponse de ER.");
            return;
        }

        if (connectResponse.Type == PrimitiveType.N_DISCONNECT_IND)
        {
            context.State = ConnectionState.Closed;

            string refusalLabel = connectResponse.Reason switch
            {
                (byte)ReleaseReason.UserRefused => "Connexion refusée par le distant.",
                (byte)ReleaseReason.ProviderRefused => "Connexion refusée par le fournisseur.",
                _ => $"Connexion libérée/refusée (raison=0x{connectResponse.Reason:X2})."
            };

            _fileService.WriteResult(endpointId, src, dst, refusalLabel);
            return;
        }

        if (connectResponse.Type != PrimitiveType.N_CONNECT_CONF)
        {
            context.State = ConnectionState.Closed;
            _fileService.WriteResult(endpointId, src, dst, "Erreur : réponse inattendue à la connexion.");
            return;
        }

        context.ConnectionNumber = connectResponse.ConnectionNumber;
        context.State = ConnectionState.Established;

        byte[] data = Encoding.UTF8.GetBytes(message);
        Primitive dataReq = Primitive.DataReq(endpointId, data);

        Primitive? dataResponse = _networkEntity.HandlePrimitive(dataReq);

        if (dataResponse is not null && dataResponse.Type == PrimitiveType.N_DISCONNECT_IND)
        {
            context.State = ConnectionState.Closed;

            string result = dataResponse.Reason switch
            {
                (byte)ReleaseReason.ProviderRefused => "Échec du transfert : erreur réseau ou absence d'acquittement.",
                (byte)ReleaseReason.UserRefused => "Échec du transfert : libération distante.",
                _ => $"Échec du transfert (raison=0x{dataResponse.Reason:X2})."
            };

            _fileService.WriteResult(endpointId, src, dst, result);
            return;
        }

        Primitive disconnectReq = Primitive.DisconnectReq(endpointId);
        Primitive? disconnectResponse = _networkEntity.HandlePrimitive(disconnectReq);

        context.State = ConnectionState.Closed;

        if (disconnectResponse is not null && disconnectResponse.Type == PrimitiveType.N_DISCONNECT_IND)
        {
            _fileService.WriteResult(
                endpointId,
                src,
                dst,
                $"Communication réussie | conn={context.ConnectionNumber} | {data.Length} octets transmis.");
        }
        else
        {
            _fileService.WriteResult(
                endpointId,
                src,
                dst,
                $"Communication terminée avec état incertain | conn={context.ConnectionNumber}.");
        }
    }
    
    private IEnumerable<string> ReadMessages()
    {
        foreach (var line in _fileService.ReadRequests().Cast<string>())
        {
            yield return line;
        }
    }
    
    private (int src, int dst) GenerateDistinctAddresses()
    {
        int src = _random.Next(0, 255);
        int dst;

        do
        {
            dst = _random.Next(0, 255);
        }
        while (dst == src);

        return (src, dst);
    }

    private int AllocateEndpointId()
    {
        return _nextEndpointId++;
    }

    public ConnectionContext? GetConnectionContext(int endpointId)
    {
        _connections.TryGetValue(endpointId, out var context);
        return context;
    }
}