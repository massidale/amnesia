using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity.Editor
{
    /// Toglie i LOD dagli oggetti selezionati: tiene solo LOD0 (la mesh piena),
    /// rimuove il componente LODGroup e cancella i figli LOD1/LOD2. Per una
    /// scena a distanza ravvicinata i LOD non servono, e i loro cambi di livello
    /// fanno lampeggiare/sparire le prop (tipico degli asset URP come Slavika).
    ///
    /// USALO IN PREFAB MODE sulla radice del prefab (doppio clic sul prefab →
    /// seleziona la radice → lancia): cosi' la correzione vale per tutte le
    /// copie. Funziona anche su oggetti in scena. Annullabile con Cmd+Z.
    public static class AppiattisciLOD
    {
        [MenuItem("Amnesia/Appiattisci LOD (tieni solo LOD0)")]
        static void Appiattisci()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Seleziona prima uno o piu' oggetti (o la radice di un prefab in Prefab Mode).");
                return;
            }

            int gruppi = 0, cancellati = 0;
            foreach (var scelto in Selection.gameObjects)
                foreach (var lod in scelto.GetComponentsInChildren<LODGroup>(true))
                {
                    var livelli = lod.GetLODs();
                    var tieni = new HashSet<Renderer>();
                    if (livelli.Length > 0)
                        foreach (var r in livelli[0].renderers)
                            if (r != null) tieni.Add(r);

                    var root = lod.gameObject;
                    // Cancella le mesh dei livelli oltre LOD0.
                    foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                        if (!tieni.Contains(r))
                        {
                            Undo.DestroyObjectImmediate(r.gameObject);
                            cancellati++;
                        }
                    Undo.DestroyObjectImmediate(lod);
                    gruppi++;
                }

            Debug.Log($"Appiattiti {gruppi} LODGroup (tenuto LOD0), cancellate {cancellati} mesh LOD1/LOD2. " +
                      "Se eri in Prefab Mode, vale per tutte le copie. Cmd+Z per annullare.");
        }
    }
}
