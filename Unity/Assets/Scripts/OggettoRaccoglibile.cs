using UnityEngine;

namespace AmnesiaUnity
{
    public sealed class OggettoRaccoglibile : MonoBehaviour
    {
        public string Id;
        public string Luogo = "magazzino_b17";
        public bool Fisso;
        public const string CassettaAperta = "contenitore_aperto:cassetta_latta";
        private Bootstrap _gioco;

        public static OggettoRaccoglibile Puntato(Camera camera, Vector3 player)
        {
            if (camera == null || !Physics.Raycast(camera.ViewportPointToRay(new Vector3(.5f,.5f)),
                    out var hit, 5f, ~0, QueryTriggerInteraction.Ignore)) return null;
            if (Vector3.Distance(player + Vector3.up, hit.point) > 2.5f) return null;
            return hit.collider.GetComponentInParent<OggettoRaccoglibile>();
        }

        public static bool Aperta(Bootstrap gioco) => gioco.World.Flags.TryGetValue(CassettaAperta, out var value) && value;

        private void Start() { _gioco = FindFirstObjectByType<Bootstrap>(); Aggiorna(); }
        private void Update() { Aggiorna(); }

        private void Aggiorna()
        {
            if (_gioco == null || _gioco.Session == null) return;
            if (!Fisso && _gioco.World.ItemOwners.ContainsKey(Id)) { Destroy(gameObject); return; }
            if (Id == "cassetta_latta")
            {
                var lid = transform.Find("coperchio");
                if (lid != null) lid.localRotation = Quaternion.Euler(Aperta(_gioco) ? 115 : 0, 0, 0);
                var stringTie = transform.Find("spago");
                if (stringTie != null) stringTie.gameObject.SetActive(!Aperta(_gioco));
                var bracelet = transform.Find("braccialetto");
                if (bracelet != null) bracelet.gameObject.SetActive(Aperta(_gioco) && !_gioco.World.ItemOwners.ContainsKey("braccialetto"));
            }
        }

        public string Azione(Bootstrap gioco)
        {
            var name = gioco.Items.Find(Id)?.Name ?? Id;
            if (Fisso) return "Esamina " + name;
            if (Id == "cassetta_latta" && !Aperta(gioco)) return "Sciogli lo spago e apri la cassetta";
            return "Raccogli " + name;
        }

        public void Interagisci(Bootstrap gioco, Pannello pannello)
        {
            if (Fisso) { pannello.Avviso(gioco.Items.Find(Id)?.Visible ?? Id); return; }
            if (!gioco.Porte.IsOpen(gioco.World, Luogo)) return;
            if (Id == "cassetta_latta" && !Aperta(gioco))
            {
                gioco.World.Flags[CassettaAperta] = true;
                Aggiorna();
                pannello.Avviso("Dentro: due scarpe da bambino, un fermaglio e un braccialetto d'argento.");
                return;
            }
            if (Id == "braccialetto" && !Aperta(gioco)) return;
            var result = gioco.Porte.Raccogli(gioco.World, Luogo, Id);
            if (!result.IsOk) return;
            pannello.Avviso("Hai raccolto: " + (gioco.Items.Find(Id)?.Name ?? Id));
            Destroy(gameObject);
        }
    }
}
