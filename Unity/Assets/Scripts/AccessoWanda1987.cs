using System.Linq;
using Amnesia.Game;
using UnityEngine;

namespace AmnesiaUnity
{
    public sealed class AccessoWanda1987 : MonoBehaviour
    {
        Bootstrap gioco;
        GameObject chiusura;
        Transform portaAperta;
        Transform maniglia;
        Transform elena;
        public bool Aperto => gioco != null && ConsegneNarrative.AccessoElena(gioco.World);

        public static void Installa(Bootstrap gioco)
        {
            var casa = FindObjectsByType<Luogo1987>(FindObjectsSortMode.None).FirstOrDefault(l => l.Id == "casa_wanda");
            if (!casa || casa.GetComponent<AccessoWanda1987>()) return;
            var accesso = casa.gameObject.AddComponent<AccessoWanda1987>();
            accesso.Prepara(gioco, casa.Dimensioni.y);
        }

        void Prepara(Bootstrap game, float profondita)
        {
            gioco = game;
            portaAperta = transform.Find("porta_aperta");
            maniglia = transform.Find("maniglia");
            var materiale = portaAperta.GetComponent<Renderer>().sharedMaterial;
            chiusura = Pezzo(transform, "porta_chiusa_wanda", new Vector3(0, 1.325f, -profondita / 2),
                new Vector3(2.1f, 2.65f, .15f), materiale);
            Pezzo(chiusura.transform, "pomello", new Vector3(.36f, -.08f, -.65f), new Vector3(.04f, .025f, .5f), materiale);
            var wanda = gioco.Corpi["wanda"];
            elena = gioco.Corpi["elena"];
            wanda.localPosition = new Vector3(1.8f, .025f, -profondita / 2 - .95f);
            wanda.localRotation = Quaternion.identity;
            var sedia = new GameObject("sedia_wanda_soglia").transform;
            sedia.SetParent(transform, false);
            sedia.localPosition = wanda.localPosition;
            Pezzo(sedia, "sedile", new Vector3(0, .43f, 0), new Vector3(.5f, .07f, .5f), materiale);
            foreach (float x in new[] { -.20f, .20f }) foreach (float z in new[] { -.20f, .20f })
                Pezzo(sedia, "gamba", new Vector3(x, .21f, z), new Vector3(.055f, .42f, .055f), materiale);
            Pezzo(sedia, "schienale", new Vector3(0, .8f, .23f), new Vector3(.5f, .65f, .06f), materiale);
            Update();
        }

        static GameObject Pezzo(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = name; p.transform.SetParent(parent, false);
            p.transform.localPosition = position; p.transform.localScale = scale;
            p.GetComponent<Renderer>().sharedMaterial = material;
            return p;
        }

        void Update()
        {
            if (!chiusura || gioco.Session == null) return;
            bool aperto = Aperto;
            chiusura.SetActive(!aperto);
            portaAperta.gameObject.SetActive(aperto);
            if (maniglia) maniglia.gameObject.SetActive(aperto);
            if (elena) elena.gameObject.SetActive(aperto);
        }
    }
}
