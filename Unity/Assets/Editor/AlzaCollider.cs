using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity.Editor
{
    /// Alza i collider degli oggetti selezionati (rocce lungo il fiume, muretti,
    /// ecc.) fino a ~4 m d'altezza, tenendo la base a terra: cosi' fanno da
    /// barriera senza il muro invisibile sottile in cui ci si incastra.
    ///
    /// Per ogni mesh sotto la selezione: se non ha un BoxCollider ne aggiunge uno
    /// (Unity lo dimensiona sulla mesh), poi ne alza l'altezza. E' RIESEGUIBILE
    /// senza compounding: porta a un'altezza-obiettivo, non moltiplica ogni volta.
    /// Annullabile con Cmd+Z.
    public static class AlzaCollider
    {
        const float AltezzaMondo = 4f; // metri

        [MenuItem("Amnesia/Alza collider (selezione)")]
        static void Alza()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Seleziona prima le rocce (o gli oggetti) da alzare.");
                return;
            }

            int fatti = 0;
            foreach (var scelto in Selection.gameObjects)
                foreach (var mf in scelto.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    var go = mf.gameObject;

                    var box = go.GetComponent<BoxCollider>();
                    if (box == null) box = Undo.AddComponent<BoxCollider>(go); // si adatta alla mesh
                    else Undo.RecordObject(box, "Alza collider");

                    float scalaY = Mathf.Max(go.transform.lossyScale.y, 0.001f);
                    float bersaglioLocale = AltezzaMondo / scalaY;
                    if (bersaglioLocale <= box.size.y) { fatti++; continue; } // gia' abbastanza alto

                    float baseY = box.center.y - box.size.y / 2f;   // tieni ferma la base
                    box.size = new Vector3(box.size.x, bersaglioLocale, box.size.z);
                    box.center = new Vector3(box.center.x, baseY + bersaglioLocale / 2f, box.center.z);
                    fatti++;
                }

            Debug.Log($"Alzati {fatti} collider a ~{AltezzaMondo} m. Cmd+Z per annullare.");
        }
    }
}
