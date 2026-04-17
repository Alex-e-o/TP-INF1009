using INF1009.Models;

namespace INF1009.Services;

// ER (Entité Réseau) : reçoit les primitives d'ET et gère les échanges avec la couche liaison
public class NetworkEntity
{
    private readonly LinkServiceSimulator _linkServiceSimulator;
    private readonly SegmentationService  _segmentationService;
    private readonly PrimitiveChannel     _channel;

    // Double index pour retrouver un contexte soit par endpointId (côté ET) soit par numéro de connexion (côté liaison)
    private readonly Dictionary<int, ConnectionContext> _contextsByEndpointId = new();
    private readonly Dictionary<int, ConnectionContext> _contextsByConnectionNumber = new();

    private int _nextConnectionNumber = 1;

    public NetworkEntity(LinkServiceSimulator linkServiceSimulator,
                         SegmentationService  segmentationService,
                         PrimitiveChannel     channel)
    {
        _linkServiceSimulator = linkServiceSimulator;
        _segmentationService  = segmentationService;
        _channel              = channel;
    }

    // Boucle principale d'ER : traite les primitives envoyées par ET,
    // répond à chacune, et s'arrête quand ET a signalé la fin.
    public void RunLoop()
    {
        foreach (Primitive req in _channel.ConsumeRequests())
        {
            Primitive? response = HandlePrimitive(req);
            _channel.SendResponse(response);
        }
    }

    // Traite une primitive reçue d'ET (utilisé en interne par RunLoop)
    private Primitive? HandlePrimitive(Primitive primitive)
    {
        return primitive.Type switch
        {
            PrimitiveType.N_CONNECT_REQ    => HandleConnectRequest(primitive),
            PrimitiveType.N_DATA_REQ       => HandleDataRequest(primitive),
            PrimitiveType.N_DISCONNECT_REQ => HandleDisconnectRequest(primitive),
            _ => throw new InvalidOperationException($"Primitive non supportée : {primitive.Type}")
        };
    }

    private Primitive? HandleConnectRequest(Primitive primitive)
    {
        // Refus du fournisseur si src est multiple de 27 (§3.6) — vérifié AVANT l'attribution
        // d'un numéro de connexion, car aucune ressource ne doit être allouée en cas de refus.
        if (primitive.SourceAddress % 27 == 0)
        {
            return Primitive.DisconnectInd(
                endpointId: primitive.EndpointId,
                reason: (byte)ReleaseReason.ProviderRefused);
        }

        int connectionNumber = _nextConnectionNumber++;

        var context = new ConnectionContext
        {
            EndpointId         = primitive.EndpointId,
            ConnectionNumber   = connectionNumber,
            SourceAddress      = primitive.SourceAddress,
            DestinationAddress = primitive.DestinationAddress,
            State              = ConnectionState.WaitingForConfirmation,
            PS = 0,
            PR = 0
        };

        _contextsByEndpointId[context.EndpointId] = context;
        _contextsByConnectionNumber[context.ConnectionNumber] = context;

        // Envoi du paquet d'appel vers la couche liaison
        var callPacket = Packet.CallPacket(
            connNum: context.ConnectionNumber,
            src: context.SourceAddress,
            dst: context.DestinationAddress);

        Packet? response = _linkServiceSimulator.Send(callPacket, context.SourceAddress);

        // Pas de réponse = timeout → aucune réponse du distant
        if (response is null)
        {
            context.State = ConnectionState.Closed;
            return Primitive.DisconnectInd(
                endpointId: context.EndpointId,
                reason: (byte)ReleaseReason.Timeout);
        }

        if (response.Type == PacketType.ConnectionGranted)
        {
            context.State = ConnectionState.Established;
            return Primitive.ConnectConf(
                endpointId: context.EndpointId,
                connNum: context.ConnectionNumber);
        }

        // Le distant a refusé la connexion
        if (response.Type == PacketType.Release)
        {
            context.State = ConnectionState.Closed;
            return Primitive.DisconnectInd(
                endpointId: context.EndpointId,
                reason: (byte)response.Reason);
        }

        // Réponse inattendue
        context.State = ConnectionState.Closed;
        return Primitive.DisconnectInd(
            endpointId: context.EndpointId,
            reason: (byte)ReleaseReason.ProviderRefused);
    }

    private Primitive? HandleDataRequest(Primitive primitive)
    {
        if (!_contextsByEndpointId.TryGetValue(primitive.EndpointId, out var context))
            throw new InvalidOperationException(
                $"Aucun contexte trouvé pour EndpointId={primitive.EndpointId}");

        if (context.State != ConnectionState.Established)
            return Primitive.DisconnectInd(
                endpointId: context.EndpointId,
                reason: (byte)ReleaseReason.ProviderRefused);

        if (primitive.UserData is null || primitive.UserData.Length == 0)
            return Primitive.DisconnectInd(
                endpointId: context.EndpointId,
                reason: (byte)ReleaseReason.None);

        context.PendingData = primitive.UserData;

        // Découpe les données en paquets (segmentation si > 128 octets)
        var packets = _segmentationService.BuildDataPackets(context, context.PendingData);

        foreach (var packet in packets)
        {
            // Chaque paquet a droit à un seul ré-essai en cas d'échec
            bool success = SendDataPacketWithSingleRetry(context, packet);

            if (!success)
            {
                context.State = ConnectionState.Closed;
                return Primitive.DisconnectInd(
                    endpointId: context.EndpointId,
                    reason: (byte)ReleaseReason.ProviderRefused);
            }
        }

        return null; // tous les paquets ont été acquittés
    }

    private Primitive? HandleDisconnectRequest(Primitive primitive)
    {
        if (!_contextsByEndpointId.TryGetValue(primitive.EndpointId, out var context))
            throw new InvalidOperationException(
                $"Aucun contexte trouvé pour EndpointId={primitive.EndpointId}");

        context.State = ConnectionState.Releasing;

        // Envoie le paquet de libération (pas de réponse attendue)
        var releasePacket = Packet.ReleasePacket(
            connNum: context.ConnectionNumber,
            src: context.SourceAddress,
            dst: context.DestinationAddress,
            reason: ReleaseReason.None);

        _linkServiceSimulator.Send(releasePacket, context.SourceAddress);

        context.State = ConnectionState.Closed;

        _contextsByConnectionNumber.Remove(context.ConnectionNumber);
        _contextsByEndpointId.Remove(context.EndpointId);

        return Primitive.DisconnectInd(
            endpointId: context.EndpointId,
            reason: (byte)ReleaseReason.None);
    }

    // Tente d'envoyer un paquet de données avec au maximum un ré-essai
    private bool SendDataPacketWithSingleRetry(ConnectionContext context, Packet packet)
    {
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            Packet? response = _linkServiceSimulator.Send(packet, context.SourceAddress);

            if (response is null)
                continue; // pas de réponse, on réessaie

            if (response.Type == PacketType.Ack)
            {
                context.PR = response.PR;
                return true;
            }

            if (response.Type == PacketType.NegativeAck)
            {
                if (attempt == 2)
                    return false; // deux échecs → abandon
                continue;
            }

            // Release ou réponse inattendue → échec immédiat
            return false;
        }

        return false;
    }
}
