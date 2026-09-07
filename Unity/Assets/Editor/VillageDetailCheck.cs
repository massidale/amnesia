using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class VillageDetailCheck
    {
        public static void SurfaceReport()
        {
            EditorSceneManager.OpenScene(Build.ScenePath);
            foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)) {
                var r=f.GetComponent<Renderer>(); if(!r) continue; var b=r.bounds;
                if(b.min.x < -6 && b.max.x > -6 && b.min.z < -80 && b.max.z > -80 && b.min.y<1 && b.max.y>-.2f)
                    Debug.Log("SURFACE " + f.name + " " + b + " mesh=" + f.sharedMesh.name);
            }
        }
        public static void Reference()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Village.unity");
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var c in cameras) Debug.Log("REFERENCE_CAMERA " + c.name + " " + c.transform.position + " " + c.transform.forward);
            foreach (var r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                Debug.Log("REFERENCE_ROOT " + r.name + " " + r.transform.position);
            var cam = cameras.FirstOrDefault(c => c.name == "Main Camera") ?? cameras.First();
            Build.Shot("village_reference", cam.transform.position, cam.transform.position + cam.transform.forward * 20);
        }

        [MenuItem("Amnesia/San Rocco 1987/Verifica dettagli Village")]
        public static void Verify()
        {
            if (EditorApplication.isPlaying) throw new Exception("Uscire da Play prima della verifica.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Build.ScenePath);
            var root = GameObject.Find("09_Dettagli_Village");
            if (!root) throw new Exception("Livello dettagli Village assente");
            if (root.GetComponentsInChildren<Collider>().Length != 0) throw new Exception("Decorazioni con nuove collisioni");
            var meshes = root.GetComponentsInChildren<MeshFilter>();
            if (meshes.Length < 15 || meshes.Length > 160) throw new Exception("Numero batch decorativi fuori budget");
            if (meshes.Any(m => !m.sharedMesh || m.sharedMesh.vertexCount == 0)) throw new Exception("Mesh mancante");
            if (root.GetComponentsInChildren<Renderer>().Any(r => !r.sharedMaterial || r.sharedMaterial.shader.name == "Hidden/InternalErrorShader"))
                throw new Exception("Materiale mancante");
            var roads = new RoadFootprints();
            var ground = GameObject.Find("terreno").GetComponent<MeshCollider>();
            foreach (var f in meshes) foreach (var vertex in f.sharedMesh.vertices) {
                var p = f.transform.TransformPoint(vertex);
                if (ground.Raycast(new Ray(p + Vector3.up * 100, Vector3.down), out var hit, 200)
                    && p.y - hit.point.y < .55f && roads.Contains(p, .02f))
                    throw new Exception("Vegetazione sulla strada: " + p);
            }
            Debug.Log("VILLAGE_DETAILS_OK: " + meshes.Length + " batch, zero nuove collisioni");
        }
    }

    // The saved ribbons are render meshes, not necessarily physics colliders.
    internal sealed class RoadFootprints
    {
        readonly System.Collections.Generic.Dictionary<Vector2Int,System.Collections.Generic.List<(Vector2 a,Vector2 b,Vector2 c,Vector2 min,Vector2 max)>> cells = new System.Collections.Generic.Dictionary<Vector2Int,System.Collections.Generic.List<(Vector2,Vector2,Vector2,Vector2,Vector2)>>();
        public RoadFootprints()
        {
            foreach (var f in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None)) {
                if (!new[]{"via_","strada_","accesso_","anello_","mulattiera_","sentiero_","sagrato","piazza"}.Any(s=>f.name.StartsWith(s)) || !f.sharedMesh) continue;
                var v=f.sharedMesh.vertices; var t=f.sharedMesh.triangles;
                for(int i=0;i<t.Length;i+=3) {
                    var a=XZ(f.transform.TransformPoint(v[t[i]]));var b=XZ(f.transform.TransformPoint(v[t[i+1]]));var c=XZ(f.transform.TransformPoint(v[t[i+2]]));
                    var min=Vector2.Min(a,Vector2.Min(b,c));var max=Vector2.Max(a,Vector2.Max(b,c));
                    for(int x=Mathf.FloorToInt((min.x-4)/16);x<=Mathf.FloorToInt((max.x+4)/16);x++) for(int z=Mathf.FloorToInt((min.y-4)/16);z<=Mathf.FloorToInt((max.y+4)/16);z++) {
                        var key=new Vector2Int(x,z);
                        if(!cells.TryGetValue(key,out var cell)) cells[key]=cell=new System.Collections.Generic.List<(Vector2,Vector2,Vector2,Vector2,Vector2)>();
                        cell.Add((a,b,c,min,max));
                    }
                }
            }
        }
        static Vector2 XZ(Vector3 p) => new Vector2(p.x,p.z);
        static float Cross(Vector2 a,Vector2 b) => a.x*b.y-a.y*b.x;
        static float Distance(Vector2 p,Vector2 a,Vector2 b)
        {
            var d=b-a; return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(d.sqrMagnitude,.000001f)));
        }
        public bool Contains(Vector3 point,float margin)
        {
            var p=XZ(point);
            if(margin>4) throw new ArgumentOutOfRangeException(nameof(margin));
            if(!cells.TryGetValue(new Vector2Int(Mathf.FloorToInt(p.x/16),Mathf.FloorToInt(p.y/16)),out var cell)) return false;
            foreach(var t in cell) {
                if(p.x<t.min.x-margin || p.x>t.max.x+margin || p.y<t.min.y-margin || p.y>t.max.y+margin) continue;
                float a=Cross(t.b-t.a,p-t.a),b=Cross(t.c-t.b,p-t.b),c=Cross(t.a-t.c,p-t.c);
                if ((a>=0&&b>=0&&c>=0)||(a<=0&&b<=0&&c<=0)) return true;
                if (Distance(p,t.a,t.b)<=margin || Distance(p,t.b,t.c)<=margin || Distance(p,t.c,t.a)<=margin) return true;
            }
            return false;
        }
    }
}
