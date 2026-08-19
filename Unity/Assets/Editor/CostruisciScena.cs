using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AmnesiaUnity.Editor
{
    /// Crea la scena di prova da una voce di menu invece che a mano. La scena
    /// contiene tre oggetti vuoti e nient'altro: tutto il resto lo costruisce il
    /// codice all'avvio, cosi' non c'e' un file che vada tenuto allineato al
    /// contenuto ogni volta che il paese cambia.
    public static class CostruisciScena
    {
        [MenuItem("Amnesia/Costruisci la scena di prova")]
        public static void Costruisci()
        {
            var scena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject("gioco").AddComponent<Bootstrap>();
            new GameObject("pannello").AddComponent<Pannello>();
            new GameObject("giocatore").AddComponent<Giocatore>();
            new GameObject("menu").AddComponent<Menu>();

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scena, "Assets/Scenes/Prova.unity");
            EditorSceneManager.MarkSceneDirty(scena);

            Debug.Log("Scena di prova pronta. Premi Play: WASD per camminare, E per parlare, Esc per la pausa.");
        }
    }
}
