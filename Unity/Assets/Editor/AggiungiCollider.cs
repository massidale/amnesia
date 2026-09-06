using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity.Editor
{
    /// Mette un MeshCollider a tutto quello che selezioni, scendendo nei figli:
    /// utile per rendere solide in un colpo le case (muri) o un pavimento fatto
    /// di tante piastrelle, senza toccare l'import di ogni modello.
    ///
    /// Salta chi ha gia' un collider e chi non ha una mesh. Annullabile con Cmd+Z.
    /// Nota: sui muri il MeshCollider e' concavo — per camminarci contro va bene;
    /// non usarlo su oggetti che devono cadere/rimbalzare (li' serve un convex).
    public static class AggiungiCollider
    {
        [MenuItem("Amnesia/Aggiungi MeshCollider alla selezione")]
        static void Aggiungi()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Seleziona prima uno o piu' oggetti in scena (o nella gerarchia).");
                return;
            }

            int messi = 0, saltati = 0;
            foreach (var scelto in Selection.gameObjects)
            {
                foreach (var mf in scelto.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    if (mf.GetComponent<Collider>() != null) { saltati++; continue; }
                    var mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
                    mc.sharedMesh = mf.sharedMesh;
                    messi++;
                }
            }
            Debug.Log($"MeshCollider aggiunti: {messi}. Gia' con collider (saltati): {saltati}. "
                + "Annullabile con Cmd+Z.");
        }
    }
}
