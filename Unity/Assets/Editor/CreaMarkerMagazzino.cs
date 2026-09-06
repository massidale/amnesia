using System.IO;
using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity.Editor
{
    /// Crea il prefab INVISIBILE (nessuna mesh) col PortaMarker del MAGAZZINO
    /// B-17. Dentro ci sono registro, quaderno di Vittorio e — per ora — anche
    /// la cassetta di latta col braccialetto di Elena (il seminterrato e' stato
    /// tolto). Si apre con la chiave B-17 che Giorgio ha gia' in tasca.
    ///
    /// Trascinalo in scena sulla soglia del magazzino: nella finzione e'
    /// «l'edificio davanti alla chiesa, dall'altro lato del fiume» (il grande
    /// grigio). Il gioco lo registra dal suo PortaMarker.id. Invisibile in gioco.
    public static class CreaMarkerMagazzino
    {
        [MenuItem("Amnesia/Crea marker magazzino (invisibile)")]
        static void Crea()
        {
            Directory.CreateDirectory("Assets/Amnesia");
            var go = new GameObject("magazzino_b17");
            go.AddComponent<PortaMarker>().Id = "magazzino_b17";

            var path = "Assets/Amnesia/marker_magazzino.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            AssetDatabase.SaveAssets();
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"Creato {path}. Trascinalo nell'edificio davanti alla chiesa (dall'altro lato del " +
                      "fiume), sulla soglia. Dentro c'e' tutto: registro, quaderno e la cassetta col braccialetto.");
        }
    }
}
