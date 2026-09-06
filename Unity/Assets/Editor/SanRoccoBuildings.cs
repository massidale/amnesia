using UnityEngine;
using UnityEditor;
using static AmnesiaUnity.Editor.SanRocco.Kit;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class Buildings
    {
        static void Wall(Transform p,string name,Vector3 pos,Vector3 size,Material m) => Box(p,name,pos,size,m);
        public static GameObject Create(Site s,int index,bool furnished=true)
        {
            if(s.kind=="depot") return Depots.Create(s);
            var p=Group(s.id); float w=s.w,d=s.d,h=s.h;
            var plaster=index%4==0?Mat("intonaco_salvia","#A9BAAB"):index%4==1?Mat("intonaco_rosa","#C3A4A0"):Plaster;
            if(s.kind=="warehouse") plaster=Stone;
            var anchor=p.gameObject.AddComponent<Luogo1987>(); anchor.Id=s.id; anchor.Nome=s.label; anchor.Dimensioni=new Vector2(w,d);
            Box(p,"fondazione",new Vector3(0,-.24f,0),new Vector3(w+.4f,.4f,d+.4f),Stone);
            Box(p,"pavimento",new Vector3(0,-.025f,0),new Vector3(w,.1f,d),Mat("pavimento","#AFB3AA"));
            float door=s.kind=="church"?2.8f:2.1f;
            // Only Matteo's workshop needs a rear exit into the woods.
            foreach(float z in new[]{-d/2,d/2}) {
                if(z>0 && s.id!="bottega") {
                    Wall(p,"parete_posteriore",new Vector3(0,h/2,z),new Vector3(w,h,.3f),plaster);
                    continue;
                }
                foreach(float sign in new[]{-1f,1f}) Wall(p,"parete_porta",new Vector3(sign*(w+door)/4,h/2,z),new Vector3((w-door)/2,h,.3f),plaster);
                Wall(p,"architrave",new Vector3(0,(h+2.65f)/2,z),new Vector3(door,h-2.65f,.3f),plaster);
                foreach(float x in new[]{-door/2,door/2}) Box(p,"stipite",new Vector3(x,1.35f,z),new Vector3(.16f,2.7f,.46f),Stone);
                Box(p,"soglia",new Vector3(0,.015f,z),new Vector3(door,.06f,.65f),Stone);
                Box(p,"porta_aperta",new Vector3(-door/2+.12f,1.3f,z+(z<0?-.85f:.85f)),new Vector3(.13f,2.5f,1.7f),Green);
                Box(p,"maniglia",new Vector3(-door/2+.22f,1.1f,z+(z<0?-1.4f:1.4f)),new Vector3(.08f,.05f,.18f),Dark,false);
            }
            // Side windows have glazing, lintels, sills and shutters, not painted rectangles.
            foreach(float sign in new[]{-1f,1f}) {
                float x=sign*w/2, slot=d/3;
                for(int k=0;k<3;k++) {
                    float z=-d/2+slot*(k+.5f), gap=1.65f;
                    Wall(p,"sottofinestra",new Vector3(x,.6f,z),new Vector3(.3f,1.2f,slot),plaster);
                    Wall(p,"soprafinestra",new Vector3(x,(h+2.5f)/2,z),new Vector3(.3f,h-2.5f,slot),plaster);
                    foreach(float side in new[]{-1f,1f}) Wall(p,"spalla_finestra",new Vector3(x,1.85f,z+side*(slot+gap)/4),new Vector3(.3f,1.3f,(slot-gap)/2),plaster);
                    Box(p,"vetro",new Vector3(x,1.85f,z),new Vector3(.08f,1.3f,gap),Glass);
                    Box(p,"davanzale",new Vector3(x,1.2f,z),new Vector3(.55f,.12f,gap+.2f),Stone);
                    Box(p,"telaio",new Vector3(x,1.85f,z),new Vector3(.15f,1.3f,.06f),Wood);
                    foreach(float side in new[]{-1f,1f}) Box(p,"persiana",new Vector3(x+sign*.2f,1.85f,z+side*1.15f),new Vector3(.12f,1.4f,.48f),Green);
                }
            }
            RoofGable(p,w,d,h,plaster);
            Box(p,"soffitto",new Vector3(0,h-.06f,0),new Vector3(w,.12f,d),Plaster);
            Box(p,"camino",new Vector3(-w*.28f,h+1.4f,d*.2f),new Vector3(.75f,2.3f,.75f),Stone);
            Box(p,"comignolo",new Vector3(-w*.28f,h+2.6f,d*.2f),new Vector3(1,.18f,1),Roof);
            foreach(float side in new[]{-1f,1f}) Box(p,"grondaia",new Vector3(side*(w/2+.4f),h,-.1f),new Vector3(.12f,.12f,d+1),Dark,false);
            if(s.kind!="church" && s.kind!="house") Sign(p,s.label,new Vector3(0,h+(s.kind=="bar" || s.kind=="bakery"?.15f:-.35f),-d/2-.23f),Mathf.Min(w-1,9),s.kind=="bar"?Red:Green);
            Lamp(p,new Vector3(-w*.23f,h-.45f,0)); Lamp(p,new Vector3(w*.23f,h-.45f,d*.25f));
            Art.Building(p,s,index);
            if(furnished) InteriorFinish(p,s);
            if(furnished || s.kind=="church") switch(s.kind) {
                case "church": Church(p,s); break;
                case "bar": Bar(p,s); break;
                case "bakery": Bakery(p,s); break;
                case "shop": Shop(p,s); break;
                case "stationery": Stationery(p,s); break;
                case "workshop": Workshop(p,s); break;
                case "school": School(p,s); break;
                case "coop": case "warehouse": Warehouse(p,s); break;
                case "station": Station(p,s); break;
                default: House(p,s,index); break;
            }
            if(s.id=="magazzino_b17") {
                var shutter=Box(p,"saracinesca_B17",new Vector3(0,1.25f,-d/2),new Vector3(2,2.5f,.12f),Dark);
                shutter.AddComponent<PortaMarker>().Id="magazzino_b17";
                for(float y=.15f;y<2.5f;y+=.18f) Box(shutter.transform,"lamiera",new Vector3(0,y/2.5f-.5f,-.65f),new Vector3(.98f,.02f,.4f),Stone,false);
            }
            if(s.id.EndsWith("_chivasso") || s.id=="casa_wanda")
                foreach(var renderer in p.GetComponentsInChildren<MeshRenderer>())
                    if(renderer.name=="tetto_lose" || renderer.name=="colmo") renderer.sharedMaterial=Mat("coppi_chivasso","#987B6C");
            return Save(p,"Buildings",s.id);
        }
        static void House(Transform p,Site s,int i)
        {
            var room=Group("arredi_casa",p);float back=s.d/2,left=-s.w*.23f,bedX=s.w/2-2;
            // Tall storage uses the blind rear wall. Side windows retain a clear strip.
            var shelf=Group("libreria_parete",room);shelf.localPosition=new Vector3(-s.w/2+1.8f,0,back-.41f);
            Shelf(shelf,Vector3.zero,1.7f);
            Box(shelf,"schiena_libreria",new Vector3(0,1.1f,.22f),new Vector3(1.8f,2.2f,.04f),Wood,false);
            for(int k=0;k<9;k++) Box(shelf,"libro",new Vector3(-.65f+k%5*.28f,.59f+k/5*.5f,0),new Vector3(.17f,.36f,.3f),k%2==0?Red:Green,false);
            Box(room,"armadio",new Vector3(-.95f,1.1f,back-.55f),new Vector3(1.7f,2.2f,.8f),Wood);
            foreach(float x in new[]{-1.08f,-.82f}) Box(room,"pomello_armadio",new Vector3(x,1.15f,back-.97f),new Vector3(.05f,.09f,.05f),Dark,false);
            bool twin=s.id=="casa_wanda";if(twin) bedX=4.65f;
            Box(room,"letto",new Vector3(bedX,.3f,back-1.65f),new Vector3(twin?1.2f:1.8f,.5f,2.5f),Wood);
            Box(room,"testata_letto",new Vector3(bedX,.65f,back-.35f),new Vector3(twin?1.3f:1.9f,1.2f,.12f),Wood);
            Box(room,"coperta",new Vector3(bedX,.6f,back-1.65f),new Vector3(twin?1.15f:1.7f,.16f,2.35f),i%2==0?Green:Red);
            if(twin) {
                var bed=Group("secondo_letto",room);bed.localPosition=new Vector3(2.65f,0,back-1.65f);
                Box(bed,"rete",new Vector3(0,.3f,0),new Vector3(1.2f,.5f,2.5f),Wood);
                Box(bed,"testata",new Vector3(0,.65f,1.3f),new Vector3(1.3f,1.2f,.12f),Wood);
                Box(bed,"coperta",new Vector3(0,.6f,0),new Vector3(1.15f,.16f,2.35f),Green);
                Box(bed,"cuscino",new Vector3(0,.73f,.72f),new Vector3(.8f,.13f,.45f),Plaster,false);
            }
            Box(room,"cuscino",new Vector3(bedX,.74f,back-.9f),new Vector3(1.2f,.15f,.5f),Plaster);
            Box(room,"divisorio_notte",new Vector3(1.25f,s.h/2,back/2+.35f),new Vector3(.14f,s.h,back-.9f),Plaster);
            float kitchenX=-s.w/4-.3f;
            Box(room,"parete_cucina",new Vector3(kitchenX,s.h/2,1.3f),new Vector3(s.w/2-1.3f,s.h,.15f),Plaster);
            Box(room,"cucina_smalto",new Vector3(kitchenX,.45f,.8f),new Vector3(2.4f,.9f,.8f),Plaster);
            for(int k=0;k<2;k++) Cone(room,"fornello",new Vector3(kitchenX-.75f+k*.48f,.93f,.8f),.18f,.035f,Dark,10,.18f);
            Box(room,"lavello",new Vector3(kitchenX+.65f,.91f,.8f),new Vector3(.65f,.05f,.5f),Stone,false);
            Table(room,new Vector3(left,0,-1.8f),2.2f,1.3f);
            Chair(room,new Vector3(left,0,-2.8f),180);Chair(room,new Vector3(left,0,-.8f));
            var seat=Group("posto_tavolo",p);seat.localPosition=new Vector3(left,.025f,-.8f);
            Book(room,new Vector3(left,.9f,-1.8f),s.id=="canonica"?Red:Green);
            if(s.id=="casa_lipari") Box(room,"scatola_carte_Andrea",new Vector3(-.95f,2.36f,back-.55f),new Vector3(.65f,.3f,.4f),Mat("cartone","#B6A178"));
        }
        static void Book(Transform p,Vector3 pos,Material cover)
        { Box(p,"copertina",pos,new Vector3(.4f,.06f,.3f),cover,false); Box(p,"pagine",pos+Vector3.up*.04f,new Vector3(.37f,.045f,.28f),Plaster,false); }
        static void Bar(Transform p,Site s)
        {
            Box(p,"banco_bar",new Vector3(3,.6f,0),new Vector3(1.3f,1.2f,6),Wood);
            Box(p,"banco_formica",new Vector3(3,1.25f,0),new Vector3(1.5f,.12f,6.2f),Mat("formica","#AEC5BF"));
            Box(p,"macchina_espresso",new Vector3(3,1.65f,1.2f),new Vector3(.8f,.65f,1.1f),Mat("alluminio","#BDC9CA"));
            Box(p,"griglia_espresso",new Vector3(2.5f,1.35f,1.2f),new Vector3(.35f,.05f,1.1f),Dark,false);
            foreach(float z in new[]{.9f,1.5f}) {
                Beam(p,"erogatore_caffe",new Vector3(2.6f,1.65f,z),new Vector3(2.45f,1.65f,z),.09f,Dark);
                Box(p,"pulsante_espresso",new Vector3(2.58f,1.85f,z),new Vector3(.035f,.09f,.09f),Red,false);
            }
            for(int i=0;i<3;i++) { Cone(p,"tazzina",new Vector3(2.7f,1.34f,-1+i*.4f),.09f,.12f,Plaster,8,.11f); }
            Shelf(p,new Vector3(3,0,s.d/2-.41f),2.4f);
            for(int i=0;i<8;i++) Cone(p,"bottiglia",new Vector3(2.1f+(i%4)*.5f,.40f+(i/4)*.5f,s.d/2-.41f),.07f,.29f,Green,8,.045f);
            foreach(float z in new[]{-2.5f,1.5f}) { Table(p,new Vector3(-3,0,z),1.8f,1.2f); Chair(p,new Vector3(-3,0,z-.9f),180); Chair(p,new Vector3(-3,0,z+.9f)); }
            Box(p,"radio",new Vector3(3,1.65f,-2),new Vector3(.5f,.4f,.8f),Dark);
        }
        static void Bakery(Transform p,Site s)
        {
            var brick=Mat("mattone_forno","#AF7563");
            Box(p,"forno_muratura",new Vector3(-3,1.1f,2.5f),new Vector3(3,2.2f,2.3f),brick);
            Box(p,"bocca_forno",new Vector3(-3,1.05f,1.3f),new Vector3(1.5f,.75f,.07f),Dark,false);
            Box(p,"cappa",new Vector3(-3,2.75f,2.5f),new Vector3(2.2f,1.1f,1.8f),Plaster);
            Table(p,new Vector3(3,0,1.5f),3.5f,1.4f); Table(p,new Vector3(-3,0,-2.5f),3.5f,1.3f);
            for(int i=0;i<9;i++) Art.Facet(p,"pagnotta",new Vector3(-4+(i%3)*.85f,1.02f,-2.9f+(i/3)*.35f),new Vector3(.29f,.15f,.16f),Mat("crosta_pane","#D2AE6F"),i);
            Box(p,"impasto",new Vector3(3,.91f,1.5f),new Vector3(.8f,.08f,.55f),Plaster,false);
            Beam(p,"mattarello",new Vector3(2.35f,.98f,1.8f),new Vector3(3.2f,.98f,1.8f),.09f,Wood);
            for(int i=0;i<4;i++) Box(p,"corso_mattoni_forno",new Vector3(-3,.3f+i*.45f,1.33f),new Vector3(2.9f,.025f,.025f),Stone,false);
            for(int i=0;i<3;i++) Cone(p,"sacco_farina",new Vector3(4,.05f,-2+i*.75f),.32f,.85f,Plaster,8,.26f);
            Beam(p,"pala_forno",new Vector3(-4,.25f,1),new Vector3(-4,2.6f,1.4f),.09f,Wood);
        }
        static void Shop(Transform p,Site s)
        {
            for(int j=0;j<2;j++) { float x=j==0?-4:4; Shelf(p,new Vector3(x,0,s.d/2-.41f),2.5f);
                for(int i=0;i<12;i++) Box(p,"conserva",new Vector3(x-1+(i%4)*.62f,.57f+(i/4)*.5f,s.d/2-.41f),new Vector3(.32f,.33f,.3f),i%3==0?Red:i%3==1?Green:Plaster,false);
            }
            Table(p,new Vector3(3,0,-1.3f),3.2f,1.3f);
            Box(p,"bilancia",new Vector3(3,1.05f,-1.3f),new Vector3(.55f,.4f,.5f),Green);
            Box(p,"piatto_bilancia",new Vector3(3,1.3f,-1.3f),new Vector3(.8f,.05f,.65f),Stone);
            Box(p,"cassetta_mele",new Vector3(-3,.4f,-3),new Vector3(1.8f,.8f,1),Wood);
            for(int i=0;i<8;i++) Cone(p,"mela",new Vector3(-3.65f+(i%4)*.4f,.82f,-3.2f+(i/4)*.4f),.16f,.24f,Red,6,.08f);
        }
        static void Stationery(Transform p,Site s)
        {
            foreach(float x in new[]{-3f,0f,3f}) {
                Shelf(p,new Vector3(x,0,s.d/2-.41f),2.5f);
                for(int row=0;row<3;row++) for(int i=0;i<7;i++)
                    Box(p,"quaderno_esposto",new Vector3(x-1+i*.31f,.59f+row*.5f,s.d/2-.43f),new Vector3(.2f,.36f,.3f),i%3==0?Red:i%3==1?Green:Plaster,false);
            }
            Box(p,"banco_cartoleria",new Vector3(3,.55f,-.7f),new Vector3(3,1.1f,.95f),Wood);
            Box(p,"blocco_carta",new Vector3(2.5f,1.16f,-.7f),new Vector3(.65f,.1f,.45f),Plaster,false);
            Cone(p,"portamatite",new Vector3(3.6f,1.12f,-.7f),.12f,.22f,Green,8,.12f);
            for(int i=0;i<5;i++) Box(p,"matita",new Vector3(3.53f+i*.035f,1.4f,-.7f),new Vector3(.02f,.35f,.02f),Wood,false);
            Table(p,new Vector3(-3,0,-2),2,1);
            for(int i=0;i<3;i++) Box(p,"risma_carta",new Vector3(-3,.9f+i*.11f,-2),new Vector3(.8f,.1f,.55f),Plaster,false);
        }
        static void Workshop(Transform p,Site s)
        {
            Table(p,new Vector3(-4,0,0),4,1.6f); Box(p,"morsa",new Vector3(-4,1.04f,-.6f),new Vector3(.55f,.3f,.45f),Dark);
            for(int i=0;i<9;i++) Box(p,"tavola_castagno",new Vector3(4,.14f+i*.12f,2),new Vector3(1.6f,.1f,6),Wood);
            for(int i=0;i<6;i++) { var o=Box(p,"trucioli",new Vector3(-4+(i%3)*.4f,.045f,-1.1f-i*.1f),new Vector3(.3f,.035f,.09f),Mat("segatura","#C9B785"),false); o.transform.localRotation=Quaternion.Euler(0,i*47,0); }
            Shelf(p,new Vector3(-5,0,s.d/2-.41f)); Chair(p,new Vector3(3,0,-4));
            Box(p,"pannello_utensili",new Vector3(-3.2f,1.8f,s.d/2-.19f),new Vector3(2.7f,1.4f,.06f),Wood,false);
            for(int i=0;i<5;i++) { float x=-4.2f+i*.5f;Beam(p,"utensile",new Vector3(x,1.4f,s.d/2-.28f),new Vector3(x,2.1f,s.d/2-.28f),.065f,Wood); Box(p,"ferro_utensile",new Vector3(x,2.1f,s.d/2-.3f),new Vector3(.28f,.1f,.08f),Dark,false); }
            HouseCorner(p,new Vector3(3,0,5));
        }
        static void HouseCorner(Transform p,Vector3 pos) { Box(p,"branda",pos+Vector3.up*.35f,new Vector3(1.5f,.6f,2.3f),Wood); Box(p,"coperta",pos+Vector3.up*.7f,new Vector3(1.45f,.15f,2.2f),Green); }
        static void Church(Transform p,Site s)
        {
            for(int i=0;i<6;i++) foreach(float x in new[]{-4.2f,4.2f}) {
                Box(p,"panca",new Vector3(x,.48f,-7+i*2.2f),new Vector3(4,.14f,.6f),Wood);
                Box(p,"schienale_panca",new Vector3(x,.88f,-7.22f+i*2.2f),new Vector3(4,.72f,.13f),Wood);
                foreach(float dx in new[]{-1.5f,1.5f}) Box(p,"piede_panca",new Vector3(x+dx,.23f,-7+i*2.2f),new Vector3(.15f,.46f,.6f),Wood);
            }
            Box(p,"altare",new Vector3(0,.7f,8),new Vector3(3.5f,1.4f,1.5f),Stone);
            Box(p,"tovaglia",new Vector3(0,1.43f,8),new Vector3(3.7f,.05f,1.7f),Plaster,false);
            Box(p,"croce",new Vector3(0,4.6f,12.8f),new Vector3(.25f,3,.2f),Wood,false);
            Box(p,"croce_traversa",new Vector3(0,5.2f,12.8f),new Vector3(1.65f,.22f,.2f),Wood,false);
            var tower=Group("campanile",p); tower.localPosition=new Vector3(-s.w/2+2,0,s.d/2-2);
            Box(tower,"torre",new Vector3(0,6.7f,0),new Vector3(3.5f,13.4f,3.5f),Plaster);
            foreach(float x in new[]{-1.5f,1.5f}) foreach(float z in new[]{-1.5f,1.5f}) Box(tower,"pilastro_cella",new Vector3(x,15,z),new Vector3(.45f,3.2f,.45f),Stone);
            Cone(tower,"campana",new Vector3(0,14,0),.8f,1.2f,Mat("bronzo","#8C8769"),10,.3f);
            Cone(tower,"cuspide",new Vector3(0,16.6f,0),2.8f,3.8f,Roof,4);
            // Visible clock face on the south side of the tower.
            Box(tower,"quadrante",new Vector3(0,11,-1.79f),new Vector3(1.8f,1.8f,.05f),Plaster,false);
            Box(tower,"lancetta",new Vector3(0,11.3f,-1.85f),new Vector3(.08f,.7f,.04f),Dark,false);
            Box(tower,"lancetta_ore",new Vector3(.25f,11,-1.85f),new Vector3(.55f,.08f,.04f),Dark,false);
        }
        static void School(Transform p,Site s)
        {
            foreach(float x in new[]{-4f,4f}) for(int z=-2;z<4;z+=2) { Table(p,new Vector3(x,0,z),2.3f,.9f); Chair(p,new Vector3(x,0,z-.75f)); }
            Box(p,"lavagna",new Vector3(0,1.8f,s.d/2-.22f),new Vector3(5,1.6f,.1f),Dark,false);
            Text(p,"13 OTTOBRE 1987",new Vector3(0,2,s.d/2-.29f),.3f,Color.white,Quaternion.identity);
        }
        static void Warehouse(Transform p,Site s)
        {
            foreach(float x in new[]{-s.w*.33f,s.w*.33f}) { Shelf(p,new Vector3(x,0,s.d/2-.41f),3);
                for(int i=0;i<4;i++) Box(p,"cassa_archivio",new Vector3(x,.6f+i*.5f,s.d/2-.41f),new Vector3(1.2f,.4f,.4f),Wood);
            }
            if(s.id=="magazzino_b17") {
                Table(p,new Vector3(-3,0,0),2.8f,1.2f); Book(p,new Vector3(-3,.9f,0),Dark); Book(p,new Vector3(-3.8f,.9f,0),Red);
                Box(p,"cassetta_latta",new Vector3(-2.2f,1,0),new Vector3(.6f,.25f,.4f),Mat("latta","#8FA3A4"));
            }
            else for(int i=0;i<4;i++) Cone(p,"sacco",new Vector3(-3,0,-3+i),.5f,1.2f,Plaster,8,.35f);
        }
        static void Station(Transform p,Site s)
        {
            foreach(float x in new[]{-5f,5f}) Art.Bench(p,new Vector3(x,0,2),4);
            Box(p,"biglietteria",new Vector3(5,.53f,-1),new Vector3(3,1.06f,.65f),Wood);
            Box(p,"sportello_biglietti",new Vector3(5,1.12f,-1),new Vector3(3.2f,.12f,.9f),Stone);
            foreach(float x in new[]{3.5f,6.5f}) Box(p,"telaio_sportello",new Vector3(x,1.85f,-1),new Vector3(.09f,1.5f,.1f),Wood);
            Sign(p,"BIGLIETTI",new Vector3(5,2.7f,-1.25f),3,Green);
            Box(p,"registro_biglietti",new Vector3(5,1.23f,-1),new Vector3(.65f,.07f,.4f),Red,false);
            Sign(p,s.id=="stazione_chivasso"?"CORRIERA / SAN ROCCO":"CORRIERA / CHIVASSO 14:00",new Vector3(-5,1.8f,s.d/2-.3f),6,Dark);
            var canopy=Group("pensilina_stazione",p);
            float front=-s.d/2;
            Box(canopy,"copertura_lamiera",new Vector3(0,3,front-1.3f),new Vector3(s.w+1,.16f,2.8f),Roof);
            foreach(float x in new[]{-s.w/2+1,s.w/2-1}) {
                Box(canopy,"pilastro_ferro",new Vector3(x,1.45f,front-2.3f),new Vector3(.12f,2.9f,.12f),Green);
                Beam(canopy,"saetta",new Vector3(x,2.25f,front-2.3f),new Vector3(x,2.9f,front-1.6f),.09f,Green);
            }
            var clock=Group("orologio_stazione",p);clock.localPosition=new Vector3(0,5.15f,front-.25f);
            Box(clock,"cassa",Vector3.zero,new Vector3(1.1f,1.1f,.12f),Dark,false);
            Box(clock,"quadrante",new Vector3(0,0,-.08f),new Vector3(.97f,.97f,.05f),Plaster,false);
            Box(clock,"minuti",new Vector3(0,.2f,-.12f),new Vector3(.045f,.4f,.03f),Dark,false);
            Box(clock,"ore",new Vector3(.13f,-.06f,-.12f),new Vector3(.28f,.045f,.03f),Dark,false);
            foreach(float x in new[]{-6f,6f}) { Art.Bench(p,new Vector3(x,0,front-.9f),2.8f); }
            Box(p,"valigia",new Vector3(-7,.3f,2),new Vector3(.8f,.6f,.3f),Wood);
            Beam(p,"maniglia_valigia",new Vector3(-7.2f,.66f,2),new Vector3(-6.8f,.66f,2),.06f,Dark);
            Box(p,"bacheca_orari",new Vector3(-s.w/2+.2f,1.7f,-1),new Vector3(.1f,1.5f,2.2f),Wood,false);
            for(int i=0;i<3;i++) Box(p,"foglio_orari",new Vector3(-s.w/2+.27f,1.7f,-1.7f+i*.65f),new Vector3(.02f,1.1f,.5f),Plaster,false);
        }
        static void InteriorFinish(Transform p,Site s)
        {
            var finish=Group("finiture_interne",p);
            var skirting=s.kind=="house"?Wood:Stone;
            foreach(float x in new[]{-s.w/2+.19f,s.w/2-.19f}) Box(finish,"battiscopa",new Vector3(x,.15f,0),new Vector3(.08f,.23f,s.d-.4f),skirting,false);
            Box(finish,"battiscopa_fondo",new Vector3(0,.15f,s.d/2-.19f),new Vector3(s.w-.4f,.23f,.08f),skirting,false);
            var vertices=new[]{new System.Collections.Generic.List<Vector3>(),new System.Collections.Generic.List<Vector3>()};
            var indices=new[]{new System.Collections.Generic.List<int>(),new System.Collections.Generic.List<int>()};
            for(float x=-s.w/2+.22f;x<s.w/2-.3f;x+=.8f) for(float z=-s.d/2+.22f;z<s.d/2-.3f;z+=.8f) {
                int m=(Mathf.RoundToInt((x+s.w/2)/.8f)+Mathf.RoundToInt((z+s.d/2)/.8f))%2,n=vertices[m].Count;
                float xx=Mathf.Min(x+.775f,s.w/2-.22f),zz=Mathf.Min(z+.775f,s.d/2-.22f);
                vertices[m].AddRange(new[]{new Vector3(x,.032f,z),new Vector3(x,.032f,zz),new Vector3(xx,.032f,z),new Vector3(xx,.032f,zz)});
                indices[m].AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3});
            }
            for(int i=0;i<2;i++) Mesh(finish,"piastrelle_interne",vertices[i].ToArray(),indices[i].ToArray(),Mat("graniglia_"+i,i==0?"#96998B":"#828A80"),false);
        }
    }
}
