using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using static AmnesiaUnity.Editor.SanRocco.Kit;
using static AmnesiaUnity.Editor.SanRocco.PeopleGeometry;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class People
    {
        public static readonly string[] Ids={"rosa","matteo","anna","laura","don_carlo","nino","teresa","piero","marisa","beppe","lidia","gino","wanda","elena","giorgio"};
        static readonly string[] Clothes={"#7D8698","#697765","#48434B","#536F78","#343A3D","#64744C","#88758B","#7A7162","#908D9C","#DDD9CC","#966F72","#587488","#7C7E88","#9C8174","#597A77"};
        static readonly string[] Hair={"#655145","#51483D","#8A8781","#473F38","#AEADA1","#7C7665","#B9B5AD","#B0ADA3","#69534C","#9A9487","#4C3F37","#584F43","#C3BDB2","#594638","#53433A"};
        static readonly float[] Heights={1.65f,1.79f,1.61f,1.70f,1.76f,1.80f,1.55f,1.73f,1.64f,1.75f,1.68f,1.82f,1.58f,1.68f,1.79f};
        public static bool Seated(string id)=>new[]{"rosa","laura","teresa","piero","wanda"}.Contains(id);
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);

        public static GameObject Create(string id,int index)
        {
            int i=Array.IndexOf(Ids,id);if(i<0) throw new Exception("Personaggio sconosciuto: "+id);
            bool seated=Seated(id),woman=new[]{"rosa","anna","laura","teresa","marisa","lidia","wanda","elena"}.Contains(id);
            bool apron=new[]{"matteo","marisa","beppe","lidia"}.Contains(id);
            var p=Group(id);var visual=Group(seated?"posa_seduta":"posa_in_piedi",p);
            var cloth=Mat("vestito_"+id,Clothes[i]);var hair=Mat("capelli_"+id,Hair[i]);
            var skin=Mat("incarnato_"+id,i%3==0?"#D0A58D":i%3==1?"#BC947B":"#D9B29A");
            var trousers=Mat("pantaloni_"+id,i%2==0?"#505B60":"#625F55");
            float breadth=id=="beppe"?.29f:id=="nino"||id=="gino"?.27f:woman?.235f:.25f;
            float hip=seated?(id=="teresa"?.67f:.65f):.87f,shoulder=hip+.48f,head=shoulder+.24f;
            Torso(visual,V(0,hip-.11f,0),V(breadth,.61f,.18f),cloth);
            Soft(visual,"bacino",V(0,hip-.065f,0),V(breadth*.84f,seated?.10f:.145f,.165f),trousers);
            Soft(visual,"cintura",V(0,hip+.015f,0),V(breadth*.82f,.035f,.174f),trousers);
            foreach(float sign in new[]{-1f,1f}) {
                string side=sign<0?"sx":"dx";
                var pelvis=V(sign*.115f,hip-(seated?.05f:.12f),0);var knee=V(sign*.13f,seated?hip-.08f:.46f,seated?-.36f:.025f);
                var ankle=V(sign*.13f,.14f,seated?-.36f:0);
                var leg=Joint(visual,"anca_"+side,pelvis,.094f,trousers);
                Limb(leg,"coscia",Vector3.zero,knee-pelvis,.101f,trousers);
                var shin=Joint(leg,"ginocchio_"+side,knee-pelvis,.085f,trousers);
                Limb(shin,"polpaccio",Vector3.zero,ankle-knee,.079f,trousers);
                Soft(shin,"scarpa",V(sign*.13f,.085f,seated?-.425f:-.065f)-knee,V(.096f,.078f,.17f),Dark);
                var elbow=V(sign*(breadth+.08f),hip+.19f,seated?-.12f:-.02f);
                var hand=V(sign*.21f,hip+(seated?.17f:.03f),seated?-.35f:apron?-.25f:-.08f);
                var shoulderPoint=V(sign*breadth*.83f,shoulder-.075f,0);
                var arm=Joint(visual,"spalla_"+side,shoulderPoint,.092f,cloth);
                Limb(arm,"manica",Vector3.zero,elbow-shoulderPoint,.086f,cloth);
                var forearm=Joint(arm,"gomito_"+side,elbow-shoulderPoint,.073f,cloth);
                var wrist=hand+(elbow-hand).normalized*.085f;
                Limb(forearm,"avambraccio",Vector3.zero,wrist-elbow,.060f,cloth);
                Soft(forearm,"mano",hand-elbow,V(.059f,.080f,.048f),skin);
                Soft(visual,"orecchio",V(sign*.155f,head,0),V(.033f,.054f,.04f),skin);
            }
            if(woman || id=="don_carlo") {
                if(seated) Soft(visual,"gonna",V(0,hip-.08f,-.15f),V(breadth+.015f,.09f,.31f),cloth);
                else Skirt(visual,V(0,.34f,0),V(breadth+.015f,.55f,.19f),cloth);
            }
            Soft(visual,"collo",V(0,shoulder+.05f,0),V(.075f,.13f,.073f),skin);
            Soft(visual,"viso",V(0,head,0),V(.158f,.20f,.15f),skin);
            Soft(visual,"capigliatura",V(0,head+.13f,.022f),V(.167f,.100f,.156f),hair);
            if(woman) {
                Soft(visual,"capelli_nuca",V(0,head-.04f,.13f),V(.16f,.15f,.085f),hair);
                if(id=="rosa"||id=="teresa"||id=="lidia") Soft(visual,"chignon",V(0,head+.025f,.23f),V(.085f,.08f,.09f),hair);
                if(id=="elena") foreach(float x in new[]{-.15f,.15f}) Soft(visual,"ciocca",V(x,head-.1f,.04f),V(.04f,.15f,.08f),hair);
            } else Soft(visual,"capelli_nuca",V(0,head+.025f,.108f),V(.151f,.145f,.068f),hair);
            foreach(float x in new[]{-.058f,.058f}) {
                Soft(visual,"occhio",V(x,head+.025f,-.140f),V(.015f,.011f,.009f),Dark);
                Soft(visual,"sopracciglio",V(x,head+.061f,-.134f),V(.026f,.008f,.010f),hair);
            }
            Soft(visual,"naso",V(0,head-.018f,-.149f),V(.030f,.045f,.044f),skin);
            Soft(visual,"bocca",V(0,head-.087f,-.133f),V(.033f,.008f,.008f),Mat("labbra","#956C61"));
            if(new[]{"nino","piero","beppe","matteo"}.Contains(id)) Soft(visual,"baffi",V(0,head-.064f,-.143f),V(.058f,.018f,.018f),hair);
            if(id=="matteo" || id=="nino" || id=="giorgio") {
                bool beard=id!="giorgio";
                Soft(visual,beard?"barba":"barba_accennata",V(0,head-.135f,-.034f),V(.122f,beard?.073f:.042f,.116f),hair);
                foreach(float sign in new[]{-1f,1f}) Soft(visual,"basetta",V(sign*.137f,head-.035f,.012f),V(.018f,.073f,.080f),hair);
            }
            if(new[]{"don_carlo","teresa","piero","wanda","anna","laura"}.Contains(id)) {
                foreach(float x in new[]{-.061f,.061f}) {
                    for(int j=0;j<12;j++) {
                        float a=j*Mathf.PI/6,b=(j+1)*Mathf.PI/6;
                        Limb(visual,"montatura_occhiali",V(x+Mathf.Cos(a)*.052f,head+.022f+Mathf.Sin(a)*.032f,-.161f),V(x+Mathf.Cos(b)*.052f,head+.022f+Mathf.Sin(b)*.032f,-.161f),.0045f,Dark);
                    }
                    Limb(visual,"asta_occhiali",V(Mathf.Sign(x)*.116f,head+.022f,-.159f),V(Mathf.Sign(x)*.156f,head+.022f,.02f),.004f,Dark);
                }
                Box(visual,"ponte_occhiali",V(0,head+.025f,-.167f),V(.035f,.012f,.015f),Dark,false);
            }
            if(apron) {
                var fabric=Mat("grembiule_"+id,id=="matteo"?"#A68B65":id=="marisa"?"#7B9B89":"#D7D2BB");
                Apron(visual,hip,breadth,fabric);
                Box(visual,"tasca_grembiule",V(0,hip-.02f,-breadth*.9f-.029f),V(.22f,.15f,.02f),cloth,false);
            } else if(woman && id!="elena") {
                Cone(visual,"sciarpa",V(0,shoulder-.01f,0),.14f,.12f,Mat("sciarpa_"+id,i%2==0?"#B5A28A":"#989D91"),8,.13f);
            }
            if(id=="beppe") Cone(visual,"berretto_fornaio",V(0,head+.21f,0),.19f,.13f,Plaster,9,.16f);
            if(id=="nino"||id=="piero") {
                Cone(visual,"berretto",V(0,head+.2f,0),.19f,.07f,cloth,9,.15f);
                Box(visual,"visiera",V(0,head+.20f,-.13f),V(.3f,.045f,.22f),cloth,false);
            }
            if(id=="don_carlo") Box(visual,"collarino",V(0,shoulder+.04f,-.095f),V(.085f,.055f,.04f),Plaster,false);
            if(id=="gino") foreach(float x in new[]{-.13f,.13f}) Box(visual,"bretella",V(x,hip+.24f,-.236f),V(.045f,.43f,.035f),Dark,false);
            if(id=="teresa") {
                Box(visual,"borsa_in_grembo",V(0,.76f,-.31f),V(.38f,.28f,.19f),Mat("borsa_teresa","#61505A"),false);
                Beam(visual,"manico_borsa",V(-.12f,.92f,-.31f),V(.12f,.92f,-.31f),.035f,Dark);
            }
            if(id=="piero") Box(visual,"giornale",V(0,.85f,-.48f),V(.46f,.025f,.32f),Plaster,false);
            Details(visual,id,head,shoulder,hip,cloth,hair,skin);
            // Pose geometry is non-solid: the actor owns a single interaction collider.
            foreach(var collider in visual.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            float scale=Heights[i]/1.80f;visual.localScale=new Vector3(1,seated?1:scale,1);
            var body=p.gameObject.AddComponent<CapsuleCollider>();body.radius=.25f;body.height=seated?1.42f:Heights[i];body.center=V(0,body.height/2+.025f,seated?-.1f:0);
            p.gameObject.AddComponent<Personaggio>().Id=id=="giorgio"?"player":id;
            if(id=="giorgio") {
                var walk=p.gameObject.AddComponent<PassoPersonaggio>();
                var nodes=visual.GetComponentsInChildren<Transform>();
                Transform Find(string name)=>nodes.Single(t=>t.name==name);
                walk.AncaSinistra=Find("anca_sx");walk.AncaDestra=Find("anca_dx");
                walk.GinocchioSinistro=Find("ginocchio_sx");walk.GinocchioDestro=Find("ginocchio_dx");
                walk.SpallaSinistra=Find("spalla_sx");walk.SpallaDestra=Find("spalla_dx");
                walk.GomitoSinistro=Find("gomito_sx");walk.GomitoDestro=Find("gomito_dx");
            }
            return Save(p,"Characters",id);
        }

        static void Details(Transform p,string id,float head,float shoulder,float hip,Material cloth,Material hair,Material skin)
        {
            var brass=Mat("ottone_personaggi","#AA9160");var pearl=Mat("perle_personaggi","#DDD4BA");
            if(new[]{"rosa","teresa","wanda","marisa","lidia"}.Contains(id)) foreach(float x in new[]{-.164f,.164f})
                Soft(p,"orecchino",V(x,head-.06f,-.013f),Vector3.one*(id=="lidia"?.024f:.014f),id=="lidia"?brass:pearl);
            if(id=="rosa"||id=="teresa") Soft(p,"spilla",V(-.12f,shoulder-.15f,-.17f),V(.028f,.024f,.012f),brass);
            if(id=="wanda") for(int j=0;j<9;j++) Soft(p,"collana_perle",V((j-4)*.022f,shoulder-.07f-Mathf.Sin(j*Mathf.PI/8)*.06f,-.17f),Vector3.one*.012f,pearl);
            if(id=="elena"||id=="anna") {
                Limb(p,"catenina",V(-.065f,shoulder+.01f,-.10f),V(0,shoulder-.15f,-.182f),.004f,brass);
                Limb(p,"catenina",V(.065f,shoulder+.01f,-.10f),V(0,shoulder-.15f,-.182f),.004f,brass);
                Soft(p,"ciondolo",V(0,shoulder-.17f,-.187f),V(.018f,.024f,.008f),id=="elena"?Mat("ciondolo_elena","#577F88"):brass);
            }
            if(id=="marisa") for(int j=0;j<5;j++) Soft(p,"ricciolo",V((j-2)*.058f,head+.12f,-.085f),V(.047f,.047f,.047f),hair);
            if(id=="laura"||id=="giorgio") Soft(p,"ciuffo_laterale",V(-.063f,head+.116f,-.089f),V(.083f,.049f,.070f),hair);
            if(id=="gino") foreach(float x in new[]{-.141f,.141f}) Soft(p,"basetta",V(x,head-.017f,0),V(.018f,.067f,.075f),hair);
            if(id=="beppe") foreach(float y in new[]{hip+.15f,hip+.3f}) Soft(p,"bottone_giacca",V(-.09f,y,-.196f),Vector3.one*.014f,cloth);
            if(id=="matteo") Limb(p,"matita_tasca",V(.07f,hip+.08f,-.231f),V(.065f,hip+.25f,-.231f),.009f,brass);
            if(id=="giorgio") {
                Limb(p,"cerniera",V(0,hip+.08f,-.184f),V(0,shoulder-.1f,-.17f),.007f,brass);
                foreach(float x in new[]{-.07f,.07f}) Soft(p,"colletto_giacca",V(x,shoulder-.035f,-.085f),V(.045f,.025f,.040f),cloth);
            }
        }

        public static void Populate(System.Collections.Generic.Dictionary<string,Transform> places)
        {
            var root=Group("03_Abitanti");
            foreach(var id in Ids) Create(id,Array.IndexOf(Ids,id));
            void Place(string id,Transform location,Vector3 pos,float yaw=0) {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Characters/"+id+".prefab");
                var person=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);
                person.transform.position=location?location.TransformPoint(pos):pos;
                person.transform.rotation=(location?location.rotation:Quaternion.identity)*Quaternion.Euler(0,yaw,0);
            }
            foreach(var id in new[]{"rosa","laura"}) {
                var home=places[id=="rosa"?"casa_lipari":"casa_valli"];
                Place(id,home.Find("posto_tavolo"),Vector3.zero);
            }
            Place("anna",places["casa_ferro"],V(1.7f,.025f,-2.3f),25);
            Place("matteo",places["bottega"],V(-4,.025f,1.55f));
            Place("don_carlo",places["canonica"],V(1.7f,.025f,-2.3f),-20);
            Place("piero",places["bar"],V(-3,.025f,-1.6f));
            Place("lidia",places["bar"],V(4.65f,.025f,-.8f),90);
            Place("beppe",places["panetteria"],V(3,.025f,2.8f));
            Place("marisa",places["negozio"],V(3,.025f,.1f));
            Place("teresa",GameObject.Find("panchina_teresa_piazza").transform,V(0,.025f,0));
            Place("gino",null,V(8,.07f,-2),45);
            float x=-29,z=99;
            if(!Physics.Raycast(V(x,80,z),Vector3.down,out var hit,100)) throw new Exception("Suolo assente per Nino");
            Place("nino",null,hit.point+Vector3.up*.025f,-65);
        }
    }
}
