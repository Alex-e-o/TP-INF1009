namespace INF1009.Models;

// États possibles d'une connexion réseau du côté appelant (§3.6).
public enum ConnectionState
{
    // Demande de connexion émise, en attente de N_CONNECT.conf ou N_DISCONNECT.ind
    WaitingForConfirmation,

    // Connexion établie, transfert de données possible
    Established,

    // Connexion en cours de libération
    Releasing,

    // Connexion libérée ou refusée — ressources disponibles pour réutilisation
    Closed
}

// Représente le contexte d'une connexion active ou en cours d'établissement.
// Maintenu à la fois par ET (via EndpointId) et par ER (via ConnectionNumber).

public class ConnectionContext
{
    // Identification
    
    // Identifiant d'extrémité de connexion, attribué par ET.
    public int EndpointId { get; init; }
    
    // Numéro de connexion logique, attribué par ER lors de l'acceptation .
    // Vaut 0 tant que la connexion n'est pas établie.
    
    public int ConnectionNumber { get; set; }
    
    // Adressage

    // Adresse de la station source (0-254), tirée aléatoirement par ET 
    public int SourceAddress { get; init; }

    // Adresse de la station destination (0-254), tirée aléatoirement par ET 
    public int DestinationAddress { get; init; }
    
    // État
    // État courant de la connexion
    public ConnectionState State { get; set; } = ConnectionState.WaitingForConfirmation;
    
    // Numéros de séquence — gérés par ER côté émission
    
    // Numéro de séquence du prochain paquet à émettre p(s), modulo 8.
    // Incrémenté après chaque paquet acquitté positivement.
    public int PS { get; set; }
    
    // Numéro du prochain paquet attendu en réception p(r), modulo 8.
    public int PR { get; set; }
    
    // Données en attente d'émission (déposées par ET, consommées par ER)
    
    // Message utilisateur complet à transmettre.
    
    public byte[]? PendingData { get; set; }
    
    // Helpers

    // Incrémente p(s) modulo 8
    public void AdvancePS() => PS = (PS + 1) % 8;

    // Incrémente p(r) modulo 8
    public void AdvancePR() => PR = (PR + 1) % 8;

    public override string ToString() =>
        $"[Conn] id={EndpointId} connNum={ConnectionNumber} " +
        $"src={SourceAddress} dst={DestinationAddress} " +
        $"state={State} ps={PS} pr={PR}";
}