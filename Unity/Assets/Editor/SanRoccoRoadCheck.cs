using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class RoadCheck
    {
        [MenuItem("Amnesia/San Rocco 1987/Verifica superficie strada principale")]
        public static void Run()
        {
            var road=GameObject.Find("via_principale").GetComponent<MeshFilter>();
            var ground=GameObject.Find("terreno").GetComponent<MeshCollider>();
            var mesh=road.sharedMesh;var vertices=mesh.vertices;var triangles=mesh.triangles;
            var overlaps=road.transform.parent.Cast<Transform>().Where(t=>t!=road.transform && (t.name.StartsWith("accesso_") || new[]{"anello_paese","strada_stazione","sagrato"}.Contains(t.name)))
                .Select(t=> { var c=t.gameObject.AddComponent<MeshCollider>();c.sharedMesh=t.GetComponent<MeshFilter>().sharedMesh;return c; }).ToArray();
            float minimum=float.MaxValue;int buried=0,close=0,total=0,coplanar=0;
            for(int i=0;i<triangles.Length;i+=3)
                for(int a=1;a<5;a++) for(int b=1;b<5-a;b++)
                {
                    var p=road.transform.TransformPoint(vertices[triangles[i]]*(a/5f)+vertices[triangles[i+1]]*(b/5f)+vertices[triangles[i+2]]*(1-(a+b)/5f));
                    if(!ground.Raycast(new Ray(p+Vector3.up*20,Vector3.down),out var hit,40)) continue;
                    float delta=p.y-hit.point.y;minimum=Mathf.Min(minimum,delta);total++;
                    if(delta<0) buried++; if(delta<.008f) close++;
                    if(overlaps.Any(c=>c.Raycast(new Ray(p+Vector3.up,Vector3.down),out var intersection,2) && Mathf.Abs(intersection.point.y-p.y)<.002f)) coplanar++;
                }
            foreach(var collider in overlaps) Object.DestroyImmediate(collider);
            Debug.Log($"STRADA_AUDIT: triangoli={triangles.Length/3} campioni={total} sotto_terreno={buried} troppo_vicini={close} sovrapposti={coplanar} distacco_min={minimum:F4}m");
            var output=Path.GetFullPath("../artifacts/sopralluogo/strada");Directory.CreateDirectory(output);
            var camera=new GameObject("verifica_strada").AddComponent<Camera>();
            var rt=new RenderTexture(1200,720,24);var image=new Texture2D(1200,720,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try
            {
                camera.transform.position=new Vector3(.3f,1.62f,-64);
                camera.transform.LookAt(new Vector3(0,0,-34));camera.fieldOfView=65;
                camera.farClipPlane=400;camera.targetTexture=rt;camera.Render();
                RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1200,720),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(output,buried>0||coplanar>0?"prima.png":"dopo.png"),image.EncodeToPNG());
            }
            finally { RenderTexture.active=previous;camera.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(image);Object.DestroyImmediate(camera.gameObject); }
            if(close>0) throw new Exception("Strada intersecata dal terreno: " + close + " campioni");
            if(coplanar>0) throw new Exception("Strade sovrapposte alla stessa quota: " + coplanar + " campioni");
        }
    }
}
