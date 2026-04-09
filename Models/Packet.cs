namespace INF1009.Models;


public enum PacketType
{
    Call,
    CallAccepted,
    Data,
    Ack,
    Nack,
    Disconnect
}

public class Packet
{
    public PacketType Type { get; set; }

    public int ConnectionNumber { get; set; }

    public byte SourceAddress { get; set; }
    public byte DestinationAddress { get; set; }

    public int Ps { get; set; } = 0;
    public int Pr { get; set; } = 0;

    public bool MoreData { get; set; } = false; // bit M

    public string? Payload { get; set; }

    public DisconnectReason Reason { get; set; } = DisconnectReason.None;

    public bool IsPositiveAck => Type == PacketType.Ack;
    public bool IsNegativeAck => Type == PacketType.Nack;

    public override string ToString()
    {
        return $"Packet={Type}, Conn={ConnectionNumber}, Src={SourceAddress}, Dst={DestinationAddress}, " +
               $"PS={Ps}, PR={Pr}, M={MoreData}, Reason={Reason}, Payload={Payload}";
    }
}