using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static AmnesiaUnity.Editor.SanRocco.Kit;
using Object = UnityEngine.Object;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class Items1987
    {
        public const string Folder = "Assets/Resources/Oggetti1987";
        public static readonly string[] Ids = { "fotografia", "foglio_indirizzo", "chiave_b17", "taccuino",
            "quaderno_vittorio", "registro", "braccialetto", "cassetta_latta", "giacca", "due_righe_matteo", "lapide" };
        static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
        static Material Paper => Mat("oggetti_carta", "#D9D7BF");
        static Material Ink => Mat("oggetti_inchiostro", "#38434A");
        static Material Silver => Mat("oggetti_argento", "#B7C5C8");
        static Material Tin => Mat("oggetti_latta", "#698C89");
        static Material Leather => Mat("oggetti_cuoio", "#72594C");
        static void Part(Transform p,string n,Vector3 v,Vector3 s,Material m) => Box(p,n,v,s,m,false);

        [MenuItem("Amnesia/San Rocco 1987/Crea oggetti e aggiorna B-17")]
        public static void Install()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Uscire da Play prima di creare gli asset.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            var path = Root + "/Buildings/deposito_b.prefab";
            var depot = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var cell = depot.transform.Find("unita_B_17");
                Populate(cell, (depot.GetComponent<Luogo1987>().Dimensioni.x - .4f - 3.3f) / 2);
                PrefabUtility.SaveAsPrefabAsset(depot,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(depot); }
            var quarryPath = Root + "/Buildings/cava_pian_della_soglia.prefab";
            var quarry = PrefabUtility.LoadPrefabContents(quarryPath);
            try { ReplaceMemorial(quarry.transform); PrefabUtility.SaveAsPrefabAsset(quarry, quarryPath); }
            finally { PrefabUtility.UnloadPrefabContents(quarry); }
            var scenePath = Root + "/Scenes/SanRocco1987.unity";
            if (!File.Exists(scenePath)) scenePath = "Assets/Scenes/SanRocco1987.unity";
            var scene = EditorSceneManager.OpenScene(scenePath);
            // Older generated scenes have unpacked building instances.
            var building = GameObject.Find("deposito_b");
            if (building != null && !PrefabUtility.IsPartOfPrefabInstance(building))
                Populate(building.transform.Find("unita_B_17"), (building.GetComponent<Luogo1987>().Dimensioni.x-.4f-3.3f)/2);
            var old = GameObject.Find("lapide_collettiva");
            if (old != null && !PrefabUtility.IsPartOfPrefabInstance(old)) ReplaceMemorial(old.transform.parent);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("OGGETTI_INSTALLATI: 11 prefab, quaderno e cassetta singoli nel B-17, braccialetto interno.");
        }

        static void ReplaceMemorial(Transform root)
        {
            var old = root.Find("lapide_collettiva");
            if (old != null)
            {
                var model = Instance("lapide", old.parent, old.localPosition - Vector3.up);
                model.transform.localRotation = old.localRotation;
                model.AddComponent<OggettoRaccoglibile>().Id = "lapide";
                model.GetComponent<OggettoRaccoglibile>().Fisso = true;
                foreach (var text in old.parent.GetComponentsInChildren<TextMesh>())
                    if (text.text.StartsWith("14 OTTOBRE 1966")) Object.DestroyImmediate(text.gameObject);
                Object.DestroyImmediate(old.gameObject);
            }
        }

        public static void Build()
        {
            Directory.CreateDirectory(Folder);
            Directory.CreateDirectory(Root + "/Materials");
            Directory.CreateDirectory(Root + "/Meshes");
            AssetDatabase.Refresh();
            Begin("oggetti_");
            try
            {
                foreach (var id in Ids)
                {
                    var p = Group(id);
                    switch (id)
                    {
                        case "fotografia": Photograph(p); break;
                        case "foglio_indirizzo": Sheet(p,false); break;
                        case "due_righe_matteo": Sheet(p,true); break;
                        case "chiave_b17": Key(p); break;
                        case "taccuino": Book(p,Mat("oggetti_tela_rossa","#8A534F"),false,"APPUNTI"); break;
                        case "quaderno_vittorio": Book(p,Mat("oggetti_tela_blu","#536C81"),true,""); break;
                        case "registro": Book(p,Mat("oggetti_tela_verde","#557561"),false,"1958 - 1966"); p.localScale=V(1.25f,1.5f,1.3f); break;
                        case "braccialetto": Bracelet(p); break;
                        case "cassetta_latta": BoxOfMemories(p); break;
                        case "giacca": Jacket(p); p.localRotation=Quaternion.Euler(0,180,0); break;
                        case "lapide": Memorial(p); break;
                    }
                    var bounds = new Bounds(); bool first = true;
                    foreach (var renderer in p.GetComponentsInChildren<Renderer>())
                    { if (first) { bounds=renderer.bounds; first=false; } else bounds.Encapsulate(renderer.bounds); }
                    if (first || bounds.size.sqrMagnitude < .001f) throw new Exception("Modello vuoto: " + id);
                    if (id == "cassetta_latta")
                    {
                        foreach(var part in p.GetComponentsInChildren<MeshRenderer>())
                            if(new[]{"fondo","fianco","bordo","lamiera"}.Contains(part.name)) part.gameObject.AddComponent<BoxCollider>();
                    }
                    else
                    {
                        var collider=p.gameObject.AddComponent<BoxCollider>();
                        collider.center=p.InverseTransformPoint(bounds.center);
                        collider.size=new Vector3(bounds.size.x/p.localScale.x,bounds.size.y/p.localScale.y,bounds.size.z/p.localScale.z);
                    }
                    PrefabUtility.SaveAsPrefabAsset(p.gameObject,Folder+"/"+id+".prefab");
                    Object.DestroyImmediate(p.gameObject);
                }
            }
            finally { Begin(); }
            RenderIcons();
        }

        static void RenderIcons()
        {
            var directory=Folder+"/Icone";Directory.CreateDirectory(directory);
            var studio=new GameObject("studio_temporaneo_oggetti");studio.transform.position=V(12000,12000,12000);
            var camera=Group("camera",studio.transform).gameObject.AddComponent<Camera>();
            camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1.05f;
            camera.nearClipPlane=.1f;camera.farClipPlane=8;
            camera.transform.localPosition=V(0,2.2f,-2.7f);camera.transform.LookAt(studio.transform);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.79f,.82f,.79f);
            var lamp=Group("luce",studio.transform).gameObject.AddComponent<Light>();
            lamp.transform.localPosition=V(-1,2,-2);lamp.type=LightType.Point;lamp.range=7;lamp.intensity=2.2f;
            var target=new RenderTexture(256,256,24){antiAliasing=4};target.Create();camera.targetTexture=target;
            var previous=RenderTexture.active;
            try
            {
                foreach(var id in Ids)
                {
                    var model=Instance(id,studio.transform,Vector3.zero);
                    var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
                    foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    float scale=1.5f/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
                    model.transform.localScale*=scale;model.transform.localPosition=(studio.transform.position-bounds.center)*scale;
                    camera.Render();RenderTexture.active=target;
                    var image=new Texture2D(256,256,TextureFormat.RGB24,false);
                    image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();
                    File.WriteAllBytes(directory+"/"+id+".png",image.EncodeToPNG());
                    Object.DestroyImmediate(image);Object.DestroyImmediate(model);
                }
            }
            finally
            { RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(studio); }
            AssetDatabase.Refresh();
        }

        public static GameObject Instance(string id,Transform parent,Vector3 position)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/"+id+".prefab");
            if (prefab == null) throw new Exception("Generare prima gli oggetti: " + id);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
            go.name=id; go.transform.localPosition=position; return go;
        }

        public static void Populate(Transform cell,float depth)
        {
            foreach (var name in new[]{"registro_presenze","quaderno_vittorio","cassetta_latta","spago","carte_Andrea"})
            { var old=cell.Find(name); if(old!=null) Object.DestroyImmediate(old.gameObject); }
            float back=depth/2-.41f;
            var notebook=Instance("quaderno_vittorio",cell,V(-1.05f,.895f,back));
            notebook.AddComponent<OggettoRaccoglibile>().Id="quaderno_vittorio";
            var tin=Instance("cassetta_latta",cell,V(1.05f,.395f,back));
            tin.AddComponent<OggettoRaccoglibile>().Id="cassetta_latta";
            var bracelet=tin.transform.Find("braccialetto");
            bracelet.gameObject.AddComponent<OggettoRaccoglibile>().Id="braccialetto";
            bracelet.gameObject.SetActive(false);
        }

        static void Label(Transform p,string text,Vector3 position,float width,float height,bool horizontal=true)
        {
            var label=Group("incisione_"+text,p); label.localPosition=position;
            if(horizontal) label.localRotation=Quaternion.Euler(90,0,0);
            Lettering.Paint(label,text,width,height,Ink);
        }
        static void Ring(Transform p,string name,Vector3 center,float radius,float tube,Material material,int sides=12)
        {
            var vertices=new Vector3[sides*6]; var triangles=new int[sides*6*6]; int k=0;
            for(int i=0;i<sides;i++) for(int j=0;j<6;j++)
            {
                float a=2*Mathf.PI*i/sides,b=2*Mathf.PI*j/6;
                vertices[i*6+j]=center+V(Mathf.Cos(a)*(radius+tube*Mathf.Cos(b)),tube*Mathf.Sin(b),Mathf.Sin(a)*(radius+tube*Mathf.Cos(b)));
                int v=i*6+j,n=((i+1)%sides)*6+j,v1=i*6+(j+1)%6,n1=((i+1)%sides)*6+(j+1)%6;
                foreach(int index in new[]{v,v1,n,n,v1,n1}) triangles[k++]=index;
            }
            Mesh(p,name,vertices,triangles,material,false);
        }
        static void Key(Transform p)
        {
            Ring(p,"impugnatura_forgiata",V(-.10f,.012f,0),.04f,.009f,Dark);
            Part(p,"stelo",V(.016f,.012f,0),V(.17f,.017f,.017f),Dark);
            Part(p,"dente_lungo",V(.081f,.012f,.024f),V(.014f,.018f,.045f),Dark);
            Part(p,"dente_corto",V(.052f,.012f,.018f),V(.014f,.018f,.033f),Dark);
            Ring(p,"spago",V(-.125f,.014f,.047f),.026f,.002f,Paper,10);
            Part(p,"targhetta_ottone",V(-.124f,.01f,.107f),V(.066f,.006f,.083f),Mat("oggetti_ottone","#B5A16D"));
            Label(p,"B-17",V(-.124f,.014f,.11f),.054f,.018f);
        }
        static void Bracelet(Transform p)
        {
            Ring(p,"argento_sfaccettato",V(0,.014f,0),.045f,.004f,Silver,16);
            Part(p,"piastrina",V(0,.016f,-.043f),V(.04f,.007f,.015f),Silver);
            var engraving=Group("incisione_interna_EV",p);engraving.localPosition=V(0,.0115f,-.042f);
            engraving.localRotation=Quaternion.Euler(-90,0,0);
            Lettering.Paint(engraving,"EV",.026f,.008f,Ink);
            Part(p,"chiusura",V(0,.014f,.044f),V(.014f,.008f,.01f),Ink);
        }
        static void Sheet(Transform p,bool torn)
        {
            float w=torn?.17f:.22f,d=torn?.16f:.28f;
            // Uneven corners and a raised crease, rather than a perfectly flat rectangle.
            Mesh(p,"foglio_piegato",new[]{V(-w/2,0,-d/2),V(0,.004f,-d/2),V(w/2,0,-d/2+.008f),V(w/2,.002f,d/2),V(0,.005f,d/2),V(-w/2,0,d/2-.005f)},new[]{0,4,1,0,5,4,1,3,2,1,4,3},Paper,false);
            Part(p,"retro",V(0,-.001f,0),V(w,.001f,d-.012f),Paper);
            if(torn)
            {
                for(int i=0;i<11;i++) Part(p,"strappo",V(-w/2+i*w/11,0,-d/2),V(.009f,.001f,.008f+(i%3)*.003f),Paper);
                Writing(p,2,.007f,w*.72f,d*.35f);
                Label(p,"MATTEO",V(.022f,.008f,-.06f),.065f,.012f);
            }
            else
            {
                Label(p,"VIA SANT ORSOLA 14",V(0,.007f,.035f),.185f,.014f);
                Label(p,"CHIVASSO",V(0,.007f,-.025f),.12f,.018f);
                Part(p,"piega_orizzontale",V(0,.005f,0),V(w,.001f,.0015f),Mat("oggetti_piega","#BBBBA8"));
            }
        }
        static void Writing(Transform p,int lines,float y,float width,float depth)
        {
            for(int row=0;row<lines;row++) for(int word=0;word<4;word++)
            {
                float length=width*(.11f+.025f*((row+word)%3));
                var stroke=Box(p,"inchiostro",V(-width*.38f+word*width*.24f,y,-depth/2+row*depth/Mathf.Max(1,lines-1)),V(length,.001f,.0015f),Ink,false);
                stroke.transform.localRotation=Quaternion.Euler(0,(row+word)%3-1,0);
            }
        }
        static void Book(Transform p,Material cover,bool open,string title)
        {
            float w=open?.38f:.21f,d=.28f;
            Part(p,"copertina_inferiore",V(0,.006f,0),V(w+.014f,.012f,d+.014f),cover);
            Part(p,"pagine",V(0,.026f,0),V(w,.03f,d),Paper);
            for(int i=0;i<4;i++) Part(p,"taglio_pagine",V(w/2+.0005f,.016f+i*.007f,0),V(.001f,.001f,d-.004f),Mat("oggetti_pagina_ombra","#ABAFA0"));
            Part(p,"dorso",V(-w/2-.004f,.028f,0),V(.015f,.055f,d+.015f),cover);
            if(open)
            {
                Part(p,"piega_centrale",V(0,.042f,0),V(.005f,.003f,d),cover);
                foreach(float x in new[]{-.098f,.098f})
                { var page=Group("pagina_manoscritta",p);page.localPosition=V(x,0,0);Writing(page,17,.043f,.16f,.24f); }
            }
            else
            {
                Part(p,"copertina_superiore",V(0,.048f,0),V(w+.014f,.012f,d+.014f),cover);
                Part(p,"etichetta_carta",V(0,.055f,-.023f),V(.16f,.001f,.058f),Paper);
                Label(p,title,V(0,.056f,-.023f),.14f,.015f);
            }
            Part(p,"segnalibro",V(.05f,.025f,.155f),V(.012f,.001f,.06f),Red);
            foreach(float z in new[]{-.125f,.125f}) Part(p,"angolo_consumato",V(-w/2,.056f,z),V(.019f,.002f,.024f),Paper);
        }
        static void Photograph(Transform p)
        {
            Part(p,"carta_fotografica",V(0,.002f,0),V(.24f,.004f,.17f),Paper);
            for(int i=0;i<16;i++) foreach(float z in new[]{-.084f,.084f})
                Part(p,"bordo_dentellato",V(-.114f+i*.015f,.001f,z),V(.01f,.002f,.006f),Paper);
            Part(p,"stampa_cava",V(0,.0045f,0),V(.213f,.001f,.143f),Mat("oggetti_foto_grigio","#78827D"));
            for(int i=0;i<5;i++) Part(p,"gradone_fotografato",V(0,.005f,.054f-i*.007f),V(.208f,.001f,.003f),Mat("oggetti_foto_roccia","#ACB0A1"));
            for(int i=0;i<11;i++)
            {
                float x=-.091f+i*.0182f,z=-.022f+(i%3)*.003f;
                Part(p,"persona_"+(i+1),V(x,.006f,z),V(.012f,.001f,.038f),Ink);
                Part(p,"volto_"+(i+1),V(x,.006f,z+.023f),V(.009f,.001f,.011f),Paper);
            }
            var back=Group("dedica_sul_retro",p);back.localPosition=V(0,-.0002f,0);back.localRotation=Quaternion.Euler(-90,0,0);
            Lettering.Paint(back,"CIRCOLO DELLA SOGLIA - 1961",.216f,.013f,Ink);
        }
        static void BoxOfMemories(Transform p)
        {
            Part(p,"fondo",V(0,.009f,0),V(.42f,.018f,.28f),Tin);
            foreach(float x in new[]{-.204f,.204f}) Part(p,"fianco",V(x,.076f,0),V(.012f,.14f,.28f),Tin);
            foreach(float z in new[]{-.134f,.134f}) Part(p,"bordo",V(0,.076f,z),V(.408f,.14f,.012f),Tin);
            Part(p,"fascia_biscotti",V(0,.072f,-.1405f),V(.35f,.053f,.001f),Paper);
            Label(p,"BISCOTTI",V(0,.072f,-.142f),.26f,.029f,false);
            var lid=Group("coperchio",p);lid.localPosition=V(0,.15f,.14f);
            Part(lid,"lamiera",V(0,0,-.14f),V(.437f,.016f,.297f),Tin);
            Part(lid,"cornice",V(0,.009f,-.14f),V(.355f,.002f,.22f),Paper);
            Label(lid,"BISCOTTI",V(0,.011f,-.14f),.25f,.036f);
            var tie=Group("spago",p);
            Part(tie,"legatura",V(.036f,.161f,0),V(.005f,.004f,.30f),Paper);
            Part(tie,"filo_frontale",V(.036f,.081f,-.143f),V(.005f,.16f,.004f),Paper);
            Ring(tie,"nodo",V(.036f,.165f,0),.014f,.002f,Paper,8);
            foreach(float x in new[]{-.11f,-.025f})
            {
                Part(p,"suola_scarpa",V(x,.029f,.012f),V(.067f,.015f,.135f),Ink);
                Cone(p,"scarpa_bambino",V(x,.037f,.012f),.036f,.047f,Leather,8,.027f).transform.localScale=V(1,1,1.7f);
                Cone(p,"interno_scarpa",V(x,.0845f,.033f),.019f,.001f,Ink,8,.019f).transform.localScale=V(1,1,1.25f);
                Part(p,"laccetto_scarpa",V(x,.079f,.004f),V(.04f,.004f,.007f),Paper);
            }
            var clip=Box(p,"fermaglio_celluloide",V(.108f,.032f,.059f),V(.067f,.009f,.017f),Mat("oggetti_celluloide","#AD6E67"),false);
            clip.transform.localRotation=Quaternion.Euler(0,-25,0);
            Part(p,"carta_ripiegata",V(.113f,.05f,-.056f),V(.116f,.062f,.116f),Paper);
            var bracelet=Instance("braccialetto",p,V(.113f,.071f,-.056f));
            // Box collider must leave the open top accessible to the nested bracelet.
        }
        static void Jacket(Transform p)
        {
            var cloth=Mat("oggetti_fustagno","#687369");
            Mesh(p,"corpo_fustagno",new[]{V(-.2f,.02f,-.28f),V(.2f,.02f,-.28f),V(.17f,.02f,.28f),V(-.17f,.02f,.28f),V(-.18f,.09f,-.28f),V(.18f,.09f,-.28f),V(.15f,.065f,.28f),V(-.15f,.065f,.28f)},new[]{0,4,1,1,4,5,4,7,5,5,7,6,1,5,2,2,5,6,2,6,3,3,6,7,3,7,0,0,7,4,0,1,2,0,2,3},cloth,false);
            foreach(float side in new[]{-1f,1f})
            {
                var sleeve=Box(p,"manica_ripiegata",V(side*.22f,.065f,.015f),V(.14f,.065f,.39f),cloth,false);
                sleeve.transform.localRotation=Quaternion.Euler(0,side*18,0);
                var lapel=Box(p,"risvolto",V(side*.065f,.104f,-.17f),V(.082f,.018f,.19f),Mat("oggetti_fustagno_ombra","#46594F"),false);
                lapel.transform.localRotation=Quaternion.Euler(0,side*22,0);
                Part(p,"tasca",V(side*.097f,.082f,.13f),V(.091f,.012f,.095f),cloth);
                Part(p,"bordo_tasca",V(side*.097f,.09f,.084f),V(.093f,.008f,.009f),Ink);
            }
            for(int i=0;i<4;i++) Cone(p,"bottone",V(.015f,.088f,-.02f+i*.065f),.008f,.006f,Leather,8,.008f);
            Part(p,"etichetta_interna",V(0,.099f,-.25f),V(.06f,.003f,.024f),Paper);
        }
        static void Memorial(Transform p)
        {
            Part(p,"basamento",V(0,.10f,0),V(1.55f,.20f,.60f),Stone);
            Part(p,"pietra_commemorativa",V(0,1.0f,0),V(1.34f,1.7f,.22f),Plaster);
            Part(p,"cimasa",V(0,1.87f,0),V(1.42f,.08f,.28f),Stone);
            string[] lines={"14 OTTOBRE 1966","PIETRO FERRO","GIUSEPPE BOASSO","MICHELE AIMAR","PICCOLA ELENA VALLI","IL PAESE DI SAN ROCCO"};
            for(int i=0;i<lines.Length;i++) Label(p,lines[i],V(0,1.58f-i*.225f,-.112f),1.12f,.072f,false);
            Part(p,"vaso_fiori",V(.53f,.25f,-.22f),V(.16f,.23f,.16f),Red);
            for(int i=0;i<3;i++) Cone(p,"fiore",V(.48f+i*.04f,.39f,-.22f),.045f,.025f,Paper,6,.016f);
        }
    }
}
