using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class RoadSurface
    {
        const float Clearance=.085f;
        const float Grid=4;
        static Vector2 XZ(Vector3 p)=>new Vector2(p.x,p.z);
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        public static bool IsJunctionRoad(string name)=>name=="via_principale" || name.StartsWith("accesso_")
            || name=="anello_paese" || name=="strada_stazione" || name=="sagrato";

        [MenuItem("Amnesia/San Rocco 1987/Ripara strada principale")]
        public static void Repair()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Uscire da Play prima della riparazione.");
            var road=GameObject.Find("via_principale").GetComponent<MeshFilter>();
            var ground=GameObject.Find("terreno").GetComponent<MeshCollider>();
            foreach(Transform child in road.transform.parent)
                if(IsJunctionRoad(child.name)) Conform(child.GetComponent<MeshFilter>(),ground);
            // Keep the existing wear marks above the repaired asphalt.
            var collider=road.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=road.sharedMesh;
            try
            {
                foreach(var patch in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="rappezzo_stradale"))
                    if(collider.Raycast(new Ray(patch.position+Vector3.up*5,Vector3.down),out var hit,10))
                        patch.position=hit.point+Vector3.up*.007f;
            }
            finally { Object.DestroyImmediate(collider); }
            RoadCheck.Run();
            EditorSceneManager.MarkSceneDirty(road.gameObject.scene);
            EditorSceneManager.SaveScene(road.gameObject.scene);
            AssetDatabase.SaveAssets();
        }

        public static void Conform(MeshFilter road,MeshCollider ground)
        {
            // Clip each road triangle to the actual terrain faces. Every resulting
            // face has exactly the terrain slope, without frame-time projection.
            var terrain=ground.sharedMesh;
            var gv=terrain.vertices.Select(ground.transform.TransformPoint).ToArray();
            var gt=terrain.triangles;
            var buckets=new Dictionary<Vector2Int,List<int>>();
            for(int i=0;i<gt.Length;i+=3)
                foreach(var cell in Cells(XZ(gv[gt[i]]),XZ(gv[gt[i+1]]),XZ(gv[gt[i+2]])))
                {
                    if(!buckets.TryGetValue(cell,out var list)) buckets[cell]=list=new List<int>();
                    list.Add(i);
                }
            var source=road.sharedMesh;var rv=source.vertices.Select(road.transform.TransformPoint).ToArray();var rt=source.triangles;
            var vertices=new List<Vector3>();var indices=new List<int>();
            for(int i=0;i<rt.Length;i+=3)
            {
                var a=XZ(rv[rt[i]]);var b=XZ(rv[rt[i+1]]);var c=XZ(rv[rt[i+2]]);
                if(Mathf.Abs(Cross(b-a,c-a))<.000001f) continue;
                var candidates=new HashSet<int>();
                foreach(var cell in Cells(a,b,c)) if(buckets.TryGetValue(cell,out var list)) candidates.UnionWith(list);
                foreach(int triangle in candidates)
                {
                    var p=gv[gt[triangle]];var q=gv[gt[triangle+1]];var r=gv[gt[triangle+2]];
                    float winding=Mathf.Sign(Cross(XZ(q-p),XZ(r-p)));
                    var polygon=new List<Vector2>{a,b,c};
                    polygon=Clip(polygon,XZ(p),XZ(q),winding);
                    polygon=Clip(polygon,XZ(q),XZ(r),winding);
                    polygon=Clip(polygon,XZ(r),XZ(p),winding);
                    if(polygon.Count<3) continue;
                    var normal=Vector3.Cross(q-p,r-p);
                    if(Mathf.Abs(normal.y)<.00001f) continue;
                    for(int k=1;k<polygon.Count-1;k++)
                    {
                        if(Mathf.Abs(Cross(polygon[k]-polygon[0],polygon[k+1]-polygon[0]))<.000001f) continue;
                        foreach(var point in new[]{polygon[0],polygon[k],polygon[k+1]})
                        {
                            float y=p.y-(normal.x*(point.x-p.x)+normal.z*(point.y-p.z))/normal.y+(road.name=="via_principale"?Clearance:.06f);
                            indices.Add(vertices.Count);vertices.Add(road.transform.InverseTransformPoint(new Vector3(point.x,y,point.y)));
                        }
                    }
                }
            }
            if(indices.Count==0) throw new Exception("Proiezione strada vuota");
            string path=Kit.Root+"/Meshes/"+road.name+"_superficie.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null) { mesh=new Mesh{name="via_principale_superficie"};AssetDatabase.CreateAsset(mesh,path); }
            mesh.Clear();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            road.sharedMesh=mesh;EditorUtility.SetDirty(mesh);EditorUtility.SetDirty(road);
        }

        static IEnumerable<Vector2Int> Cells(Vector2 a,Vector2 b,Vector2 c)
        {
            int minX=Mathf.FloorToInt(Mathf.Min(a.x,b.x,c.x)/Grid),maxX=Mathf.FloorToInt(Mathf.Max(a.x,b.x,c.x)/Grid);
            int minY=Mathf.FloorToInt(Mathf.Min(a.y,b.y,c.y)/Grid),maxY=Mathf.FloorToInt(Mathf.Max(a.y,b.y,c.y)/Grid);
            for(int x=minX;x<=maxX;x++) for(int y=minY;y<=maxY;y++) yield return new Vector2Int(x,y);
        }
        static List<Vector2> Clip(List<Vector2> input,Vector2 a,Vector2 b,float sign)
        {
            var result=new List<Vector2>();if(input.Count==0) return result;
            var previous=input[input.Count-1];float pd=Cross(b-a,previous-a)*sign;
            foreach(var current in input)
            {
                float cd=Cross(b-a,current-a)*sign;
                bool inside=cd>=0,wasInside=pd>=0;
                if(inside!=wasInside) result.Add(Vector2.LerpUnclamped(previous,current,pd/(pd-cd)));
                if(inside) result.Add(current);
                previous=current;pd=cd;
            }
            return result;
        }
    }
}
