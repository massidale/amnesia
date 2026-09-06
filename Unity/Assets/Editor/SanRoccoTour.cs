using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static AmnesiaUnity.Editor.SanRocco.Kit;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class Tour
    {
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        [MenuItem("Amnesia/San Rocco 1987/Registra sopralluogo delle due scene")]
        public static void Record()
        {
            if(EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try {
                foreach(string path in new[]{Build.ScenePath,ChivassoScene.ScenePath}) {
                    EditorSceneManager.OpenScene(path);Physics.SyncTransforms();Capture();
                }
            } finally { EditorSceneManager.OpenScene(Build.ScenePath); }
        }
        static void Capture()
        {
            string scene=SceneManager.GetActiveScene().name,folder=Path.GetFullPath("../artifacts/sopralluogo/"+scene);
            Directory.CreateDirectory(folder);var lines=new List<string>();
            var camera=Group("camera_sopralluogo").gameObject.AddComponent<Camera>();
            camera.fieldOfView=70;camera.nearClipPlane=.07f;camera.farClipPlane=600;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.7f,.81f,.83f);
            var rt=new RenderTexture(960,600,24);var texture=new Texture2D(960,600,TextureFormat.RGB24,false);
            camera.targetTexture=rt;int frame=0;
            void Frame(Vector3 eye,Vector3 target) {
                camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye));
                camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,960,600),0,0);texture.Apply();
                File.WriteAllBytes(Path.Combine(folder,frame.ToString("D4")+".png"),texture.EncodeToPNG());frame++;
            }
            try {
                var places=UnityEngine.Object.FindObjectsByType<Luogo1987>(FindObjectsSortMode.None).OrderBy(p=>p.Id).ToArray();
                foreach(var place in places) {
                    if(place.Id=="cava" || place.Id=="magazzino_b17") continue;
                    int first=frame;float depth=place.Dimensioni.y;
                    for(int i=0;i<32;i++) {
                        float u=i/31f;var local=V(0,1.72f,Mathf.Lerp(-depth/2-3,-depth/2+2.3f,u));
                        var eye=place.transform.TransformPoint(local);
                        Frame(eye,place.transform.TransformPoint(V(0,1.6f,1)));
                        if(Physics.OverlapSphere(eye,.12f).Any(c=>c.name!="Visitatore_fase_1")) throw new Exception("Camera dentro geometria: "+place.Id+" / "+i);
                    }
                    var pivot=place.transform.TransformPoint(V(0,1.72f,-depth/2+2.3f));
                    for(int i=0;i<48;i++) {
                        float angle=Mathf.Lerp(-85,85,i/47f);var direction=place.transform.rotation*Quaternion.Euler(0,angle,0)*Vector3.forward;
                        Frame(pivot,pivot+direction*5);
                    }
                    lines.Add(first+"-"+(frame-1)+": "+place.Id+" ingresso e panorama interno");
                }
                var centre=scene=="SanRocco1987"?V(0,8,45):V(0,0,5);
                int outside=frame;
                for(int i=0;i<120;i++) {
                    float a=i/120f*Mathf.PI*2;float radius=scene=="SanRocco1987"?160:85;
                    Frame(centre+V(Mathf.Sin(a)*radius,scene=="SanRocco1987"?100:65,Mathf.Cos(a)*radius),centre);
                }
                lines.Add(outside+"-"+(frame-1)+": giro esterno completo");
                if(scene=="SanRocco1987") {
                    var quarry=places.Single(p=>p.Id=="cava").transform;int start=frame;
                    for(int i=0;i<72;i++) Frame(quarry.TransformPoint(V(-10,1.72f,Mathf.Lerp(-12,6,i/71f))),quarry.TransformPoint(V(-9,2,12)));
                    lines.Add(start+"-"+(frame-1)+": accesso laterale cava");
                    var ground=GameObject.Find("terreno").GetComponent<MeshCollider>();
                    var border=GameObject.Find("orizzonte_continuo").GetComponent<MeshCollider>();
                    Vector3 Eye(float x,float z) {
                        var ray=new Ray(V(x,150,z),Vector3.down);
                        if(ground.Raycast(ray,out var hit,200) || border.Raycast(ray,out hit,200)) return hit.point+Vector3.up*1.72f;
                        throw new Exception("Suolo sopralluogo assente");
                    }
                    start=frame;
                    for(int i=0;i<64;i++) Frame(Eye(72,Mathf.Lerp(24,41,i/63f)),V(72,1.7f,46));
                    lines.Add(start+"-"+(frame-1)+": cimitero e cenotafio");
                    start=frame;
                    for(int i=0;i<64;i++) Frame(Eye(Mathf.Lerp(-29,-18,i/63f),105),Eye(-15,105));
                    lines.Add(start+"-"+(frame-1)+": castagneto e punto del ritrovamento");
                    var depot=places.Single(p=>p.Id=="deposito_b").transform;start=frame;
                    for(int i=0;i<64;i++) Frame(depot.TransformPoint(V(0,1.72f,Mathf.Lerp(-11,11,i/63f))),depot.TransformPoint(V(.2f,1.8f,13)));
                    lines.Add(start+"-"+(frame-1)+": corridoio B completo");
                    var b17=places.Single(p=>p.Id=="magazzino_b17").transform;start=frame;
                    for(int i=0;i<48;i++) Frame(b17.TransformPoint(V(Mathf.Lerp(-.4f,.4f,i/47f),1.72f,-1.8f)),b17.TransformPoint(V(0,1.2f,2)));
                    lines.Add(start+"-"+(frame-1)+": ispezione interna B-17, senza aprire la serranda narrativa");
                    start=frame;
                    for(int i=0;i<96;i++) {
                        float u=i/95f,n=u+.015f;
                        Frame(Eye(-135*u*u,-108-120*u),Eye(-135*n*n,-108-120*n));
                    }
                    lines.Add(start+"-"+(frame-1)+": strada esterna fino alla galleria");
                    start=frame;
                    for(int i=0;i<96;i++) {
                        float a=i/95f*Mathf.PI*2;Frame(V(Mathf.Sin(a)*18,1.8f,Mathf.Cos(a)*18),V(0,1.5f,0));
                    }
                    lines.Add(start+"-"+(frame-1)+": piazza circolare, panchine e fronti commerciali");
                }
                foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                    if(r.sharedMaterials.Any(m=>!m || !m.shader || m.shader.name.Contains("InternalError"))) throw new Exception("Materiale mancante: "+r.name);
                lines.Add("Frame: "+frame+"; materiali validi; ingressi senza intersezioni della camera.");
                File.WriteAllLines(Path.Combine(folder,"indice.txt"),lines);
                Debug.Log("SOPRALLUOGO_OK "+scene+" / "+frame+" fotogrammi / "+folder);
            } finally {
                RenderTexture.active=null;camera.targetTexture=null;
                UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }
        }
    }
    [InitializeOnLoad]
    public static class TravelSmoke
    {
        const string Key="SanRocco.TravelSmoke";
        static double ready,deadline;
        static Amnesia.Game.ConversationSession session;
        static int initialMinute;
        static TravelSmoke() { EditorApplication.update+=Tick; }
        [MenuItem("Amnesia/San Rocco 1987/Verifica viaggio andata e ritorno")]
        static void Start()
        {
            if(EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Build.ScenePath);SessionState.SetInt(Key,1);EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            int phase=SessionState.GetInt(Key,0);if(phase==0 || !EditorApplication.isPlaying || EditorApplication.isPaused) return;
            if(deadline==0) { deadline=EditorApplication.timeSinceStartup+65;ready=EditorApplication.timeSinceStartup+3; }
            if(EditorApplication.timeSinceStartup>deadline) { Finish("Timeout viaggio",false);return; }
            if(EditorApplication.timeSinceStartup<ready) return;
            string expected=phase==2?"Chivasso1987":"SanRocco1987";
            if(SceneManager.GetActiveScene().name!=expected) return;
            var trip=UnityEngine.Object.FindFirstObjectByType<Viaggio1987>();
            if(!trip || trip.InViaggio) return;
            var player=UnityEngine.Object.FindFirstObjectByType<Giocatore>();
            var walkers=UnityEngine.Object.FindObjectsByType<FreeWalk>(FindObjectsSortMode.None);
            if(session!=null && !player) { Finish("Perso il giocatore narrativo nel cambio scena",false);return; }
            if(walkers.Length+UnityEngine.Object.FindObjectsByType<Giocatore>(FindObjectsSortMode.None).Length!=1 || UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.enabled)!=1) { Finish("Duplicazione visitatore o camera",false);return; }
            if(player) {
                var game=UnityEngine.Object.FindFirstObjectByType<Bootstrap>();
                if(!game || game.Session==null) { Finish("Sessione narrativa non inizializzata",false);return; }
                if(phase==1) {
                    session=game.Session;initialMinute=session.World.Minute;
                    session.World.Flags["__test_viaggio"]=true;
                    if(game.Porte.Apri(session.World,"magazzino_b17").IsOk) { Finish("B-17 aperto senza dichiarazione",false);return; }
                    new Amnesia.Register(session.World).Record("matteo","magazzino_dove");
                    if(!game.Porte.Apri(session.World,"magazzino_b17",raccogliContenuto:false).IsOk) { Finish("B-17 non apribile con requisiti",false);return; }
                    if(session.World.ItemOwners.ContainsKey("quaderno_vittorio") || !game.Porte.Raccogli(session.World,"magazzino_b17","quaderno_vittorio").IsOk) { Finish("Raccolta singola non riuscita",false);return; }
                    game.SpalancaLaPorta("magazzino_b17");
                } else if(!ReferenceEquals(session,game.Session) || !game.Session.World.Flags.ContainsKey("__test_viaggio") || game.Session.World.Minute!=initialMinute+(phase-1)*120) {
                    Finish("Stato narrativo perso durante il viaggio",false);return;
                }
                int expectedActors=phase==2?2:12;
                if(game.Corpi.Count!=expectedActors || !session.World.ItemOwners.ContainsKey("fotografia")) { Finish("Registro NPC o inventario errato",false);return; }
                var prompts=Amnesia.Dialogue.PromptLibrary.Load(Path.GetFullPath("../content/prompts"));
                foreach(var actor in game.Corpi) {
                    if(!prompts.Keys.Any(k=>Amnesia.Dialogue.PromptLibrary.PersonaDi(k)==actor.Key)) { Finish("Scheda assente: "+actor.Key,false);return; }
                    if(game.PiuVicino(actor.Value.position,.1f)!=actor.Key) { Finish("Identita non raggiungibile: "+actor.Key,false);return; }
                    game.Accoglienza.Apri(session.World,session.Log,actor.Key);
                    if(phase!=2 && session.Log.Recent(actor.Key,1).Count==0) { Finish("Saluto non collegato: "+actor.Key,false);return; }
                }
                var menu=UnityEngine.Object.FindFirstObjectByType<AmnesiaUnity.Menu>();
                for(int page=0;page<3;page++) { menu.Apri(page);if(!menu.Aperto) { Finish("Menu non inizializzato",false);return; }menu.Chiudi(); }
                if(phase==3 && (UnityEngine.Object.FindObjectsByType<PortaMarker>(FindObjectsSortMode.None).Any(p=>p.Id=="magazzino_b17") || !session.World.ItemOwners.ContainsKey("quaderno_vittorio"))) {
                    Finish("Porta o inventario B-17 non conservati",false);return;
                }
                if(phase==3 && (UnityEngine.Object.FindObjectsByType<OggettoRaccoglibile>(FindObjectsSortMode.None).Any(o=>o.Id=="quaderno_vittorio") || session.World.ItemOwners.ContainsKey("cassetta_latta"))) {
                    Finish("Oggetti duplicati o raccolti automaticamente dopo il viaggio",false);return;
                }
            }
            if(phase==3) { Finish(player?"PARTITA_OK: 14 NPC e schede, menu, B-17 e sessione conservati nel viaggio reciproco":"VIAGGIO_OK: visita libera andata e ritorno",true);return; }
            Behaviour visitor=player ? (Behaviour)player : walkers[0];var cc=visitor.GetComponent<CharacterController>();
            cc.enabled=false;visitor.transform.position=trip.PuntoSalita.position+Vector3.up*.2f;cc.enabled=true;
            if(phase==2 && UnityEngine.Object.FindObjectsByType<Personaggio>(FindObjectsSortMode.None).Length!=2) { Finish("Popolazione Chivasso errata",false);return; }
            trip.Parti();SessionState.SetInt(Key,phase+1);ready=EditorApplication.timeSinceStartup+7;
        }
        static void Finish(string message,bool success)
        {
            SessionState.SetInt(Key,0);deadline=0;
            session=null;
            if(success) Debug.Log(message);else Debug.LogError(message);
            EditorApplication.ExitPlaymode();
        }
    }
}
