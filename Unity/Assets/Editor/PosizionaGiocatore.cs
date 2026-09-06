using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity.Editor
{
    /// Mette l'oggetto «giocatore» accanto a Rosa (il Personaggio con id «rosa»)
    /// nella scena aperta. In modalita' «mappa a mano» il giocatore parte dove
    /// sta questo oggetto nell'editor: se e' rimasto a (0,0,0) spawni lontano da
    /// tutti e non riesci a parlare con nessuno. Questo lo rimette al posto giusto.
    public static class PosizionaGiocatore
    {
        [MenuItem("Amnesia/Giocatore accanto a Rosa")]
        static void Metti()
        {
            var g = GameObject.Find("giocatore");
            if (g == null)
            {
                var gio = Object.FindFirstObjectByType<Giocatore>();
                g = gio != null ? gio.gameObject : null;
            }
            if (g == null)
            {
                Debug.LogWarning("Non trovo l'oggetto «giocatore». Hai lanciato «Prepara mappa giocabile»?");
                return;
            }

            Transform rosa = null;
            foreach (var p in Object.FindObjectsByType<Personaggio>(FindObjectsSortMode.None))
                if (p.Id.Trim() == "rosa") { rosa = p.transform; break; }
            if (rosa == null)
            {
                Debug.LogWarning("Non trovo Rosa in scena (un Personaggio con id «rosa»).");
                return;
            }

            Undo.RecordObject(g.transform, "Giocatore accanto a Rosa");
            g.transform.position = rosa.position + rosa.right * 2f + Vector3.up * 1f;
            EditorUtility.SetDirty(g);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(g.scene);
            Debug.Log($"Giocatore messo accanto a Rosa: {g.transform.position}. SALVA e premi Play.");
        }
    }
}
