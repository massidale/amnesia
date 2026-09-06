using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmnesiaUnity
{
    public sealed class Viaggio1987 : MonoBehaviour
    {
        public string DestinazioneScena;
        public string Destinazione;
        public Transform PuntoSalita;
        public Transform PuntoArrivo;
        public Transform PostoPasseggero;
        static string arrivo;
        static bool inViaggio;
        Behaviour visitatore;
        float oscuramento;
        string errore;
        public bool InViaggio => inViaggio;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetTrip() { arrivo=null;inViaggio=false; }
        IEnumerator Start()
        {
            yield return null; // Giocatore creates its camera and controller in Start.
            visitatore=FindFirstObjectByType<Giocatore>();
            if(!visitatore) visitatore=FindFirstObjectByType<FreeWalk>();
            inViaggio=false;
            if(arrivo==SceneManager.GetActiveScene().name && visitatore && PuntoArrivo) {
                if(visitatore is Giocatore player) player.Posiziona(PuntoArrivo.position,PuntoArrivo.rotation);
                else {
                    var cc=visitatore.GetComponent<CharacterController>();cc.enabled=false;
                    visitatore.transform.SetPositionAndRotation(PuntoArrivo.position,PuntoArrivo.rotation);
                    cc.enabled=true;
                }
                arrivo=null;
            }
        }
        bool Vicino => visitatore && (!(visitatore is Giocatore player) || player.LiberoPerViaggio)
            && PuntoSalita && Vector3.Distance(visitatore.transform.position,PuntoSalita.position)<3;
        void Update() { if(!inViaggio && Vicino && Input.GetKeyDown(KeyCode.E)) Parti(); }
        public void Parti()
        {
            if(inViaggio || !Vicino) return;
            if(!Application.CanStreamedLevelBeLoaded(DestinazioneScena)) { errore="Collegamento non disponibile";return; }
            if(!PostoPasseggero || !PuntoArrivo) { errore="Corriera non disponibile";return; }
            StartCoroutine(Percorso());
        }
        IEnumerator Percorso()
        {
            inViaggio=true;visitatore.enabled=false;
            visitatore.GetComponent<CharacterController>().enabled=false;
            visitatore.GetComponentInChildren<Camera>().transform.localRotation=Quaternion.identity;
            var start=transform.position;
            for(float time=0;time<3.5f;time+=Time.unscaledDeltaTime) {
                transform.position=start-transform.forward*(time*time*.55f);
                visitatore.transform.SetPositionAndRotation(PostoPasseggero.position,PostoPasseggero.rotation);
                oscuramento=Mathf.Clamp01((time-1.5f)/1.5f);yield return null;
            }
            oscuramento=1;arrivo=DestinazioneScena;
            var gioco=FindFirstObjectByType<Bootstrap>();
            if(gioco && gioco.Session!=null) {
                gioco.Session.World.Minute+=120;
                gioco.ConservaPerViaggio();
            }
            yield return SceneManager.LoadSceneAsync(DestinazioneScena,LoadSceneMode.Single);
        }
        void OnGUI()
        {
            if(!inViaggio && Vicino) {
                var width=Mathf.Min(320,Screen.width-24);var r=new Rect((Screen.width-width)/2,Screen.height-86,width,54);
                if(GUI.Button(r,errore??("Corriera per "+Destinazione))) Parti();
            }
            if(oscuramento>0) {
                var previous=GUI.color;GUI.color=new Color(0,0,0,oscuramento);
                GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=previous;
                if(oscuramento>.9f) GUI.Label(new Rect(Screen.width/2-120,Screen.height/2,240,40),"Due ore dopo - "+Destinazione);
            }
        }
    }
}
