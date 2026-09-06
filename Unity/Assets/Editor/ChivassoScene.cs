using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static AmnesiaUnity.Editor.SanRocco.Kit;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class ChivassoScene
    {
        public const string ScenePath="Assets/Scenes/Chivasso1987.unity";
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        public static void Create(bool playable=false)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var p=Group("quartiere_sant_orsola");
            var ground=Mat("prato_chivasso","#73836A");var asphalt=Mat("asfalto_chivasso","#737877");
            Box(p,"territorio_chivasso",V(0,-.32f,10),V(500,.6f,500),ground);
            Box(p,"via_sant_orsola",V(0,-.015f,-14),V(11,.09f,104),asphalt);
            Box(p,"piazzale_stazione",V(0,-.015f,22),V(88,.09f,18),asphalt);
            foreach(float x in new[]{-7f,7f}) Box(p,"marciapiede",V(x,.06f,-19),V(3,.18f,93),Stone);
            foreach(float z in new[]{12f,32f}) Box(p,"marciapiede_stazione",V(0,.06f,z),V(92,.18f,2.4f),Stone);
            for(int i=0;i<7;i++) Box(p,"attraversamento",V(-4.5f+i*1.5f,.04f,8),V(.65f,.012f,3),Plaster,false);
            for(int i=0;i<13;i++) Box(p,"mezzeria",V(0,.04f,-60+i*5),V(.12f,.012f,2),Plaster,false);
            Site[] sites={
                new Site{id="casa_wanda",label="",kind="house",x=19,z=-21,w=12,d=12,h=3.5f,yaw=90},
                new Site{id="cartoleria_chivasso",label="CARTOLERIA",kind="stationery",x=-19,z=-15,w=12,d=12,h=3.7f,yaw=-90},
                new Site{id="caffe_chivasso",label="CAFFE STAZIONE",kind="bar",x=-20,z=3,w=12,d=12,h=3.7f,yaw=-90},
                new Site{id="casa_cortile_chivasso",label="",kind="house",x=19,z=-43,w=12,d=12,h=3.5f,yaw=90},
                new Site{id="casa_portico_chivasso",label="",kind="house",x=-19,z=-39,w=12,d=12,h=3.5f,yaw=-90},
                new Site{id="stazione_chivasso",label="CHIVASSO",kind="station",x=0,z=42,w=24,d=14,h=4.5f,yaw=0}
            };
            Transform wandaHome=null;
            foreach(var s in sites) {
                var asset=Buildings.Create(s,Array.IndexOf(sites,s)+1);var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset,p);
                instance.transform.SetPositionAndRotation(V(s.x,.15f,s.z),Quaternion.Euler(0,s.yaw,0));
                if(s.id=="casa_wanda") wandaHome=instance.transform;
                if(s.kind!="station") Box(p,"accesso_"+s.id,V(Mathf.Sign(s.x)*10.5f,.09f,s.z),V(5,.16f,2.4f),Stone);
                if(s.kind!="station") {
                    var garden=Group("cortile_"+s.id,p);garden.position=V(s.x,0,s.z);
                    Box(garden,"aia",V(0,.025f,0),V(17,.08f,16),Mat("ghiaia_cortili","#92958A"));
                    foreach(float z in new[]{-7.7f,7.7f}) Box(garden,"muretto",V(3, .36f,z),V(10,.72f,.22f),Stone);
                    Cone(garden,"vaso",V(-7,.07f,5),.5f,.65f,Mat("terracotta_vasi","#A07D67"),8,.58f);
                    Art.Facet(garden,"arbusto",V(-7,1,5),V(.8f,.6f,.8f),Green,2);
                }
            }
            var street=Group("targa_via",wandaHome);street.localPosition=V(3.5f,2.7f,-6.2f);
            Box(street,"pietra_targa",Vector3.zero,V(3.6f,.52f,.06f),Plaster,false);
            var ink=Group("lettere",street);ink.localPosition=Vector3.back*.04f;Lettering.Paint(ink,"VIA SANT ORSOLA",3.3f,.23f,Dark);
            var number=Group("civico_14",wandaHome);number.localPosition=V(1.55f,1.9f,-6.2f);
            Box(number,"smalto",Vector3.zero,V(.38f,.38f,.05f),Plaster,false);
            var digits=Group("numero",number);digits.localPosition=Vector3.back*.032f;Lettering.Paint(digits,"14",.3f,.24f,Dark);
            for(int i=0;i<2;i++) {
                string id=i==0?"wanda":"elena";var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Characters/"+id+".prefab"),wandaHome);
                actor.transform.localPosition=i==0?wandaHome.Find("posto_tavolo").localPosition:V(2.4f,.025f,-1.8f);
                if(i==1) actor.transform.localRotation=Quaternion.Euler(0,90,0);
            }
            // A rear railway platform and terminal tracks, clear of the public forecourt.
            var rail=Group("binario_tronco_chivasso",p);rail.position=V(0,0,58);rail.rotation=Quaternion.Euler(0,90,0);Transport.Track(rail,150,false);
            Box(p,"banchina_ferroviaria",V(0,.2f,54),V(138,.4f,3.5f),Stone);
            var train=(GameObject)PrefabUtility.InstantiatePrefab(Transport.Train(),p);train.transform.SetPositionAndRotation(V(-17,.2f,58),Quaternion.Euler(0,90,0));
            foreach(float x in new[]{-26f,26f}) {
                Box(p,"accesso_banchina",V(x,.075f,46),V(3,.15f,18),Stone);
                var shelter=Group("pensilina_banchina",p);shelter.position=V(x,0,54);
                foreach(float z in new[]{-1f,1f}) Box(shelter,"pilastro",V(0,1.6f,z),V(.14f,3.2f,.14f),Dark);
                Box(shelter,"copertura",V(0,3.3f,0),V(14,.16f,3.6f),Green);
                Art.Bench(shelter,V(2,.4f,0),3);
            }
            Transport.Stop(p,V(-18,0,24),90,"SanRocco1987","San Rocco");
            var stop=Group("fermata_corriera",p);stop.position=V(-20,0,18);
            Box(stop,"palina",V(0,1.4f,0),V(.08f,2.8f,.08f),Dark);
            Sign(stop,"SAN ROCCO",V(0,2.4f,-.05f),2.4f,Green);
            Art.Bench(p,V(-26,.15f,12),3);
            for(int i=0;i<8;i++) {
                float x=i%2==0?-7.7f:7.7f,z=-55+i/2*18;
                Box(p,"lampione",V(x,2,z),V(.1f,4,.1f),Dark);
                Box(p,"lanterna",V(x,4.15f,z),V(.35f,.4f,.35f),Plaster,false);
            }
            var tree=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Nature/castagno_0.prefab");
            for(int i=0;i<32;i++) {
                if(-63+i/2*8>48 && -63+i/2*8<68) continue;
                var plant=(GameObject)PrefabUtility.InstantiatePrefab(tree,p);plant.transform.position=V(i%2==0?-38:38,0,-63+i/2*8);
            }
            // Courtyard walls conceal the ends of side streets; distant blocks extend the town.
            var background=new GameObject[2];
            for(int i=0;i<2;i++) {
                var block=Group("palazzina_chivasso_"+i);
                Box(block,"facciata",V(0,4,0),V(15,8,16),i==0?Plaster:Mat("intonaco_citta","#AFB7AC"));
                RoofGable(block,15,16,8,Plaster);
                foreach(float side in new[]{-1f,1f}) for(int floor=0;floor<2;floor++) for(int j=0;j<3;j++)
                    Box(block,"finestra",V(-4+j*4,2+floor*3,side*8.04f),V(1.5f,1.7f,.08f),Glass,false);
                background[i]=Save(block,"Buildings",block.name);
            }
            foreach(float side in new[]{-1f,1f}) {
                Box(p,"muro_cortili",V(side*80,1.2f,0),V(.35f,2.4f,158),Stone);
                for(int i=0;i<5;i++) {
                    var block=(GameObject)PrefabUtility.InstantiatePrefab(background[i%2],p);block.transform.position=V(side*(62+i%2*8),0,-65+i*23);
                }
            }
            Box(p,"chiusura_cortile",V(0,1.4f,-79),V(160,2.8f,.4f),Stone);
            Box(p,"recinto_ferroviario",V(0,1.4f,79),V(160,2.8f,.4f),Stone);
            var end=Group("piazzetta_fondo_via",p);end.position=V(0,0,-65);
            Cone(end,"slargo",V(0,.015f,0),9,.07f,asphalt,24,9);
            foreach(float x in new[]{-6f,6f}) { Art.Bench(end,V(x,.09f,-3),2.5f); }
            Build.Lighting();
            if(playable) Build.Player(V(-14,.3f,18),Quaternion.Euler(0,180,0));
            else Build.Visitor(V(-14,.3f,18),Quaternion.Euler(0,180,0));
            Physics.SyncTransforms();Check();
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            Build.Shot("23_chivasso",V(75,70,-88),V(0,0,5),true,64);
            Build.Shot("24_via_sant_orsola",V(0,1.8f,-7),V(15,2,-21));
            Build.Shot("25_casa_wanda",wandaHome.TransformPoint(V(0,1.7f,-4.8f)),wandaHome.TransformPoint(V(-1,1.3f,-.8f)));
            Build.Shot("26_treno_chivasso",V(-38,3,51),V(0,2.3f,58));
            if(playable) {
                Build.Shot("27_pianta_chivasso",V(0,200,5),V(0,0,5),true,85);
                StylizedMap.Assign();
                EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            }
        }
        public static void Check()
        {
            var actors=UnityEngine.Object.FindObjectsByType<Personaggio>(FindObjectsSortMode.None);
            if(actors.Length!=2 || !actors.Any(a=>a.Id=="wanda") || !actors.Any(a=>a.Id=="elena")) throw new Exception("Chivasso deve contenere Wanda ed Elena");
            foreach(var actor in actors) {
                var q=actor.transform.TransformPoint(V(0,actor.Id=="wanda"?1.08f:1.25f,0));
                if(Physics.OverlapSphere(q,.13f).Any(c=>!c.transform.IsChildOf(actor.transform))) throw new Exception("Arredo sul personaggio: "+actor.Id);
            }
            foreach(var place in UnityEngine.Object.FindObjectsByType<Luogo1987>(FindObjectsSortMode.None)) {
                for(float z=-place.Dimensioni.y/2-.5f;z<0;z+=.4f) {
                    var q=place.transform.TransformPoint(V(0,0,z));
                    if(Physics.CheckCapsule(q+Vector3.up*.5f,q+Vector3.up*1.5f,.28f)) throw new Exception("Ingresso Chivasso ostruito: "+place.Id);
                }
            }
            var trips=UnityEngine.Object.FindObjectsByType<Viaggio1987>(FindObjectsSortMode.None);
            if(trips.Length!=1 || trips[0].DestinazioneScena!="SanRocco1987") throw new Exception("Ritorno a San Rocco assente");
            foreach(var trip in trips) foreach(var point in new[]{trip.PuntoArrivo,trip.PuntoSalita})
                if(Physics.CheckCapsule(point.position+Vector3.up*.5f,point.position+Vector3.up*1.5f,.28f)) throw new Exception("Fermata ostruita: "+point.name);
            Debug.Log("CHIVASSO_OK: sei edifici, due identita, accessi e corriera di ritorno verificati");
        }
    }
}
