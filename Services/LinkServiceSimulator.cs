using INF1009.Models;

namespace INF1009.Services;

// Simule le comportement de la couche liaison : envoie un paquet et retourne une réponse simulée
public class LinkServiceSimulator
{
    private readonly FileService _fileService;
    private readonly Random _random = new();

    public LinkServiceSimulator(FileService fileService)
    {
        _fileService = fileService;
    }

    // Enregistre le paquet émis, simule la réponse et l'enregistre aussi
    public Packet? Send(Packet outgoingPacket, int sourceAddress)
    {
        _fileService.WriteEmittedPacket(
            outgoingPacket.ConnectionNumber,
            GetPacketLabel(outgoingPacket),
            outgoingPacket.ToBinaryString());

        Packet? response = outgoingPacket.Type switch
        {
            PacketType.Call    => SimulateCallResponse(outgoingPacket),
            PacketType.Release => SimulateReleaseResponse(outgoingPacket),
            PacketType.Data    => SimulateDataResponse(outgoingPacket, sourceAddress),
            _                  => null
        };

        if (response is null)
        {
            _fileService.WriteReceivedResponse(outgoingPacket.ConnectionNumber, "AUCUNE_REPONSE");
        }
        else
        {
            _fileService.WriteReceivedResponse(
                response.ConnectionNumber,
                GetPacketLabel(response),
                response.ToBinaryString());
        }

        return response;
    }

    // Simule la réponse au paquet d'appel selon l'adresse source
    // src % 19 == 0 → pas de réponse (timeout)
    // src % 13 == 0 → refus du distant
    // sinon         → connexion acceptée
    private Packet? SimulateCallResponse(Packet callPacket)
    {
        if (callPacket.SourceAddress % 19 == 0)
            return null;

        if (callPacket.SourceAddress % 13 == 0)
            return Packet.ReleasePacket(callPacket.ConnectionNumber, callPacket.SourceAddress, callPacket.DestinationAddress, ReleaseReason.UserRefused);

        return Packet.ConnectionGrantedPacket(callPacket.ConnectionNumber, callPacket.SourceAddress, callPacket.DestinationAddress);
    }

    // Simule la réponse à un paquet de données
    // src % 15 == 0 → pas d'ACK (timeout)
    // sinon → ACK positif ou négatif (négatif si p(s) == valeur tirée au hasard)
    private Packet? SimulateDataResponse(Packet dataPacket, int sourceAddress)
    {
        if (sourceAddress % 15 == 0)
            return null;

        int drawn = _random.Next(0, 8);
        bool negativeAck = dataPacket.PS == drawn;
        return Packet.AckPacket(dataPacket.ConnectionNumber, (dataPacket.PS + 1) % 8, negativeAck);
    }

    // La couche liaison ne répond jamais aux paquets de libération
    private Packet? SimulateReleaseResponse(Packet releasePacket)
    {
        return null;
    }

    // Retourne le label textuel d'un paquet pour les fichiers de trace
    private static string GetPacketLabel(Packet packet)
    {
        return packet.Type switch
        {
            PacketType.Call             => "PAQUET_APPEL",
            PacketType.ConnectionGranted => "COMMUNICATION_ETABLIE",
            PacketType.Release          => packet.Reason switch
            {
                ReleaseReason.UserRefused     => "LIBERATION_REFUS_DISTANT",
                ReleaseReason.ProviderRefused => "LIBERATION_REFUS_FOURNISSEUR",
                _                             => "LIBERATION"
            },
            PacketType.Data        => $"DONNEES ps={packet.PS} pr={packet.PR} M={(packet.MoreBit ? 1 : 0)}",
            PacketType.Ack         => $"ACK_POSITIF pr={packet.PR}",
            PacketType.NegativeAck => $"ACK_NEGATIF pr={packet.PR}",
            _                      => packet.Type.ToString()
        };
    }
}
