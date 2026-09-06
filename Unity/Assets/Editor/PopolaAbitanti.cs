using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity.Editor
{
    /// Mette gli abitanti di San Rocco appena FUORI dalle case (le case sono
    /// gusci chiusi: dentro non ci si sta), il don davanti alla chiesa, e Anna
    /// con una panca in piazza. Usa gli stessi modelli, pose e colori del gioco
    /// (Resources/paese/gente + il materiale Polytope a tinte separate), cosi'
    /// quello che vedi nell'editor e' quello che gira al Play.
    ///
    /// Ogni figura riceve il componente Personaggio col suo id: e' quello che
    /// permette di PARLARCI (il gioco cerca gli NPC da li').
    ///
    /// Mette anche la porta del magazzino B-17 nella casa piu' a sinistra.
    ///
    /// E' RIESEGUIBILE: rifa' da capo il gruppo «abitanti». Dopo, SALVA (Cmd+S).
    public static class PopolaAbitanti
    {
        const string CartellaGente = "Assets/Resources/paese/gente/";
        const string CartellaMat = "Assets/Amnesia/Materials";

        // Centro del paese: gli abitanti si girano verso qui e stanno sul lato
        // della casa che da' sulla piazza.
        static readonly Vector3 Centro = new Vector3(854f, 0f, 496f);
        // Quanto lontano dal centro della casa: abbastanza da uscire dal guscio
        // chiuso e restarci accanto. Se una figura finisce nel muro, alzalo.
        const float FuoriDallaCasa = 6.5f;

        struct Abitante
        {
            public string id;
            public string modello;      // prefab in Resources/paese/gente
            public Vector3 casa;
            public float altezza;
            public bool seduto;
            public Color stoffa;
            public Color capelli;
        }

        static readonly Abitante[] Gente =
        {
            new Abitante{ id="don_carlo", modello="PT_Male_Peasant_01",     casa=new Vector3(784.1f, 9.05f,570f),   altezza=1.02f, seduto=false, stoffa=new Color(0.09f,0.09f,0.10f), capelli=new Color(0.72f,0.70f,0.66f) },
            new Abitante{ id="rosa",      modello="PT_Female_Peasant_01_a", casa=new Vector3(783.8f, 9.70f,423.8f),  altezza=0.97f, seduto=false, stoffa=new Color(0.42f,0.38f,0.42f), capelli=new Color(0.35f,0.28f,0.24f) },
            new Abitante{ id="laura",     modello="PT_Female_Peasant_01_a", casa=new Vector3(810.82f,9.00f,513.79f), altezza=0.99f, seduto=false, stoffa=new Color(0.28f,0.31f,0.38f), capelli=new Color(0.24f,0.18f,0.15f) },
            new Abitante{ id="matteo",    modello="PT_Male_Peasant_01",     casa=new Vector3(844.15f,7.55f,483.62f), altezza=1.04f, seduto=false, stoffa=new Color(0.40f,0.30f,0.20f), capelli=new Color(0.22f,0.17f,0.14f) },
            new Abitante{ id="anna",      modello="PT_Female_Peasant_01_b", casa=new Vector3(881.3f, 7.10f,435.2f),  altezza=0.96f, seduto=true,  stoffa=new Color(0.11f,0.10f,0.11f), capelli=new Color(0.62f,0.60f,0.57f) },
            new Abitante{ id="nino",      modello="PT_Male_Peasant_01",     casa=new Vector3(946.68f,6.08f,465.62f), altezza=1.06f, seduto=false, stoffa=new Color(0.26f,0.30f,0.21f), capelli=new Color(0.55f,0.54f,0.51f) },
        };

        // Casa piu' a sinistra: ci va il magazzino B-17 (si apre con la chiave
        // che il giocatore ha gia' in tasca).
        static readonly Vector3 CasaMagazzino = new Vector3(764.9f, 9.42f, 478.2f);

        const string GuidPanca = "3c5e3be2ea8ae4e6c9271a675b2c2ab5"; // rpgpp_lt_bench_wood_01

        [MenuItem("Amnesia/Popola abitanti")]
        static void Popola()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Fallo in edit mode, non in Play.");
                return;
            }

            var vecchio = GameObject.Find("abitanti");
            if (vecchio != null) Object.DestroyImmediate(vecchio);
            var radice = new GameObject("abitanti");

            Directory.CreateDirectory(CartellaMat);
            var prefPanca = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(GuidPanca));

            int messi = 0;
            foreach (var a in Gente)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CartellaGente + a.modello + ".prefab");
                if (prefab == null)
                {
                    Debug.LogError($"Manca il modello {a.modello} in {CartellaGente}");
                    continue;
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, radice.transform);
                go.name = a.id;
                go.AddComponent<Personaggio>().Id = a.id;   // <-- serve per parlarci

                Vector3 verso = Verso(a.casa);
                Vector3 punto = a.casa + verso * FuoriDallaCasa;
                punto.y = Terra(punto);
                go.transform.position = punto;
                go.transform.rotation = Quaternion.LookRotation(verso, Vector3.up);
                go.transform.localScale = Vector3.one * a.altezza;

                BracciaGiu(go);
                Vesti(go, a.id, a.stoffa, a.capelli);
                Solido(go, a.altezza);

                if (a.seduto && prefPanca != null)
                    Panca(prefPanca, radice.transform, punto, verso, a.id);

                messi++;
            }

            Magazzino(radice.transform);

            EditorUtility.SetDirty(radice);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log($"Messi {messi} abitanti FUORI dalle case + porta magazzino nella casa a sinistra. " +
                      "SALVA con Cmd+S. Nota: senza animazioni le braccia sono abbassate a mano ma la posa e' " +
                      "fissa, e Anna e' IN PIEDI accanto alla panca (una vera posa seduta serve una clip).");
        }

        // --- aiutanti ---------------------------------------------------------

        static Vector3 Verso(Vector3 casa)
        {
            Vector3 v = Centro - casa; v.y = 0f;
            return v.sqrMagnitude < 0.01f ? Vector3.forward : v.normalized;
        }

        static float Terra(Vector3 p)
        {
            if (Physics.Raycast(new Vector3(p.x, 300f, p.z), Vector3.down, out var hit, 1000f))
                return hit.point.y;
            return p.y;
        }

        /// Le braccia giu' invece della T di legatura. Stessa logica del gioco:
        /// si guarda dove pende la mano e si gira la spalla finche' il braccio
        /// non punta in basso. Funziona su qualunque gabbia.
        static void BracciaGiu(GameObject figura)
        {
            var ossa = new Dictionary<string, Transform>();
            foreach (var osso in figura.GetComponentsInChildren<Transform>()) ossa[osso.name] = osso;
            Penzoloni(ossa, "PT_LeftArm", "PT_LeftHand", new Vector3(-0.22f, -1f, 0.06f));
            Penzoloni(ossa, "PT_RightArm", "PT_RightHand", new Vector3(0.22f, -1f, 0.06f));
        }

        static void Penzoloni(Dictionary<string, Transform> ossa, string spalla, string mano, Vector3 voluta)
        {
            if (!ossa.TryGetValue(spalla, out var alto) || !ossa.TryGetValue(mano, out var basso)) return;
            var braccio = basso.position - alto.position;
            if (braccio.sqrMagnitude < 0.0001f || braccio.normalized.y < -0.6f) return;
            alto.rotation = Quaternion.FromToRotation(braccio, voluta) * alto.rotation;
        }

        /// Stoffa e capelli, come nel gioco, ma su un materiale salvato come
        /// asset (npc_<id>.mat) cosi' resta anche dopo aver chiuso Unity.
        static void Vesti(GameObject go, string id, Color stoffa, Color capelli)
        {
            var renders = go.GetComponentsInChildren<Renderer>();
            if (renders.Length == 0) return;

            var path = $"{CartellaMat}/npc_{id}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) AssetDatabase.DeleteAsset(path);

            var m = new Material(renders[0].sharedMaterial) { name = "npc_" + id };
            if (m.HasProperty("_CLOTH1COLOR"))
            {
                m.SetColor("_CLOTH1COLOR", stoffa);
                m.SetColor("_CLOTH2COLOR", stoffa * 0.78f);
                m.SetColor("_CLOTH3COLOR", stoffa * 1.15f);
                m.SetColor("_CLOTH4COLOR", stoffa * 0.9f);
            }
            if (m.HasProperty("_HAIRCOLOR")) m.SetColor("_HAIRCOLOR", capelli);
            AssetDatabase.CreateAsset(m, path);

            foreach (var r in renders)
            {
                var slots = r.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = m;
                r.sharedMaterials = slots;
            }
        }

        static void Solido(GameObject go, float scala)
        {
            var cap = go.GetComponent<CapsuleCollider>();
            if (cap == null) cap = go.AddComponent<CapsuleCollider>();
            cap.height = 1.8f / Mathf.Max(scala, 0.01f);
            cap.radius = 0.3f / Mathf.Max(scala, 0.01f);
            cap.center = new Vector3(0f, cap.height * 0.5f, 0f);
        }

        static void Panca(GameObject prefab, Transform radice, Vector3 puntoNpc, Vector3 verso, string id)
        {
            var panca = (GameObject)PrefabUtility.InstantiatePrefab(prefab, radice);
            panca.name = $"panca_{id}";
            Vector3 p = puntoNpc - verso * 0.5f;
            p.y = Terra(p);
            panca.transform.position = p;
            panca.transform.rotation = Quaternion.LookRotation(verso, Vector3.up);
        }

        /// La porta del magazzino B-17 nella casa piu' a sinistra: un battente
        /// scuro col PortaMarker giusto. Il gioco lo registra e ci si apre con la
        /// chiave B-17 (che il giocatore ha dal primo minuto).
        static void Magazzino(Transform radice)
        {
            Vector3 verso = Verso(CasaMagazzino);
            Vector3 p = CasaMagazzino + verso * FuoriDallaCasa;
            p.y = Terra(p) + 1.1f;

            var battente = GameObject.CreatePrimitive(PrimitiveType.Cube);
            battente.name = "magazzino_b17";
            battente.transform.SetParent(radice);
            battente.transform.position = p;
            battente.transform.rotation = Quaternion.LookRotation(verso, Vector3.up);
            battente.transform.localScale = new Vector3(1.2f, 2.2f, 0.25f);
            battente.AddComponent<PortaMarker>().Id = "magazzino_b17";

            var matPath = $"{CartellaMat}/porta_scura.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard")) { name = "porta_scura" };
                mat.color = new Color(0.24f, 0.21f, 0.18f);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            battente.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
