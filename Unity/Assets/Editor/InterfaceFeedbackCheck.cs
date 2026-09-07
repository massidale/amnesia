using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class InterfaceFeedbackCheck
    {
        public static void Run()
        {
            foreach (var id in Items1987.Ids) {
                var image = new Texture2D(2,2);
                image.LoadImage(File.ReadAllBytes(Items1987.Folder+"/Icone/"+id+".png"));
                CheckAlpha(image, id);
                Object.DestroyImmediate(image);
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var preview = new GameObject("preview",typeof(RectTransform),typeof(RawImage)).AddComponent<AnteprimaOggetto>();
            preview.Mostra("chiave_b17");
            var rt = (RenderTexture)preview.GetComponent<RawImage>().texture;
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var pixels = new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
            pixels.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); pixels.Apply();
            RenderTexture.active = previous;
            CheckAlpha(pixels,"anteprima 3D");
            Object.DestroyImmediate(pixels);
            var panel = new GameObject("dialogo").AddComponent<Pannello>();
            Call(panel,"Costruisci");
            Call(panel,"ImpostaAttesa",true);
            var label = Field<Text>(panel,"_attesa");
            var input = Field<InputField>(panel,"_campo");
            if(!label.gameObject.activeSelf || input.interactable) throw new Exception("Attesa non visibile o input attivo");
            Call(panel,"AggiornaAttesa",0f); var first=label.text;
            Call(panel,"AggiornaAttesa",.45f);
            if(first==label.text) throw new Exception("Indicatore non animato");
            Field<GameObject>(panel,"_radice").SetActive(true);
            Field<Text>(panel,"_chi").text="Rosa";
            Field<Text>(panel,"_detto").text="Giorgio: Buongiorno, Rosa.";
            var canvas=label.GetComponentInParent<Canvas>();
            preview.transform.SetParent(canvas.transform,false);
            Stile.Ancora((RectTransform)preview.transform,new Vector2(.05f,.52f),new Vector2(.29f,.947f));
            for(int i=0;i<Items1987.Ids.Length;i++) {
                var icon=new GameObject("icona",typeof(RectTransform),typeof(RawImage));
                icon.transform.SetParent(canvas.transform,false);
                float x=.4f+(i%6)*.09f,y=.56f+(i/6)*.20f;
                Stile.Ancora((RectTransform)icon.transform,new Vector2(x,y),new Vector2(x+.08f,y+.14f));
                icon.GetComponent<RawImage>().texture=Resources.Load<Texture2D>("Oggetti1987/Icone/"+Items1987.Ids[i]);
            }
            Capture(canvas,"/tmp/amnesia-ui-attesa.png");
            var status=Field<Text>(panel,"_stato"); status.text="errore di prova";
            Call(panel,"ImpostaAttesa",false);
            if(label.gameObject.activeSelf || !input.interactable || status.text!="errore di prova") throw new Exception("Fine attesa nasconde errore o blocca input");
            Capture(canvas,"/tmp/amnesia-ui-errore.png");
            Debug.Log("INTERFACE_FEEDBACK_OK: 11 icone, anteprima trasparente, attesa animata e ripristino input");
        }
        static void Capture(Canvas canvas,string path)
        {
            var camera=new GameObject("verifica UI").AddComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Stile.Pannello;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            var rt=new RenderTexture(1600,900,24);rt.Create();camera.targetTexture=rt;
            Canvas.ForceUpdateCanvases();camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1600,900,TextureFormat.RGBA32,false);
            image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            Object.DestroyImmediate(image);rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(camera.gameObject);
        }
        static void CheckAlpha(Texture2D image,string name)
        {
            if(image.GetPixel(0,0).a>.01f || image.GetPixel(image.width-1,image.height-1).a>.01f) throw new Exception(name+": sfondo opaco");
            if(!Array.Exists(image.GetPixels32(),p=>p.a>200)) throw new Exception(name+": oggetto invisibile");
        }
        static T Field<T>(object o,string name) => (T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
        static void Call(object o,string name,params object[] args)
        {
            var method=o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic);
            if(method==null) throw new Exception("Metodo mancante: "+name);
            method.Invoke(o,args);
        }
    }
}
