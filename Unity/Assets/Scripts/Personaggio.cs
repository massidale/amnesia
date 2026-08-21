using UnityEngine;

namespace AmnesiaUnity
{
    /// Mettilo su una figura piazzata a mano nell'editor per dirle CHI e'.
    /// L'id deve essere quello del personaggio nel gioco — «anna», «matteo»,
    /// «laura», «don_carlo», «nino», «rosa», «teresa», «piero», «marisa»,
    /// «beppe», «lidia», «gino» — minuscolo, uguale al nome della sua scheda.
    /// Quando c'e', Bootstrap usa QUESTA figura invece di generarne una: monti
    /// il paese a mano, il gioco ci gira sopra.
    public sealed class Personaggio : MonoBehaviour
    {
        [Tooltip("Id minuscolo: anna, matteo, laura, don_carlo, nino, rosa, teresa, piero, marisa, beppe, lidia, gino")]
        public string Id = "";
    }
}
