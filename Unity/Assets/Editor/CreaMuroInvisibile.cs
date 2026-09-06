using System.IO;
using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity.Editor
{
    /// Crea un prefab «muro invisibile»: solo un BoxCollider, nessuna mesh. Lo
    /// trascini in scena e lo allunghi/ruoti (scala X per la lunghezza, Y per
    /// l'altezza) per fare una barriera lungo le rive del fiume o i bordi mappa,
    /// cosi' Giorgio non ci finisce dentro. In gioco non si vede; nell'editor lo
    /// vedi come riquadro verde (se i gizmo dei collider sono accesi) o quando lo
    /// selezioni.
    ///
    /// Ne servono pochi lunghi, non tanti corti: uno per sponda, allungato.
    public static class CreaMuroInvisibile
    {
        [MenuItem("Amnesia/Crea muro invisibile")]
        static void Crea()
        {
            Directory.CreateDirectory("Assets/Amnesia");
            var go = new GameObject("muro_invisibile");
            var box = go.AddComponent<BoxCollider>();
            // Un segmento di muro: 6 di lungo, 3 di alto, sottile. Poggia a terra.
            box.size = new Vector3(6f, 3f, 0.4f);
            box.center = new Vector3(0f, 1.5f, 0f);

            var path = "Assets/Amnesia/muro_invisibile.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            AssetDatabase.SaveAssets();
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"Creato {path}. Trascinalo lungo la riva: allungalo con la scala X, " +
                      "ruotalo per seguire la sponda. Invisibile in gioco.");
        }
    }
}
