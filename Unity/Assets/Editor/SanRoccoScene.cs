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
    [Serializable] public class Site {
        public string id,label,kind; public float x,z,w,d,h,yaw;
        public float Width => Mathf.Abs(Mathf.Cos(yaw*Mathf.Deg2Rad))*w+Mathf.Abs(Mathf.Sin(yaw*Mathf.Deg2Rad))*d;
        public float Depth => Mathf.Abs(Mathf.Sin(yaw*Mathf.Deg2Rad))*w+Mathf.Abs(Mathf.Cos(yaw*Mathf.Deg2Rad))*d;
    }
    [Serializable] public class Layout { public Site[] sites; }

    public static class Build
    {
        public const string ScenePath="Assets/Scenes/SanRocco1987.unity";
        static Site[] sites;
        static readonly Dictionary<string,Transform> places=new Dictionary<string,Transform>();
        static readonly List<string> report=new List<string>();
        static readonly List<(string name,Vector2[] points)> routes=new List<(string,Vector2[])>();
        static System.Random random;
        static MeshCollider terrainCollider;
        static float Rand(float a,float b) => a+(float)random.NextDouble()*(b-a);
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        static float BaseHeight(float x,float z)
        {
            float road = Mathf.SmoothStep(0,11,Mathf.InverseLerp(60,100,z))+Mathf.SmoothStep(0,15,Mathf.InverseLerp(100,180,z));
            return road;
        }
        public static float Height(float x,float z)
        {
            float h=BaseHeight(x,z);
            float cemetery=Mathf.Max(Mathf.Abs(x-72)-14,Mathf.Abs(z-37)-12);
            if(cemetery<5) h=Mathf.Lerp(h,BaseHeight(72,37),1-Mathf.Clamp01(cemetery/5));
            float quarry=Mathf.Max(Mathf.Abs(x)-23,Mathf.Abs(z-190)-32);
            if(quarry<7) h=Mathf.Lerp(h,26,1-Mathf.Clamp01(quarry/7));
            foreach(var s in sites) {
                float dist=Mathf.Max(Mathf.Abs(x-s.x)-s.Width/2-2,Mathf.Abs(z-s.z)-s.Depth/2-3);
                if(dist<5) h=Mathf.Lerp(h,BaseHeight(s.x,s.z),1-Mathf.Clamp01(dist/5));
            }
            if(z>=79 && z<=100) {
                float ramp=Mathf.Lerp(BaseHeight(-24,72),BaseHeight(-24,100),(z-79)/21);
                h=Mathf.Lerp(h,ramp,1-Mathf.Clamp01((Mathf.Abs(x+24)-2)/3));
            }
            return h;
        }
        [MenuItem("Amnesia/San Rocco 1987/Crea nuova scena completa")]
        public static void Create()
        {
            CreateScene(true);
        }
        [MenuItem("Amnesia/San Rocco 1987/Crea fase 1 - mappa esplorabile")]
        public static void CreateMap()
        {
            CreateScene(false);
        }
        static void CreateScene(bool complete)
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Begin(); random=new System.Random(1987); places.Clear(); report.Clear(); routes.Clear();
            foreach(var f in new[]{"Materials","Meshes","Buildings","Nature","Characters","Props","Previews"}) Directory.CreateDirectory(Root+"/"+f);
            AssetDatabase.Refresh();
            Items1987.Build();
            sites=JsonUtility.FromJson<Layout>(File.ReadAllText(Root+"/layout.json")).sites;
            ValidateLayout();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Transport.Coach();
            var ground=Group("01_Territorio"); Terrain(ground); Roads(ground);
            report.Add("FASE 1: quote, piazzole, viabilita e relazioni narrative validate.");
            var buildings=Group("02_Edifici_visitabili");
            for(int i=0;i<sites.Length;i++) {
                var s=sites[i]; var prefab=Buildings.Create(s,i,true);
                var o=(GameObject)PrefabUtility.InstantiatePrefab(prefab,buildings);
                o.transform.position=V(s.x,Height(s.x,s.z)+.05f,s.z); places[s.id]=o.transform;
                o.transform.rotation=Quaternion.Euler(0,s.yaw,0);
            }
            Landscape(ground); VillageProps(); EntranceGardens(); Quarry(); Cemetery(); Lighting();
            Characters();
            if(complete) Player(V(-13,.3f,-65),Quaternion.Euler(0,25,0)); else MapVisitor();
            Physics.SyncTransforms(); ValidateBuildings(); ValidatePaths(); ValidateAccessConnections(); CheckEntrances(); Depots.Check(); VisualChecks.CheckPolish(); VisualChecks.CheckPeopleAndRooms();
            AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene,ScenePath);
            RenderPreviews();
            if(complete) {
                StylizedMap.Assign();
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            // Never replace the user's build list. Add the two new scenes if needed.
            ChivassoScene.Create(complete);
            var entries=EditorBuildSettings.scenes.ToList();
            foreach(string p in new[]{ScenePath,ChivassoScene.ScenePath}) if(!entries.Any(e=>e.path==p)) entries.Add(new EditorBuildSettingsScene(p,true));
            EditorBuildSettings.scenes=entries.ToArray();
            EditorSceneManager.OpenScene(ScenePath);
            report.Add("Prefab e scene salvati; immagini di verifica in Assets/SanRocco1987/Previews.");
            if(!complete) report.Add("Mappa arredata e popolata: 12 abitanti con identita Personaggio, pose statiche; dialoghi non attivati nella visita libera.");
            File.WriteAllText(Root+"/validation.txt",string.Join("\n",report));
            AssetDatabase.Refresh(); Debug.Log("SAN_ROCCO_OK\n"+string.Join("\n",report));
        }
        static void ValidateLayout()
        {
            if(sites.Select(s=>s.id).Distinct().Count()!=sites.Length) throw new Exception("ID duplicati");
            for(int i=0;i<sites.Length;i++) for(int j=i+1;j<sites.Length;j++) {
                var a=sites[i];var b=sites[j];
                if(Mathf.Abs(a.x-b.x)<(a.Width+b.Width)/2+2 && Mathf.Abs(a.z-b.z)<(a.Depth+b.Depth)/2+2) throw new Exception("Edifici sovrapposti: "+a.id+" / "+b.id);
            }
            if(sites.Single(s=>s.id=="bottega").z>=105) throw new Exception("La bottega deve stare sotto il castagneto");
        }
        static void Terrain(Transform p)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            const int nx=160,nz=180; const float step=2;
            for(int z=0;z<=nz;z++) for(int x=0;x<=nx;x++) { float px=-160+x*step,pz=-110+z*step; vertices.Add(V(px,Height(px,pz)-.07f,pz)); }
            for(int z=0;z<nz;z++) for(int x=0;x<nx;x++) { int a=z*(nx+1)+x;triangles.AddRange(new[]{a,a+nx+1,a+1,a+1,a+nx+1,a+nx+2}); }
            terrainCollider=Mesh(p,"terreno",vertices.ToArray(),triangles.ToArray(),Mat("prato_ottobre","#788C68")).GetComponent<MeshCollider>();
            var terrainMesh=terrainCollider.sharedMesh;var corners=terrainMesh.vertices;
            var colors=new Color[corners.Length];
            for(int i=0;i<corners.Length;i++) {
                var q=corners[i];float noise=Mathf.PerlinNoise(q.x*.024f+12,q.z*.024f+12);
                var color=Color.Lerp(new Color(.34f,.42f,.29f),new Color(.55f,.60f,.42f),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.75f,noise)));
                float forest=Mathf.Min(q.z-84,139-q.z,q.x+76,33-q.x);
                color=Color.Lerp(color,new Color(.41f,.38f,.30f),Mathf.Clamp01(forest/7)*.7f);
                color=Color.Lerp(color,new Color(.52f,.54f,.5f),Mathf.Clamp01((q.z-143)/23)*(1-noise));
                foreach(var s in sites) {
                    float distance=Mathf.Max(Mathf.Abs(q.x-s.x)-s.Width/2,Mathf.Abs(q.z-s.z)-s.Depth/2);
                    color=Color.Lerp(color,new Color(.59f,.57f,.49f),1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,4,distance)));
                }
                colors[i]=color*(.94f+.08f*Mathf.PerlinNoise(q.x*.23f+20,q.z*.23f+20));
            }
            terrainMesh.colors=colors;
            var paint=Mat("terreno_dipinto","#FFFFFF");paint.shader=Shader.Find("SanRocco/TerrainColor");EditorUtility.SetDirty(paint);
            terrainCollider.GetComponent<Renderer>().sharedMaterial=paint;
            terrainMesh.UploadMeshData(false);EditorUtility.SetDirty(terrainMesh);
        }
        static void Ribbon(Transform p,string name,Vector2[] points,float width,Material m)
        {
            if(!name.StartsWith("accesso_") && name!="sagrato") {
                var curved=new List<Vector2>();
                for(int k=0;k<points.Length-1;k++) {
                    var a=points[k];var b=points[k+1];var n=new Vector2(-(b-a).y,(b-a).x).normalized;
                    int count=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/4));
                    for(int j=0;j<count;j++) { float u=(float)j/count;curved.Add(Vector2.Lerp(a,b,u)+n*(Mathf.Sin(u*Mathf.PI)*Mathf.Sin(u*Mathf.PI*2+k)*.65f)); }
                }
                curved.Add(points[points.Length-1]);points=curved.ToArray();
            }
            routes.Add((name,points));
            var v=new List<Vector3>();var t=new List<int>();
            var samples=new List<Vector2>();
            for(int k=0;k<points.Length-1;k++) {
                int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(points[k],points[k+1])/.6f));
                for(int j=0;j<steps;j++) samples.Add(Vector2.Lerp(points[k],points[k+1],(float)j/steps));
            }
            samples.Add(points[points.Length-1]);
            for(int i=0;i<samples.Count;i++) {
                var c=samples[i];var direction=samples[Mathf.Min(i+1,samples.Count-1)]-samples[Mathf.Max(0,i-1)];
                var normal=new Vector2(-direction.y,direction.x).normalized*width/2*(1+.07f*Mathf.Sin(c.x*.73f+c.y*.51f));
                foreach(var q in new[]{c-normal,c+normal}) {
                    if(!terrainCollider.Raycast(new Ray(V(q.x,80,q.y),Vector3.down),out var hit,100)) throw new Exception("Strada fuori terreno: "+name);
                    v.Add(hit.point+Vector3.up*.06f);
                }
                if(i<samples.Count-1) { int n=i*2;t.AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3}); }
            }
            var ribbon=Mesh(p,name,v.ToArray(),t.ToArray(),m,false);
            if(RoadSurface.IsJunctionRoad(name)) RoadSurface.Conform(ribbon.GetComponent<MeshFilter>(),terrainCollider);
        }
        static Vector2 P(float x,float z)=>new Vector2(x,z);
        static Vector2 MainRoadPoint(float z)
        {
            // Use the final wavy centreline, not the old x=0 alignment.
            var points=routes.Single(r=>r.name=="via_principale").points;
            for(int i=0;i<points.Length-1;i++) {
                var a=points[i];var b=points[i+1];
                if(z>=a.y && z<=b.y && b.y>a.y)
                    return Vector2.Lerp(a,b,(z-a.y)/(b.y-a.y));
            }
            throw new ArgumentOutOfRangeException(nameof(z),"Raccordo fuori dalla strada principale");
        }
        static void Roads(Transform p)
        {
            var asphalt=Mat("asfalto_consumato","#777E7B");var gravel=Mat("ghiaia","#B6B5A6");
            Ribbon(p,"via_principale",new[]{P(0,-108),P(0,-92),P(3,-78),P(3.5f,-65),P(0,-49),P(-3,-32),P(0,-18),P(0,17),P(-12,17),P(-12,51),P(-24,59),P(-24,64)},4.2f,asphalt);
            Ribbon(p,"anello_paese",new[]{MainRoadPoint(-70),P(-64,-70),P(-66,45),P(-12,45)},3.6f,gravel);
            Ribbon(p,"strada_stazione",new[]{P(0,-92),P(100,-92)},5.5f,asphalt);
            Ribbon(p,"mulattiera_cava",new[]{P(15,18),P(15,48),P(45,65),P(54,107),P(64,126),P(19,145),P(5,166)},3.2f,gravel);
            Ribbon(p,"sentiero_boscaiolo",new[]{P(-24,79),P(-24,96),P(-17,113),P(5,123),P(54,107)},1.7f,Mat("terra_bosco","#88866A"));
            Ribbon(p,"via_cimitero",new[]{P(15,26),P(72,25),P(72,27)},2.5f,gravel);
            foreach(var s in sites) {
                Vector3 entrance=V(s.x,0,s.z)+Quaternion.Euler(0,s.yaw,0)*V(0,0,-s.d/2);
                var frontPoint=P(entrance.x,entrance.z);
                Vector2[] access=null;
                switch(s.id) {
                    case "casa_lipari": case "casa_ferro": case "scuola": case "cooperativa": case "casa_peirano":
                        access=new[]{frontPoint,MainRoadPoint(s.z)}; break;
                    case "casa_piero": access=new[]{frontPoint,P(-40,-58),P(-40,-48),MainRoadPoint(-48)}; break;
                    case "casa_chiapello": access=new[]{frontPoint,P(-46,-70)}; break;
                    case "casa_ravera": access=new[]{frontPoint,P(-36,14),P(-36,22),P(-12,22)}; break;
                    case "casa_nino": access=new[]{frontPoint,P(-48,63),P(-48,55.5f),P(-24,55.5f),P(-12,51)}; break;
                    case "stazione": access=new[]{frontPoint,P(35,-70),MainRoadPoint(-70)}; break;
                    case "deposito_a": access=new[]{frontPoint,P(51,-77),P(51,-92)}; break;
                    case "deposito_b": access=new[]{frontPoint,P(87,-77),P(87,-92)}; break;
                    case "canonica": access=new[]{frontPoint,P(0,49),P(-12,49)}; break;
                }
                if(access!=null) { Ribbon(p,"accesso_"+s.id,access,2.4f,gravel); continue; }
                if(s.id=="casa_valli") { Ribbon(p,"accesso_"+s.id,new[]{P(22,64),P(22,61),P(37,61),P(37,55),P(15,48)},2.4f,gravel); continue; }
                if(s.yaw!=0) {
                    float edge=Mathf.Abs(s.z)<18?Mathf.Min(15,Mathf.Sqrt(18*18-s.z*s.z)-.2f):15;
                    Ribbon(p,"accesso_"+s.id,new[]{frontPoint,P(Mathf.Sign(s.x)*edge,s.z)},2.4f,gravel);
                    continue;
                }
                float front=s.z-s.d/2;
                float roadX=s.z>48?-24:0;
                if(s.x < -40 && s.z < 48) roadX=-64;
                if(s.id=="chiesa") { Ribbon(p,"sagrato",new[]{P(0,front),P(0,18)},10,gravel); continue; }
                if(s.kind=="station"||s.kind=="warehouse") Ribbon(p,"accesso_"+s.id,new[]{P(s.x,front),P(s.x,-92)},3.4f,gravel);
                else Ribbon(p,"accesso_"+s.id,new[]{P(s.x,front),P(s.x,front-3),P(roadX,front-3)},2.4f,gravel);
            }
            Cone(p,"piazza_circolare",V(0,-.03f,0),18,.07f,Mat("selciato","#7C817B"),96,18);
            Art.Square(p);
        }
        static GameObject Tree(bool chestnut,int variant)
        {
            var p=Group(chestnut?"castagno_"+variant:"pino_"+variant);
            float h=chestnut?4.4f:6.5f;
            Cone(p,"tronco",Vector3.zero,.32f,h,Wood,7,.18f);
            var col=p.gameObject.AddComponent<CapsuleCollider>();col.height=h;col.radius=.32f;col.center=Vector3.up*h/2;
            var leaf=chestnut?Mat("foglie_castagno_"+variant,variant==0?"#8C9851":variant==1?"#B5A65E":"#687E4D"):Mat("aghi_pino","#3C655C");
            if(chestnut) for(int i=0;i<3;i++) {
                float a=i*2.094f; Vector3 q=V(Mathf.Cos(a)*1.2f,h-.3f+i*.25f,Mathf.Sin(a)*1.2f);
                Beam(p,"ramo",V(0,h-1.5f,0),q,.2f,Wood);
                Art.Facet(p,"chioma_castagno",q+Vector3.up*.65f,V(2.15f,1.65f,2.1f),leaf,i+variant*5);
            }
            else for(int i=0;i<3;i++) Cone(p,"fronda",V(0,2+i*1.55f,0),2.6f-i*.6f,3.6f,leaf,7);
            return Save(p,"Nature",p.name);
        }
        static GameObject Rock()
        {
            var p=Group("masso");
            var o=Cone(p,"roccia_scheggiata",Vector3.zero,1.4f,1.4f,Stone,5,.65f);
            o.AddComponent<MeshCollider>().sharedMesh=o.GetComponent<MeshFilter>().sharedMesh;
            return Save(p,"Nature","masso");
        }
        static void Scatter(GameObject prefab,Transform parent,Vector3 pos,Vector3 scale,float yaw)
        {
            var o=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);o.transform.position=pos;
            o.transform.localScale=scale;o.transform.rotation=Quaternion.Euler(0,yaw,0);
        }
        static void Landscape(Transform ground)
        {
            var woods=Group("03_Castagneto",ground);var hills=Group("04_Creste",ground);
            var trees=new[]{Tree(true,0),Tree(true,1),Tree(true,2)};var pine=Tree(false,0);var rock=Rock();
            for(int i=0;i<150;i++) {
                float x=Rand(-67,27),z=Rand(88,131);
                if(NearRoute(P(x,z),2.4f)) continue;
                if(Mathf.Abs(x+24)<4 || (x>-26 && x<-8 && z>98 && z<114)) continue;
                Scatter(trees[i%3],woods,V(x,Height(x,z),z),Vector3.one*Rand(.8f,1.25f),Rand(0,360));
            }
            for(int i=0;i<100;i++) {
                float x=Rand(-125,125),z=Rand(-90,210);
                if(NearRoute(P(x,z),2.4f)) continue;
                if(x>-77&&x<103&&z<155) continue;
                if(Mathf.Abs(x)<45&&z>145) continue;
                Scatter(z>130?pine:trees[i%3],woods,V(x,Height(x,z),z),Vector3.one*Rand(.85f,1.4f),Rand(0,360));
            }
            Boundary.Create(hills);
            for(int i=0;i<110;i++) {
                float x=Rand(-75,30),z=Rand(89,137);
                if(NearRoute(P(x,z),2.4f)) continue;
                Art.Facet(woods,"cespuglio",V(x,Height(x,z)+.35f,z),V(.8f,.5f,.7f),Mat("sottobosco","#526B4D"),i);
            }
            var found=Group("Punto_ritrovamento_1985",woods);found.position=V(-16,Height(-16,105),105);
            Cone(found,"ceppaia",V(2,0,0),.45f,.55f,Wood,8,.4f);
            for(int i=0;i<20;i++) Box(found,"foglie",V(Rand(-2,2),.025f,Rand(-2,2)),V(.16f,.018f,.09f),Mat("foglie_cadute","#B6A26D"),false);
            report.Add("Castagneto a latifoglie dietro la bottega; conifere sulle quote alte.");
        }
        static bool NearRoute(Vector2 point,float clearance)
        {
            foreach(var route in routes) for(int i=0;i<route.points.Length-1;i++) {
                var a=route.points[i];var d=route.points[i+1]-a;
                float t=d.sqrMagnitude==0?0:Mathf.Clamp01(Vector2.Dot(point-a,d)/d.sqrMagnitude);
                if(Vector2.Distance(point,a+t*d)<clearance) return true;
            }
            return false;
        }
        static void EntranceGardens()
        {
            var parent=Group("06_Campione_ingresso_piazza");
            var tree=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Nature/castagno_1.prefab");
            var shrubRoot=Group("arbusto_cortile");
            Art.Facet(shrubRoot,"foglie",V(0,.45f,0),V(1,.65f,.85f),Mat("verde_cortile","#5E784F"),4);
            var shrub=Save(shrubRoot,"Nature","arbusto_cortile");
            var centres=new[]{P(-11,-80),P(11,-47),P(-13,-26)};
            for(int i=0;i<centres.Length;i++) {
                var centre=centres[i];var garden=Group("cortile_alberato_"+i,parent);
                foreach(var offset in new[]{P(-1.4f,.5f),P(1.7f,1.6f)}) {
                    var point=centre+offset;
                    if(!GardenClear(point,2.8f)) continue;
                    Scatter(tree,garden,V(point.x,Height(point.x,point.y),point.y),Vector3.one*(offset.x<0?.68f:.9f),i*67+23);
                }
                for(int j=0;j<7;j++) {
                    float a=(j*37+i*23)*Mathf.Deg2Rad;
                    var point=centre+P(Mathf.Cos(a)*3.1f,Mathf.Sin(a)*2.4f);
                    if(!GardenClear(point,1.5f)) continue;
                    Scatter(shrub,garden,V(point.x,Height(point.x,point.y),point.y),Vector3.one*(.65f+(j%3)*.18f),j*49);
                }
                // Short, open-ended boundaries frame courtyards without enclosing entrances.
                for(int j=0;j<3;j++) {
                    var point=centre+P(-2+j*1.35f,4.1f+j*.15f);
                    if(!GardenClear(point,1.6f)) continue;
                    var wall=Box(garden,"muretto_cortile",V(point.x,Height(point.x,point.y)+.28f,point.y),V(1.4f,.56f,.42f),Stone);
                    wall.transform.localRotation=Quaternion.Euler(0,-6,0);
                }
            }
            report.Add("Campione ingresso-piazza: curva morbida, case Lipari/Ferro ruotate, tre cortili alberati aperti; prefab della vegetazione condivisi.");
        }
        static bool GardenClear(Vector2 point,float radius)
        {
            if(NearRoute(point,4.5f)) return false;
            return !sites.Any(s=>Mathf.Abs(point.x-s.x)<s.Width/2+radius+1 && Mathf.Abs(point.y-s.z)<s.Depth/2+radius+1);
        }
        static void VillageProps()
        {
            var p=Group("05_Arredo_urbano");
            // Fountain is an ordinary public water point, not a medieval well.
            Box(p,"vasca_fontana",V(6,.35f,5),V(3,.7f,1.6f),Stone);
            Box(p,"acqua_fontana",V(6,.72f,5),V(2.7f,.03f,1.3f),Mat("acqua","#79A6A7"),false);
            Box(p,"colonna_fontana",V(6,1.1f,5.6f),V(.65f,2.2f,.65f),Stone);
            Beam(p,"rubinetto",V(6,1.3f,5.2f),V(6,1.3f,4.95f),.07f,Dark);
            foreach(float z in new[]{-54f,-24f,8f,32f}) foreach(float x in new[]{-7f,8f}) {
                if(sites.Any(s=>Mathf.Abs(x-s.x)<s.Width/2+1 && Mathf.Abs(z-s.z)<s.Depth/2+1)) continue;
                float y=Height(x,z); Cone(p,"lampione",V(x,y,z),.09f,4,Dark,8,.06f);
                Box(p,"lanterna_stradale",V(x,y+4,z),V(.45f,.35f,.45f),Plaster,false);
            }
            var garden=Group("giardino_pubblico",p);garden.position=V(-18,0,-40);
            for(int i=0;i<3;i++) Art.Bench(garden,V(i*3,0,0));
            foreach(float x in new[]{-11f,11f}) {
                var tree=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Nature/castagno_1.prefab");
                Scatter(tree,p,V(x,0,15),Vector3.one*.75f,0);
            }
            foreach(float angle in new[]{35f,145f,215f,325f}) {
                var seat=Group(angle==35f?"panchina_teresa_piazza":"seduta_piazza",p);float a=angle*Mathf.Deg2Rad;
                seat.position=V(Mathf.Sin(a)*15.5f,.05f,Mathf.Cos(a)*15.5f);seat.rotation=Quaternion.Euler(0,angle,0);
                Art.Bench(seat,Vector3.zero,2.7f);
            }
            VillageDetails(p);
            // A modest platform and disused rail siding establish B-17's identity.
            Box(p,"marciapiede_stazione",V(50,.12f,-99),V(88,.24f,3.2f),Stone);
            var siding=Group("raccordo_dismesso",p);siding.position=V(50,0,-102.725f);siding.rotation=Quaternion.Euler(0,90,0);Transport.Track(siding,120,true);
            Transport.Stop(p,V(16,0,-90),90,"Chivasso1987","Chivasso");
            Box(p,"palina_corriera",V(16,1.5f,-95.5f),V(.09f,3,.09f),Dark);
            Sign(p,"CORRIERA",V(16,2.6f,-95.5f),2,Green);
            foreach(var id in new[]{"casa_ferro","casa_nino"}) {
                var owner=places[id]; float d=sites.Single(s=>s.id==id).d;
                var gardenOwner=Group("orto_e_legnaia",owner);gardenOwner.localPosition=V(-3,0,d/2+4);
                for(int j=0;j<4;j++) Box(gardenOwner,"filare",V(j*.75f,.05f,0),V(.4f,.12f,3),Mat("terra_orto","#6D7160"));
                for(int j=0;j<5;j++) Cone(gardenOwner,"tronco_tagliato",V(4,.2f,j*.35f),.22f,1.4f,Wood,7,.2f).transform.localRotation=Quaternion.Euler(0,0,90);
            }
        }
        static void VillageDetails(Transform parent)
        {
            var p=Group("dettagli_villaggio",parent);
            foreach(var s in sites) {
                var owner=places[s.id];
                var details=Group("oggetti_esterni",owner);
                // Keep the centre of every threshold and the rear workshop exit clear.
                float x=s.w/2-1.4f,z=-s.d/2-.8f;
                if(s.kind=="house" || s.kind=="workshop") {
                    for(int row=0;row<3;row++) for(int col=0;col<3-row;col++) {
                        var log=Cone(details,"legna_accatastata",V(-x+col*.31f+row*.15f,.18f+row*.29f,z),.15f,.8f,Wood,7,.14f);
                        log.transform.localRotation=Quaternion.Euler(90,0,0);
                    }
                } else {
                    Box(details,"cassetta_consegne",V(-x,.25f,z),V(.8f,.5f,.65f),Wood);
                    for(int i=0;i<3;i++) Box(details,"stecca_cassetta",V(-x,.1f+i*.16f,z-.34f),V(.86f,.09f,.035f),Stone,false);
                }
                Cone(details,"bidone_zincato",V(-x-1.05f,.02f,z),.28f,.7f,Mat("zinco","#879A98"),10,.28f);
                Cone(details,"coperchio_bidone",V(-x-1.05f,.74f,z),.32f,.06f,Dark,10,.32f);
                if(s.kind=="house") {
                    Box(details,"cassetta_postale",V(-1.65f,1.35f,-s.d/2-.22f),V(.38f,.45f,.18f),Green,false);
                    Box(details,"fessura_posta",V(-1.65f,1.45f,-s.d/2-.32f),V(.25f,.025f,.015f),Dark,false);
                }
            }
            var earth=Mat("chiazze_terra","#8A826F");var worn=Mat("rappezzi_asfalto","#626C69");
            var leaves=Mat("lettiera_ottobre","#A59960");
            for(int i=0;i<180;i++) {
                float x=Rand(-75,99),z=Rand(-96,145);
                if(new Vector2(x,z).magnitude<19 || sites.Any(s=>Mathf.Abs(x-s.x)<s.Width/2+1&&Mathf.Abs(z-s.z)<s.Depth/2+1)) continue;
                if(NearRoute(P(x,z),1.7f)) continue;
                var v=new List<Vector3>();var t=new List<int>();float radius=Rand(.4f,1.8f);
                for(int k=0;k<9;k++) {
                    float a=k*Mathf.PI/4;float r=k==0?0:radius*Rand(.75f,1.1f);
                    float px=x+Mathf.Sin(a)*r,pz=z+Mathf.Cos(a)*r;
                    if(terrainCollider.Raycast(new Ray(V(px,80,pz),Vector3.down),out var hit,100)) v.Add(hit.point+Vector3.up*.023f);
                }
                if(v.Count!=9) continue;
                for(int k=1;k<8;k++) t.AddRange(new[]{0,k,k+1});t.AddRange(new[]{0,8,1});
                Mesh(p,"chiazza_naturale",v.ToArray(),t.ToArray(),i%3==0?leaves:earth,false);
            }
            foreach(var route in routes.Where(r=>r.name=="via_principale"||r.name=="strada_stazione")) {
                for(int i=2;i<route.points.Length-2;i+=3) {
                    var q=route.points[i];if(q.magnitude<19) continue;
                    if(!terrainCollider.Raycast(new Ray(V(q.x,80,q.y),Vector3.down),out var hit,100)) continue;
                    Box(p,"rappezzo_stradale",hit.point+Vector3.up*(route.name=="via_principale"?.092f:.067f),V(.8f,.006f,1.15f),worn,false).transform.localRotation=Quaternion.Euler(0,i*13,0);
                }
            }
            // Independent platform furniture, clear of the road and rail running surface.
            foreach(float x in new[]{25f,45f,65f}) {
                var seat=Group("panchina_banchina",p);seat.position=V(x,.24f,-99);Art.Bench(seat,Vector3.zero,2.5f);
            }
            Sign(p,"SAN ROCCO DI VALDIERI",V(35,2.7f,-100.2f),8,Green);
            foreach(float x in new[]{31f,39f}) Box(p,"palo_cartello_stazione",V(x,1.35f,-100.2f),V(.09f,2.7f,.09f),Dark);
        }
        static void Quarry()
        {
            var p=Group("cava");p.position=V(0,Height(0,180),180);
            var a=p.gameObject.AddComponent<Luogo1987>();a.Id="cava";a.Nome="PIAN DELLA SOGLIA";a.Dimensioni=P(42,55);
            QuarryLandform.Create(p);
            // Two independent entries: sealed main gallery, passable side entrance.
            Box(p,"soffitto_galleria",V(0,4,5),V(7,.8f,23),Stone);
            foreach(float x in new[]{-3.4f,3.4f}) Box(p,"parete_galleria",V(x,2,2),V(.7f,4,17),Stone);
            Box(p,"muratura_1966",V(0,1.6f,-6),V(6.2f,3.2f,.5f),Mat("muro_1966","#A8A59A"));
            for(int i=0;i<4;i++) for(int j=0;j<9;j++) Box(p,"giunto_muratura",V(-2.9f+j*.7f+(i%2)*.2f,.4f+i*.75f,-6.27f),V(.62f,.025f,.025f),Stone,false);
            Box(p,"passaggio_laterale_pavimento",V(-10,.1f,5),V(5,.2f,32),Stone);
            Box(p,"passaggio_laterale_tetto",V(-10,4,7),V(5,.7f,28),Stone);
            Box(p,"parete_esterna_secondo_imbocco",V(-12.6f,2,7),V(.5f,4,28),Stone);
            Box(p,"volta_raccordo",V(-5.5f,4,3),V(4.6f,.8f,20),Stone);
            Box(p,"parete_interna_secondo_imbocco",V(-7.5f,2,-1.4f),V(.4f,4,11.2f),Stone);
            Box(p,"parete_interna_secondo_imbocco",V(-7.5f,2,11.2f),V(.4f,4,2.6f),Stone);
            for(int z=-3;z<=13;z+=5) {
                foreach(float x in new[]{-11.9f,-8.1f}) Beam(p,"puntello",V(x,0,z),V(x,3.6f,z),.25f,Wood);
                Beam(p,"trave",V(-12,3.6f,z),V(-8,3.6f,z),.3f,Wood);
            }
            var room=Group("Stanza_del_rito",p);room.localPosition=V(0,0,20);
            Box(room,"soffitto_stanza",V(0,5.4f,0),V(26,1,15),Stone);
            foreach(float x in new[]{-13f,13f}) Box(room,"parete_stanza_laterale",V(x,2.6f,0),V(.5f,5.2f,15),Stone);
            Box(room,"parete_stanza_fondo",V(0,2.6f,7.5f),V(26,5.2f,.5f),Stone);
            Box(room,"parete_stanza_fronte_destra",V(8.15f,2.6f,-7.5f),V(9.7f,5.2f,.5f),Stone);
            Box(room,"parete_stanza_fronte_intermedia",V(-5.6f,2.6f,-7.5f),V(4.4f,5.2f,.5f),Stone);
            Box(room,"parete_stanza_fronte_sinistra",V(-12.7f,2.6f,-7.5f),V(.6f,5.2f,.5f),Stone);
            // Recessed water surrounded by four walkable floor strips.
            foreach(float x in new[]{-8f,8f}) Box(room,"pavimento_laterale",V(x,.1f,0),V(9,.2f,15),Stone);
            foreach(float z in new[]{-5.5f,5.5f}) Box(room,"pavimento_bordo",V(0,.1f,z),V(7,.2f,4),Stone);
            Box(room,"cavita_acqua",V(0,.02f,0),V(6.8f,.03f,6.8f),Mat("acqua_cava","#3C686D"),false);
            Box(room,"fondo_cavita",V(0,-.25f,0),V(7,.2f,7),Stone);
            foreach(float x in new[]{-3.5f,3.5f}) Box(room,"bordo_roccioso",V(x,.25f,0),V(.25f,.4f,7),Stone);
            Lamp(room,V(-7,3.2f,-2)); Lamp(p,V(-10,3.4f,1));
            var side=Group("stanza_laterale_Giorgio",p);side.localPosition=V(-5.5f,0,8);
            Box(side,"pavimento_vano",V(0,.1f,0),V(4,.2f,7),Stone);
            foreach(float z in new[]{-3.5f,3.5f}) Box(side,"parete_vano",V(0,2,z),V(4,4,.3f),Stone);
            Box(side,"panca",V(0,.3f,0),V(2,.6f,.8f),Wood);Box(side,"coperta",V(0,.65f,0),V(1.7f,.1f,.7f),Green);Lamp(side,V(0,2,0));
            var memorial=Items1987.Instance("lapide",p,V(7,0,-11));
            var inspect=memorial.AddComponent<OggettoRaccoglibile>();inspect.Id="lapide";inspect.Fisso=true;
            var prefab=Save(p,"Buildings","cava_pian_della_soglia");
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);instance.transform.position=V(0,Height(0,180),180);
            report.Add("Cava: imbocco principale murato, ingresso laterale percorribile, Stanza, acqua, puntelli e memoriale.");
        }
        static void Cemetery()
        {
            var p=Group("cimitero");
            Box(p,"ghiaia",V(0,0,0),V(24,.1f,20),Mat("ghiaia","#B6B5A6"));
            foreach(float x in new[]{-12f,12f}) Box(p,"muro_cimitero",V(x,.8f,0),V(.5f,1.6f,20),Stone);
            Box(p,"muro_fondo",V(0,.8f,10),V(24,1.6f,.5f),Stone);
            foreach(float x in new[]{-7f,7f}) Box(p,"muro_ingresso",V(x,.8f,-10),V(10,1.6f,.5f),Stone);
            for(int i=0;i<12;i++) {
                float x=i%2==0?-5:5,z=-6+(i/2)*2.6f;
                Box(p,"tomba",V(x,.15f,z),V(1.7f,.3f,2),Plaster);
                Box(p,"lapide",V(x,.8f,z+.8f),V(1.2f,1.4f,.18f),Stone);
                if(i==0) Text(p,"ELENA VALLI\n1963 - 1966",V(x,.9f,z+.69f),.15f,Color.white,Quaternion.identity);
            }
            var prefab=Save(p,"Buildings","cimitero");var o=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            o.transform.position=V(72,Height(72,37),37);
        }
        public static GameObject Human(string id,int index) => People.Create(id,index);
        static void Characters()
        {
            People.Populate(places);
            report.Add("15 prefab distinti, 12 abitanti collocati; Wanda ed Elena riservate a Chivasso, Giorgio al giocatore.");
        }
        public static void Lighting()
        {
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.3f,.35f,.4f);
            RenderSettings.ambientEquatorColor=new Color(.22f,.25f,.26f);
            RenderSettings.ambientGroundColor=new Color(.12f,.13f,.12f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.69f,.78f,.78f);RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=190;RenderSettings.fogEndDistance=480;
            var sun=Group("sole_ottobre").gameObject.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.9f;sun.color=new Color(1,.93f,.82f);
            sun.transform.rotation=Quaternion.Euler(32,-40,0);sun.shadows=LightShadows.Soft;sun.shadowStrength=.8f;sun.renderMode=LightRenderMode.ForcePixel;
            RenderSettings.sun=sun;
            QualitySettings.shadowDistance=260;QualitySettings.pixelLightCount=8;QualitySettings.shadowResolution=ShadowResolution.VeryHigh;
        }
        public static void Player(Vector3 position,Quaternion rotation)
        {
            // Existing dialogue client, configured for a handmade scene.
            var boot=Group("gioco").gameObject.AddComponent<Bootstrap>();
            var so=new SerializedObject(boot);so.FindProperty("mappaAMano").boolValue=true;so.FindProperty("generaScenografia").boolValue=false;
            so.FindProperty("villaggioCompleto").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
            Group("pannello").gameObject.AddComponent<Pannello>();Group("menu").gameObject.AddComponent<Menu>();
            var player=Group("giocatore");player.SetPositionAndRotation(position,rotation);
            var controller=player.gameObject.AddComponent<Giocatore>();
            controller.FiguraPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Characters/giorgio.prefab");
            player.gameObject.AddComponent<AudioListener>();
            var adapter=Group("luoghi_narrativi").gameObject.AddComponent<LuoghiScena1987>();adapter.Gioco=boot;adapter.Giocatore=player;
        }
        static void MapVisitor()
        {
            Visitor(V(0,.2f,-23),Quaternion.identity);
        }
        public static void Visitor(Vector3 position,Quaternion rotation)
        {
            var player=Group("Visitatore_fase_1");player.SetPositionAndRotation(position,rotation);
            var cc=player.gameObject.AddComponent<CharacterController>();
            cc.height=1.8f;cc.radius=.28f;cc.center=V(0,.9f,0);cc.stepOffset=.35f;cc.slopeLimit=50;
            player.gameObject.AddComponent<FreeWalk>();
            var camera=Group("occhio",player).gameObject.AddComponent<Camera>();
            camera.transform.localPosition=V(0,1.65f,0);camera.fieldOfView=65;
            camera.farClipPlane=600;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.7f,.81f,.83f);camera.gameObject.AddComponent<AudioListener>();
        }
        static void ValidateBuildings()
        {
            foreach(var s in sites) {
                var p=places[s.id];
                // Test the full walking opening, excluding the intentional B-17 lock.
                if(s.id!="magazzino_b17") foreach(float z in new[]{-s.d/2-.6f,-s.d/2,-s.d/2+.6f}) {
                    var q=p.TransformPoint(V(0,0,z));
                    var hits=Physics.OverlapCapsule(q+Vector3.up*.45f,q+Vector3.up*1.5f,.27f);
                    if(hits.Length>0) throw new Exception("Porta ostruita: "+s.id+" / "+string.Join(",",hits.Select(c=>c.name)));
                }
                if(!Physics.Raycast(p.TransformPoint(V(0,2,-s.d/2+1)),Vector3.down,3)) throw new Exception("Pavimento assente: "+s.id);
                for(float z=-s.d/2+1;z<Mathf.Min(s.d/2-2,5);z+=.5f) {
                    var q=p.TransformPoint(V(0,0,z));
                    var hits=Physics.OverlapCapsule(q+Vector3.up*.5f,q+Vector3.up*1.5f,.28f);
                    if(hits.Length>0) throw new Exception("Passaggio interno ostruito: "+s.id+" / "+string.Join(",",hits.Select(c=>c.name)));
                }
            }
            report.Add("Soglie anteriori libere (B-17 chiuso intenzionalmente); pavimenti e passaggi centrali degli interni verificati.");
        }
        [MenuItem("Amnesia/San Rocco 1987/Verifica scritte e cava")]
        public static void CheckAppearance()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            foreach(var text in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                if(Vector3.Dot(text.transform.localRotation*Vector3.right,Vector3.right)<.99f)
                    throw new Exception("Scritta specchiata: "+text.text);
            var quarry=UnityEngine.Object.FindObjectsByType<Luogo1987>(FindObjectsSortMode.None).Single(l=>l.Id=="cava").gameObject;
            if(quarry.transform.Find("fronte_tagliato") || !quarry.transform.Find("versante_roccioso"))
                throw new Exception("La cava deve avere un versante irregolare, non un perimetro rettangolare");
            Physics.SyncTransforms();
            foreach(float z in new[]{-12f,-10f,-8f,-6f,-4f,0f,5f}) {
                var p=quarry.transform.TransformPoint(V(-10,.2f,z));
                if(Physics.CheckCapsule(p+Vector3.up*.5f,p+Vector3.up*1.5f,.28f)) throw new Exception("Ingresso laterale cava ostruito a "+z);
            }
            foreach(var point in new[]{V(-10,1.7f,1),V(-5.5f,1.7f,8),V(0,1.7f,20)})
                if(!Physics.Raycast(quarry.transform.TransformPoint(point),Vector3.up,8)) throw new Exception("Volta cava aperta: "+point);
            Debug.Log("SAN_ROCCO_APPEARANCE_OK");
        }
        [MenuItem("Amnesia/San Rocco 1987/Verifica ingressi")]
        public static void CheckEntrances()
        {
            foreach(var place in UnityEngine.Object.FindObjectsByType<Luogo1987>(FindObjectsSortMode.None)) {
                if(place.Id=="cava" || place.Id=="magazzino_b17") continue;
                int doors=place.GetComponentsInChildren<Transform>().Count(t=>t.name=="porta_aperta");
                if(doors!=(place.Id=="bottega"?2:1)) throw new Exception("Numero porte errato: "+place.Id+" / "+doors);
            }
            Debug.Log("SAN_ROCCO_ENTRANCES_OK");
        }
        static void ValidatePaths()
        {
            var ground=GameObject.Find("terreno").GetComponent<MeshCollider>();
            var errors=new HashSet<string>(); int samples=0;
            foreach(var route in routes) {
                for(int k=0;k<route.points.Length-1;k++) {
                    var a=route.points[k];var b=route.points[k+1];int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.6f));
                    for(int i=0;i<=steps;i++) {
                        var q=Vector2.Lerp(a,b,(float)i/steps);
                        if(route.name=="accesso_magazzino_b17" && Vector2.Distance(q,route.points[0])<1) continue;
                        if(!ground.Raycast(new Ray(V(q.x,80,q.y),Vector3.down),out var floor,100)) { errors.Add(route.name+": terreno assente"); continue; }
                        // Follow thresholds and low steps, as the visitor controller does.
                        Vector3 feet=floor.point;
                        if(Physics.Raycast(feet+Vector3.up*.34f,Vector3.down,out var step,.5f) && Vector3.Angle(step.normal,Vector3.up)<48) feet=step.point;
                        var hits=Physics.OverlapCapsule(feet+Vector3.up*.5f,feet+Vector3.up*1.5f,.28f);
                        foreach(var hit in hits) if(hit.name!="Visitatore_fase_1" && hit.bounds.max.y>feet.y+.35f) errors.Add(route.name+": "+hit.name+" @ "+q);
                        if(Vector3.Angle(floor.normal,Vector3.up)>48) errors.Add(route.name+": pendenza eccessiva @ "+q);
                        samples++;
                    }
                }
            }
            if(errors.Count>0) throw new Exception("Percorsi ostruiti:\n"+string.Join("\n",errors.Take(35)));
            report.Add("Percorsi: "+samples+" campioni con capsula di 56 cm, gradini fino a 35 cm, suolo presente e pendenza sotto 48 gradi. Controllo geometrico, non sostitutivo del playtest.");
        }
        static void ValidateAccessConnections()
        {
            foreach(var site in sites) {
                var route=routes.Single(r=>r.name==(site.id=="chiesa"?"sagrato":"accesso_"+site.id));
                var front=places[site.id].TransformPoint(V(0,0,-site.d/2));
                if(Vector2.Distance(route.points[0],P(front.x,front.z))>.02f) throw new Exception("Accesso scollegato dalla porta: "+site.id);
                var end=route.points[route.points.Length-1];
                bool connected=end.magnitude<=18;
                foreach(var other in routes) if(other.name!=route.name) for(int i=0;i<other.points.Length-1;i++) {
                    var a=other.points[i];var delta=other.points[i+1]-a;
                    float t=delta.sqrMagnitude==0?0:Mathf.Clamp01(Vector2.Dot(end-a,delta)/delta.sqrMagnitude);
                    if(Vector2.Distance(end,a+t*delta)<.9f) connected=true;
                }
                if(!connected) throw new Exception("Accesso isolato dalla rete viaria: "+site.id);
            }
            report.Add("20 ingressi collegati ai percorsi; chiesa con un solo ingresso, unica uscita posteriore nella bottega di Matteo.");
        }
        public static void Shot(string name,Vector3 pos,Vector3 target,bool ortho=false,float size=110)
        {
            var cam=Group("camera_verifica").gameObject.AddComponent<Camera>();cam.transform.position=pos;cam.transform.LookAt(target);
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.7f,.81f,.83f);cam.farClipPlane=700;cam.nearClipPlane=.05f;
            cam.fieldOfView=65;cam.orthographic=ortho;cam.orthographicSize=size;
            var rt=new RenderTexture(1600,1000,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();
            File.WriteAllBytes(Root+"/Previews/"+name+".png",image.EncodeToPNG());
            RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(cam.gameObject);
        }
        static void RenderPreviews()
        {
            Shot("00_campione_ingresso",V(39,35,-99),V(-7,1,-48));
            Shot("00_campione_pedonale",V(1,1.75f,-83),V(-2,2,-29));
            Shot("01_paese",V(155,145,-170),V(0,5,20),true,115);
            Shot("02_piazza",V(-1,1.8f,-22),V(0,4,28));
            Shot("00_pianta",V(0,230,55),V(0,0,55),true,175);
            Shot("03_panificio",places["panetteria"].TransformPoint(V(0,1.7f,-4)),places["panetteria"].TransformPoint(V(-1,1.5f,2)));
            Shot("04_bar",places["bar"].TransformPoint(V(0,1.7f,-4)),places["bar"].TransformPoint(V(2,1.3f,1)));
            Shot("05_chiesa",places["chiesa"].TransformPoint(V(0,1.7f,-11)),places["chiesa"].TransformPoint(V(0,3,9)));
            Shot("06_cava",V(30,51,143),V(0,30,189));
            Shot("07_bottega",places["bottega"].TransformPoint(V(0,1.7f,-5)),places["bottega"].TransformPoint(V(-3,1.4f,1)));
            Shot("08_insegna",places["panetteria"].TransformPoint(V(0,2.8f,-11)),places["panetteria"].TransformPoint(V(0,2.8f,-5)));
            Shot("09_cava_ingressi",V(-17,28,156),V(-5,30,179));
            Shot("10_stazione",places["stazione"].TransformPoint(V(0,2.5f,-13)),places["stazione"].TransformPoint(V(0,2.8f,-3)));
            Shot("11_sala_attesa",places["stazione"].TransformPoint(V(0,1.7f,-3.8f)),places["stazione"].TransformPoint(V(2,1.5f,2)));
            Shot("12_casa_lipari",places["casa_lipari"].TransformPoint(V(0,2,-4.6f)),places["casa_lipari"].TransformPoint(V(-1,1.2f,1)));
            Shot("13_piero_lidia",places["bar"].TransformPoint(V(-.4f,1.7f,-4)),places["bar"].TransformPoint(V(-2,1.2f,-.4f)));
            var teresa=GameObject.Find("panchina_teresa_piazza").transform;
            Shot("14_teresa",teresa.TransformPoint(V(-1.7f,1.5f,-3)),teresa.TransformPoint(V(0,1,0)));
            Shot("15_matteo",places["bottega"].TransformPoint(V(-2,1.6f,-2.4f)),places["bottega"].TransformPoint(V(-4,1.15f,1.55f)));
            Shot("17_depositi",V(48,14,-110),V(85,2,-77));
            Shot("18_corridoio_B",places["deposito_b"].TransformPoint(V(0,1.7f,-11)),places["deposito_b"].TransformPoint(V(.2f,1.8f,11)));
            var b17=places["deposito_b"].Find("unita_B_17");
            Shot("19_B17",b17.TransformPoint(V(0,2.5f,-5.3f)),b17.TransformPoint(V(0,2.45f,-2.5f)));
            Shot("20_interno_B17",b17.TransformPoint(V(0,1.7f,-1.8f)),b17.TransformPoint(V(0,1.1f,2)));
            var gate=GameObject.Find("sbarramento_ferroviario").transform;
            Shot("21_confine",gate.TransformPoint(V(4,2,-11)),gate.TransformPoint(V(0,1.4f,1)));
            var tunnel=GameObject.Find("galleria_uscita_valle").transform;var eye=tunnel.TransformPoint(V(0,0,-20));
            var border=GameObject.Find("orizzonte_continuo").GetComponent<MeshCollider>();
            if(border.Raycast(new Ray(V(eye.x,200,eye.z),Vector3.down),out var surface,250)) eye.y=surface.point.y+2;
            Shot("22_galleria_valle",eye,tunnel.TransformPoint(V(0,3,0)));
            var gallery=Group("galleria_verifica");gallery.position=V(400,0,0);
            for(int i=0;i<People.Ids.Length;i++) {
                string id=People.Ids[i];var position=V((i%5-2)*2.15f,(2-i/5)*2.3f,0);
                var figure=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Characters/"+id+".prefab"),gallery);
                figure.transform.localPosition=position;
                if(People.Seated(id)) Chair(gallery,position);
                Text(gallery,id.Replace('_',' ').ToUpperInvariant(),position+V(0,-.25f,-.1f),.18f,Color.black,Quaternion.identity);
            }
            var fill=Group("luce_galleria",gallery).gameObject.AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.6f;fill.transform.rotation=Quaternion.Euler(25,0,0);
            Shot("16_personaggi",V(400,3,-12),V(400,3,0),true,3.65f);
            UnityEngine.Object.DestroyImmediate(gallery.gameObject);
        }
        static void Chivasso()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var s=new Site{id="casa_wanda",label="VIA SANT'ORSOLA 14",kind="house",w=12,d=12,h=3.5f};
            var prefab=Buildings.Create(s,1);var home=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Box(null,"marciapiede",V(0,-.16f,-8),V(35,.3f,4),Stone);Box(null,"strada",V(0,-.22f,-15),V(50,.3f,10),Dark);
            for(int i=0;i<2;i++) {
                var npc=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Characters/"+(i==0?"wanda":"elena")+".prefab"),home.transform);
                npc.transform.localPosition=i==0?home.transform.Find("posto_tavolo").localPosition:V(2,.025f,-2);
            }
            Lighting();
            var player=Group("visitatore");player.position=V(0,.3f,-10);var cc=player.gameObject.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.28f;cc.center=V(0,.9f,0);cc.stepOffset=.3f;
            player.gameObject.AddComponent<FreeWalk>();var c=Group("occhio",player).gameObject.AddComponent<Camera>();c.transform.localPosition=V(0,1.6f,0);c.gameObject.AddComponent<AudioListener>();
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/Chivasso1987.unity");AssetDatabase.SaveAssets();
        }
    }
}
