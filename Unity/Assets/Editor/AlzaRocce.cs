using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity.Editor
{
    /// Alza le rocce selezionate scalandole in ALTEZZA (Y), invece di aggiungere
    /// un box quadrato: cosi' resta la forma vera della roccia, solo piu' alta, e
    /// il suo MeshCollider si alza con lei. Toglie l'eventuale BoxCollider messo
    /// prima (quello squadrato) e si assicura che ci sia un MeshCollider.
    ///
    /// `Fattore` = quanto le allunga in su (2 = doppie). Rieseguibile: allunga
    /// ancora, quindi vacci piano. Annullabile con Cmd+Z.
    public static class AlzaRocce
    {
        const float Fattore = 2f;

        [MenuItem("Amnesia/Alza le rocce (in altezza)")]
        static void Alza()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Seleziona prima le rocce da alzare.");
                return;
            }

            int fatte = 0;
            foreach (var go in Selection.gameObjects)
            {
                // Via il box squadrato, se c'era; tieni la forma vera.
                var box = go.GetComponent<BoxCollider>();
                if (box != null) Undo.DestroyObjectImmediate(box);

                // Ogni mesh senza collider ne prende uno che ne segue la forma.
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    if (mf.GetComponent<Collider>() == null)
                        Undo.AddComponent<MeshCollider>(mf.gameObject);
                }

                Undo.RecordObject(go.transform, "Alza roccia");
                var s = go.transform.localScale;
                go.transform.localScale = new Vector3(s.x, s.y * Fattore, s.z);
                fatte++;
            }

            Debug.Log($"Alzate {fatte} rocce (×{Fattore} in altezza). Se sono troppo stirate, " +
                      "annulla con Cmd+Z e abbassa il Fattore, o scalale a mano in modo uniforme.");
        }
    }
}
