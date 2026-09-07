using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class PeopleRevisionCheck
    {
        [MenuItem("Amnesia/San Rocco 1987/Aggiorna solo personaggi")]
        public static void BuildAll()
        {
            if(EditorApplication.isPlaying) throw new Exception("Uscire da Play.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Kit.Begin("persone_raccordi_");
            for(int i=0;i<People.Ids.Length;i++) People.Create(People.Ids[i],i);
            AssetDatabase.SaveAssets();Verify();
            EditorSceneManager.OpenScene(Build.ScenePath);VisualChecks.CheckPeopleAndRooms();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Build.Lighting();
            var gallery=Kit.Group("catalogo");
            for(int i=0;i<People.Ids.Length;i++) {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Kit.Root+"/Characters/"+People.Ids[i]+".prefab");
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,gallery);
                instance.transform.position=new Vector3((i%5-2)*1.25f,(2-i/5)*2.2f,0);
                Kit.Text(gallery,People.Ids[i].Replace('_',' '),instance.transform.position+new Vector3(0,-.16f,-.1f),.11f,Color.white,Quaternion.identity);
            }
            Build.Shot("personaggi_v2_tutti",new Vector3(0,3.1f,-12),new Vector3(0,3.1f,0),true,3.6f);
            UnityEngine.Object.DestroyImmediate(gallery.gameObject);
            foreach(string id in new[]{"matteo","teresa","giorgio"}) {
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Kit.Root+"/Characters/"+id+".prefab"));
                float h=People.Seated(id)?1.05f:1.25f;
                Build.Shot("personaggi_v2_"+id,new Vector3(-1.2f,h,-2.2f),new Vector3(0,h,0),true,.72f);
                var walk=instance.GetComponent<PassoPersonaggio>();
                if(walk) {
                    for(int j=0;j<12;j++) walk.Advance(Vector3.back*.035f,1/60f);
                    Build.Shot("personaggi_v2_passo",new Vector3(-2,1.1f,-3),new Vector3(0,.95f,0),true,1.03f);
                }
                UnityEngine.Object.DestroyImmediate(instance);
            }
            EditorSceneManager.OpenScene(Build.ScenePath);
            Debug.Log("PERSONAGGI_AGGIORNATI: solo prefab, nessuna rigenerazione delle mappe");
        }
        [MenuItem("Amnesia/San Rocco 1987/Verifica anatomia personaggi")]
        public static void Verify()
        {
            foreach(var id in People.Ids) {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Kit.Root+"/Characters/"+id+".prefab");
                var nodes=prefab.GetComponentsInChildren<Transform>();
                foreach(var name in new[]{"bacino","anca_sx","anca_dx","spalla_sx","spalla_dx","ginocchio_sx","ginocchio_dx","gomito_sx","gomito_dx"})
                    if(!nodes.Any(t=>t.name==name)) throw new Exception(id+": raccordo mancante "+name);
                if(prefab.GetComponentsInChildren<Collider>().Length!=1) throw new Exception(id+": collider duplicati");
                if(prefab.GetComponent<Personaggio>().Id!=(id=="giorgio"?"player":id)) throw new Exception("Identita' alterata "+id);
                if(!prefab.transform.Find(People.Seated(id)?"posa_seduta":"posa_in_piedi")) throw new Exception("Posa alterata "+id);
                foreach(var filter in prefab.GetComponentsInChildren<MeshFilter>())
                    if(!filter.sharedMesh || filter.sharedMesh.vertexCount==0) throw new Exception("Mesh vuota "+id);
            }
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Kit.Root+"/Characters/giorgio.prefab"));
            try {
                var walk=actor.GetComponent<PassoPersonaggio>();
                if(!walk) throw new Exception("Camminata mancante");
                for(int i=0;i<12;i++) walk.Advance(Vector3.back*.035f,1/60f);
                if(Quaternion.Angle(walk.AncaSinistra.localRotation,walk.AncaDestra.localRotation)<10) throw new Exception("Gambe non alternate");
                for(int i=0;i<60;i++) walk.Advance(Vector3.zero,1/60f);
                if(Quaternion.Angle(walk.AncaSinistra.localRotation,Quaternion.identity)>.01f) throw new Exception("Camminata non si ferma");
                walk.Advance(Vector3.forward*5,1/60f);
                if(Quaternion.Angle(walk.AncaDestra.localRotation,Quaternion.identity)>.01f) throw new Exception("Teletrasporto animato come passo");
            } finally { UnityEngine.Object.DestroyImmediate(actor); }
            Debug.Log("PERSONAGGI_ANATOMIA_OK: 15 identita', articolazioni e collider verificati");
        }
    }
}
