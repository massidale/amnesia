using UnityEngine;

namespace AmnesiaUnity
{
    /// Mettilo su un oggetto piazzato a mano per segnare una PORTA (o una
    /// botola). L'id deve essere quello del luogo in luoghi.json — «bottega»,
    /// «casa_ferro», «magazzino»… Quando c'e', Bootstrap usa questo battente
    /// invece di generarlo. Attenzione: quando la porta si apre l'oggetto viene
    /// distrutto, quindi mettici il solo battente, non l'intera casa.
    public sealed class PortaMarker : MonoBehaviour
    {
        [Tooltip("Id del luogo in luoghi.json: bottega, casa_ferro, casa_valli, canonica, magazzino…")]
        public string Id = "";
    }
}
