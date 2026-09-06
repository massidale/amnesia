using System;
using System.Linq;
using UnityEngine;
using static AmnesiaUnity.Editor.SanRocco.Kit;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class Depots
    {
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        public static GameObject Create(Site s)
        {
            var p=Group(s.id);bool buildingB=s.id=="deposito_b";string letter=buildingB?"B":"A";
            float w=s.w,d=s.d,h=s.h;
            var identity=p.gameObject.AddComponent<Luogo1987>();identity.Id=s.id;identity.Nome="Deposito "+letter;identity.Dimensioni=new Vector2(w,d);
            var plaster=Mat("intonaco_depositi","#979B92");
            var metal=Mat("lamiera_depositi","#65777A");
            Box(p,"fondazione",V(0,-.2f,0),V(w+.4f,.4f,d+.4f),Stone);
            Box(p,"pavimento",V(0,-.025f,0),V(w,.1f,d),Mat("cemento_depositi","#999E94"));
            foreach(float sign in new[]{-1f,1f}) {
                Box(p,"parete_esterna",V(sign*w/2,h/2,0),V(.3f,h,d),plaster);
                Box(p,"facciata_ingresso",V(sign*(w+3)/4,h/2,-d/2),V((w-3)/2,h,.3f),plaster);
                Box(p,"stipite",V(sign*1.5f,1.45f,-d/2),V(.16f,2.9f,.4f),Stone);
            }
            Box(p,"parete_posteriore",V(0,h/2,d/2),V(w,h,.3f),plaster);
            Box(p,"architrave",V(0,(h+2.9f)/2,-d/2),V(3,h-2.9f,.3f),plaster);
            Box(p,"porta_aperta",V(-1.38f,1.4f,-d/2-.9f),V(.13f,2.8f,1.8f),metal);
            Box(p,"soglia",V(0,.02f,-d/2),V(3,.07f,.6f),Stone);
            Box(p,"soffitto",V(0,h-.06f,0),V(w,.12f,d),plaster);
            RoofGable(p,w,d,h,plaster);
            var designation=Group("lettera_fabbricato",p);designation.localPosition=V(2.15f,2,-d/2-.19f);
            Lettering.Paint(designation,letter,1,.55f,Mat("vernice_depositi","#D3C9A2"));
            float corridor=3.3f,cellDepth=(w-.4f-corridor)/2,stride=(d-4)/5;
            foreach(float sign in new[]{-1f,1f}) for(int row=0;row<=5;row++)
                Box(p,"tramezzo_unita",V(sign*(corridor/2+cellDepth/2),h/2,-d/2+2+row*stride),V(cellDepth,h,.12f),plaster);
            for(int row=0;row<5;row++) foreach(int side in new[]{-1,1}) {
                int index=row*2+(side>0?1:0),number=(buildingB?8:1)+index;
                string code=letter+"-"+number.ToString("00");bool locked=code=="B-17";
                var cell=Group("unita_"+letter+"_"+number.ToString("00"),p);
                cell.localPosition=V(side*(corridor/2+cellDepth/2),0,-d/2+2+(row+.5f)*stride);
                cell.localRotation=Quaternion.Euler(0,side*90,0);
                float front=-cellDepth/2;
                if(locked) { var a=cell.gameObject.AddComponent<Luogo1987>();a.Id="magazzino_b17";a.Nome="B-17";a.Dimensioni=new Vector2(stride,cellDepth); }
                foreach(float sign in new[]{-1f,1f}) {
                    Box(cell,"parete_corridoio",V(sign*(stride+1.7f)/4,h/2,front),V((stride-1.7f)/2,h,.12f),plaster);
                    Box(cell,"guida_saracinesca",V(sign*.9f,1.35f,front-.07f),V(.09f,2.7f,.13f),Dark);
                }
                Box(cell,"architrave_unita",V(0,(h+2.7f)/2,front),V(1.7f,h-2.7f,.12f),plaster);
                bool open=!locked && index%3==0;
                var shutter=Box(cell,locked?"saracinesca_B17":"saracinesca",V(0,open?3.1f:1.35f,front),V(1.7f,open?.65f:2.7f,.09f),metal);
                if(locked) shutter.AddComponent<PortaMarker>().Id="magazzino_b17";
                // Lamella details are children of the moving leaf, never of the building.
                for(int j=0;j<(open?4:15);j++) Box(shutter.transform,"nervatura",V(0,-.46f+j/(open?4f:15f),-.6f),V(.98f,.014f,.25f),Stone,false);
                var plate=Group("targhetta_"+code,cell);plate.localPosition=V(0,3.6f,front-.09f);
                Box(plate,"ottone",Vector3.zero,V(1.1f,.34f,.04f),Mat("ottone_targhette","#A5996D"),false);
                var writing=Group("codice",plate);writing.localPosition=V(0,0,-.026f);Lettering.Paint(writing,code,.92f,.22f,Dark);
                if(locked) {
                    Box(shutter.transform,"serratura",V(.33f,-.08f,-.7f),V(.09f,.055f,.3f),Dark,false);
                    Lamp(cell,V(0,h-.45f,0));
                    Shelf(cell,V(-1.05f,0,cellDepth/2-.41f),1.8f);Shelf(cell,V(1.05f,0,cellDepth/2-.41f),1.8f);
                    Items1987.Populate(cell,cellDepth);
                } else {
                    Box(cell,"cassa_deposito",V(-1,.4f,cellDepth/2-1),V(1.1f,.8f,.85f),Wood);
                    if(index%2==0) Box(cell,"cassa_sovrapposta",V(-1,.98f,cellDepth/2-1),V(.85f,.35f,.7f),Wood);
                }
            }
            foreach(float z in new[]{-9f,0f,9f}) Lamp(p,V(0,h-.45f,z));
            return Save(p,"Buildings",s.id);
        }
        public static void Check()
        {
            foreach(string id in new[]{"deposito_a","deposito_b"}) {
                var root=GameObject.Find(id).transform;
                if(root.Cast<Transform>().Count(t=>t.name.StartsWith("unita_"))!=10) throw new Exception("Attese dieci unita: "+id);
                var anchor=root.GetComponent<Luogo1987>();
                for(float z=-anchor.Dimensioni.y/2-.4f;z<anchor.Dimensioni.y/2-.5f;z+=.4f) {
                    var q=root.TransformPoint(V(0,0,z));
                    if(Physics.CheckCapsule(q+Vector3.up*.5f,q+Vector3.up*1.5f,.28f)) throw new Exception("Corridoio deposito ostruito: "+id+" / "+z);
                }
                foreach(Transform cell in root) if(cell.name.StartsWith("unita_")) {
                    var shutter=cell.Find(cell.name=="unita_B_17"?"saracinesca_B17":"saracinesca").GetComponent<Collider>();
                    bool enabled=shutter.enabled;shutter.enabled=false;
                    try {
                        float depth=(anchor.Dimensioni.x-.4f-3.3f)/2;
                        foreach(float z in new[]{-depth/2-.4f,-depth/2,-depth/2+.5f,0,depth/2-1}) {
                            var q=cell.TransformPoint(V(0,0,z));
                            if(Physics.CheckCapsule(q+Vector3.up*.5f,q+Vector3.up*1.5f,.28f)) throw new Exception("Locale ostruito a serranda aperta: "+cell.name);
                        }
                    } finally { shutter.enabled=enabled; }
                }
            }
            var gates=UnityEngine.Object.FindObjectsByType<PortaMarker>(FindObjectsSortMode.None).Where(p=>p.Id=="magazzino_b17").ToArray();
            if(gates.Length!=1 || gates[0].transform.parent.name!="unita_B_17") throw new Exception("B-17 deve essere una sola serranda interna");
            Debug.Log("SAN_ROCCO_DEPOTS_OK: 20 unita, corridoi liberi, serratura B-17 interna");
        }
    }
}
