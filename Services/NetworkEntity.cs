using INF1009.Models;

namespace INF1009.Services;

public class NetworkEntity
{
    private readonly LinkServiceSimulator _linkService;
    private readonly SegmentationService _segmentationService;
    private readonly Dictionary<int, ConnectionContext> _connections = new();
    private readonly Random _random = new();

    private int _nextConnectionNumber = 1;

    public NetworkEntity(LinkServiceSimulator linkService, SegmentationService segmentationService)
    {
        _linkService = linkService;
        _segmentationService = segmentationService;
    }

    public Primitive HandlePrimitive(Primitive primitive)
    {
        return primitive.Type switch
        {
            PrimitiveType.NConnectReq => HandleConnectRequest(primitive),
            PrimitiveType.NDataReq => HandleDataRequest(primitive),
            PrimitiveType.NDisconnectReq => HandleDisconnectRequest(primitive),
            _ => throw new NotSupportedException($"Primitive non supportée : {primitive.Type}")
        };
    }

    private Primitive HandleConnectRequest(Primitive primitive)
    {
        if (primitive.SourceAddress % 27 == 0)
        {
            return new Primitive
            {
                Type = PrimitiveType.NDisconnectInd,
                EndpointId = primitive.EndpointId,
                SourceAddress = primitive.SourceAddress,
                DestinationAddress = primitive.DestinationAddress,
                Reason = DisconnectReason.ProviderRefusal
            };
        }

        var context = new ConnectionContext
        {
            EndpointId = primitive.EndpointId,
            ConnectionNumber = _nextConnectionNumber++,
            SourceAddress = primitive.SourceAddress,
            DestinationAddress = primitive.DestinationAddress,
            State = ConnectionState.WaitingForEstablishmentConfirmation
        };

        _connections[primitive.EndpointId] = context;

        var callPacket = new Packet
        {
            Type = PacketType.Call,
            ConnectionNumber = context.ConnectionNumber,
            SourceAddress = context.SourceAddress,
            DestinationAddress = context.DestinationAddress
        };

        Packet? response = _linkService.SendPacket(callPacket);

        if (response is null || response.Type == PacketType.Disconnect)
        {
            context.State = ConnectionState.Refused;

            return new Primitive
            {
                Type = PrimitiveType.NDisconnectInd,
                EndpointId = context.EndpointId,
                SourceAddress = context.SourceAddress,
                DestinationAddress = context.DestinationAddress,
                Reason = response?.Reason ?? DisconnectReason.Timeout
            };
        }

        context.State = ConnectionState.Established;

        return new Primitive
        {
            Type = PrimitiveType.NConnectConf,
            EndpointId = context.EndpointId,
            SourceAddress = context.SourceAddress,
            DestinationAddress = context.DestinationAddress
        };
    }

    private Primitive HandleDataRequest(Primitive primitive)
    {
        if (!_connections.TryGetValue(primitive.EndpointId, out ConnectionContext? context))
        {
            return new Primitive
            {
                Type = PrimitiveType.NDisconnectInd,
                EndpointId = primitive.EndpointId,
                Reason = DisconnectReason.NormalRelease
            };
        }

        List<Packet> packets = _segmentationService.SegmentData(context, primitive.UserData ?? string.Empty);

        foreach (Packet packet in packets)
        {
            Packet? response = _linkService.SendPacket(packet);

            if (response is null || response.Type == PacketType.Nack)
            {
                if (context.RetransmissionAttempted)
                {
                    return new Primitive
                    {
                        Type = PrimitiveType.NDisconnectInd,
                        EndpointId = context.EndpointId,
                        SourceAddress = context.SourceAddress,
                        DestinationAddress = context.DestinationAddress,
                        Reason = response is null ? DisconnectReason.Timeout : DisconnectReason.NegativeAck
                    };
                }

                context.RetransmissionAttempted = true;
                response = _linkService.SendPacket(packet);

                if (response is null || response.Type == PacketType.Nack)
                {
                    return new Primitive
                    {
                        Type = PrimitiveType.NDisconnectInd,
                        EndpointId = context.EndpointId,
                        SourceAddress = context.SourceAddress,
                        DestinationAddress = context.DestinationAddress,
                        Reason = response is null ? DisconnectReason.Timeout : DisconnectReason.NegativeAck
                    };
                }
            }

            context.RetransmissionAttempted = false;
            context.ExpectedReceiveNumber = response.Pr;
        }

        return new Primitive
        {
            Type = PrimitiveType.NDataInd,
            EndpointId = primitive.EndpointId,
            SourceAddress = primitive.SourceAddress,
            DestinationAddress = primitive.DestinationAddress,
            UserData = primitive.UserData
        };
    }

    private Primitive HandleDisconnectRequest(Primitive primitive)
    {
        if (!_connections.TryGetValue(primitive.EndpointId, out ConnectionContext? context))
        {
            return new Primitive
            {
                Type = PrimitiveType.NDisconnectInd,
                EndpointId = primitive.EndpointId,
                Reason = DisconnectReason.NormalRelease
            };
        }

        var disconnectPacket = new Packet
        {
            Type = PacketType.Disconnect,
            ConnectionNumber = context.ConnectionNumber,
            SourceAddress = context.SourceAddress,
            DestinationAddress = context.DestinationAddress,
            Reason = DisconnectReason.NormalRelease
        };

        _linkService.SendPacket(disconnectPacket);

        context.State = ConnectionState.Released;
        _connections.Remove(primitive.EndpointId);

        return new Primitive
        {
            Type = PrimitiveType.NDisconnectInd,
            EndpointId = primitive.EndpointId,
            SourceAddress = context.SourceAddress,
            DestinationAddress = context.DestinationAddress,
            Reason = DisconnectReason.NormalRelease
        };
    }
}