using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static AmnesiaUnity.Editor.SanRocco.Kit;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class Boundary
    {
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        public static void Create(Transform p)
        {
            var root=Group("creste_continue",p);var edge=new List<Vector2>();
            for(int x=-160;x<160;x+=2) edge.Add(new Vector2(x,-110));
            for(int z=-110;z<250;z+=2) edge.Add(new Vector2(160,z));
            for(int x=160;x>-160;x-=2) edge.Add(new Vector2(x,250));
            for(int z=250;z>-110;z-=2) edge.Add(new Vector2(-160,z));
            var v=new List<Vector3>();var t=new List<int>();float[] scales={1,1.14f,1.4f,1.85f,2.8f,2.9f,3.6f};
            for(int ring=0;ring<scales.Length;ring++) foreach(var e in edge) {
                float x=e.x*scales[ring],z=70+(e.y-70)*scales[ring];
                float height=Build.Height(e.x,e.y)-.07f;
                float variation=Mathf.PerlinNoise(x*.009f+17,z*.009f+11);
                float rise=ring==0?0:ring==1?3:ring==2?28:ring==3?68:40;
                height+=rise*(.45f+variation);
                // The road leaves through a shallow southern valley, not a sheer rim.
                if(z<-110) height*=Mathf.Lerp(.13f,1,Mathf.SmoothStep(0,1,Mathf.Abs(x+45)/110));
                if(ring>=5) {
                    var inner=v[4*edge.Count+v.Count%edge.Count];
                    height=inner.y+40+(ring==6?-15:0);
                }
                v.Add(V(x,height,z));
            }
            for(int r=0;r<scales.Length-1;r++) for(int i=0;i<edge.Count;i++) {
                int a=r*edge.Count+i,b=r*edge.Count+(i+1)%edge.Count,c=a+edge.Count,d=b+edge.Count;
                t.AddRange(new[]{a,b,c,b,d,c});
            }
            var ground=Mesh(root,"orizzonte_continuo",v.ToArray(),t.ToArray(),Mat("terreno_dipinto","#FFFFFF"));
            var mesh=ground.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var colors=new Color[vertices.Length];
            for(int i=0;i<vertices.Length;i++) {
                var q=vertices[i];float n=Mathf.PerlinNoise(q.x*.024f+12,q.z*.024f+12);
                var color=Color.Lerp(new Color(.34f,.42f,.29f),new Color(.55f,.60f,.42f),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.75f,n)));
                color=Color.Lerp(color,new Color(.52f,.54f,.5f),Mathf.Clamp01((q.z-143)/23)*(1-n));
                color=Color.Lerp(color,new Color(.46f,.51f,.49f),Mathf.Clamp01((q.y-25)/50));
                colors[i]=color*(.94f+.08f*Mathf.PerlinNoise(q.x*.23f+20,q.z*.23f+20));
            }
            // Share normals at coincident vertices; long skirt triangles must not form stripes.
            var normals=mesh.normals;var average=new Dictionary<Vector3,Vector3>();
            for(int i=0;i<vertices.Length;i++) average[vertices[i]]=average.TryGetValue(vertices[i],out var n)?n+normals[i]:normals[i];
            for(int i=0;i<vertices.Length;i++) normals[i]=average[vertices[i]].normalized;
            mesh.normals=normals;mesh.colors=colors;mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);
            var collider=ground.GetComponent<MeshCollider>();
            for(int i=0;i<edge.Count;i++) {
                var q=(v[4*edge.Count+i]+v[5*edge.Count+i])*.5f;
                if(!collider.Raycast(new Ray(V(q.x,300,q.z),Vector3.down),out var face,350) || Vector3.Angle(face.normal,Vector3.up)<55)
                    throw new System.Exception("Cresta esterna attraversabile: "+i);
            }
            Debug.Log("SAN_ROCCO_BOUNDARY_OK: giunzione continua e 680 campioni della cresta esterna oltre 55 gradi");
            var limits=Group("confine_perimetrale_invisibile",root);
            var walls=new List<BoxCollider>();
            void Wall(string side,Vector3 centre,Vector3 size) {
                var wall=Group("muro_invisibile_"+side,limits).gameObject.AddComponent<BoxCollider>();
                wall.center=centre;wall.size=size;walls.Add(wall);
            }
            // Below the lowest terrain and above every reachable ridge; corners overlap.
            Wall("nord",V(0,100,250),V(322,400,1));
            Wall("sud",V(0,100,-111),V(322,400,1));
            Wall("est",V(160,100,69.5f),V(1,400,363));
            Wall("ovest",V(-160,100,69.5f),V(1,400,363));
            Physics.SyncTransforms();int samples=0;
            foreach(var wall in walls) {
                bool horizontal=wall.size.x>wall.size.z;
                float span=horizontal?320:361;
                var outward=horizontal?V(0,0,Mathf.Sign(wall.center.z-69.5f)):V(Mathf.Sign(wall.center.x),0,0);
                for(float along=-span/2;along<=span/2;along+=.5f) foreach(float height in new[]{-1f,50f,150f}) {
                    var point=wall.center+(horizontal?V(along,0,0):V(0,0,along));point.y=height;
                    if(!wall.Raycast(new Ray(point-outward*2,outward),out var hit,4))
                        throw new System.Exception("Varco nel confine: "+wall.name+" / "+point);
                    samples++;
                }
                if(wall.isTrigger || wall.GetComponent<Renderer>()) throw new System.Exception("Confine non solido o visibile");
            }
            Debug.Log("PERIMETRO_OK: quattro muri invisibili continui, angoli sovrapposti, "+samples+" campioni");
            var core=GameObject.Find("terreno").GetComponent<MeshCollider>();
            float Surface(float x,float z) {
                var ray=new Ray(V(x,150,z),Vector3.down);
                if(core.Raycast(ray,out var hit,200) || collider.Raycast(ray,out hit,200)) return hit.point.y;
                throw new System.Exception("Confine senza suolo: "+x+", "+z);
            }
            var road=new List<Vector3>();var tri=new List<int>();
            const int steps=300,columns=10;
            for(int i=0;i<=steps;i++) {
                float u=i/(float)steps,x=-135*u*u,z=-108-120*u;
                var normal=new Vector2(120,-270*u).normalized;
                for(int j=0;j<=columns;j++) {
                    float side=-1+2*j/(float)columns;
                    float px=x+normal.x*side*2.5f,pz=z+normal.y*side*2.5f;
                    float shoulder=Mathf.InverseLerp(.6f,1,Mathf.Abs(side));
                    float elevation=Mathf.Lerp(.2f,.35f,Mathf.Clamp01(u/.04f));
                    road.Add(V(px,Mathf.Lerp(Surface(x,z)+elevation,Surface(px,pz)+.05f,shoulder),pz));
                }
                if(i>0) for(int j=0;j<columns;j++) {
                    int a=(i-1)*(columns+1)+j,b=a+columns+1;tri.AddRange(new[]{a,a+1,b,a+1,b+1,b});
                }
            }
            var ballast=Mat("massicciata_valle","#777975");
            var asphalt=Mesh(root,"strada_oltre_la_curva",road.ToArray(),tri.ToArray(),ballast);
            var roadMesh=asphalt.GetComponent<MeshFilter>().sharedMesh;var rv=roadMesh.vertices;var rn=roadMesh.normals;
            var smooth=new Dictionary<Vector3,Vector3>();
            for(int i=0;i<rv.Length;i++) smooth[rv[i]]=smooth.TryGetValue(rv[i],out var normal)?normal+rn[i]:rn[i];
            for(int i=0;i<rv.Length;i++) rn[i]=smooth[rv[i]].normalized;
            roadMesh.normals=rn;roadMesh.UploadMeshData(false);EditorUtility.SetDirty(roadMesh);
            var railway=Group("ferrovia_dismessa_valle",root);
            Vector3 TrackPoint(float u,float side) {
                var normal=new Vector2(120,-270*u).normalized;
                float cx=-135*u*u,cz=-108-120*u;
                return V(cx+normal.x*side,Surface(cx,cz)+.46f,cz+normal.y*side);
            }
            float sleeperDistance=2;
            for(int i=0;i<steps;i++) {
                float u=.04f+.96f*i/steps,next=.04f+.96f*(i+1)/steps;
                foreach(float side in new[]{-.725f,.725f}) {
                    var a=TrackPoint(u,side);var b=TrackPoint(next,side);
                    var rail=Box(railway,"rotaia_curva",(a+b)/2,V(.07f,.12f,Vector3.Distance(a,b)+.025f),Dark,false);
                    rail.transform.rotation=Quaternion.LookRotation(b-a);
                }
                var centre=TrackPoint(u,0);var ahead=TrackPoint(next,0);
                sleeperDistance+=Vector3.Distance(centre,ahead);
                if(sleeperDistance>=.85f) {
                    var sleeper=Box(railway,"traversina_valle",centre-Vector3.up*.08f,V(2.35f,.12f,.22f),Wood,false);
                    sleeper.transform.rotation=Quaternion.LookRotation(ahead-centre);sleeperDistance=0;
                }
            }
            var buffer=Group("respingente_inizio_valle",railway);buffer.position=TrackPoint(.04f,0);
            buffer.rotation=Quaternion.LookRotation(TrackPoint(.05f,0)-buffer.position);
            foreach(float side in new[]{-1f,1f}) Box(buffer,"supporto_respingente",V(side*.85f,.45f,0),V(.18f,.9f,.65f),Dark);
            Box(buffer,"traversa_respingente",V(0,.9f,0),V(2.4f,.22f,.2f),Red);
            // A disused railway portal clearly identifies the non-playable continuation.
            var tunnel=Group("galleria_uscita_valle",root);tunnel.position=V(-135,Surface(-135,-228),-228);
            tunnel.rotation=Quaternion.LookRotation(V(-270,0,-120));
            var rock=Mat("roccia_galleria","#66716C");
            foreach(float side in new[]{-1f,1f}) {
                Box(tunnel,"spalla_rocciosa",V(side*8,4,11),V(10,12,27),rock);
                Art.Facet(tunnel,"affioramento",V(side*18,3,9),V(12,13,19),rock,side<0?7:11);
            }
            Box(tunnel,"volta_rocciosa",V(0,8,12),V(26,6,29),rock);
            Art.Facet(tunnel,"calotta_rocciosa",V(0,12,15),V(23,6,25),rock,5);
            Box(tunnel,"fondo_galleria",V(0,2,24),V(26,7,.5f),Dark);
            Box(tunnel,"pavimento_galleria",V(0,.25f,12),V(6,.2f,25),ballast);
            foreach(float side in new[]{-.725f,.725f}) Box(tunnel,"rotaia_galleria",V(side,.46f,12),V(.07f,.12f,24),Dark,false);
            for(float z=.25f;z<24;z+=.85f) Box(tunnel,"traversina_galleria",V(0,.38f,z),V(2.35f,.12f,.22f),Wood,false);
            for(int j=0;j<9;j++) {
                float a=j*Mathf.PI/9,b=(j+1)*Mathf.PI/9;
                var block=new List<Vector3>();
                foreach(float z in new[]{-.15f,.4f}) foreach(float r in new[]{3f,3.65f}) foreach(float angle in new[]{a,b})
                    block.Add(V(Mathf.Cos(angle)*r,2+Mathf.Sin(angle)*r,z));
                Mesh(tunnel,"concio_portale",block.ToArray(),new[]{0,1,2,1,3,2,4,6,5,5,6,7,0,4,1,1,4,5,2,3,6,3,7,6},Stone);
            }
            foreach(float side in new[]{-1f,1f}) Box(tunnel,"piedritto",V(side*3.32f,1,-.05f),V(.65f,2,.6f),Stone);
            var gate=Group("sbarramento_ferroviario",railway);
            gate.position=TrackPoint(.025f,0)-Vector3.up*.46f;
            var direction=TrackPoint(.035f,0)-TrackPoint(.025f,0);direction.y=0;
            gate.rotation=Quaternion.LookRotation(direction);
            var white=Mat("smalto_sbarra","#DEDCD2");
            foreach(float side in new[]{-1f,1f}) Box(gate,"montante_sbarra",V(side*2.65f,.95f,0),V(.2f,1.9f,.24f),Dark);
            Box(gate,"sbarra_chiusa",V(0,1.1f,0),V(5.5f,.25f,.18f),white);
            for(int i=-4;i<=4;i++) Box(gate,"fascia_rossa",V(i*.6f,1.1f,-.101f),V(.28f,.25f,.025f),Red,false);
            var sign=Group("cartello_divieto_ferrovia",gate);sign.localPosition=V(0,2,0);
            Sign(sign,"FERROVIA - ACCESSO VIETATO",Vector3.zero,5,Dark);
            var limit=Group("confine_galleria_invisibile",gate).gameObject.AddComponent<BoxCollider>();
            limit.center=V(0,3,0);limit.size=V(26,10,.6f);
            Physics.SyncTransforms();
            foreach(float x in new[]{-2.8f,-1.4f,0,1.4f,2.8f}) {
                var ray=new Ray(gate.TransformPoint(V(x,1.5f,-2)),gate.forward);
                if(!limit.Raycast(ray,out var hit,4)) throw new System.Exception("Confine galleria attraversabile: "+x);
            }
            Debug.Log("CONFINE_FERROVIARIO_OK: sbarra, divieto e collider continuo verificati");
            var pine=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Nature/pino_0.prefab");
            if(pine) for(int i=0;i<60;i++) {
                float u=.06f+(i+.5f)/60*.88f;
                var offset=new Vector2(120,-270*u).normalized*(i%2==0?-1:1)*(9+i%7*2);
                float x=-135*u*u+offset.x,z=-108-120*u+offset.y;
                var tree=(GameObject)PrefabUtility.InstantiatePrefab(pine,root);tree.transform.position=V(x,Surface(x,z),z);tree.transform.localScale=Vector3.one*(1.1f+i%4*.18f);
            }
        }
    }
}
