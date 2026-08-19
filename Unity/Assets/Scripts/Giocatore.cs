using Amnesia.Core;
using UnityEngine;

namespace AmnesiaUnity
{
    /// Cammina, e si ferma quando si parla. La telecamera sta negli occhi e
    /// scende in faccia a chi hai davanti quando comincia una conversazione: in
    /// un gioco in cui la meccanica e' guardare in faccia uno che mente,
    /// l'inquadratura non e' un dettaglio di regia.
    public sealed class Giocatore : MonoBehaviour
    {
        public const float PortataDiParola = 2.5f;

        private const float Passo = 4.5f;
        private const float Sensibilita = 2.2f;
        private const float GradiAlSecondo = 110f;
        private const float PendenzaMassima = 75f;

        /// Dove stanno gli occhi quando il paese e' tuo. L'inquadratura della
        /// conversazione sposta la telecamera nel mondo, quindi questo e' il
        /// posto a cui deve tornare: senza, ogni chiacchierata te la lasciava
        /// dove l'aveva messa l'ultima faccia.
        private static readonly Vector3 SedeDellOcchio = new Vector3(0f, 0.6f, 0f);

        private Bootstrap _gioco;
        private Camera _occhio;
        private Pannello _pannello;
        private Transform _bersaglio;
        private float _imbardata;
        private float _beccheggio;

        private void Start()
        {
            _gioco = FindFirstObjectByType<Bootstrap>();
            _pannello = FindFirstObjectByType<Pannello>();

            var posizione = _gioco.World.ActorOf("player").Position ?? new Cell(0, 0);
            transform.position = _gioco.InScena(posizione) + Vector3.up * 0.9f;

            // Il paese sta verso -Z, e un giocatore che nasce con rotazione zero
            // guarda verso +Z: di spalle a tutto. E' quello che faceva sembrare
            // W invertito — non lo era, camminava solo fuori dalla mappa.
            _imbardata = 180f;
            transform.rotation = Quaternion.Euler(0f, _imbardata, 0f);

            _occhio = new GameObject("occhio").AddComponent<Camera>();
            _occhio.transform.SetParent(transform, false);
            _occhio.transform.localPosition = SedeDellOcchio;
            _occhio.fieldOfView = 62f;

            Libera(false);
        }

        /// Il puntatore sparisce mentre si cammina e torna quando si parla,
        /// perche' nel pannello bisogna poter scrivere e cliccare.
        private static void Libera(bool libero)
        {
            Cursor.lockState = libero ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = libero;
        }

        private void Update()
        {
            if (_gioco == null || !_gioco.enabled)
            {
                return;
            }
            if (_pannello != null && _pannello.Aperto)
            {
                Libera(true);
                InquadraIlVolto();
                return;
            }
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Libera(false);
            }

            Guarda();
            Cammina();

            // Il nome compare avvicinandosi, non dopo aver chiesto: chi vive in
            // un paese sa gia' chi ha davanti.
            var chi = _gioco.PiuVicino(transform.position, PortataDiParola);
            _pannello.Suggerisci(chi);
            if (Input.GetKeyDown(KeyCode.E) && !string.IsNullOrEmpty(chi))
            {
                _bersaglio = GameObject.Find(chi).transform;
                _pannello.Apri(chi);
            }
        }

        /// Mouse per guardarsi intorno, frecce per chi preferisce i tasti. Il
        /// beccheggio sta sulla telecamera e l'imbardata sul corpo: girare la
        /// testa in su non deve inclinare il paese.
        private void Guarda()
        {
            var giroTasti = (Premuto(KeyCode.RightArrow) - Premuto(KeyCode.LeftArrow)) * GradiAlSecondo * Time.deltaTime;
            var alzataTasti = (Premuto(KeyCode.DownArrow) - Premuto(KeyCode.UpArrow)) * GradiAlSecondo * Time.deltaTime;

            _imbardata += Input.GetAxisRaw("Mouse X") * Sensibilita + giroTasti;
            _beccheggio = Mathf.Clamp(
                _beccheggio - Input.GetAxisRaw("Mouse Y") * Sensibilita + alzataTasti,
                -PendenzaMassima, PendenzaMassima);

            transform.rotation = Quaternion.Euler(0f, _imbardata, 0f);
            // Posizione e rotazione insieme, ogni fotogramma: e' quello che
            // disfa l'inquadratura della conversazione appena si torna a
            // camminare, qualunque cosa le avesse fatto.
            _occhio.transform.localPosition = SedeDellOcchio;
            _occhio.transform.localRotation = Quaternion.Euler(_beccheggio, 0f, 0f);
        }

        /// I tasti si leggono uno per uno e non con GetAxis: gli assi di Unity
        /// includono anche le frecce, e non c'e' modo di distinguerle da WASD.
        private void Cammina()
        {
            var avanti = Premuto(KeyCode.W) - Premuto(KeyCode.S);
            var lato = Premuto(KeyCode.D) - Premuto(KeyCode.A);
            var verso = transform.forward * avanti + transform.right * lato;
            if (verso.sqrMagnitude > 0.001f)
            {
                transform.position += verso.normalized * (Passo * Time.deltaTime);
            }
        }

        private static float Premuto(KeyCode tasto) => Input.GetKey(tasto) ? 1f : 0f;

        /// Mentre si parla la telecamera va addosso, e ci resta. Il resto del
        /// paese smette di esistere: e' l'unica cosa che questa scena di prova
        /// deve dimostrare.
        private void InquadraIlVolto()
        {
            if (_bersaglio == null)
            {
                return;
            }
            var verso = _bersaglio.position + Vector3.up * 0.35f;
            var da = verso - (verso - transform.position).normalized * 1.6f;
            _occhio.transform.position = Vector3.Lerp(_occhio.transform.position, da + Vector3.up * 0.2f, Time.deltaTime * 4f);
            _occhio.transform.rotation = Quaternion.Slerp(
                _occhio.transform.rotation, Quaternion.LookRotation(verso - _occhio.transform.position), Time.deltaTime * 5f);
        }
    }
}
