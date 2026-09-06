using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AmnesiaUnity.Editor.SanRocco
{
    public static class MapCheck
    {
        [MenuItem("Amnesia/San Rocco 1987/Verifica etichette mappa in Play")]
        public static void Check()
        {
            if(!EditorApplication.isPlaying) { Debug.LogError("Avviare Play prima del controllo mappa");return; }
            UnityEngine.Object.FindFirstObjectByType<AmnesiaUnity.Menu>().Apri(1);
            Canvas.ForceUpdateCanvases();
            var labels=UnityEngine.Object.FindObjectsByType<EtichettaMappa>(FindObjectsSortMode.None);
            if(labels.Length==0 || labels.Any(l=>l.GetComponent<Text>().fontSize!=10)) throw new Exception("Nomi della mappa assenti o troppo grandi");
            for(int i=0;i<labels.Length;i++) for(int j=i+1;j<labels.Length;j++) {
                var a=(RectTransform)labels[i].transform;var b=(RectTransform)labels[j].transform;
                if(new Rect(a.anchoredPosition-a.sizeDelta/2,a.sizeDelta).Overlaps(new Rect(b.anchoredPosition-b.sizeDelta/2,b.sizeDelta)))
                    throw new Exception("Etichette sovrapposte: "+labels[i].GetComponent<Text>().text+" / "+labels[j].GetComponent<Text>().text);
            }
            var label=labels[0];label.OnPointerEnter(null);
            double finish=EditorApplication.timeSinceStartup+.4;
            void Tick() {
                if(EditorApplication.timeSinceStartup<finish) return;
                EditorApplication.update-=Tick;
                if(!EditorApplication.isPlaying || !label) return;
                if(label.transform.localScale.x<1.025f || label.transform.localScale.x>1.05f) throw new Exception("Hover non applicato o eccessivo");
                string folder=Path.GetFullPath("../artifacts/sopralluogo");Directory.CreateDirectory(folder);
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"mappa_stilizzata_gioco.png"));
                label.OnPointerExit(null);
                Debug.Log("MAPPA_UI_OK: "+labels.Length+" nomi a 10 px, hover animato e punti indipendenti");
            }
            EditorApplication.update+=Tick;
        }
    }
}
