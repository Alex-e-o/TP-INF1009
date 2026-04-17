using System.Collections.Concurrent;
using INF1009.Models;

namespace INF1009.Services;

// Canal de communication entre ET et ER.
// Permet aux deux entités de s'exécuter dans des threads séparés.
//
// Principe :
//   ET appelle SendAndWait(primitive) → la primitive est placée dans la file
//   de requêtes, puis ET se bloque jusqu'à ce qu'ER dépose la réponse.
//   ER consomme les requêtes via ConsumeRequests() et répond via SendResponse().
public class PrimitiveChannel
{
    // Enveloppe interne permettant de transporter null dans BlockingCollection
    // (BlockingCollection<T> rejette null même pour les types référence)
    private readonly record struct Réponse(Primitive? Primitive);

    private readonly BlockingCollection<Primitive> _requêtes = new();
    private readonly BlockingCollection<Réponse>   _réponses = new();

    // Appelé par ET : envoie une primitive et attend la réponse d'ER (bloquant)
    public Primitive? SendAndWait(Primitive primitive)
    {
        _requêtes.Add(primitive);
        return _réponses.Take().Primitive;
    }

    // Appelé par ER : itère sur les primitives envoyées par ET.
    // La boucle se termine automatiquement quand ET appelle CompleteAdding().
    public IEnumerable<Primitive> ConsumeRequests() =>
        _requêtes.GetConsumingEnumerable();

    // Appelé par ER : dépose la réponse vers ET (null = succès N_DATA_REQ)
    public void SendResponse(Primitive? response) =>
        _réponses.Add(new Réponse(response));

    // Appelé par ET à la fin du traitement : signale qu'aucune autre primitive ne viendra
    public void CompleteAdding() => _requêtes.CompleteAdding();
}
