using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class PeopleGeometry
    {
        static Mesh sphere, torso;
        static Mesh Shape(bool body)
        {
            string path=Kit.Root+"/Meshes/personaggi_"+(body?"busto":"sfera")+"_morbidi.asset";
            var cached=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var v=new List<Vector3>();var t=new List<int>();const int sides=12,rings=8;
            for(int r=0;r<=rings;r++) for(int j=0;j<=sides;j++) {
                float a=j*Mathf.PI*2/sides,y,rx,rz;
                if(body) {
                    y=r/(float)rings;
                    rx=Mathf.Lerp(.76f,1,Mathf.Sin(y*Mathf.PI));
                    if(y>.75f) rx=Mathf.Lerp(.94f,.48f,(y-.75f)*4);
                    rz=rx;
                } else { y=Mathf.Cos(r*Mathf.PI/rings);rx=rz=Mathf.Sin(r*Mathf.PI/rings); }
                v.Add(new Vector3(Mathf.Cos(a)*rx,y,Mathf.Sin(a)*rz));
            }
            for(int r=0;r<rings;r++) for(int j=0;j<sides;j++) {
                int a=r*(sides+1)+j,b=a+1,c=a+sides+1,d=c+1;
                if(body) t.AddRange(new[]{a,c,b,b,c,d});else t.AddRange(new[]{a,b,c,b,d,c});
            }
            if(body) {
                int low=v.Count;v.Add(Vector3.zero);int high=v.Count;v.Add(Vector3.up);
                for(int j=0;j<sides;j++) {int a=rings*(sides+1)+j;t.AddRange(new[]{low,j,j+1,high,a+1,a});}
            }
            var mesh=cached?cached:new Mesh();mesh.Clear();mesh.name=body?"busto_raccordato":"volume_morbido";
            mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            // Match seam normals without splitting the soft silhouette.
            var normals=mesh.normals;
            for(int r=0;r<=rings;r++) {int a=r*(sides+1),b=a+sides;normals[a]=normals[b]=(normals[a]+normals[b]).normalized;}
            mesh.normals=normals;if(!cached) AssetDatabase.CreateAsset(mesh,path);EditorUtility.SetDirty(mesh);return mesh;
        }
        public static GameObject Soft(Transform parent,string name,Vector3 p,Vector3 size,Material mat)
        {
            if(!sphere) sphere=Shape(false);
            return Part(parent,name,p,size,mat,sphere);
        }
        public static GameObject Torso(Transform parent,Vector3 p,Vector3 size,Material mat)
        {
            if(!torso) torso=Shape(true);
            return Part(parent,"busto",p,size,mat,torso);
        }
        static GameObject Part(Transform parent,string name,Vector3 p,Vector3 size,Material mat,Mesh mesh)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;return go;
        }
        public static void Limb(Transform parent,string name,Vector3 a,Vector3 b,float radius,Material mat)
        {
            int ratio=Mathf.RoundToInt((b-a).magnitude/radius*20);
            string path=Kit.Root+"/Meshes/personaggi_capsula_"+ratio+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh) {
                float length=ratio/20f;var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();
                float[] yy={-1,-.866f,-.5f,0,length,length+.5f,length+.866f,length+1};
                float[] rr={0,.5f,.866f,1,1,.866f,.5f,0};const int sides=10;
                for(int r=0;r<8;r++) for(int j=0;j<=sides;j++) {
                    float angle=j*Mathf.PI*2/sides;var radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                    vertices.Add(radial*rr[r]+Vector3.up*yy[r]);
                    normals.Add((radial*rr[r]+Vector3.up*(r<4?yy[r]:yy[r]-length)).normalized);
                }
                for(int r=0;r<7;r++) for(int j=0;j<sides;j++) {int x=r*(sides+1)+j,y=x+sides+1;triangles.AddRange(new[]{x,y,x+1,x+1,y,y+1});}
                mesh=new Mesh {name="arto_continuo"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            var go=Part(parent,name,a,Vector3.one*radius,mat,mesh);
            go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
        }
        public static void Skirt(Transform parent,Vector3 p,Vector3 size,Material mat)
        {
            const string path=Kit.Root+"/Meshes/personaggi_gonna.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh) {
                var v=new List<Vector3>();var t=new List<int>();
                for(int r=0;r<5;r++) for(int j=0;j<=12;j++) {
                    float y=r/4f,a=j*Mathf.PI/6,radius=Mathf.Lerp(1.05f,.78f,y);
                    v.Add(new Vector3(Mathf.Cos(a)*radius,y,Mathf.Sin(a)*radius));
                }
                for(int r=0;r<4;r++) for(int j=0;j<12;j++) {int a=r*13+j,b=a+13;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
                mesh=new Mesh {name="gonna_svasata"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);
            }
            Part(parent,"gonna",p,size,mat,mesh);
        }
        public static void Apron(Transform parent,float hip,float breadth,Material mat)
        {
            // A shallow curved cloth panel, not a solid ellipsoid over the body.
            var v=new List<Vector3>();var t=new List<int>();
            for(int r=0;r<4;r++) for(int j=0;j<=8;j++) {
                float y=hip-.22f+r*.17f,x=(j/8f*2-1)*breadth*(r==3?.47f:.79f);
                v.Add(new Vector3(x,y,-.206f+Mathf.Pow(x/breadth,2)*.04f));
            }
            for(int r=0;r<3;r++) for(int j=0;j<8;j++) {int a=r*9+j,b=a+9;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            string path=Kit.Root+"/Meshes/personaggi_grembiule_"+mat.name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh) {mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}mesh.Clear();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            Part(parent,"grembiule",Vector3.zero,Vector3.one,mat,mesh);
        }
        public static Transform Joint(Transform parent,string name,Vector3 position,float radius,Material mat)
        {
            var joint=Kit.Group(name,parent);joint.localPosition=position;
            Soft(joint,"raccordo",Vector3.zero,Vector3.one*radius,mat);return joint;
        }
    }
}
