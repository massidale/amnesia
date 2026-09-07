using System;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace AmnesiaUnity.Editor.SanRocco
{
    [InitializeOnLoad]
    public static class PeopleWalkPlayCheck
    {
        const string Key="Amnesia.PeopleWalkCheck";
        static PeopleWalkPlayCheck() { EditorApplication.update+=Tick; }
        [MenuItem("Amnesia/San Rocco 1987/Verifica orientamento e passo Giorgio")]
        public static void Run()
        {
            if(EditorApplication.isPlaying) throw new Exception("Uscire da Play.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PeopleRevisionCheck.Verify();
            EditorSceneManager.OpenScene(Build.ScenePath);
            SessionState.SetBool(Key,true);SessionState.SetInt(Key+"phase",0);
            SessionState.SetFloat(Key+"start",(float)EditorApplication.timeSinceStartup);
            SessionState.SetBool(Key+"swing",false);EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false) || EditorApplication.isCompiling) return;
            if(EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+"start",0)>60) {Finish(false,"timeout");return;}
            if(!EditorApplication.isPlaying) return;
            try {
                var player=Object.FindFirstObjectByType<Giocatore>();
                if(!player) return;
                var rig=player.GetComponentInChildren<PassoPersonaggio>();if(!rig) return;
                int phase=SessionState.GetInt(Key+"phase",0);
                if(phase==0) {
                    SessionState.SetInt(Key+"captureFramerate",Time.captureFramerate);
                    // Batch runs otherwise produce tiny controller steps unlike normal play.
                    Time.captureFramerate=60;
                    player.Posiziona(new Vector3(0,.2f,-10),Quaternion.identity);
                    if(Vector3.Dot(-rig.transform.forward,player.transform.forward)<.99f) throw new Exception("Giorgio guarda al contrario");
                    typeof(Giocatore).GetMethod("Mostra",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(player,new object[]{true});
                    var teresa=Object.FindObjectsByType<Personaggio>(FindObjectsSortMode.None).Single(p=>p.Id=="teresa").transform;
                    Build.Shot("teresa_panchina_v2",teresa.TransformPoint(new Vector3(-1.6f,1.25f,-2.4f)),teresa.TransformPoint(new Vector3(0,.9f,0)));
                    SessionState.SetInt(Key+"phase",1);
                    SessionState.SetInt(Key+"lastFrame",-1);SessionState.SetFloat(Key+"maxAngle",0);
                    player.gameObject.AddComponent<PeopleWalkTestDriver>();
                } else if(phase==1) {
                    if(Time.frameCount==SessionState.GetInt(Key+"lastFrame",-1)) return;
                    SessionState.SetInt(Key+"lastFrame",Time.frameCount);
                    var driver=player.GetComponent<PeopleWalkTestDriver>();
                    float elapsed=driver.Elapsed;
                    float angle=Quaternion.Angle(rig.AncaSinistra.localRotation,rig.AncaDestra.localRotation);
                    SessionState.SetFloat(Key+"maxAngle",Mathf.Max(angle,SessionState.GetFloat(Key+"maxAngle",0)));
                    if(angle>12) SessionState.SetBool(Key+"swing",true);
                    if(elapsed>=1.5f && (angle>25 || elapsed>=2.5f)) {
                        if(!SessionState.GetBool(Key+"swing",false)) throw new Exception("Passo assente nello spostamento reale: pos="+player.transform.position+", angolo="+SessionState.GetFloat(Key+"maxAngle",0));
                        Shot("giorgio_play_cammina",player.transform);
                        Debug.Log("GIORGIO_WALK_SAMPLE: secondi="+elapsed+", pos="+player.transform.position+", angolo="+angle);
                        Object.Destroy(driver);
                        SessionState.SetInt(Key+"phase",2);SessionState.SetFloat(Key+"changed",Time.time);
                    }
                } else if(Time.time-SessionState.GetFloat(Key+"changed",0)>1) {
                    if(Quaternion.Angle(rig.AncaSinistra.localRotation,Quaternion.identity)>.2f) throw new Exception("Passo attivo da fermo");
                    Shot("giorgio_play_fermo",player.transform);Finish(true,"orientamento +Z, passo durante Move, arresto, vista terza persona");
                }
            } catch(Exception e) { Finish(false,e.ToString()); }
        }
        static void Shot(string name,Transform p)
        {
            Build.Shot(name,p.position-p.forward*2.6f+p.right*.9f+Vector3.up*1.65f,p.position+Vector3.up*1.1f);
        }
        static void Finish(bool ok,string detail)
        {
            SessionState.SetBool(Key,false);
            Time.captureFramerate=SessionState.GetInt(Key+"captureFramerate",0);
            if(ok) Debug.Log("GIORGIO_PLAY_OK: "+detail);else Debug.LogError("GIORGIO_PLAY_FAIL: "+detail);
            EditorApplication.isPlaying=false;
            if(Application.isBatchMode) EditorApplication.delayCall+=()=>EditorApplication.Exit(ok?0:1);
        }
    }
    public sealed class PeopleWalkTestDriver : MonoBehaviour
    {
        public float Elapsed { get; private set; }
        string lastHit;
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if(hit.normal.y<.5f && lastHit!=hit.collider.name) {
                lastHit=hit.collider.name;
                Debug.Log("WALK_OBSTACLE: "+lastHit+" bounds="+hit.collider.bounds+" normal="+hit.normal);
            }
        }
        void Update()
        {
            Elapsed+=Time.deltaTime;
            GetComponent<CharacterController>().Move(transform.forward*(2.2f*Time.deltaTime));
        }
    }
}
