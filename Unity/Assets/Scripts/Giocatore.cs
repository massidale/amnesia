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

        private const float Passo = 3.6f;
        private const float Gravita = 18f;
        private const float Sensibilita = 2.2f;
        private const float GradiAlSecondo = 110f;
        private const float PendenzaMassima = 75f;
        private const float AltezzaDelVolto = 1.55f;
        private static readonly Vector3 SpallaDestra = new Vector3(0.45f, 1.75f, -2.6f);

        /// Dove stanno gli occhi quando il paese e' tuo. L'inquadratura della
        /// conversazione sposta la telecamera nel mondo, quindi questo e' il
        /// posto a cui deve tornare: senza, ogni chiacchierata te la lasciava
        /// dove l'aveva messa l'ultima faccia.
        private static readonly Vector3 SedeDellOcchio = new Vector3(0f, 1.62f, 0f);

        private Bootstrap _gioco;
        private Camera _occhio;
        private Pannello _pannello;
        private Menu _menu;
        private Transform _bersaglio;
        private CharacterController _corpo;
        private float _caduta;
        private Transform _figura;
        private bool _diSpalle;
        private float _imbardata;
        private float _beccheggio;

        private void Start()
        {
            _gioco = FindFirstObjectByType<Bootstrap>();
            _pannello = FindFirstObjectByType<Pannello>();
            // Se la scena non ce l'ha, il menu se lo fa da solo: la scena di
            // prova e' tre oggetti vuoti, e non deve diventare una cosa da
            // tenere allineata a mano ogni volta che si aggiunge una schermata.
            _menu = FindFirstObjectByType<Menu>();
            if (_menu == null)
            {
                _menu = new GameObject("menu").AddComponent<Menu>();
            }

            var posizione = _gioco.World.ActorOf("player").Position ?? new Cell(0, 0);
            transform.position = _gioco.InScena(posizione) + Vector3.up * 0.2f;

            // Un corpo, finalmente: i muri fermano, la montagna ferma, e un
            // paese in cui si passa attraverso le case non ha ne' dentro ne'
            // fuori — e questo gioco e' tutto un entrare in casa d'altri.
            _corpo = gameObject.AddComponent<CharacterController>();
            _corpo.height = 1.75f;
            _corpo.radius = 0.28f;
            _corpo.center = new Vector3(0f, 0.88f, 0f);
            _corpo.slopeLimit = 50f;
            _corpo.stepOffset = 0.35f;

            // Il paese sta verso -Z, e un giocatore che nasce con rotazione zero
            // guarda verso +Z: di spalle a tutto. E' quello che faceva sembrare
            // W invertito — non lo era, camminava solo fuori dalla mappa.
            _imbardata = 180f;
            transform.rotation = Quaternion.Euler(0f, _imbardata, 0f);

            _occhio = new GameObject("occhio").AddComponent<Camera>();
            _occhio.transform.SetParent(transform, false);
            _occhio.transform.localPosition = SedeDellOcchio;
            _occhio.fieldOfView = 62f;
            _occhio.clearFlags = CameraClearFlags.SolidColor;
            _occhio.backgroundColor = Scenografia.ColoreDelCielo;
            _occhio.farClipPlane = 120f;

            // Il corpo di Giorgio esiste sempre e si vede solo di spalle: in
            // prima persona guardarsi addosso vuol dire vedersi il collo da
            // dentro.
            _figura = Bootstrap.Figura("giorgio", "c").transform;
            _figura.name = "giorgio";
            _figura.SetParent(transform, false);
            _figura.localPosition = Vector3.zero;
            // Il corpo si vede, non urta: chi urta e' il CharacterController, e
            // due collisori sulla stessa persona si spingono a vicenda.
            foreach (var urto in _figura.GetComponentsInChildren<Collider>())
            {
                Destroy(urto);
            }
            Mostra(false);

            Libera(false);
        }

        /// La terza persona non e' un vezzo: in un gioco in cui si sta fermi a
        /// guardare in faccia qualcuno, vedere anche se stessi cambia cosa si
        /// sta guardando. Si passa da una all'altra con V.
        private void Mostra(bool diSpalle)
        {
            _diSpalle = diSpalle;
            if (_figura != null)
            {
                foreach (var pezzo in _figura.GetComponentsInChildren<Renderer>())
                {
                    pezzo.enabled = diSpalle;
                }
            }
            _occhio.transform.localPosition = diSpalle ? DietroLaSpalla() : SedeDellOcchio;
        }

        /// Dove sta davvero la telecamera in terza persona: dietro la spalla, o
        /// piu' vicino se in mezzo c'e' un muro. Senza questo, chi si mette con
        /// la schiena a una casa si ritrova a guardare il paese da dentro
        /// l'intonaco.
        private Vector3 DietroLaSpalla()
        {
            var testa = transform.TransformPoint(new Vector3(0f, SpallaDestra.y, 0f));
            var voluta = transform.TransformPoint(SpallaDestra);
            var verso = voluta - testa;
            // Solo la scenografia: le figure e il proprio corpo non contano, o
            // la telecamera scatterebbe in avanti ogni volta che passi accanto
            // a qualcuno.
            if (Physics.SphereCast(testa, 0.22f, verso.normalized, out var urto, verso.magnitude,
                    ~0, QueryTriggerInteraction.Ignore)
                && urto.collider.GetComponentInParent<Giocatore>() == null)
            {
                var quanto = Mathf.Max(urto.distance - 0.15f, 0.1f);
                return transform.InverseTransformPoint(testa + verso.normalized * quanto);
            }
            return SpallaDestra;
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
            if (_menu != null && _menu.Aperto)
            {
                Libera(true);
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _menu.Chiudi();
                }
                else if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.RightArrow))
                {
                    _menu.Scorri(1);
                }
                else if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    _menu.Scorri(-1);
                }
                else if (Input.GetKeyDown(KeyCode.I))
                {
                    _menu.Alterna(0);
                }
                else if (Input.GetKeyDown(KeyCode.M))
                {
                    _menu.Alterna(1);
                }
                else if (Input.GetKeyDown(KeyCode.T))
                {
                    _menu.Alterna(2);
                }
                return;
            }
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Libera(false);
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                Mostra(!_diSpalle);
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _menu.Apri();
                return;
            }
            if (Input.GetKeyDown(KeyCode.I))
            {
                _menu.Apri(0);
                return;
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                _menu.Apri(1);
                return;
            }
            if (Input.GetKeyDown(KeyCode.T))
            {
                _menu.Apri(2);
                return;
            }

            Guarda();
            Cammina();

            // Il nome compare avvicinandosi, non dopo aver chiesto: chi vive in
            // un paese sa gia' chi ha davanti. Una porta chiusa si annuncia
            // allo stesso modo, ed e' l'unica cosa che in questo gioco non si
            // apre parlando.
            var porta = _gioco.PortaVicina(transform.position, PortataDiParola);
            var chi = _gioco.PiuVicino(transform.position, PortataDiParola);
            if (!string.IsNullOrEmpty(porta))
            {
                _pannello.Suggerimento($"E   {_gioco.DescrizioneDellaPorta(porta)}");
            }
            else
            {
                _pannello.Suggerimento(string.IsNullOrEmpty(chi) ? "" : $"E   parla con {_gioco.NomeDi(chi)}");
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (!string.IsNullOrEmpty(porta))
                {
                    Prova(porta);
                }
                else if (!string.IsNullOrEmpty(chi))
                {
                    _bersaglio = GameObject.Find(chi).transform;
                    _pannello.Apri(chi);
                }
            }
        }

        /// Aprire una porta. Il rifiuto non e' un errore da nascondere: e'
        /// l'unico modo che ha il gioco di dirti cosa ti manca senza dirti dove
        /// andare a prenderlo.
        private void Prova(string porta)
        {
            var esito = _gioco.Porte.Apri(_gioco.Session.World, porta);
            if (!esito.IsOk)
            {
                _pannello.Avviso(Rifiuto(esito.Code), true);
                return;
            }
            _gioco.SpalancaLaPorta(porta);
            var racconto = esito.Value.Racconto;
            if (esito.Value.Presi.Count > 0)
            {
                var nomi = new System.Collections.Generic.List<string>();
                foreach (var id in esito.Value.Presi)
                {
                    var oggetto = _gioco.Items.Find(id);
                    nomi.Add(oggetto == null || string.IsNullOrEmpty(oggetto.Name) ? id : oggetto.Name);
                }
                racconto += "\n\nPrendi: " + string.Join(", ", nomi);
            }
            _pannello.Avviso(racconto);
        }

        private static string Rifiuto(string codice)
        {
            switch (codice)
            {
                case "needs_item":
                    return "Chiusa. La chiave che serve non ce l'hai.";
                case "needs_knowledge":
                    return "Saracinesche uguali, le targhette tolte.\nQuale sia la B-17 non lo sai.";
                case "already_open":
                    return "E' gia' aperta.";
                default:
                    return "Qui non c'e' niente da aprire.";
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
            _occhio.transform.localPosition = _diSpalle ? DietroLaSpalla() : SedeDellOcchio;
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
                verso = verso.normalized * Passo;
            }
            else
            {
                verso = Vector3.zero;
            }
            // La gravita' serve anche in piano: senza, il controller resta
            // appeso al primo gradino che sale e non ridiscende piu'.
            _caduta = _corpo.isGrounded ? -1f : _caduta - Gravita * Time.deltaTime;
            verso.y = _caduta;
            _corpo.Move(verso * Time.deltaTime);
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
            // Fra due facce, non fra due paia di piedi. Le figure — e il
            // giocatore — hanno l'origine a terra: prendere `transform.position`
            // come punto di vista significa guardare l'altro dal selciato.
            var verso = _bersaglio.position + Vector3.up * AltezzaDelVolto;
            var occhi = transform.position + Vector3.up * SedeDellOcchio.y;
            var da = verso - (verso - occhi).normalized * 1.5f;
            _occhio.transform.position = Vector3.Lerp(_occhio.transform.position, da, Time.deltaTime * 4f);
            _occhio.transform.rotation = Quaternion.Slerp(
                _occhio.transform.rotation, Quaternion.LookRotation(verso - _occhio.transform.position), Time.deltaTime * 5f);
        }
    }
}
