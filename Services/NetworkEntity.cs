using INF1009.Models;

namespace INF1009.Services;

public class NetworkEntity
{
    private readonly LinkServiceSimulator _linkServiceSimulator;
    private readonly SegmentationService _segmentationService;

    private readonly Dictionary<int, ConnectionContext> _contextsByEndpointId = new();
    private readonly Dictionary<int, ConnectionContext> _contextsByConnectionNumber = new();

    private int _nextConnectionNumber = 1;

    public NetworkEntity(LinkServiceSimulator linkServiceSimulator, SegmentationService segmentationService)
    {
        _linkServiceSimulator = linkServiceSimulator;
        _segmentationService = segmentationService;
    }

    public Primitive? HandlePrimitive(Primitive primitive)
    {
        return primitive.Type switch
        {
            PrimitiveType.N_CONNECT_REQ => HandleConnectRequest(primitive),
            PrimitiveType.N_DATA_REQ => HandleDataRequest(primitive),
            PrimitiveType.N_DISCONNECT_REQ => HandleDisconnectRequest(primitive),
            _ => throw new InvalidOperationException($"Primitive non supportée : {primitive.Type}")
        };
    }

    private Primitive? HandleConnectRequest(Primitive primitive)
    {
        int connectionNumber = _nextConnectionNumber++;

        var context = new ConnectionContext
        {
            EndpointId = primitive.EndpointId,
            ConnectionNumber = connectionNumber,
            SourceAddress = primitive.SourceAddress,
            DestinationAddress = primitive.DestinationAddress,
            State = ConnectionState.WaitingForConfirmation,
            PS = 0,
            PR = 0
        };

        _contextsByEndpointId[context.EndpointId] = context;
        _contextsByConnectionNumber[context.ConnectionNumber] = context;

        // --- DÉBUT DE LA CORRECTION ---
        // Vérification du refus du fournisseur (multiple de 27) selon la section 3.6 du PDF
        if (context.SourceAddress % 27 == 0)
        {
            context.State = ConnectionState.Closed;

            return Primitive.DisconnectInd(
                endpointId: context.EndpointId,
                reason: (byte)ReleaseReason.ProviderRefused);
        }

        var callPacket = Packet.CallPacket(
            connNum: context.ConnectionNumber,
            src: context.SourceAddress,
            dst: context.DestinationAddress);

        Packet? response = _linkServiceSimulator.Send(callPacket, context.SourceAddress);

        if (response is null)
        {
            context.State = ConnectionState.Closed;

            return Primitive.DisconnectInd(
                endpointId: context.EndpointId,
                reason: (byte)ReleaseReason.ProviderRefused);
        }

        if (response.Type == PacketType.ConnectionGranted)
        {
            context.State = ConnectionState.Established;

            return Primitive.ConnectConf(
                endpointId: context.EndpointId,
                connNum: context.ConnectionNumber);
        }

        if (response.Type == PacketType.Release)
        {
            context.State = ConnectionState.Closed;

            return Primitive.DisconnectInd(
                endpointId: context.EndpointId,
                reason: (byte)response.Reason);
        }

        context.State = ConnectionState.Closed;

        return Primitive.DisconnectInd(
            endpointId: context.EndpointId,
            reason: (byte)ReleaseReason.ProviderRefused);
    }

    private Primitive? HandleDataRequest(Primitive primitive)
    {
        if (!_contextsByEndpointId.TryGetValue(primitive.EndpointId, out var context))
        {
            throw new InvalidOperationException(
                $"Aucun contexte trouvé pour EndpointId={primitive.EndpointId}");
        }

        if (context.State != ConnectionState.Established)
        {
            return Primitive.DisconnectInd(
                endpointId: context.EndpointId,
                reason: (byte)ReleaseReason.ProviderRefused);
        }

        if (primitive.UserData is null || primitive.UserData.Length == 0)
        {
            return Primitive.DisconnectInd(
                endpointId: context.EndpointId,
                reason: (byte)ReleaseReason.None);
        }

        context.PendingData = primitive.UserData;

        // 1. Appel de la bonne méthode qui génère directement la liste des paquets finis
        var packets = _segmentationService.BuildDataPackets(context, context.PendingData);

        int packetIndex = 0;
        foreach (var packet in packets)
        {
            packetIndex++;

            // 2. Envoi direct du paquet pré-construit
            bool success = SendDataPacketWithSingleRetry(context, packet);

            if (!success)
            {
                context.State = ConnectionState.Closed;

                return Primitive.DisconnectInd(
                    endpointId: context.EndpointId,
                    reason: (byte)ReleaseReason.ProviderRefused);
            }
        }

        return null;
    }

    private Primitive? HandleDisconnectRequest(Primitive primitive)
    {
        if (!_contextsByEndpointId.TryGetValue(primitive.EndpointId, out var context))
        {
            throw new InvalidOperationException(
                $"Aucun contexte trouvé pour EndpointId={primitive.EndpointId}");
        }

        context.State = ConnectionState.Releasing;

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

    private bool SendDataPacketWithSingleRetry(ConnectionContext context, Packet packet)
    {
        for (int attempt = 1; attempt <= 2; attempt++)
        {
            Packet? response = _linkServiceSimulator.Send(packet, context.SourceAddress);

            if (response is null)
            {
                continue;
            }

            if (response.Type == PacketType.Ack)
            {
                context.PR = response.PR;
                return true;
            }

            if (response.Type == PacketType.NegativeAck)
            {
                if (attempt == 2)
                {
                    return false;
                }

                continue;
            }

            if (response.Type == PacketType.Release)
            {
                return false;
            }

            return false;
        }

        return false;
    }
}