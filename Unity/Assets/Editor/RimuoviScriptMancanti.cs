using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmnesiaUnity.Editor
{
    /// Toglie da TUTTA la scena aperta i componenti «script mancante» — quelli
    /// che fanno «The referenced script is missing!». Non tocca niente d'altro:
    /// il GameObject (camera, luce, mesh) resta, sparisce solo il componente
    /// morto. Utile con le scene demo di pacchetti che non hanno incluso i loro
    /// script d'esempio.
    ///
    /// Annullabile? No: usa l'API di Unity che li rimuove in blocco. Se vuoi
    /// prudenza, duplica la scena prima. Dopo: SALVA (Cmd+S).
    public static class RimuoviScriptMancanti
    {
        [MenuItem("Amnesia/Rimuovi script mancanti (scena aperta)")]
        static void Rimuovi()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Fallo in edit mode, non in Play.");
                return;
            }

            int tot = 0, oggetti = 0;
            var scena = SceneManager.GetActiveScene();
            foreach (var root in scena.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var go = t.gameObject;
                    int n = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                    if (n <= 0) continue;
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    tot += n;
                    oggetti++;
                }

            EditorSceneManagerMarkDirty(scena);
            Debug.Log($"Rimossi {tot} script mancanti da {oggetti} oggetti. Ora SALVA con Cmd+S.");
        }

        static void EditorSceneManagerMarkDirty(Scene s) =>
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s);
    }
}
