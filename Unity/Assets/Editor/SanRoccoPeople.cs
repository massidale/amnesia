using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using static AmnesiaUnity.Editor.SanRocco.Kit;

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
            float hip=seated?.58f:.87f,shoulder=hip+.48f,head=shoulder+.24f;
            Cone(visual,"busto",V(0,hip-.08f,0),breadth,.58f,cloth,8,breadth*.92f);
            Box(visual,"cintura",V(0,hip-.015f,-.01f),V(breadth*1.9f,.07f,.32f),trousers,false);
            foreach(float sign in new[]{-1f,1f}) {
                var pelvis=V(sign*.13f,hip-.04f,0);var knee=V(sign*.13f,seated?.51f:.46f,seated?-.36f:.025f);
                var ankle=V(sign*.13f,.14f,seated?-.36f:0);
                Beam(visual,"coscia",pelvis,knee,.19f,trousers);Beam(visual,"gamba",knee,ankle,.16f,trousers);
                Box(visual,"scarpa",V(sign*.13f,.09f,seated?-.43f:-.075f),V(.20f,.16f,.34f),Dark,false);
                var elbow=V(sign*(breadth+.08f),hip+.19f,seated?-.12f:-.02f);
                var hand=V(sign*.21f,hip+(seated?.17f:.03f),seated?-.35f:apron?-.25f:-.08f);
                Beam(visual,"manica",V(sign*breadth,shoulder-.06f,0),elbow,.15f,cloth);
                Beam(visual,"avambraccio",elbow,hand,.12f,cloth);
                Art.Facet(visual,"mano",hand,V(.07f,.09f,.065f),skin,i);
                Art.Facet(visual,"orecchio",V(sign*.162f,head,0),V(.043f,.063f,.055f),skin,i);
            }
            if(woman || id=="don_carlo") Cone(visual,"gonna",V(0,seated?.43f:.33f,0),breadth+.035f,seated?.21f:.5f,cloth,9,breadth*.85f);
            Cone(visual,"collo",V(0,shoulder-.01f,0),.077f,.16f,skin,8,.077f);
            Art.Facet(visual,"viso",V(0,head,0),V(.158f,.20f,.15f),skin,i);
            Art.Facet(visual,"capigliatura",V(0,head+.125f,.022f),V(.174f,.104f,.162f),hair,i);
            if(woman) {
                Art.Facet(visual,"capelli_nuca",V(0,head-.04f,.13f),V(.17f,.15f,.09f),hair,i);
                if(id=="rosa"||id=="teresa"||id=="lidia") Art.Facet(visual,"chignon",V(0,head+.025f,.23f),V(.10f,.09f,.10f),hair,i);
                if(id=="elena") foreach(float x in new[]{-.15f,.15f}) Art.Facet(visual,"ciocca",V(x,head-.1f,.04f),V(.045f,.15f,.09f),hair,i);
            }
            foreach(float x in new[]{-.058f,.058f}) {
                Box(visual,"occhio",V(x,head+.025f,-.142f),V(.027f,.021f,.018f),Dark,false);
                Box(visual,"sopracciglio",V(x,head+.067f,-.14f),V(.045f,.012f,.013f),hair,false);
            }
            Art.Facet(visual,"naso",V(0,head-.018f,-.157f),V(.036f,.049f,.055f),skin,i);
            Box(visual,"bocca",V(0,head-.092f,-.133f),V(.065f,.014f,.017f),Mat("labbra","#956C61"),false);
            if(id=="nino"||id=="piero") Box(visual,"baffi",V(0,head-.066f,-.153f),V(.11f,.031f,.027f),hair,false);
            if(id=="matteo") Art.Facet(visual,"barba_corta",V(0,head-.12f,-.035f),V(.128f,.064f,.125f),hair,i);
            if(new[]{"don_carlo","teresa","piero","wanda"}.Contains(id)) {
                foreach(float x in new[]{-.061f,.061f}) {
                    foreach(float y in new[]{-.025f,.025f}) Box(visual,"montatura_occhiali",V(x,head+.02f+y,-.163f),V(.095f,.012f,.016f),Dark,false);
                    foreach(float side in new[]{-.045f,.045f}) Box(visual,"montatura_occhiali",V(x+side,head+.02f,-.163f),V(.012f,.05f,.016f),Dark,false);
                }
                Box(visual,"ponte_occhiali",V(0,head+.025f,-.167f),V(.035f,.012f,.015f),Dark,false);
            }
            if(apron) {
                var fabric=Mat("grembiule_"+id,id=="matteo"?"#A68B65":id=="marisa"?"#7B9B89":"#D7D2BB");
                Box(visual,"grembiule",V(0,hip+.05f,-breadth*.9f),V(breadth*1.5f,.65f,.045f),fabric,false);
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
            // Pose geometry is non-solid: the actor owns a single interaction collider.
            foreach(var collider in visual.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            float scale=Heights[i]/1.80f;visual.localScale=new Vector3(1,seated?1:scale,1);
            var body=p.gameObject.AddComponent<CapsuleCollider>();body.radius=.25f;body.height=seated?1.42f:Heights[i];body.center=V(0,body.height/2+.025f,seated?-.1f:0);
            p.gameObject.AddComponent<Personaggio>().Id=id=="giorgio"?"player":id;
            return Save(p,"Characters",id);
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
