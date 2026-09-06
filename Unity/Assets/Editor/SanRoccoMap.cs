using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class StylizedMap
    {
        [MenuItem("Amnesia/San Rocco 1987/Aggiorna mappe stilizzate")]
        public static void Refresh()
        {
            if(EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string original=SceneManager.GetActiveScene().path;
            try {
                foreach(string path in new[]{Build.ScenePath,ChivassoScene.ScenePath}) {
                    var scene=EditorSceneManager.OpenScene(path);
                    if(Object.FindFirstObjectByType<Bootstrap>()) { Assign();EditorSceneManager.SaveScene(scene); }
                }
                AssetDatabase.SaveAssets();Debug.Log("MAPPE_STILIZZATE_OK: due scene aggiornate senza rigenerare gli edifici");
            } finally { if(!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original); }
        }

        public static void Assign()
        {
            bool village=SceneManager.GetActiveScene().name=="SanRocco1987";
            var top=new Vector2(village?-85:-85,village?220:85);
            var bottom=new Vector2(village?125:85,village?-112:-80);
            string path=Kit.Root+"/Previews/mappa_"+SceneManager.GetActiveScene().name+".png";
            Render(path,top,bottom,village);
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
            var settings=new SerializedObject(Object.FindFirstObjectByType<Bootstrap>());
            settings.FindProperty("mappaImmagine").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            settings.FindProperty("mappaMondoAltoSinistra").vector2Value=top;
            settings.FindProperty("mappaMondoBassoDestra").vector2Value=bottom;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Render(string path,Vector2 top,Vector2 bottom,bool village)
        {
            var renderers=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            var enabled=renderers.Select(r=>r.enabled).ToArray();
            var transforms=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            var places=Object.FindObjectsByType<Luogo1987>(FindObjectsSortMode.None);
            var meshes=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
            var temporary=new List<Object>();var root=new GameObject("diagramma_temporaneo");
            Material Ink(string hex) {
                var m=new Material(Shader.Find("Unlit/Color"));ColorUtility.TryParseHtmlString(hex,out var c);m.color=c;temporary.Add(m);return m;
            }
            var outline=Ink("#17221F");var building=Ink("#C3CBC7");var forest=Ink("#405D50");
            var foliage=Ink("#527363");var road=Ink("#929B91");var stone=Ink("#7B8B87");
            var accent=Ink("#B39E70");var track=Ink("#677B76");
            void Shape(Vector3[] vertices,int[] triangles,Material material) {
                var mesh=new Mesh();mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateBounds();temporary.Add(mesh);
                var go=new GameObject("simbolo");go.transform.SetParent(root.transform,false);
                go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            }
            Vector3 Flat(Vector3 p,float y)=>new Vector3(p.x,y,p.z);
            void Rect(Vector3 centre,float width,float depth,Quaternion rotation,float layer,Material material) {
                var corners=new[]{new Vector3(-width/2,0,-depth/2),new Vector3(width/2,0,-depth/2),new Vector3(width/2,0,depth/2),new Vector3(-width/2,0,depth/2)};
                Shape(corners.Select(c=>Flat(centre+rotation*c,layer)).ToArray(),new[]{0,2,1,0,3,2},material);
            }
            void Disc(Vector3 centre,float radius,float layer,Material material,int sides=16) {
                var v=new List<Vector3>{Flat(centre,layer)};var t=new List<int>();
                for(int i=0;i<sides;i++) { float a=i*Mathf.PI*2/sides;v.Add(Flat(centre+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*radius,layer)); }
                for(int i=0;i<sides;i++) t.AddRange(new[]{0,i+1,(i+1)%sides+1});
                Shape(v.ToArray(),t.ToArray(),material);
            }
            RenderTexture rt=null;Texture2D texture=null;
            try {
                foreach(var renderer in renderers) renderer.enabled=false;
                foreach(var tree in transforms.Where(t=>t.name.StartsWith("castagno_")||t.name.StartsWith("pino_"))) {
                    if(tree.GetComponent<MeshFilter>()) continue;
                    Disc(tree.position,3.2f,.1f,forest,9);Disc(tree.position,2.2f,.11f,foliage,9);
                }
                foreach(var mesh in meshes) {
                    string n=mesh.name;
                    if(!(n.StartsWith("via_")||n.StartsWith("accesso_")||n=="anello_paese"||n=="strada_stazione"||n=="mulattiera_cava"||n=="sentiero_boscaiolo"||n=="sagrato"||n=="piazzale_stazione")) continue;
                    Shape(mesh.sharedMesh.vertices.Select(v=>Flat(mesh.transform.TransformPoint(v),.3f)).ToArray(),mesh.sharedMesh.triangles,road);
                }
                if(village) { Disc(Vector3.zero,18,.4f,outline,64);Disc(Vector3.zero,17.6f,.41f,road,64);Disc(Vector3.zero,2,.42f,accent,16); }
                foreach(var place in places.Where(p=>p.Id!="magazzino_b17")) {
                    if(place.Id=="cava") { Disc(place.transform.position,17,.5f,stone,9);Disc(place.transform.position,11,.51f,outline,9);continue; }
                    var p=place.transform;float w=place.Dimensioni.x,d=place.Dimensioni.y;
                    Rect(p.position,w+1.1f,d+1.1f,p.rotation,.6f,outline);
                    Rect(p.position,w,d,p.rotation,.61f,building);
                    Rect(p.TransformPoint(new Vector3(0,0,-d/2)),2,.8f,p.rotation,.62f,accent);
                }
                var cemetery=transforms.FirstOrDefault(t=>t.name=="cimitero");
                if(cemetery) {
                    Rect(cemetery.position,24,20,Quaternion.identity,.5f,stone);
                    for(int row=0;row<4;row++) foreach(float x in new[]{-6f,6f}) Rect(cemetery.position+new Vector3(x,0,-6+row*4),2,2,Quaternion.identity,.51f,building);
                }
                var rail=transforms.FirstOrDefault(t=>t.name==(village?"raccordo_dismesso":"binario_tronco_chivasso"));
                if(rail) {
                    foreach(float x in new[]{-.8f,.8f}) Rect(rail.TransformPoint(new Vector3(x,0,0)),.3f,village?120:150,rail.rotation,.5f,track);
                    for(float z=-58;z<59;z+=3) Rect(rail.TransformPoint(new Vector3(0,0,z)),3,.35f,rail.rotation,.49f,track);
                }
                var cameraObject=new GameObject("camera_cartografica");cameraObject.transform.SetParent(root.transform,false);
                var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=(top.y-bottom.y)/2;
                camera.transform.SetPositionAndRotation(new Vector3((top.x+bottom.x)/2,100,(top.y+bottom.y)/2),Quaternion.Euler(90,0,0));
                camera.clearFlags=CameraClearFlags.SolidColor;ColorUtility.TryParseHtmlString("#293B33",out var background);camera.backgroundColor=background;
                int height=1200,width=Mathf.RoundToInt(height*(bottom.x-top.x)/(top.y-bottom.y));
                rt=new RenderTexture(width,height,24);camera.targetTexture=rt;camera.Render();
                RenderTexture.active=rt;texture=new Texture2D(width,height,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
                var pixels=texture.GetPixels32();var first=pixels[0];
                if(pixels.Count(c=>c.r!=first.r || c.g!=first.g || c.b!=first.b)<pixels.Length/100)
                    throw new System.Exception("Mappa vuota: controllare camera e simboli");
                File.WriteAllBytes(path,texture.EncodeToPNG());
            } finally {
                RenderTexture.active=null;Object.DestroyImmediate(root);
                foreach(var obj in temporary) Object.DestroyImmediate(obj);
                if(texture) Object.DestroyImmediate(texture);if(rt) Object.DestroyImmediate(rt);
                for(int i=0;i<renderers.Length;i++) if(renderers[i]) renderers[i].enabled=enabled[i];
            }
        }
    }
}
