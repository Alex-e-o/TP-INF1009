using System.Text;
using INF1009.Models;

namespace INF1009.Services;

public class SegmentationService
{
    public const int MaxPayloadSize = 128;

    public List<Packet> SegmentData(ConnectionContext connection, string data)
    {
        var packets = new List<Packet>();

        if (string.IsNullOrEmpty(data))
            return packets;

        byte[] bytes = Encoding.UTF8.GetBytes(data);
        int offset = 0;

        while (offset < bytes.Length)
        {
            int length = Math.Min(MaxPayloadSize, bytes.Length - offset);
            byte[] chunk = bytes[offset..(offset + length)];
            bool moreData = offset + length < bytes.Length;

            var packet = new Packet
            {
                Type = PacketType.Data,
                ConnectionNumber = connection.ConnectionNumber,
                SourceAddress = connection.SourceAddress,
                DestinationAddress = connection.DestinationAddress,
                Ps = connection.SendSequenceNumber % 8,
                Pr = connection.ExpectedReceiveNumber % 8,
                MoreData = moreData,
                Payload = Encoding.UTF8.GetString(chunk)
            };

            packets.Add(packet);

            connection.SendSequenceNumber = (connection.SendSequenceNumber + 1) % 8;
            offset += length;
        }

        return packets;
    }
}