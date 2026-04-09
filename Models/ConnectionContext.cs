namespace INF1009.Models;


public enum ConnectionState
{
    WaitingForEstablishmentConfirmation,
    Established,
    Releasing,
    Released,
    Refused
}

public class ConnectionContext
{
    public int EndpointId { get; set; }
    public int ConnectionNumber { get; set; }

    public byte SourceAddress { get; set; }
    public byte DestinationAddress { get; set; }

    public ConnectionState State { get; set; } = ConnectionState.WaitingForEstablishmentConfirmation;

    public int SendSequenceNumber { get; set; } = 0;     // ps
    public int ExpectedReceiveNumber { get; set; } = 0;  // pr

    public string? UserData { get; set; }

    public bool IsAwaitingAck { get; set; } = false;
    public bool RetransmissionAttempted { get; set; } = false;

    public override string ToString()
    {
        return $"EndpointId={EndpointId}, Conn={ConnectionNumber}, " +
               $"Src={SourceAddress}, Dst={DestinationAddress}, " +
               $"State={State}, PS={SendSequenceNumber}, PR={ExpectedReceiveNumber}";
    }
}