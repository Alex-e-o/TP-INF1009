using INF1009.Models;

namespace INF1009.Services;

public class SegmentationService
{
    private const int MaxDataSize = 128;
    
    public List<Packet> BuildDataPackets(ConnectionContext context, byte[] userData)
    {
        var packets = new List<Packet>();

        if (userData is null || userData.Length == 0)
            return packets;

        int offset = 0;

        while (offset < userData.Length)
        {
            int remaining = userData.Length - offset;
            int chunkSize = Math.Min(MaxDataSize, remaining);

            byte[] chunk = userData
                .Skip(offset)
                .Take(chunkSize)
                .ToArray();

            bool moreBit = (offset + chunkSize) < userData.Length;

            Packet packet = Packet.DataPacket(
                connNum: context.ConnectionNumber,
                ps: context.PS,
                pr: context.PR,
                moreBit: moreBit,
                data: chunk
            );

            packets.Add(packet);

            context.AdvancePS();
            offset += chunkSize;
        }

        return packets;
    }
    
    public bool RequiresSegmentation(byte[]? userData)
    {
        return userData is not null && userData.Length > MaxDataSize;
    }
    
    public int GetSegmentCount(byte[]? userData)
    {
        if (userData is null || userData.Length == 0)
            return 0;

        return (int)Math.Ceiling(userData.Length / (double)MaxDataSize);
    }
}