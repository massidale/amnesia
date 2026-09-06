using System.IO;
using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity.Editor
{
    /// Crea un prefab per ogni abitante di San Rocco a partire dai corpi di
    /// «Low Poly People» (David Jalbert): corpo diverso per ciascuno, componente
    /// Personaggio col suo id (serve per parlarci), posa idle cotta nel prefab
    /// (niente T-pose), e una palette di colori SU MISURA per il carattere
    /// (Anna in nero, don Carlo tonaca, Matteo terra…). Salva tutto in
    /// Assets/Amnesia/Abitanti, pronto da trascinare in scena.
    ///
    /// Le palette per-personaggio (pal_<id>.png) sono generate a parte e tengono
    /// pelle e occhi naturali; qui creo un materiale per ciascuno e glielo metto.
    /// Rieseguibile: sovrascrive prefab e materiali.
    public static class CreaAbitanti
    {
        const string Corpi = "Assets/DavidJalbert/LowPolyPeople/Prefabs/";
        const string Anim = "Assets/DavidJalbert/LowPolyPeople/Prefabs/Animations/";
        const string Out = "Assets/Amnesia/Abitanti";
        const string Palette = "Assets/Amnesia/Abitanti/Palettes";

        struct Ab { public string id, corpo, tipo; }

        static readonly Ab[] Gente =
        {
            new Ab{ id="matteo",    corpo="strong man a",   tipo="strong" },
            new Ab{ id="nino",      corpo="strong man b",   tipo="strong" },
            new Ab{ id="don_carlo", corpo="normal man a",   tipo="normal" },
            new Ab{ id="piero",     corpo="stout man a",    tipo="stout"  },
            new Ab{ id="beppe",     corpo="stout man b",    tipo="stout"  },
            new Ab{ id="gino",      corpo="normal man b",   tipo="normal" },
            new Ab{ id="rosa",      corpo="normal woman a", tipo="normal" },
            new Ab{ id="laura",     corpo="normal woman b", tipo="normal" },
            new Ab{ id="anna",      corpo="stout woman a",  tipo="stout"  },
            new Ab{ id="teresa",    corpo="strong woman a", tipo="strong" },
            new Ab{ id="marisa",    corpo="stout woman b",  tipo="stout"  },
            new Ab{ id="lidia",     corpo="normal woman c", tipo="normal" },
        };

        [MenuItem("Amnesia/Crea prefab abitanti")]
        static void Crea()
        {
            Directory.CreateDirectory(Out);
            AssetDatabase.Refresh(); // importa le palette generate fuori dall'editor

            var standard = Shader.Find("Standard");
            int n = 0;
            foreach (var a in Gente)
            {
                var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Corpi + a.corpo + ".prefab");
                if (basePrefab == null)
                {
                    Debug.LogError($"Manca il corpo «{a.corpo}» in {Corpi}");
                    continue;
                }

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
                inst.name = a.id;

                var p = inst.GetComponent<Personaggio>();
                if (p == null) p = inst.AddComponent<Personaggio>();
                p.Id = a.id;

                var mat = MaterialePerId(a.id, standard);
                if (mat != null)
                    foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                    {
                        var slots = r.sharedMaterials;
                        for (int i = 0; i < slots.Length; i++) slots[i] = mat;
                        r.sharedMaterials = slots;
                    }

                // Un corpo solido: il giocatore non ci passa attraverso.
                var cap = inst.GetComponent<CapsuleCollider>();
                if (cap == null) cap = inst.AddComponent<CapsuleCollider>();
                cap.height = 1.8f;
                cap.radius = 0.28f;
                cap.center = new Vector3(0f, 0.9f, 0f);

                var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + a.tipo + " idle.anim");
                if (idle != null) idle.SampleAnimation(inst, 0.5f);

                PrefabUtility.SaveAsPrefabAsset(inst, $"{Out}/{a.id}.prefab");
                Object.DestroyImmediate(inst);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Creati {n} abitanti (corpo + colori su misura + idle) in {Out}. Trascinali in scena.");
        }

        /// Il materiale per un id: Standard con la sua palette. La texture-palette
        /// va letta a PUNTO senza mipmap, o le celle si mescolano in grigio.
        static Material MaterialePerId(string id, Shader standard)
        {
            var texPath = $"{Palette}/pal_{id}.png";
            if (!File.Exists(texPath)) { Debug.LogWarning($"Manca la palette {texPath}"); return null; }

            var ti = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (ti != null)
            {
                bool cambia = ti.filterMode != FilterMode.Point || ti.mipmapEnabled
                              || ti.textureCompression != TextureImporterCompression.Uncompressed;
                if (cambia)
                {
                    ti.filterMode = FilterMode.Point;
                    ti.mipmapEnabled = false;
                    ti.textureCompression = TextureImporterCompression.Uncompressed;
                    ti.SaveAndReimport();
                }
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

            var matPath = $"{Out}/mat_{id}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(standard); AssetDatabase.CreateAsset(mat, matPath); }
            mat.shader = standard;
            mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
