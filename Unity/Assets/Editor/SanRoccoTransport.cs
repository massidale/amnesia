using UnityEngine;
using UnityEditor;
using static AmnesiaUnity.Editor.SanRocco.Kit;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class Transport
    {
        static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
        public static GameObject Coach()
        {
            var p=Group("corriera_1987");var body=Mat("corriera_avorio","#D1D6C9");var stripe=Mat("corriera_verde","#466E64");
            Box(p,"telaio",V(0,.67f,0),V(2.45f,.32f,8.2f),Dark);
            Box(p,"pavimento",V(0,.88f,0),V(2.45f,.1f,8.2f),Wood);
            Box(p,"tetto",V(0,3.05f,0),V(2.6f,.22f,8.4f),body);
            foreach(float side in new[]{-1f,1f}) {
                Box(p,"fiancata",V(side*1.23f,1.23f,0),V(.12f,.6f,8.2f),body);
                Box(p,"fascia",V(side*1.3f,1.45f,0),V(.025f,.18f,8.1f),stripe,false);
                Box(p,"cintura_finestrini",V(side*1.23f,1.64f,0),V(.12f,.3f,8.2f),body,false);
                Box(p,"architrave_finestrini",V(side*1.23f,2.91f,0),V(.12f,.18f,8.2f),body,false);
                for(int i=0;i<6;i++) {
                    float z=-3.25f+i*1.3f;
                    Box(p,"montante_finestrino",V(side*1.23f,2.2f,z-.6f),V(.12f,1.5f,.09f),body,false);
                    Box(p,"finestrino",V(side*1.235f,2.3f,z),V(.045f,1.05f,1.13f),Glass,false);
                    if(i>0) {
                        var seat=Group("sedile_corriera",p);seat.localPosition=V(side*.78f,.5f,z);
                        Chair(seat,Vector3.zero);
                    }
                }
                foreach(float z in new[]{-2.6f,2.6f}) {
                    var wheel=Cone(p,"pneumatico",V(side*1.38f,.5f,z),.5f,.28f,Mat("gomma","#252B2B"),12,.5f);
                    wheel.transform.localRotation=Quaternion.Euler(0,0,side*90);
                    var hub=Cone(p,"mozzo",V(side*1.4f,.5f,z),.25f,.03f,Stone,10,.25f);hub.transform.localRotation=wheel.transform.localRotation;
                }
            }
            foreach(float end in new[]{-1f,1f}) {
                Box(p,"testata",V(0,1.35f,end*4.08f),V(2.48f,.9f,.12f),body);
                Box(p,"parabrezza",V(0,2.25f,end*4.09f),V(2.28f,1.02f,.045f),Glass,false);
                Box(p,"paraurti",V(0,.91f,end*4.2f),V(2.56f,.18f,.18f),Dark);
                foreach(float x in new[]{-.85f,.85f}) Box(p,"fanale",V(x,1.32f,end*4.17f),V(.3f,.22f,.04f),end<0?Plaster:Red,false);
            }
            var door=Group("porta_pieghevole",p);door.localPosition=V(1.31f,1.95f,-2.6f);
            Box(door,"anta",Vector3.zero,V(.05f,1.65f,.9f),Dark,false);
            Box(door,"vetro_porta",V(.03f,.15f,0),V(.03f,1.15f,.68f),Glass,false);
            var driver=Group("sedile_guida",p);driver.localPosition=V(-.7f,.5f,-3);Chair(driver,Vector3.zero);
            var wheelDrive=Cone(p,"volante",V(-.7f,1.75f,-3.5f),.23f,.045f,Dark,10,.23f);wheelDrive.transform.localRotation=Quaternion.Euler(45,0,0);
            Group("punto_salita",p).localPosition=V(2.7f,0,-2.6f);
            var arrival=Group("punto_arrivo",p);arrival.localPosition=V(3.7f,.25f,-2.6f);arrival.localRotation=Quaternion.Euler(0,90,0);
            var passenger=Group("posto_passeggero",p);passenger.localPosition=V(7,1.5f,-8);passenger.localRotation=Quaternion.LookRotation(V(-7,-2.5f,8));
            return Save(p,"Props","corriera_1987");
        }
        public static Transform Stop(Transform parent,Vector3 position,float yaw,string destination,string label)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Props/corriera_1987.prefab");
            var bus=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);bus.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            var trip=bus.AddComponent<Viaggio1987>();trip.DestinazioneScena=destination;trip.Destinazione=label;
            trip.PuntoSalita=bus.transform.Find("punto_salita");trip.PuntoArrivo=bus.transform.Find("punto_arrivo");trip.PostoPasseggero=bus.transform.Find("posto_passeggero");
            var display=Group("destinazione",bus.transform);display.localPosition=V(0,2.89f,-4.22f);
            Lettering.Paint(display,label.ToUpperInvariant(),2.1f,.17f,Dark);
            return bus.transform;
        }
        public static GameObject Train()
        {
            var p=Group("treno_regionale_1987");var ivory=Mat("treno_avorio","#C8C5AD");var blue=Mat("treno_blu","#416178");
            for(int car=0;car<2;car++) {
                var c=Group("automotrice_"+car,p);c.localPosition=V(0,0,car*19);
                Box(c,"cassa",V(0,1.65f,0),V(2.9f,1.25f,18),ivory);
                Box(c,"fascia_blu",V(0,2.25f,0),V(2.95f,.3f,18.04f),blue,false);
                Box(c,"tetto",V(0,3.6f,0),V(3.02f,.3f,18.2f),Stone);
                foreach(float side in new[]{-1f,1f}) {
                    Box(c,"fiancata_superiore",V(side*1.43f,2.94f,0),V(.07f,1.3f,18),ivory);
                    for(int j=0;j<10;j++) Box(c,"finestrino",V(side*1.47f,2.88f,-7.3f+j*1.6f),V(.06f,.93f,1.24f),Glass,false);
                    foreach(float z in new[]{-6f,6f}) {
                        Box(c,"carrello",V(0,.7f,z),V(2.8f,.5f,2),Dark);
                        foreach(float delta in new[]{-.6f,.6f}) {
                            var w=Cone(c,"ruota_ferroviaria",V(side*.8f,.5f,z+delta),.45f,.15f,Dark,12,.45f);w.transform.localRotation=Quaternion.Euler(0,0,side*90);
                        }
                    }
                }
                foreach(float z in new[]{-9f,9f}) {
                    Box(c,"testata",V(0,2.75f,z),V(2.9f,1.45f,.1f),ivory);
                    Box(c,"vetro_cabina",V(0,2.95f,z+Mathf.Sign(z)*.06f),V(2.4f,.75f,.04f),Glass,false);
                    foreach(float x in new[]{-.95f,.95f}) Box(c,"fanale",V(x,1.65f,z+Mathf.Sign(z)*.09f),V(.2f,.2f,.05f),Plaster,false);
                    Box(c,"respingente",V(0,.9f,z+Mathf.Sign(z)*.35f),V(2,.2f,.55f),Dark);
                }
            }
            return Save(p,"Props","treno_regionale_1987");
        }
        public static void Track(Transform p,float length,bool disused)
        {
            Box(p,"massicciata",V(0,-.03f,0),V(3.5f,.16f,length+1),Mat("pietrisco_binari","#787D75"));
            foreach(float x in new[]{-.725f,.725f}) Box(p,"rotaia",V(x,.17f,0),V(.12f,.18f,length),Dark);
            for(float z=-length/2;z<length/2;z+=.75f) Box(p,"traversina",V(0,.06f,z),V(2.6f,.14f,.2f),Wood,false);
            foreach(float end in new[]{-1f,1f}) {
                float z=end*(length/2-.5f);
                foreach(float x in new[]{-.725f,.725f}) Beam(p,"telaio_paraurti",V(x,.2f,z-end*.8f),V(x,.95f,z),.18f,Dark);
                Box(p,"fine_corsa",V(0,.95f,z),V(2.2f,.3f,.25f),disused?Wood:Red);
            }
        }
    }
}
