using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AmnesiaUnity.Editor.SanRocco
{
    [InitializeOnLoad]
    public static class ItemsCheck
    {
        const string Pending="Amnesia.ItemsCheck";
        static int phase;
        static double next;
        static readonly string Output=Path.GetFullPath("../artifacts/sopralluogo/oggetti");
        static ItemsCheck()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state => { if(state==PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Pending,false); };
        }

        [MenuItem("Amnesia/San Rocco 1987/Verifica oggetti in Play")]
        static void Run()
        {
            if(EditorApplication.isPlaying) return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach(var id in Items1987.Ids)
                if(Resources.Load<GameObject>("Oggetti1987/"+id)==null) throw new Exception("Prefab mancante: "+id);
            EditorSceneManager.OpenScene("Assets/Scenes/SanRocco1987.unity");
            Directory.CreateDirectory(Output);
            SessionState.SetBool(Pending,true); phase=0; next=EditorApplication.timeSinceStartup+2;
            SessionState.SetFloat(Pending+".started",(float)EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying=true;
            EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        }
        static void Require(bool condition,string message) { if(!condition) throw new Exception(message); }
        static void Tick()
        {
            if(!SessionState.GetBool(Pending,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup<next) return;
            if(EditorApplication.timeSinceStartup-SessionState.GetFloat(Pending+".started",0)>120) { Finish(false,"Timeout caricamento/verifica");return; }
            next=EditorApplication.timeSinceStartup+.4;
            try
            {
                var game=Object.FindFirstObjectByType<Bootstrap>();
                if(game==null || game.Session==null) return;
                var panel=Object.FindFirstObjectByType<Pannello>();
                var menu=Object.FindFirstObjectByType<Menu>();
                if(phase==0)
                {
                    Require(!game.Porte.Apri(game.World,"magazzino_b17",raccogliContenuto:false).IsOk,"Porta aperta senza conoscenza");
                    new Amnesia.Register(game.World).Record("matteo","magazzino_dove");
                    Require(game.Porte.Apri(game.World,"magazzino_b17",raccogliContenuto:false).IsOk,"Porta non aperta");
                    Require(!game.World.ItemOwners.ContainsKey("quaderno_vittorio"),"Apertura ha raccolto il quaderno");
                    game.SpalancaLaPorta("magazzino_b17");
                    var pickups=Object.FindObjectsByType<OggettoRaccoglibile>(FindObjectsSortMode.None);
                    Require(pickups.Count(o=>o.Id=="quaderno_vittorio")==1,"Quaderno mancante o duplicato");
                    Require(!pickups.Any(o=>o.Id=="registro"),"Registro collocato erroneamente nel B-17");
                    var notebook=pickups.Single(o=>o.Id=="quaderno_vittorio");
                    CheckAim(notebook);
                    notebook.Interagisci(game,panel);
                    Require(!game.World.ItemOwners.ContainsKey("cassetta_latta"),"Raccolta non singola");
                    var tin=pickups.Single(o=>o.Id=="cassetta_latta");
                    CheckAim(tin);
                    tin.Interagisci(game,panel);
                    Require(OggettoRaccoglibile.Aperta(game),"Cassetta non aperta");
                    var bracelet=tin.transform.Find("braccialetto");
                    Require(bracelet.gameObject.activeInHierarchy,"Braccialetto non visibile dopo apertura");
                    Require(tin.transform.Find("coperchio/lamiera").position.y>tin.transform.Find("coperchio").position.y,"Coperchio aperto verso il basso");
                    CheckAim(bracelet.GetComponent<OggettoRaccoglibile>());
                    menu.Apri(0); Select(menu,game,"cassetta_latta");
                    Capture((RenderTexture)Object.FindFirstObjectByType<AnteprimaOggetto>().GetComponent<RawImage>().texture,Path.Combine(Output,"cassetta_aperta.png"));
                    Physics.SyncTransforms();
                    var collider=bracelet.GetComponent<Collider>();
                    var ray=new Ray(collider.bounds.center+Vector3.up*.08f,Vector3.down);
                    Require(Physics.Raycast(ray,out var hit,.15f) && hit.collider==collider,"Collisore cassetta copre il braccialetto");
                    tin.Interagisci(game,panel);
                    Require(!game.World.ItemOwners.ContainsKey("braccialetto"),"Braccialetto raccolto automaticamente");
                }
                else if(phase==1)
                {
                    Require(!Object.FindObjectsByType<OggettoRaccoglibile>(FindObjectsSortMode.None).Any(o=>!o.Fisso),"Oggetti raccolti ancora nella scena");
                    menu.Apri(0);
                    Select(menu,game,"cassetta_latta");
                    typeof(Menu).GetMethod("ApriCassetta",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(menu,null);
                    Require(game.World.ItemOwners.TryGetValue("braccialetto",out var owner)&&owner=="player","Braccialetto perso raccogliendo la cassetta");
                }
                else if(phase<2+Items1987.Ids.Length)
                {
                    var id=Items1987.Ids[phase-2]; Select(menu,game,id);
                    var preview=Object.FindFirstObjectByType<AnteprimaOggetto>();
                    var texture=(RenderTexture)preview.GetComponent<RawImage>().texture;
                    var before=Capture(texture,Path.Combine(Output,id+".png"));
                    preview.OnDrag(new PointerEventData(EventSystem.current){delta=new Vector2(95,28)});
                    var after=Capture(texture,null);
                    Require(before.Where((c,i)=>!c.Equals(after[i])).Count()>500,"Anteprima immobile: "+id);
                }
                else if(phase==2+Items1987.Ids.Length)
                {
                    Select(menu,game,"cassetta_latta");
                    EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
                    next=EditorApplication.timeSinceStartup+1;
                }
                else if(phase==3+Items1987.Ids.Length)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(Output,"inventario.png"));
                    next=EditorApplication.timeSinceStartup+1;
                }
                else { Finish(true,"11 modelli non vuoti, rotazione, apertura e raccolta singola, recupero braccialetto dalle tasche");return; }
                phase++;
            }
            catch(Exception e) { Finish(false,e.ToString()); }
        }
        static void Select(Menu menu,Bootstrap game,string id)
        {
            typeof(Menu).GetMethod("Esamina",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(menu,new object[]{game.Items.Find(id)});
            Canvas.ForceUpdateCanvases();
        }
        static void CheckAim(OggettoRaccoglibile item)
        {
            var cell=item.GetComponentInParent<Luogo1987>().transform;
            var target=item.GetComponentsInChildren<Renderer>().First().bounds.center;
            if(item.Id=="cassetta_latta") target=item.transform.TransformPoint(new Vector3(0,.09f,-.14f));
            var local=cell.InverseTransformPoint(target);
            var eye=cell.TransformPoint(new Vector3(local.x,1.62f,local.z-1.2f));
            var camera=new GameObject("verifica_raggio").AddComponent<Camera>();camera.enabled=false;
            try
            {
                camera.transform.position=eye;camera.transform.LookAt(target);Physics.SyncTransforms();
                Physics.Raycast(camera.ViewportPointToRay(new Vector3(.5f,.5f)),out var hit,5f);
                Require(OggettoRaccoglibile.Puntato(camera,eye-Vector3.up*1.62f)==item,"Oggetto non raggiungibile con E: "+item.Id+"; ostacolo="+(hit.collider!=null?hit.collider.name:"nessuno"));
            }
            finally { Object.DestroyImmediate(camera.gameObject); }
        }
        static Color32[] Capture(RenderTexture target,string path)
        {
            var previous=RenderTexture.active;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                var pixels=image.GetPixels32();var background=pixels[0];
                Require(pixels.Count(c=>Mathf.Abs(c.r-background.r)+Mathf.Abs(c.g-background.g)+Mathf.Abs(c.b-background.b)>25)>700,"Modello non visibile");
                if(path!=null) File.WriteAllBytes(path,image.EncodeToPNG());
                return pixels;
            }
            finally { RenderTexture.active=previous;Object.DestroyImmediate(image); }
        }
        static void Finish(bool success,string message)
        {
            SessionState.SetBool(Pending,false);
            if(success) Debug.Log("OGGETTI_PLAY_OK: "+message); else Debug.LogError("OGGETTI_PLAY_FAIL: "+message);
            EditorApplication.isPlaying=false;
        }
    }
}
