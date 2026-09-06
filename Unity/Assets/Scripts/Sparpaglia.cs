using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AmnesiaUnity
{
    /// Pianta copie casuali di un prefab come GameObject VERI (non alberi del
    /// terrain): tengono il loro collider, e li selezioni/sposti/cancelli uno a
    /// uno. Mettilo su un oggetto vuoto al centro dell'area, imposta i campi, poi
    /// tasto destro sul componente (menu tre puntini) -> "Sparpaglia".
    public sealed class Sparpaglia : MonoBehaviour
    {
        [Tooltip("Il prefab da spargere: albero, cespuglio, fungo, sasso…")]
        public GameObject prefab;
        public int quantita = 30;
        [Tooltip("Raggio dell'area attorno a questo oggetto.")]
        public float raggio = 10f;
        [Tooltip("Scala casuale: minima e massima (moltiplica la scala del prefab).")]
        public Vector2 scalaMinMax = new Vector2(0.8f, 1.3f);
        public bool rotazioneCasualeY = true;
        [Tooltip("Fa cadere ogni copia sul terreno con un raycast (serve un collider sotto).")]
        public bool appoggiaAlTerreno = true;

        [ContextMenu("Sparpaglia")]
        public void Semina()
        {
            if (prefab == null)
            {
                Debug.LogWarning("Metti un prefab nel campo 'Prefab'.");
                return;
            }
            for (var i = 0; i < quantita; i++)
            {
                // Distribuzione uniforme dentro il cerchio (sqrt per non
                // ammassare al centro).
                var ang = Random.value * Mathf.PI * 2f;
                var r = raggio * Mathf.Sqrt(Random.value);
                var pos = transform.position + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
                if (appoggiaAlTerreno
                    && Physics.Raycast(pos + Vector3.up * 200f, Vector3.down, out var colpo, 1000f))
                {
                    pos = colpo.point;
                }

                GameObject copia;
#if UNITY_EDITOR
                // In editor: resta collegata al prefab (le modifiche al prefab la
                // aggiornano).
                copia = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
                copia.transform.position = pos;
#else
                copia = Instantiate(prefab, pos, Quaternion.identity, transform);
#endif
                if (rotazioneCasualeY)
                {
                    copia.transform.rotation = Quaternion.Euler(0f, Random.value * 360f, 0f);
                }
                var s = Mathf.Lerp(scalaMinMax.x, scalaMinMax.y, Random.value);
                copia.transform.localScale = prefab.transform.localScale * s;
            }
        }

        [ContextMenu("Cancella sparpagliati")]
        public void Pulisci()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
    }
}
