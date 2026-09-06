using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AmnesiaUnity.Editor.SanRocco
{
    [InitializeOnLoad]
    public static class RoadWalk
    {
        const string Key="Amnesia.RoadWalk";
        static int lastFrame=-1;
        static double started;
        static double lastTick;
        static Giocatore player;
        static CharacterController body;
        static float elapsed,minY=float.MaxValue,maxY=float.MinValue;
        static readonly List<float> frames=new List<float>();
        static RoadWalk() { EditorApplication.update+=Tick; }
        [MenuItem("Amnesia/San Rocco 1987/Verifica camminata strada in Play")]
        static void Run()
        {
            if(EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/SanRocco1987.unity");
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
            EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying) return;
            if(started==0) started=EditorApplication.timeSinceStartup;
            if(EditorApplication.timeSinceStartup-started>75) { Finish(false,"timeout");return; }
            if(EditorApplication.timeSinceStartup-started<3 || lastFrame==Time.frameCount) return;
            lastFrame=Time.frameCount;
            if(player==null)
            {
                player=Object.FindFirstObjectByType<Giocatore>();
                if(player==null || (body=player.GetComponent<CharacterController>())==null) return;
                player.Posiziona(new Vector3(0,.1f,-96),Quaternion.identity);player.enabled=false;
                player.GetComponentInChildren<Camera>().transform.localRotation=Quaternion.Euler(9,0,0);
                lastTick=EditorApplication.timeSinceStartup;
                return;
            }
            double now=EditorApplication.timeSinceStartup;
            float dt=Mathf.Min((float)(now-lastTick),.1f);lastTick=now;elapsed+=dt;
            body.Move(new Vector3(0,-2,4)*dt);
            if(elapsed>1) { frames.Add(Time.unscaledDeltaTime*1000);minY=Mathf.Min(minY,player.transform.position.y);maxY=Mathf.Max(maxY,player.transform.position.y); }
            if(player.transform.position.z>=-24)
            {
                frames.Sort();var p95=frames[(int)((frames.Count-1)*.95f)];
                Finish(maxY-minY<.2f,$"percorso=72m campioni={frames.Count} oscillazione_verticale={maxY-minY:F4}m frame_mediano={frames[frames.Count/2]:F1}ms frame_p95={p95:F1}ms");
            }
        }
        static void Finish(bool ok,string message)
        {
            var folder=Path.GetFullPath("../artifacts/sopralluogo/strada");Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"camminata.txt"),(ok?"STRADA_CAMMINATA_OK: ":"STRADA_CAMMINATA_FAIL: ")+message);
            if(ok) Debug.Log("STRADA_CAMMINATA_OK: "+message);else Debug.LogError("STRADA_CAMMINATA_FAIL: "+message);
            SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;
            started=0;player=null;body=null;elapsed=0;frames.Clear();minY=float.MaxValue;maxY=float.MinValue;lastFrame=-1;
        }
    }
}
