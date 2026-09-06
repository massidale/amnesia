using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AmnesiaUnity.Editor.SanRocco
{
    // Shared geometry and materials, persisted before any prefab is saved.
    public static class Kit
    {
        public const string Root = "Assets/SanRocco1987";
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        static int serial;
        static string meshPrefix = "";
        public static void Begin(string prefix = "") { serial = 0; meshPrefix = prefix; Materials.Clear(); }
        public static Material Mat(string name, string hex)
        {
            if (Materials.TryGetValue(name, out var cached)) return cached;
            var path = Root + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat, path); }
            ColorUtility.TryParseHtmlString(hex, out var color);
            mat.color = color; mat.SetFloat("_Glossiness", 0.06f);mat.enableInstancing=true;
            Materials[name] = mat; EditorUtility.SetDirty(mat); return mat;
        }
        public static Material Plaster => Mat("intonaco_calce", "#D7D6C7");
        public static Material Stone => Mat("pietra_grigia", "#737F81");
        public static Material Roof => Mat("lose", "#455659");
        public static Material Wood => Mat("castagno", "#85664F");
        public static Material Dark => Mat("ferro", "#303B3B");
        public static Material Glass => Mat("vetro", "#45696C");
        public static Material Red => Mat("rosso_ossido", "#964E49");
        public static Material Green => Mat("verde_persiane", "#49766B");
        public static Transform Group(string name, Transform parent = null)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
        }
        public static GameObject Box(Transform p, string name, Vector3 pos, Vector3 size, Material m, bool solid = true)
        {
            var o = GameObject.CreatePrimitive(PrimitiveType.Cube); o.name = name;
            o.transform.SetParent(p, false); o.transform.localPosition = pos; o.transform.localScale = size;
            o.GetComponent<Renderer>().sharedMaterial = m;
            if (!solid) Object.DestroyImmediate(o.GetComponent<Collider>());
            return o;
        }
        public static GameObject Mesh(Transform p, string name, Vector3[] vertices, int[] triangles, Material material, bool solid = true)
        {
            // Duplicate corners so every face has a single flat normal.
            var vv = new Vector3[triangles.Length]; var tt = new int[triangles.Length];
            for (int i = 0; i < tt.Length; i++) { vv[i] = vertices[triangles[i]]; tt[i] = i; }
            var mesh = new UnityEngine.Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = vv; mesh.triangles = tt; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var path = Root + "/Meshes/" + meshPrefix + name + "_" + serial++ + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(path);
            if (existing != null) {
                existing.Clear(); existing.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
                existing.vertices=vv; existing.triangles=tt;
                existing.RecalculateNormals(); existing.RecalculateBounds();
                EditorUtility.SetDirty(existing); Object.DestroyImmediate(mesh); mesh=existing;
            }
            else AssetDatabase.CreateAsset(mesh, path);
            mesh.UploadMeshData(false);
            var o = Group(name, p).gameObject; o.AddComponent<MeshFilter>().sharedMesh = mesh;
            o.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (solid) o.AddComponent<MeshCollider>().sharedMesh = mesh;
            return o;
        }
        public static GameObject Cone(Transform p, string name, Vector3 pos, float radius, float height, Material m, int sides = 7, float top = 0)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i=0;i<sides;i++) { float a=i*Mathf.PI*2/sides; v.Add(new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius)); v.Add(new Vector3(Mathf.Cos(a)*top,height,Mathf.Sin(a)*top)); }
            v.Add(Vector3.zero); v.Add(Vector3.up*height);
            for (int i=0;i<sides;i++) { int a=i*2,b=((i+1)%sides)*2; t.AddRange(new[]{a,a+1,b,b,a+1,b+1,2*sides,a,b,2*sides+1,b+1,a+1}); }
            var o=Mesh(p,name,v.ToArray(),t.ToArray(),m,false); o.transform.localPosition=pos; return o;
        }
        public static void Beam(Transform p, string name, Vector3 a, Vector3 b, float width, Material m)
        {
            var o=Box(p,name,(a+b)/2,new Vector3(width,(b-a).magnitude,width),m);
            o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
        }
        public static void Text(Transform p, string text, Vector3 pos, float size, Color color, Quaternion rotation)
        {
            var o=Group("scritta_"+text.Replace('\n','_'),p); o.localPosition=pos; o.localRotation=rotation;
            var tm=o.gameObject.AddComponent<TextMesh>(); tm.text=text; tm.fontSize=64; tm.characterSize=size/64f*10f;
            tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            string path=Root+"/Materials/testo_profondita.mat";
            var fontMaterial=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(fontMaterial==null) { fontMaterial=new Material(Shader.Find("SanRocco/TextDepth")); AssetDatabase.CreateAsset(fontMaterial,path); }
            fontMaterial.mainTexture=tm.font.material.mainTexture;
            tm.GetComponent<Renderer>().sharedMaterial=fontMaterial;
            tm.anchor=TextAnchor.MiddleCenter; tm.alignment=TextAlignment.Center; tm.color=color;
            tm.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        public static void Sign(Transform p,string text, Vector3 pos,float width,Material backing)
        {
            var sign=Group("insegna",p);sign.localPosition=pos;
            Box(sign,"tavola",Vector3.zero,new Vector3(width,.72f,.12f),backing,false);
            foreach(float y in new[]{-.36f,.36f}) Box(sign,"bordo",new Vector3(0,y,-.02f),new Vector3(width+.08f,.045f,.14f),Wood,false);
            foreach(float x in new[]{-width/2+.12f,width/2-.12f}) Box(sign,"chiodo",new Vector3(x,0,-.07f),new Vector3(.04f,.04f,.018f),Dark,false);
            var letters=Group("vernice",sign);letters.localPosition=Vector3.back*.067f;
            Lettering.Paint(letters,text,width-.5f,.4f,Mat("vernice_insegne","#DEDCC8"));
        }
        public static void RoofGable(Transform p,float w,float d,float h,Material gable)
        {
            float x=w/2+.65f,z=d/2+.65f,r=2.3f;
            Mesh(p,"tetto_lose",new[]{new Vector3(-x,h,-z),new Vector3(x,h,-z),new Vector3(0,h+r,-z),new Vector3(-x,h,z),new Vector3(x,h,z),new Vector3(0,h+r,z)},new[]{0,3,2,3,5,2,2,5,1,5,4,1},Roof);
            Mesh(p,"intradosso_tetto",new[]{new Vector3(-x,h-.03f,-z),new Vector3(x,h-.03f,-z),new Vector3(0,h+r-.03f,-z),new Vector3(-x,h-.03f,z),new Vector3(x,h-.03f,z),new Vector3(0,h+r-.03f,z)},new[]{2,3,0,2,5,3,1,5,2,1,4,5},Wood);
            foreach(float f in new[]{-d/2,d/2}) {
                var tri=f<0?new[]{0,2,1}:new[]{0,1,2};
                Mesh(p,"timpano",new[]{new Vector3(-x,h,f),new Vector3(x,h,f),new Vector3(0,h+r,f)},tri,gable);
            }
            foreach(float f in new[]{-z,z}) {
                Beam(p,"bordo_tetto",new Vector3(-x,h,f),new Vector3(0,h+r,f),.16f,Wood);
                Beam(p,"bordo_tetto",new Vector3(0,h+r,f),new Vector3(x,h,f),.16f,Wood);
            }
            for(int row=1;row<5;row++) foreach(float sign in new[]{-1f,1f}) {
                float px=sign*x*row/5f,py=h+r*(1-row/5f);
                Box(p,"filare_lose",new Vector3(px,py+.035f,0),new Vector3(.06f,.045f,d+1.3f),Stone,false);
            }
            Box(p,"colmo",new Vector3(0,h+r+.08f,0),new Vector3(.2f,.16f,d+1.45f),Roof,false);
        }
        public static void Table(Transform p,Vector3 pos,float width=2,float depth=1, Material top=null)
        {
            var g=Group("tavolo",p); g.localPosition=pos;
            Box(g,"piano",new Vector3(0,.8f,0),new Vector3(width,.12f,depth),top??Wood);
            foreach(float x in new[]{-width/2+.15f,width/2-.15f}) foreach(float z in new[]{-depth/2+.15f,depth/2-.15f}) Box(g,"gamba",new Vector3(x,.38f,z),new Vector3(.12f,.76f,.12f),Wood);
        }
        public static void Chair(Transform p,Vector3 pos,float yaw=0)
        {
            var g=Group("sedia",p); g.localPosition=pos; g.localRotation=Quaternion.Euler(0,yaw,0);
            Box(g,"seduta",new Vector3(0,.46f,0),new Vector3(.48f,.09f,.48f),Wood);
            foreach(float x in new[]{-.18f,.18f}) foreach(float z in new[]{-.18f,.18f}) Box(g,"gamba",new Vector3(x,.23f,z),new Vector3(.07f,.46f,.07f),Wood);
            Box(g,"schienale",new Vector3(0,.81f,.21f),new Vector3(.46f,.5f,.07f),Wood);
        }
        public static void Shelf(Transform p,Vector3 pos,float width=2)
        {
            for(int j=0;j<4;j++) Box(p,"ripiano",pos+Vector3.up*(.35f+j*.5f),new Vector3(width,.09f,.48f),Wood);
            foreach(float x in new[]{-width/2,width/2}) Box(p,"montante",pos+new Vector3(x,1.1f,0),new Vector3(.1f,2.2f,.48f),Wood);
        }
        public static void Lamp(Transform p,Vector3 pos)
        {
            Box(p,"sospensione_lampada",pos+Vector3.up*.24f,new Vector3(.035f,.5f,.035f),Dark,false);
            Cone(p,"paralume",pos,.36f,.2f,Mat("smalto_lampade","#D7C997"),8,.14f);
            var l=Group("luce_interna",p).gameObject.AddComponent<Light>(); l.transform.localPosition=pos+Vector3.down*.2f;
            l.type=LightType.Point; l.color=new Color(1,.86f,.66f); l.range=9; l.intensity=.7f; l.shadows=LightShadows.Soft;
        }
        public static GameObject Save(Transform root,string folder,string name)
        {
            var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root+"/"+folder+"/"+name+".prefab");
            Object.DestroyImmediate(root.gameObject); return prefab;
        }
    }
}
