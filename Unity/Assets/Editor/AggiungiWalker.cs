using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity.Editor
{
    /// Mette il walker (FreeWalk) nella scena aperta, qualunque sia: un
    /// GameObject con CharacterController + FreeWalk e una Camera figlia agli
    /// occhi. Spegne le altre telecamere/ascoltatori cosi' non litigano.
    ///
    /// Serve per esplorare a piedi una scena (anche la demo di RPGPP). Compare
    /// dove guardi nella vista Scene. Al Play: WASD/frecce, mouse, Shift corre,
    /// Esc libera il cursore. Deve esserci un pavimento con collider sotto.
    public static class AggiungiWalker
    {
        [MenuItem("Amnesia/Aggiungi walker qui")]
        static void Aggiungi()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Fallo in edit mode, non in Play.");
                return;
            }

            if (Object.FindFirstObjectByType<FreeWalk>() != null)
            {
                Debug.LogWarning("C'e' gia' un walker in scena.");
                return;
            }

            // Dove nasce: dove stai guardando nella vista Scene, se c'e';
            // altrimenti sopra l'origine. Poi la gravita' lo posa sul pavimento.
            Vector3 dove = new Vector3(0f, 3f, 0f);
            var vista = SceneView.lastActiveSceneView;
            if (vista != null) dove = vista.pivot + Vector3.up * 2f;

            // Le telecamere/ascoltatori che c'erano si spengono: due che rendono
            // insieme si sovrappongono, e due AudioListener fanno brontolare Unity.
            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                cam.enabled = false;
            foreach (var orecchio in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                orecchio.enabled = false;

            var walker = new GameObject("walker");
            walker.transform.position = dove;

            var cc = walker.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.35f;

            walker.AddComponent<FreeWalk>();

            var occhio = new GameObject("occhio").AddComponent<Camera>();
            occhio.transform.SetParent(walker.transform, false);
            occhio.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            occhio.gameObject.AddComponent<AudioListener>();
            occhio.tag = "MainCamera";

            Selection.activeGameObject = walker;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("Walker aggiunto dove guardavi. SALVA (Cmd+S) e premi Play: WASD/frecce, "
                + "mouse per guardare, Shift corre, Esc libera il cursore. "
                + "Se sprofondi, sotto non c'e' un collider: spostalo sopra il terreno.");
        }
    }
}
