namespace INF1009.Models;

public enum PrimitiveType
{
    // Phase d'établissement
    N_CONNECT_REQ,   // ET → ER : demande de connexion
    N_CONNECT_CONF,  // ER → ET : connexion acceptée
    // N_CONNECT_IND,   
    // N_CONNECT_RESP,  

    // Phase de transfert
    N_DATA_REQ,      // ET → ER : demande d'émission de données

    // Phase de libération
    N_DISCONNECT_REQ, // ET → ER : demande de libération
    N_DISCONNECT_IND, // ER → ET : indication de libération ou refus
}

 // Représente un message logique échangé entre ET et ER.
 // Sert de conteneur générique pour tous les paramètres d'une primitive de service.
public class Primitive
{
    // Identification
    // Type de la primitive
    public PrimitiveType Type { get; init; }
    
    public int EndpointId { get; init; }

    // Adressage (N_CONNECT.req / conf / ind)
    // Adresse de la station source (0-254)
    public int SourceAddress { get; init; }

    // Adresse de la station destination (0-254)
    public int DestinationAddress { get; init; }

    // Transfert de données (N_DATA.req)
    // Données utilisateur à transmettre (peut être null si non applicable)
    public byte[]? UserData { get; init; }

    // Libération / refus (N_DISCONNECT.ind)
    
    // Raison de la libération ou du refus.
    // 0x01 = distant refuse, 0x02 = fournisseur refuse, 0x00 = libération normale.
    
    public byte Reason { get; init; }

    // Numéro de connexion réseau (N_CONNECT.conf)
    
    // Numéro de connexion logique attribué par ER après acceptation.
    // Vaut 0 si non encore attribué.
    
    public int ConnectionNumber { get; init; }

    // Constructeurs de commodité

    public static Primitive ConnectReq(int endpointId, int src, int dst) => new()
    {
        Type               = PrimitiveType.N_CONNECT_REQ,
        EndpointId         = endpointId,
        SourceAddress      = src,
        DestinationAddress = dst
    };

    public static Primitive ConnectConf(int endpointId, int connNum) => new()
    {
        Type             = PrimitiveType.N_CONNECT_CONF,
        EndpointId       = endpointId,
        ConnectionNumber = connNum
    };

    public static Primitive DataReq(int endpointId, byte[] data) => new()
    {
        Type       = PrimitiveType.N_DATA_REQ,
        EndpointId = endpointId,
        UserData   = data
    };

    public static Primitive DisconnectReq(int endpointId) => new()
    {
        Type       = PrimitiveType.N_DISCONNECT_REQ,
        EndpointId = endpointId
    };

    public static Primitive DisconnectInd(int endpointId, byte reason) => new()
    {
        Type       = PrimitiveType.N_DISCONNECT_IND,
        EndpointId = endpointId,
        Reason     = reason
    };

    public override string ToString() =>
        $"[Primitive] {Type} | EndpointId={EndpointId} " +
        $"src={SourceAddress} dst={DestinationAddress} " +
        $"connNum={ConnectionNumber} reason=0x{Reason:X2}";
}