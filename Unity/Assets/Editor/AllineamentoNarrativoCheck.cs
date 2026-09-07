using System;
using System.Linq;
using Amnesia.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AmnesiaUnity.Editor
{
    [InitializeOnLoad]
    public static class AllineamentoNarrativoCheck
    {
        const string Key = "Amnesia.NarrativeCheck";
        static double next;
        static AllineamentoNarrativoCheck() { EditorApplication.update += Tick; }

        [MenuItem("Amnesia/Verifica allineamento narrativo in Play")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new Exception("Uscire da Play prima della verifica.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/SanRocco1987.unity");
            SessionState.SetBool(Key, true);
            SessionState.SetInt(Key + ".phase", 0);
            SessionState.SetFloat(Key + ".started", (float)EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying = true;
        }

        static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Tick()
        {
            if (!SessionState.GetBool(Key, false) || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + .5;
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".started", 0) > 90) { Finish(false, "Timeout"); return; }
            if (!EditorApplication.isPlaying) return;
            try
            {
                var game = Object.FindFirstObjectByType<Bootstrap>();
                var panel = Object.FindFirstObjectByType<Pannello>();
                if (game == null || game.Session == null || panel == null) return;
                int phase = SessionState.GetInt(Key + ".phase", 0);
                if (phase == 0)
                {
                    Require(game.World.ItemOwners.Count == 0, "Inventario non vuoto all'avvio");
                    panel.Apri("rosa");
                    Require(game.World.ItemOwners.Count == 3 && !game.World.ItemOwners.ContainsKey("fotografia"), "Consegna iniziale errata");
                    var notebook = Inventario.Righe(game);
                    Require(notebook.Contains("Chi passa per primo") && !notebook.Contains("tiene la porta"), "Formula completa nel taccuino iniziale");
                    panel.Apri("don_carlo");
                    Require(Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text == "Chiedi la fotografia" && t.gameObject.activeInHierarchy), "Azione fotografia assente");
                    var photo = Resources.Load<GameObject>("Oggetti1987/fotografia");
                    Require(photo.GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("persona_")) == 8, "Numero persone nella foto errato");
                    game.ConservaPerViaggio();
                    SessionState.SetInt(Key + ".phase", 1);
                    SceneManager.LoadScene("Chivasso1987");
                }
                else if (phase == 1)
                {
                    if (SceneManager.GetActiveScene().name != "Chivasso1987") return;
                    var gate = Object.FindFirstObjectByType<AccessoWanda1987>();
                    Require(gate && !gate.Aperto, "Ingresso di Wanda aperto senza lettera");
                    Require(!game.Corpi["elena"].gameObject.activeInHierarchy && !game.Session.PuoParlare("elena"), "Elena accessibile in anticipo");
                    var closed = gate.transform.Find("porta_chiusa_wanda");
                    Require(closed.gameObject.activeInHierarchy && closed.GetComponent<Collider>().enabled, "Porta senza collisione");
                    Physics.SyncTransforms();
                    var outside = gate.transform.TransformPoint(new Vector3(0, 1.3f, -8));
                    Require(Physics.Raycast(outside, gate.transform.forward, out var hit, 3) && hit.collider == closed.GetComponent<Collider>(), "Si attraversa l'ingresso chiuso");
                    Require(game.PiuVicino(game.Corpi["wanda"].position - gate.transform.forward, 3) == "wanda", "Wanda non raggiungibile dalla strada");
                    game.World.ItemOwners["due_righe_matteo"] = "player";
                    Require(!gate.Aperto, "Possesso della lettera scambiato per presentazione");
                    game.World.MarkShown("wanda", "due_righe_matteo");
                    SessionState.SetInt(Key + ".phase", 2);
                }
                else
                {
                    var gate = Object.FindFirstObjectByType<AccessoWanda1987>();
                    Require(gate.Aperto && !gate.transform.Find("porta_chiusa_wanda").gameObject.activeInHierarchy, "Ingresso non aperto dopo lettera mostrata");
                    Require(game.Corpi["elena"].gameObject.activeInHierarchy && game.Session.PuoParlare("elena"), "Elena non disponibile dopo autorizzazione");
                    panel.Apri("elena");
                    Require(panel.Aperto, "Dialogo Elena non aperto");
                    Finish(true, "Inventario vuoto, Rosa, frammento, azione foto, otto figure, viaggio, Wanda chiusa/aperta, Elena protetta");
                }
            }
            catch (Exception error) { Finish(false, error.ToString()); }
        }

        static void Finish(bool ok, string detail)
        {
            SessionState.SetBool(Key, false);
            if (ok) Debug.Log("NARRATIVA_PLAY_OK: " + detail);
            else Debug.LogError("NARRATIVA_PLAY_FAIL: " + detail);
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.delayCall += () => EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
