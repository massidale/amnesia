using System.Collections.Generic;
using UnityEngine;
using static AmnesiaUnity.Editor.SanRocco.Kit;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class Art
    {
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        public static GameObject Facet(Transform p,string name,Vector3 pos,Vector3 radius,Material material,int seed=0)
        {
            const int sides=9,rings=5;
            var v=new List<Vector3>();var t=new List<int>();
            for(int row=0;row<=rings;row++) for(int col=0;col<sides;col++) {
                float phi=row*Mathf.PI/rings,a=col*2*Mathf.PI/sides;
                float r=1+Mathf.Sin(col*4.7f+row*2.1f+seed)*.12f;
                v.Add(Vector3.Scale(V(Mathf.Cos(a)*Mathf.Sin(phi)*r,Mathf.Cos(phi),Mathf.Sin(a)*Mathf.Sin(phi)*r),radius));
            }
            for(int row=0;row<rings;row++) for(int col=0;col<sides;col++) {
                int a=row*sides+col,b=row*sides+(col+1)%sides;
                if(row>0)t.AddRange(new[]{a,b,a+sides});
                if(row<rings-1)t.AddRange(new[]{b,b+sides,a+sides});
            }
            var o=Mesh(p,name,v.ToArray(),t.ToArray(),material,false);o.transform.localPosition=pos;return o;
        }
        public static void Bench(Transform p,Vector3 pos,float width=2)
        {
            var root=Group("panchina_completa",p);root.localPosition=pos;
            for(int i=0;i<3;i++) Box(root,"listello_seduta",V(0,.48f,-.2f+i*.2f),V(width,.09f,.17f),Wood);
            for(int i=0;i<3;i++) Box(root,"listello_schienale",V(0,.73f+i*.15f,.29f),V(width,.12f,.08f),Wood);
            foreach(float x in new[]{-width*.36f,width*.36f}) {
                Box(root,"piede_panchina",V(x,.23f,0),V(.14f,.46f,.65f),Dark);
                Box(root,"sostegno_schienale",V(x,.62f,.28f),V(.09f,.9f,.09f),Dark);
            }
        }
        public static void Building(Transform p,Site s,int index)
        {
            var trim=Mat("pietra_cornici","#A6A69A");
            float front=-s.d/2-.18f;
            foreach(float sign in new[]{-1f,1f}) {
                Box(p,"zoccolo_frontale",V(sign*(s.w/4+.65f),.24f,front),V(s.w/2-1.3f,.48f,.16f),Stone,false);
                Box(p,"zoccolo_laterale",V(sign*(s.w/2+.17f),.24f,0),V(.16f,.48f,s.d),Stone,false);
                for(int row=0;row<6;row++) Box(p,"cantonale",V(sign*(s.w/2-.2f),.35f+row*.48f,front),V(row%2==0?.8f:.45f,.4f,.15f),trim,false);
                Box(p,"cornicione",V(sign*(s.w/2+.08f),s.h-.08f,0),V(.28f,.22f,s.d+.25f),trim,false);
            }
            if(s.kind=="church") { ChurchFront(p,s); return; }
            if(s.kind!="warehouse") foreach(float sign in new[]{-1f,1f}) Window(p,V(sign*s.w*.3f,1.75f,front-.06f),index);
            Box(p,"cornice_sopraporta",V(0,2.7f,front-.1f),V(2.45f,.2f,.2f),trim,false);
            // Small domestic details stay clear of the central doorway.
            if(s.kind=="house" || s.kind=="bakery" || s.kind=="bar") {
                var pot=V(s.w*.4f,0,front-.45f);
                Cone(p,"vaso",pot,.28f,.45f,Mat("terracotta","#A46654"),8,.35f);
                Facet(p,"pianta_vaso",pot+V(0,.68f,0),V(.4f,.35f,.4f),Mat("verde_foglie","#506D48"),index);
            }
            if(s.kind=="bar" || s.kind=="bakery") {
                var awning=Box(p,"pensilina_bottega",V(0,2.85f,front-.75f),V(3.3f,.11f,1.3f),s.kind=="bar"?Red:Green,false);
                awning.transform.localRotation=Quaternion.Euler(-9,0,0);
                foreach(float x in new[]{-1.5f,1.5f}) Beam(p,"mensola_pensilina",V(x,2.3f,front),V(x,2.85f,front-1.2f),.07f,Dark);
            }
        }
        static void Window(Transform p,Vector3 pos,int index)
        {
            Box(p,"vano_finestra",pos,V(1.5f,1.45f,.12f),Dark,false);
            Box(p,"vetro_frontale",pos+V(0,0,-.07f),V(1.22f,1.22f,.035f),Glass,false);
            foreach(float x in new[]{-.69f,0,.69f}) Box(p,"telaio_finestra",pos+V(x,0,-.11f),V(.065f,1.4f,.05f),Plaster,false);
            foreach(float y in new[]{-.67f,0,.67f}) Box(p,"traverso_finestra",pos+V(0,y,-.11f),V(1.4f,.065f,.05f),Plaster,false);
            foreach(float x in new[]{-1.02f,1.02f}) {
                Box(p,"scuro_frontale",pos+V(x,0,-.04f),V(.5f,1.47f,.08f),index%2==0?Green:Mat("persiana_azzurra","#4F6979"),false);
                for(int i=0;i<6;i++) Box(p,"stecca_persiana",pos+V(x,-.54f+i*.21f,-.09f),V(.44f,.035f,.025f),Dark,false);
            }
            Box(p,"davanzale_frontale",pos+V(0,-.77f,-.09f),V(1.75f,.16f,.33f),Stone,false);
        }
        static void ChurchFront(Transform p,Site s)
        {
            float z=-s.d/2-.25f;
            var trim=Mat("pietra_cornici","#A6A69A");
            foreach(float x in new[]{-6.6f,6.6f}) {
                Box(p,"lesena",V(x,3.3f,z),V(.55f,6.6f,.35f),trim,false);
                Box(p,"capitello",V(x,6.5f,z),V(.9f,.24f,.5f),Plaster,false);
            }
            Box(p,"cornice_facciata",V(0,6.7f,z),V(s.w+.1f,.23f,.4f),trim,false);
            var rose=Group("rosone",p);rose.localPosition=V(0,4.85f,z-.04f);
            const int n=16;var vv=new List<Vector3>();var tt=new List<int>();
            for(int i=0;i<n;i++) {
                float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;int start=vv.Count;
                vv.AddRange(new[]{V(Mathf.Cos(a)*1.03f,Mathf.Sin(a)*1.03f,0),V(Mathf.Cos(b)*1.03f,Mathf.Sin(b)*1.03f,0),V(Mathf.Cos(a)*.8f,Mathf.Sin(a)*.8f,-.03f),V(Mathf.Cos(b)*.8f,Mathf.Sin(b)*.8f,-.03f)});
                tt.AddRange(new[]{start,start+2,start+1,start+1,start+2,start+3});
            }
            Mesh(rose,"anello_pietra",vv.ToArray(),tt.ToArray(),trim,false);
            var disk=Cone(rose,"vetro_rosone",V(0,0,.01f),.83f,.03f,Glass,16,.83f);disk.transform.localRotation=Quaternion.Euler(90,0,0);
            for(int i=0;i<8;i++) {
                float a=i*Mathf.PI/4;Beam(rose,"raggio_rosone",V(0,0,-.08f),V(Mathf.Cos(a)*.8f,Mathf.Sin(a)*.8f,-.08f),.045f,trim);
            }
            foreach(float x in new[]{-1.56f,1.56f}) Box(p,"cornice_portale",V(x,1.4f,z-.1f),V(.28f,2.8f,.35f),trim,false);
            Box(p,"architrave_portale",V(0,2.84f,z-.1f),V(3.45f,.28f,.35f),trim,false);
            var dedication=Group("dedica_san_rocco",p);dedication.localPosition=V(0,3.36f,z-.17f);
            Lettering.Paint(dedication,"SAN ROCCO",3,.25f,Dark);
            Box(p,"croce_facciata",V(0,s.h+2.6f,z),V(.12f,1.1f,.12f),Dark,false);
            Box(p,"croce_facciata_traversa",V(0,s.h+2.8f,z),V(.65f,.1f,.12f),Dark,false);
        }
        public static void Ridges(Transform p)
        {
            var root=Group("creste_continue",p);
            const int segments=32,rings=4;var v=new List<Vector3>();var t=new List<int>();
            for(int ring=0;ring<rings;ring++) for(int i=0;i<=segments;i++) {
                float a=Mathf.Lerp(-30,210,(float)i/segments)*Mathf.Deg2Rad;
                float scale=1+ring*.32f,px=Mathf.Cos(a)*150*scale,pz=55+Mathf.Sin(a)*185*scale;
                float h=ring==0?-.2f:ring==1?20+Mathf.Sin(i*1.7f)*7:ring==2?55+Mathf.Sin(i*.8f)*13:28;
                float fade=Mathf.Min(1,Mathf.Min(i,segments-i)/3f);
                v.Add(V(px,Build.Height(px,pz)+h*fade,pz));
            }
            for(int ring=0;ring<rings-1;ring++) for(int i=0;i<segments;i++) {
                int a=ring*(segments+1)+i,b=a+segments+1;t.AddRange(new[]{a,a+1,b,a+1,b+1,b});
            }
            Mesh(root,"rilievo_alpino",v.ToArray(),t.ToArray(),Mat("creste_pietra","#78847F"));
        }
        public static void Square(Transform p)
        {
            var v=new[]{new List<Vector3>(),new List<Vector3>(),new List<Vector3>()};
            var t=new[]{new List<int>(),new List<int>(),new List<int>()};
            for(int row=0;row<24;row++) {
              float inner=row*.75f+.025f,outer=(row+1)*.75f-.025f;
              int count=Mathf.Max(8,Mathf.RoundToInt(outer*2*Mathf.PI/1.05f));
              for(int col=0;col<count;col++) {
                float a=(col+row*.5f)*2*Mathf.PI/count+.002f,b=(col+1+row*.5f)*2*Mathf.PI/count-.002f;
                int m=(row*7+col*11)%3,n=v[m].Count;
                v[m].AddRange(new[]{V(Mathf.Sin(a)*inner,.065f,Mathf.Cos(a)*inner),V(Mathf.Sin(a)*outer,.065f,Mathf.Cos(a)*outer),V(Mathf.Sin(b)*inner,.065f,Mathf.Cos(b)*inner),V(Mathf.Sin(b)*outer,.065f,Mathf.Cos(b)*outer)});
                t[m].AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3});
              }
            }
            string[] colors={"#89908A","#93968C","#7D8884"};
            for(int i=0;i<3;i++) Mesh(p,"selciato_pietra_"+i,v[i].ToArray(),t[i].ToArray(),Mat("selciato_"+i,colors[i]),false);
        }
    }
}
