namespace INF1009.Models;

// Types de paquets réseau (NPDU) définis dans le sujet.
// La valeur correspond à l'octet de type du paquet.
public enum PacketType : byte
{
    Call              = 0x0B,  // 00001011 — Paquet d'appel          (§2.1)
    ConnectionGranted = 0x0F,  // 00001111 — Communication établie   (§2.1)
    Release           = 0x13,  // 00010011 — Libération / indication  (§2.1, §2.3)
    Data              = 0x00,  // Valeur calculée dynamiquement       (§2.2)
    Ack               = 0x01,  // Suffixe ACK positif  (5 bits bas = 00001)
    NegativeAck       = 0x09,  // Suffixe ACK négatif  (5 bits bas = 01001)
}

// Raisons de libération de connexion.
public enum ReleaseReason : byte
{
    None             = 0x00,
    UserRefused      = 0x01,  // Le distant refuse (absent, occupé, manque de ressources)
    ProviderRefused  = 0x02,  // Le fournisseur de service réseau refuse
    Timeout          = 0x03,  // Pas de réponse du distant (timeout)
}

// Représente un paquet réseau (NPDU — Network Protocol Data Unit).
// Modélise l'ensemble des paquets définis dans le sujet (§2).
public class Packet
{
    // ────────────────────────────────────────────────────────
    // Champs communs à tous les paquets
    // ────────────────────────────────────────────────────────

    // Numéro de connexion logique (1er octet de chaque paquet)
    public int ConnectionNumber { get; init; }

    // Type du paquet
    public PacketType Type { get; init; }
    
    // Champs d'adressage (paquet d'appel, comm. établie, libération)

    public int SourceAddress      { get; init; }
    public int DestinationAddress { get; init; }
    
    // Champs de transfert de données (§2.2)

    // Numéro de séquence du paquet émis (modulo 8, 3 bits)
    public int PS { get; init; }

    // Numéro du prochain paquet attendu en réception (modulo 8, 3 bits)
    public int PR { get; init; }
    
    // Bit M (More) : 1 = fragment intermédiaire, 0 = dernier fragment (ou unique).
    public bool MoreBit { get; init; }

    // Données utiles du paquet (max 128 octets)
    public byte[] Data { get; init; } = [];
    
    // Champ de libération (§2.1, §2.3)

    public ReleaseReason Reason { get; init; }
    
    // Sérialisation en tableau d'octets (format binaire du sujet)
    
    // Convertit le paquet en tableau d'octets conforme au format du sujet.
    public byte[] ToBytes() => Type switch
    {
        PacketType.Call or PacketType.ConnectionGranted =>
            [(byte)ConnectionNumber, (byte)Type, (byte)SourceAddress, (byte)DestinationAddress],

        // §2.3 : Demande de libération (appelant → réseau) — pas d'octet de raison
        PacketType.Release when Reason == ReleaseReason.None =>
            [(byte)ConnectionNumber, (byte)Type,
             (byte)SourceAddress, (byte)DestinationAddress],

        // §2.1 : Indication de libération (distant → appelant) — avec octet de raison
        PacketType.Release =>
            [(byte)ConnectionNumber, (byte)Type,
             (byte)SourceAddress, (byte)DestinationAddress,
             (byte)Reason],

        PacketType.Data =>
            BuildDataBytes(),

        PacketType.Ack =>
            [(byte)ConnectionNumber, BuildAckTypeByte(negative: false)],

        PacketType.NegativeAck =>
            [(byte)ConnectionNumber, BuildAckTypeByte(negative: true)],

        _ => throw new InvalidOperationException($"Type de paquet inconnu : {Type}")
    };
    
    // Construit le tableau d'octets d'un paquet de données.
    // Octet de type : p(r)[7:5] | M[4] | p(s)[3:1] | 0[0]
    private byte[] BuildDataBytes()
    {
        byte typeByte = (byte)(
            ((PR & 0x07) << 5) |
            ((MoreBit ? 1 : 0) << 4) |
            ((PS & 0x07) << 1)
        );
        var header = new byte[] { (byte)ConnectionNumber, typeByte };
        return [.. header, .. Data];
    }
    
    // Construit l'octet de type d'un paquet d'acquittement.
    // ACK+ : p(r)[7:5] | 0 0 0 0 1
    // ACK- : p(r)[7:5] | 0 1 0 0 1
    private byte BuildAckTypeByte(bool negative) =>
        (byte)(((PR & 0x07) << 5) | (negative ? 0x09 : 0x01));

    
    // Affichage binaire (pour les fichiers de log)

    // Représente chaque octet en binaire séparé par des espaces
    public string ToBinaryString() =>
        string.Join("  ", ToBytes().Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
    
    // Constructeurs de commodité
    public static Packet CallPacket(int connNum, int src, int dst) => new()
    {
        ConnectionNumber  = connNum,
        Type              = PacketType.Call,
        SourceAddress     = src,
        DestinationAddress = dst
    };

    public static Packet ConnectionGrantedPacket(int connNum, int src, int dst) => new()
    {
        ConnectionNumber  = connNum,
        Type              = PacketType.ConnectionGranted,
        SourceAddress     = src,
        DestinationAddress = dst
    };

    public static Packet ReleasePacket(int connNum, int src, int dst,
                                        ReleaseReason reason = ReleaseReason.None) => new()
    {
        ConnectionNumber  = connNum,
        Type              = PacketType.Release,
        SourceAddress     = src,
        DestinationAddress = dst,
        Reason            = reason
    };

    public static Packet DataPacket(int connNum, int ps, int pr,
                                     bool moreBit, byte[] data) => new()
    {
        ConnectionNumber = connNum,
        Type             = PacketType.Data,
        PS               = ps,
        PR               = pr,
        MoreBit          = moreBit,
        Data             = data
    };

    public static Packet AckPacket(int connNum, int pr, bool negative = false) => new()
    {
        ConnectionNumber = connNum,
        Type             = negative ? PacketType.NegativeAck : PacketType.Ack,
        PR               = pr
    };

    public override string ToString() =>
        $"[Packet] {Type} conn={ConnectionNumber} src={SourceAddress} " +
        $"dst={DestinationAddress} ps={PS} pr={PR} M={MoreBit} " +
        $"data={Data.Length}o reason={Reason}";
}