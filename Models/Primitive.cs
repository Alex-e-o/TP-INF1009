namespace INF1009.Models;

public enum PrimitiveType
{
    NConnectReq,
    NConnectInd,
    NConnectResp,
    NConnectConf,
    NDataReq,
    NDataInd,
    NDisconnectReq,
    NDisconnectInd
}

public enum DisconnectReason
{
    None = 0,
    RemoteRefusal = 1,
    ProviderRefusal = 2,
    Timeout = 3,
    NegativeAck = 4,
    NormalRelease = 5
}

public class Primitive
{
    public PrimitiveType Type { get; set; }

    public int EndpointId { get; set; }

    public byte SourceAddress { get; set; }
    public byte DestinationAddress { get; set; }

    public string? UserData { get; set; }

    public DisconnectReason Reason { get; set; } = DisconnectReason.None;

    public override string ToString()
    {
        return $"Primitive={Type}, EndpointId={EndpointId}, " +
               $"Src={SourceAddress}, Dst={DestinationAddress}, " +
               $"Reason={Reason}, Data={UserData}";
    }
}