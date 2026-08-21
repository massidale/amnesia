using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity
{
    /// Aggiunge in alto il menu «Amnesia» con il comando per cuocere il paese
    /// in oggetti veri nella scena aperta. Sta in una cartella Editor, quindi
    /// non entra nella build del gioco.
    public static class GeneraScenaMenu
    {
        [MenuItem("Amnesia/Genera scena statica")]
        private static void Genera()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Fallo in edit mode, non in Play.");
                return;
            }
            var boot = Object.FindFirstObjectByType<Bootstrap>();
            if (boot == null)
            {
                Debug.LogError("Non trovo il Bootstrap nella scena aperta. Apri Prova.unity e riprova.");
                return;
            }
            boot.GeneraScenaStatica();
            EditorUtility.SetDirty(boot);
            Debug.Log("Fatto. Ora SALVA la scena con Cmd+S. Al Play non verra' rigenerata.");
        }
    }
}
