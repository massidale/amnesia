using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace AmnesiaUnity.Editor.SanRocco
{
    // An additive dressing pass over the saved scene, never a layout rebuild.
    public static class VillageDetails
    {
        const string Folder = "Assets/SanRocco1987/VillageDetails";
        const string RootName = "09_Dettagli_Village";
        static readonly Dictionary<string, Batch> batches = new Dictionary<string, Batch>();
        static System.Random rng;
        static MeshCollider ground;
        static Collider[] obstacles;
        static Luogo1987[] places;
        static Transform root;
        static Material material;
        static RoadFootprints roads;
        static int trees, shrubs, tufts, props;
        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static Vector3 V(float x, float y, float z) => new Vector3(x,y,z);
        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        static Color Grass => C("#718A43");
        static Color Leaf => C("#7EAA4C");
        static Color Bark => C("#67523E");
        static Color Stone => C("#8C9280");

        [MenuItem("Amnesia/San Rocco 1987/Applica dettagli stile Village")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new Exception("Uscire da Play.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(Build.ScenePath);
            var previous = GameObject.Find(RootName);
            if (previous) Object.DestroyImmediate(previous);
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name == "Village_Dettagli").ToArray())
                Object.DestroyImmediate(t.gameObject);
            var original = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .ToDictionary(t => t, t => (t.localPosition, t.localRotation, t.localScale));
            var originalColliders = new HashSet<Collider>(Object.FindObjectsByType<Collider>(FindObjectsSortMode.None));
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            batches.Clear(); rng = new System.Random(19870907); trees = shrubs = tufts = props = 0;
            root = new GameObject(RootName).transform;
            ground = GameObject.Find("terreno").GetComponent<MeshCollider>();
            obstacles = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c => c != ground && c.enabled && !c.isTrigger).ToArray();
            places = Object.FindObjectsByType<Luogo1987>(FindObjectsSortMode.None);
            material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/colori.mat");
            if (!material) { material = new Material(Shader.Find("SanRocco/TerrainColor")); AssetDatabase.CreateAsset(material, Folder + "/colori.mat"); }
            material.color = Color.white; material.enableInstancing = true;
            Physics.SyncTransforms();
            roads = new RoadFootprints();
            if (!File.Exists(Kit.Root + "/Previews/village_prima_piazza.png")) Views("prima");
            PaintGround();
            Gardens();
            Meadows();
            GroundCover();
            RenderSettings.ambientSkyColor = C("#667989");
            RenderSettings.ambientEquatorColor = C("#505B54");
            RenderSettings.ambientGroundColor = C("#323D30");
            foreach (var b in batches.Values) b.Save();
            foreach (var guid in AssetDatabase.FindAssets("t:Mesh",new[]{Folder})) {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                if(Path.GetFileNameWithoutExtension(path)!="terreno_cromatico" && !batches.ContainsKey(Path.GetFileNameWithoutExtension(path))) AssetDatabase.DeleteAsset(path);
            }
            foreach (var pair in original)
                if (!pair.Key || pair.Key.localPosition != pair.Value.localPosition || pair.Key.localRotation != pair.Value.localRotation || pair.Key.localScale != pair.Value.localScale)
                    throw new Exception("Trasformazione originale alterata: " + pair.Key);
            var afterColliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
            if (!originalColliders.SetEquals(afterColliders)) throw new Exception("Collisioni originali alterate");
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, Build.ScenePath);
            Debug.Log($"VILLAGE_DRESSING_OK: {trees} alberi, {shrubs} arbusti, {tufts} ciuffi, {props} allestimenti, {batches.Count} batch. {original.Count} trasformazioni e collisioni originali intatte.");
            VillageDetailCheck.Verify();
            Views("dopo");
        }

        static void Views(string suffix)
        {
            Build.Shot("village_" + suffix + "_piazza", V(-2,2,-23), V(0,3,23));
            Build.Shot("village_" + suffix + "_ingresso", V(1,1.8f,-83), V(-4,2,-31));
            Build.Shot("village_" + suffix + "_panorama", V(110,110,-148), V(-4,0,-10), true, 83);
            Build.Shot("village_" + suffix + "_case", V(-5,2.3f,-48), V(-28,2,-37));
        }

        static float Y(float x, float z)
        {
            return ground.Raycast(new Ray(V(x,150,z), Vector3.down), out var h, 250) ? h.point.y : 0;
        }

        static bool Clear(Vector3 p, float radius)
        {
            if (new Vector2(p.x,p.z).magnitude < 19.5f || p.z < -96 || p.z > 137) return false;
            if (roads.Contains(p,radius+.35f)) return false;
            foreach (var place in places) {
                var q = place.transform.InverseTransformPoint(p);
                if (Mathf.Abs(q.x) < place.Dimensioni.x/2 + radius + .8f && Mathf.Abs(q.z) < place.Dimensioni.y/2 + radius + .8f) return false;
            }
            // Sample the actual saved road/collider geometry, not the newer generator layout.
            foreach (var c in obstacles) {
                var bounds = c.bounds; bounds.Expand(radius * 2);
                if (p.x < bounds.min.x || p.x > bounds.max.x || p.z < bounds.min.z || p.z > bounds.max.z) continue;
                for (int i = 0; i < 9; i++) {
                    float angle = i * Mathf.PI / 4;
                    var sample = p + (i == 8 ? Vector3.zero : V(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));
                    if (c.Raycast(new Ray(V(sample.x,p.y+12,sample.z),Vector3.down),out _,24)) return false;
                }
            }
            return true;
        }

        static Batch At(Vector3 p)
        {
            string key = "prato_" + Mathf.FloorToInt((p.x+160)/28) + "_" + Mathf.FloorToInt((p.z+110)/28);
            if (!batches.TryGetValue(key, out var b)) { b = new Batch(key, root); batches.Add(key,b); }
            return b;
        }

        static void PaintGround()
        {
            var filter = ground.GetComponent<MeshFilter>();
            var copy = Object.Instantiate(ground.sharedMesh); copy.name = "terreno_cromatico";
            var vertices = copy.vertices; var colors = new Color[vertices.Length];
            for (int i=0; i<vertices.Length; i++) {
                var p = ground.transform.TransformPoint(vertices[i]);
                float broad = Mathf.PerlinNoise(p.x*.035f+31,p.z*.035f+72);
                float grain = Mathf.PerlinNoise(p.x*.37f+9,p.z*.37f+11);
                Color c = Color.Lerp(C("#617747"), C("#98A663"), Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.75f,broad)));
                foreach (var place in places) {
                    var q = place.transform.InverseTransformPoint(p);
                    float d = Mathf.Max(Mathf.Abs(q.x)-place.Dimensioni.x/2,Mathf.Abs(q.z)-place.Dimensioni.y/2);
                    c = Color.Lerp(c,C("#A09C80"), (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.2f,3.8f,d)))*.68f);
                }
                float forest = Mathf.Clamp01((p.z-83)/12) * Mathf.Clamp01((142-p.z)/12);
                c = Color.Lerp(c,C("#69774A"),forest*.5f);
                c = Color.Lerp(c,C("#8E9682"),Mathf.Clamp01((p.z-140)/24));
                colors[i] = c * Mathf.Lerp(.9f,1.06f,grain);
            }
            // Weld only identical position/normal pairs; the rendered surface stays exact.
            var normals=copy.normals; var indices=copy.triangles;
            var unique=new Dictionary<(Vector3,Vector3),int>();var vv=new List<Vector3>();var nn=new List<Vector3>();var cc=new List<Color32>();
            var remap=new int[vertices.Length];
            for(int i=0;i<vertices.Length;i++) {
                var key=(vertices[i],normals[i]);
                if(!unique.TryGetValue(key,out int index)) { index=vv.Count;unique.Add(key,index);vv.Add(vertices[i]);nn.Add(normals[i]);cc.Add(colors[i]); }
                remap[i]=index;
            }
            for(int i=0;i<indices.Length;i++) indices[i]=remap[indices[i]];
            copy.Clear();copy.indexFormat=IndexFormat.UInt32;copy.SetVertices(vv);copy.SetNormals(nn);copy.SetColors(cc);copy.SetTriangles(indices,0);copy.RecalculateBounds();
            SaveMesh(copy,Folder + "/terreno_cromatico.asset");
            filter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/terreno_cromatico.asset");
            filter.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void Meadows()
        {
            var centers = new List<Vector3>();
            // Small groves leave open views toward the church and the central square.
            for (int i=0;i<420;i++) {
                var p = V(R(-82,92),0,R(-89,130)); p.y=Y(p.x,p.z);
                if (!Clear(p,2.5f) || centers.Any(q => Vector3.Distance(q,p)<5.8f)) continue;
                centers.Add(p); Tree(At(p),p,R(.75f,1.2f));
                for (int j=0;j<2;j++) {
                    var s = p+V(R(-3.8f,3.8f),0,R(-3.8f,3.8f)); s.y=Y(s.x,s.z);
                    if(Clear(s,1.4f)) Shrub(At(s),s,R(.6f,1.1f));
                }
            }
            for (int i=0;i<6800;i++) {
                var p=V(R(-78,100),0,R(-94,136)); p.y=Y(p.x,p.z)+.018f;
                if (!Clear(p,.32f)) continue;
                var b=At(p);
                if(i%15==0) { b.Lump(p,V(R(.25f,.7f),R(.15f,.4f),R(.25f,.65f)),Stone); }
                else GrassTuft(b,p,R(.12f,.32f));
            }
        }

        static void Tree(Batch b, Vector3 p, float size)
        {
            trees++;
            b.Taper(p,p+V(.15f,3.7f*size,0),.24f*size,.12f*size,Bark);
            for(int j=0;j<4;j++) {
                float a=j*Mathf.PI*.5f+.3f;
                var end=p+V(Mathf.Cos(a)*1.05f*size,(3.2f+j*.22f)*size,Mathf.Sin(a)*1.05f*size);
                b.Taper(p+V(0,2*size,0),end,.14f*size,.055f*size,Bark);
                b.Lump(end+V(0,.75f*size,0),V(1.65f,1.25f,1.55f)*size,Color.Lerp(Leaf,C("#A2B452"),R(0,.4f)));
            }
            b.Lump(p+V(0,4.8f*size,0),V(1.6f,1.4f,1.55f)*size,Leaf);
        }

        static void GroundCover()
        {
            for(int i=0;i<480;i++) {
                var p=V(R(-78,100),0,R(-94,130));p.y=Y(p.x,p.z)+.027f;
                float radius=R(.55f,1.65f);
                if(!Clear(p,radius*1.2f)) continue;
                var b=At(p);var ring=new Vector3[9];
                Color c=i%4==0?C("#9A9A6C"):i%4==1?C("#7E8853"):C("#727F4D");
                for(int j=0;j<ring.Length;j++) {
                    float a=j*Mathf.PI*2/ring.Length,r=radius*R(.75f,1.08f);
                    var v=p+V(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);v.y=Y(v.x,v.z)+.026f;ring[j]=v;
                }
                for(int j=0;j<ring.Length;j++) b.Triangle(p,ring[(j+1)%ring.Length],ring[j],c);
            }
        }

        static void Shrub(Batch b, Vector3 p, float size)
        {
            shrubs++;
            for (int j=0;j<2;j++) b.Lump(p+V((j-.5f)*.45f*size,.32f*size,j%2*.2f),V(.62f,.53f,.56f)*size,Color.Lerp(C("#587A43"),Leaf,R(0,.8f)));
        }

        static void GrassTuft(Batch b, Vector3 p, float size)
        {
            tufts++;
            for (int j=0;j<4;j++) {
                float a=R(0,Mathf.PI*2); var side=V(Mathf.Cos(a),0,Mathf.Sin(a))*size*.32f;
                var tip=p+V(R(-.12f,.12f),size*R(.7f,1.3f),R(-.12f,.12f));
                b.Triangle(p-side,p+side,tip,Color.Lerp(Grass,Leaf,R(0,.65f)),true);
            }
        }

        static void Gardens()
        {
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(Kit.Root+"/layout.json"));
            foreach(var s in layout.sites) {
                var owner=places.FirstOrDefault(p=>p.Id==s.id); if(!owner) continue;
                string key="edificio_"+s.id;
                var holder=new GameObject("Village_Dettagli").transform; holder.SetParent(owner.transform,false);
                var b=new Batch(key,holder); batches.Add(key,b);
                float x=s.w*.5f,z=s.d*.5f;
                // Local coordinates keep facade dressing attached to each movable building.
                if(s.kind=="house" || s.kind=="bar" || s.kind=="bakery" || s.kind=="shop") {
                    Planter(b,V(x-1.1f,0,-z-.72f));
                    Planter(b,V(-x+1.15f,0,-z-.8f),.8f);
                }
                // Masonry plinths and downpipes add depth without covering doors or windows.
                var masonry=C("#A2A08D");
                foreach(float side in new[]{-1f,1f}) {
                    b.Box(V(side*(x-.08f),s.h*.47f,-z-.23f),V(.10f,s.h*.94f,.10f),C("#66796F"));
                    for(float f=-z+.5f;f<z-.3f;f+=.83f)
                        b.Box(V(side*(x+.017f),.20f,f),V(.045f,.28f,.72f),masonry*R(.88f,1.04f));
                }
                if(s.kind=="house") {
                    // Side/rear garden, never on the threshold axis.
                    var p=V(-x-1.2f,0,z*.1f);
                    for(int j=0;j<5;j++) {
                        b.Box(p+V(0,.12f,j*.68f),V(.55f,.22f,.52f),Stone);
                        Shrub(b,p+V(0,.24f,j*.68f),.65f);
                    }
                    b.Box(V(x+.08f,.7f,z-.6f),V(.05f,1.4f,.13f),Bark);
                    b.Box(V(x+.09f,.28f,z-.6f),V(.07f,.4f,.38f),C("#6D756D"));
                }
                if(s.kind=="bar") {
                    for(int j=0;j<2;j++) {
                        var p=V(x+2,0,(j-.5f)*3.4f);
                        b.Taper(p,p+Vector3.up*.75f,.10f,.08f,C("#3F5552"));
                        b.Taper(p+Vector3.up*.75f,p+Vector3.up*.84f,.7f,.7f,C("#B5B7A1"));
                        foreach(float sign in new[]{-1f,1f}) Stool(b,p+V(sign*1.15f,0,0));
                        b.Taper(p+V(.15f,.84f,0),p+V(.15f,1.03f,0),.09f,.075f,C("#D9D8BA"));
                    }
                }
                if(s.kind=="bakery" || s.kind=="shop" || s.kind=="coop" || s.kind=="workshop" || s.kind=="depot") {
                    var p=V(x+1.0f,0,-z+1.5f);
                    Crate(b,p); Crate(b,p+V(.15f,.6f,0));
                    Crate(b,p+V(0,0,1.05f));
                    for(int j=0;j<7;j++) b.Lump(p+V(R(-.32f,.32f),1.27f,R(-.25f,.25f)),V(.11f,.10f,.12f),j%2==0?C("#AA6149"):C("#9DAD53"));
                }
                props++;
            }
        }

        static void Planter(Batch b, Vector3 p, float scale=1)
        {
            Color clay=C("#9F7057");
            b.Taper(p,p+V(0,.52f*scale,0),.24f*scale,.34f*scale,clay);
            b.Taper(p+V(0,.46f*scale,0),p+V(0,.55f*scale,0),.36f*scale,.36f*scale,clay*1.1f);
            b.Taper(p+V(0,.55f*scale,0),p+V(0,.56f*scale,0),.30f*scale,.30f*scale,Bark);
            for(int j=0;j<7;j++) {
                var stem=p+V(R(-.22f,.22f),.55f,R(-.22f,.22f))*scale;
                var tip=stem+V(0,R(.2f,.45f)*scale,0);
                b.Taper(stem,tip,.018f*scale,.012f*scale,Grass);
                b.Lump(tip,V(.09f,.055f,.09f)*scale,j%3==0?C("#D4C071"):C("#A85964"));
                b.Lump(stem+V(.06f,.08f,0),V(.12f,.045f,.07f)*scale,Leaf);
            }
        }
        static void Stool(Batch b,Vector3 p)
        {
            b.Box(p+V(0,.48f,0),V(.48f,.08f,.48f),Bark);
            foreach(float x in new[]{-.17f,.17f}) foreach(float z in new[]{-.17f,.17f}) b.Box(p+V(x,.23f,z),V(.06f,.46f,.06f),C("#43564D"));
        }
        static void Crate(Batch b,Vector3 p)
        {
            for(int j=0;j<3;j++) foreach(float side in new[]{-1f,1f}) {
                b.Box(p+V(0,.12f+j*.18f,side*.38f),V(.85f,.12f,.06f),C("#AE9670"));
                b.Box(p+V(side*.4f,.12f+j*.18f,0),V(.06f,.12f,.8f),C("#9C855E"));
            }
            b.Box(p+V(0,.04f,0),V(.8f,.08f,.75f),Bark);
        }

        static void SaveMesh(Mesh mesh,string path)
        {
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old) { EditorUtility.CopySerialized(mesh,old); old.UploadMeshData(false); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(old); }
            else AssetDatabase.CreateAsset(mesh,path);
        }

        sealed class Batch
        {
            readonly string name; readonly Transform parent;
            readonly List<Vector3> vertices=new List<Vector3>();
            readonly List<Color> colors=new List<Color>();
            readonly List<int> triangles=new List<int>();
            public Batch(string name,Transform parent) { this.name=name;this.parent=parent; }
            public void Triangle(Vector3 a,Vector3 b,Vector3 c,Color color,bool twoSided=false)
            {
                int n=vertices.Count;vertices.AddRange(new[]{a,b,c});colors.AddRange(new[]{color,color,color});triangles.AddRange(new[]{n,n+1,n+2});
                if(twoSided) Triangle(c,b,a,color);
            }
            public void Box(Vector3 p,Vector3 size,Color c)
            {
                var v=new Vector3[8];
                for(int i=0;i<8;i++) v[i]=p+Vector3.Scale(V((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1),size)*.5f;
                int[] t={0,2,1,1,2,3,4,5,6,5,7,6,0,1,4,1,5,4,2,6,3,3,6,7,0,4,2,2,4,6,1,3,5,3,7,5};
                for(int i=0;i<t.Length;i+=3) Triangle(v[t[i]],v[t[i+1]],v[t[i+2]],c);
            }
            public void Taper(Vector3 a,Vector3 b,float bottom,float top,Color c)
            {
                var q=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
                for(int i=0;i<7;i++) {
                    float t=i*Mathf.PI*2/7,u=(i+1)*Mathf.PI*2/7;
                    var v=q*V(Mathf.Cos(t),0,Mathf.Sin(t)); var w=q*V(Mathf.Cos(u),0,Mathf.Sin(u));
                    Triangle(a+v*bottom,b+v*top,a+w*bottom,c);
                    Triangle(a+w*bottom,b+v*top,b+w*top,c);
                    Triangle(b,b+w*top,b+v*top,c*1.07f);
                    Triangle(a,a+v*bottom,a+w*bottom,c);
                }
            }
            public void Lump(Vector3 p,Vector3 size,Color c)
            {
                const int count=7; var v=new Vector3[16];v[0]=p+V(0,-size.y,0);v[15]=p+V(.08f*size.x,size.y,0);
                for(int ring=0;ring<2;ring++) for(int j=0;j<count;j++) {
                    float a=(j+(ring%2)*.4f)*Mathf.PI*2/count;
                    float r=ring==1?.86f:.95f;
                    v[1+ring*count+j]=p+Vector3.Scale(V(Mathf.Cos(a)*r,ring==0?-.38f:.43f,Mathf.Sin(a)*r),size);
                }
                for(int j=0;j<count;j++) {
                    int next=(j+1)%count;
                    Triangle(v[0],v[1+j],v[1+next],c*.88f);
                    Triangle(v[15],v[8+next],v[8+j],c*1.05f);
                    for(int ring=0;ring<1;ring++) {
                        int a=1+ring*count+j,b=1+ring*count+next,d=a+count,e=b+count;
                        Color shade=c*R(.9f,1.07f);
                        Triangle(v[a],v[d],v[b],shade);Triangle(v[b],v[d],v[e],shade);
                    }
                }
            }
            public void Save()
            {
                var mesh=new Mesh { name=name,indexFormat=IndexFormat.UInt32 };
                mesh.SetVertices(vertices);mesh.SetColors(colors.Select(c=>(Color32)c).ToArray());mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                SaveMesh(mesh,Folder+"/"+name+".asset");
                var go=new GameObject(name);go.transform.SetParent(parent,false);
                go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/"+name+".asset");
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
                renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
            }
        }
    }
}
