using System.Text;
using INF1009.Models;

namespace INF1009.Services;

public class TransportEntity
{
    private readonly FileService _fileService;
    private readonly NetworkEntity _networkEntity;

    // Connexions actives indexées par endpointId
    private readonly Dictionary<int, ConnectionContext> _connections = new();
    // Taille des dernières données envoyées par connexion (pour le résultat final)
    private readonly Dictionary<int, int> _lastDataLength = new();
    private int _nextEndpointId = 1;

    public TransportEntity(FileService fileService, NetworkEntity networkEntity)
    {
        _fileService = fileService;
        _networkEntity = networkEntity;
    }

    // Lit et exécute chaque commande du fichier S_lec.txt
    public void Run()
    {
        foreach (string line in _fileService.ReadRequests())
        {
            ProcessLine(line);
        }
    }

    // Identifie la commande (CONNECT / DATA / DISCONNECT) et appelle le bon handler
    private void ProcessLine(string line)
    {
        var parts = line.Split(';', 3);
        switch (parts[0])
        {
            case "CONNECT" when parts.Length >= 3:
                HandleConnect(int.Parse(parts[1]), int.Parse(parts[2]));
                break;
            case "DATA" when parts.Length >= 3:
                HandleData(int.Parse(parts[1]), parts[2]);
                break;
            case "DISCONNECT" when parts.Length >= 2:
                HandleDisconnect(int.Parse(parts[1]));
                break;
        }
    }

    // Demande une connexion à ER avec les adresses lues dans S_lec
    private void HandleConnect(int src, int dst)
    {
        int endpointId = _nextEndpointId++;

        var context = new ConnectionContext
        {
            EndpointId = endpointId,
            SourceAddress = src,
            DestinationAddress = dst,
            State = ConnectionState.WaitingForConfirmation
        };
        _connections[endpointId] = context;

        Primitive connectReq = Primitive.ConnectReq(endpointId, src, dst);
        Primitive? connectResponse = _networkEntity.HandlePrimitive(connectReq);

        if (connectResponse is null)
        {
            context.State = ConnectionState.Closed;
            _fileService.WriteResult(endpointId, src, dst, "Erreur : aucune réponse de ER.");
            return;
        }

        // La connexion a été refusée (fournisseur ou distant) — on écrit le résultat maintenant
        if (connectResponse.Type == PrimitiveType.N_DISCONNECT_IND)
        {
            context.State = ConnectionState.Closed;
            string label = connectResponse.Reason switch
            {
                (byte)ReleaseReason.UserRefused     => "Connexion refusée par le distant.",
                (byte)ReleaseReason.ProviderRefused => "Connexion refusée par le fournisseur.",
                _ => $"Connexion libérée/refusée (raison=0x{connectResponse.Reason:X2})."
            };
            _fileService.WriteResult(endpointId, src, dst, label);
            return;
        }

        // Connexion acceptée — on retient le numéro de connexion attribué par ER
        if (connectResponse.Type == PrimitiveType.N_CONNECT_CONF)
        {
            context.ConnectionNumber = connectResponse.ConnectionNumber;
            context.State = ConnectionState.Established;
        }
    }

    // Envoie des données sur une connexion établie
    private void HandleData(int endpointId, string message)
    {
        if (!_connections.TryGetValue(endpointId, out var context))
            return;

        if (context.State != ConnectionState.Established)
            return;

        byte[] data = Encoding.UTF8.GetBytes(message);
        _lastDataLength[endpointId] = data.Length;

        Primitive dataReq = Primitive.DataReq(endpointId, data);
        Primitive? dataResponse = _networkEntity.HandlePrimitive(dataReq);

        // Si ER retourne un N_DISCONNECT.ind, le transfert a échoué
        if (dataResponse is not null && dataResponse.Type == PrimitiveType.N_DISCONNECT_IND)
        {
            context.State = ConnectionState.Closed;
            string result = dataResponse.Reason switch
            {
                (byte)ReleaseReason.ProviderRefused => "Échec du transfert : erreur réseau ou absence d'acquittement.",
                (byte)ReleaseReason.UserRefused     => "Échec du transfert : libération distante.",
                _ => $"Échec du transfert (raison=0x{dataResponse.Reason:X2})."
            };
            _fileService.WriteResult(endpointId, context.SourceAddress, context.DestinationAddress, result);
        }
    }

    // Libère une connexion établie et écrit le résultat final dans S_ecr
    private void HandleDisconnect(int endpointId)
    {
        if (!_connections.TryGetValue(endpointId, out var context))
            return;

        // Si la connexion est déjà fermée (ex. échec antérieur du transfert de données),
        // ET a déjà reçu N_DISCONNECT.ind et le résultat a été écrit.
        // On ignore silencieusement ce DISCONNECT car la connexion n'est plus active.
        if (context.State == ConnectionState.Closed)
            return;

        // Seule une connexion établie peut être libérée via N_DISCONNECT.req
        if (context.State != ConnectionState.Established)
            return;

        Primitive disconnectReq = Primitive.DisconnectReq(endpointId);
        Primitive? disconnectResponse = _networkEntity.HandlePrimitive(disconnectReq);

        context.State = ConnectionState.Closed;

        int dataLength = _lastDataLength.TryGetValue(endpointId, out int len) ? len : 0;

        if (disconnectResponse is not null && disconnectResponse.Type == PrimitiveType.N_DISCONNECT_IND)
        {
            _fileService.WriteResult(
                endpointId,
                context.SourceAddress,
                context.DestinationAddress,
                $"Communication réussie | conn={context.ConnectionNumber} | {dataLength} octets transmis.");
        }
        else
        {
            _fileService.WriteResult(
                endpointId,
                context.SourceAddress,
                context.DestinationAddress,
                $"Communication terminée avec état incertain | conn={context.ConnectionNumber}.");
        }
    }

    public ConnectionContext? GetConnectionContext(int endpointId)
    {
        _connections.TryGetValue(endpointId, out var context);
        return context;
    }
}
