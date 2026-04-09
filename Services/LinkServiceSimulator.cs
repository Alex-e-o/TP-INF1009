using INF1009.Models;

namespace INF1009.Services;

public class LinkServiceSimulator
{
    private readonly FileService _fileService;
    private readonly Random _random = new();

    public LinkServiceSimulator(FileService fileService)
    {
        _fileService = fileService;
    }

    public Packet? SendPacket(Packet outgoingPacket)
    {
        _fileService.WriteLinkOutput(outgoingPacket);

        Packet? response = outgoingPacket.Type switch
        {
            PacketType.Call => SimulateCallResponse(outgoingPacket),
            PacketType.Data => SimulateDataResponse(outgoingPacket),
            PacketType.Disconnect => null,
            _ => null
        };

        _fileService.WriteLinkInput(outgoingPacket.ConnectionNumber, response);
        return response;
    }

    private Packet? SimulateCallResponse(Packet callPacket)
    {
        if (callPacket.SourceAddress % 19 == 0)
            return null;

        if (callPacket.SourceAddress % 13 == 0)
        {
            return new Packet
            {
                Type = PacketType.Disconnect,
                ConnectionNumber = callPacket.ConnectionNumber,
                SourceAddress = callPacket.SourceAddress,
                DestinationAddress = callPacket.DestinationAddress,
                Reason = DisconnectReason.RemoteRefusal
            };
        }

        return new Packet
        {
            Type = PacketType.CallAccepted,
            ConnectionNumber = callPacket.ConnectionNumber,
            SourceAddress = callPacket.SourceAddress,
            DestinationAddress = callPacket.DestinationAddress
        };
    }

    private Packet? SimulateDataResponse(Packet dataPacket)
    {
        if (dataPacket.SourceAddress % 15 == 0)
            return null;

        int randomNumber = _random.Next(0, 8);

        if (dataPacket.Ps == randomNumber)
        {
            return new Packet
            {
                Type = PacketType.Nack,
                ConnectionNumber = dataPacket.ConnectionNumber,
                SourceAddress = dataPacket.SourceAddress,
                DestinationAddress = dataPacket.DestinationAddress,
                Pr = dataPacket.Ps
            };
        }

        return new Packet
        {
            Type = PacketType.Ack,
            ConnectionNumber = dataPacket.ConnectionNumber,
            SourceAddress = dataPacket.SourceAddress,
            DestinationAddress = dataPacket.DestinationAddress,
            Pr = (dataPacket.Ps + 1) % 8
        };
    }
}