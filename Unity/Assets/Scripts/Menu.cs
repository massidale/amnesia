using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AmnesiaUnity
{
    /// La pausa, con tre pagine: cosa hai in tasca, la pianta del paese, il
    /// taccuino.
    ///
    /// Stanno insieme perche' sono la stessa cosa vista da tre lati — un
    /// oggetto senza una riga che lo spieghi non vuol dire niente, una riga
    /// senza l'oggetto in mano non si puo' mettere in faccia a nessuno, e
    /// nessuna delle due serve se non sai dove sta la bottega.
    ///
    /// Il tempo qui non scorre e non c'e' niente da fermare: in Amnesia
    /// l'orologio avanza a ogni battuta, quindi la pausa e' esattamente cio'
    /// che sembra — il gioco fermo ad aspettarti.
    public sealed partial class Menu : MonoBehaviour
    {
        private const float ZoomMinimo = 4f;
        private const float ZoomMassimo = 26f;

        private enum Pagina { Tasche, Paese, Taccuino }

        public bool Aperto { get; private set; }

        private Bootstrap _gioco;
        private Transform _giocatore;
        private GameObject _radice;
        private Text _foglio;
        private ScrollRect _rullo;
        private Text _coda;
        private GameObject _pianta;
        private RectTransform _vista;
        private RectTransform _carta;
        private RectTransform _iosono;
        private Text _scalaMappa;
        private bool _mappaInquadrata;
        private readonly List<(RectTransform Etichetta, Vector3 Mondo)> _didascalie =
            new List<(RectTransform, Vector3)>();
        private readonly Dictionary<RectTransform,RectTransform> _segnaposti = new Dictionary<RectTransform,RectTransform>();
        private float _zoom = 12f;
        private Vector3 _presa;
        private bool _trascino;
        private readonly Dictionary<Pagina, Text> _linguette = new Dictionary<Pagina, Text>();
        private readonly Dictionary<Pagina, RectTransform> _sottolineature = new Dictionary<Pagina, RectTransform>();
        private Pagina _pagina = Pagina.Tasche;

        private void Start()
        {
            _gioco = FindFirstObjectByType<Bootstrap>();
            _giocatore = FindFirstObjectByType<Giocatore>()?.transform;
            if (_gioco == null || !_gioco.enabled)
            {
                enabled = false;
                return;
            }
            Costruisci();
        }

        public void Apri(int pagina = -1)
        {
            if (_radice == null)
            {
                return;
            }
            if (pagina >= 0)
            {
                _pagina = (Pagina)pagina;
            }
            Aperto = true;
            _radice.SetActive(true);
            Mostra();
        }

        public void Chiudi()
        {
            Aperto = false;
            _trascino = false;
            if (_radice != null)
            {
                _radice.SetActive(false);
            }
        }

        public void Alterna(int pagina)
        {
            if (Aperto && (int)_pagina == pagina)
            {
                Chiudi();
                return;
            }
            Apri(pagina);
        }

        /// La prima volta che si apre la pianta si guarda dove si e'.
        private void InquadraIlGiocatore()
        {
            if (_giocatore == null)
            {
                return;
            }
            _carta.anchoredPosition = -PostoSuCarta(_giocatore.position);
            Trattieni();
        }

        /// Le pagine girano con Tab e con le frecce, come in un menu di allora.
        public void Scorri(int verso)
        {
            _pagina = (Pagina)(((int)_pagina + verso + 3) % 3);
            Mostra();
        }

        private void Mostra()
        {
            _rullo.gameObject.SetActive(_pagina == Pagina.Taccuino);
            _tasche.SetActive(_pagina == Pagina.Tasche);
            _pianta.SetActive(_pagina == Pagina.Paese);
            if (_pagina == Pagina.Paese)
            {
                if (_zoom <= 0f)
                {
                    _zoom = 12f;
                }
                Ridisegna();
                if (!_mappaInquadrata)
                {
                    Canvas.ForceUpdateCanvases();
                    Ridisegna();
                    _zoom = Mathf.Clamp(_zoom * Mathf.Min(_vista.rect.width / _carta.sizeDelta.x,
                        _vista.rect.height / _carta.sizeDelta.y) * .94f, ZoomMinimo, ZoomMassimo);
                    Ridisegna();
                    InquadraIlGiocatore();
                    _mappaInquadrata = true;
                }
            }
            if (_pagina == Pagina.Tasche)
            {
                AggiornaTasche();
            }
            else if (_pagina == Pagina.Taccuino)
            {
                _foglio.text = Inventario.Righe(_gioco);
            }
            if (_pagina == Pagina.Taccuino)
            {
                // Una pagina si apre in cima, sempre.
                Canvas.ForceUpdateCanvases();
                _rullo.verticalNormalizedPosition = 1f;
            }
            _coda.text = _pagina == Pagina.Paese
                ? "M  CHIUDI MAPPA     TAB  CAMBIA SCHEDA     WASD  SPOSTA     ROTELLA  ZOOM"
                : "ESC  TORNA AL GIOCO     TAB / ← →  CAMBIA SCHEDA     ↑ ↓ / ROTELLA  SCORRI";
            foreach (var linguetta in _linguette)
            {
                var scelta = linguetta.Key == _pagina;
                linguetta.Value.color = scelta ? Stile.Carta : Stile.Grafite;
                _sottolineature[linguetta.Key].gameObject.SetActive(scelta);
            }
        }

        private void Update()
        {
            if (!Aperto)
            {
                return;
            }
            if (_pagina != Pagina.Paese)
            {
                // La rotella scorre solo dove sta il puntatore; le frecce
                // funzionano ovunque, e qui su e giu' non servono ad altro.
                var passo = (Premuto(KeyCode.UpArrow) - Premuto(KeyCode.DownArrow)) * Time.unscaledDeltaTime * 0.9f;
                if (passo != 0f)
                {
                    var rullo = _pagina == Pagina.Tasche ? _dettaglioRullo : _rullo;
                    rullo.verticalNormalizedPosition = Mathf.Clamp01(rullo.verticalNormalizedPosition + passo);
                }
                return;
            }
            if (_giocatore != null)
            {
                _iosono.anchoredPosition = PostoSuCarta(_giocatore.position);
            }

            var rotella = Input.mouseScrollDelta.y;
            if (Mathf.Abs(rotella) > 0.01f && RectTransformUtility.RectangleContainsScreenPoint(_vista, Input.mousePosition))
            {
                _zoom = Mathf.Clamp(_zoom * (1f + rotella * 0.12f), ZoomMinimo, ZoomMassimo);
                Ridisegna();
            }

            // Si trascina con il mouse, o con le frecce per chi ha le mani sulla
            // tastiera. Le frecce qui non cambiano pagina: cambiano vista.
            if (Input.GetMouseButtonDown(0) && RectTransformUtility.RectangleContainsScreenPoint(_vista, Input.mousePosition))
            {
                _presa = Input.mousePosition;
                _trascino = true;
            }
            if (Input.GetMouseButtonUp(0))
            {
                _trascino = false;
            }
            if (_trascino)
            {
                var scarto = Input.mousePosition - _presa;
                _presa = Input.mousePosition;
                _carta.anchoredPosition += new Vector2(scarto.x, scarto.y) / _radice.GetComponentInParent<Canvas>().scaleFactor;
                Trattieni();
            }
            else
            {
                var passo = 420f * Time.unscaledDeltaTime;
                var mossa = new Vector2(
                    (Premuto(KeyCode.A) - Premuto(KeyCode.D)) * passo,
                    (Premuto(KeyCode.S) - Premuto(KeyCode.W)) * passo);
                if (mossa != Vector2.zero)
                {
                    _carta.anchoredPosition += mossa;
                    Trattieni();
                }
            }
        }

        private static float Premuto(KeyCode tasto) => Input.GetKey(tasto) ? 1f : 0f;

        private void Costruisci()
        {
            var canvas = Stile.Tela("menu", 10);
            _radice = Stile.Riquadro(canvas.transform, "pagina", Stile.Velo, Vector2.zero, Vector2.one).gameObject;
            Stile.Riquadro(_radice.transform, "cornice", Stile.Incavo, new Vector2(.035f,.095f), new Vector2(.965f,.84f));
            Stile.Riquadro(_radice.transform, "interno", Stile.Notte, new Vector2(.037f,.098f), new Vector2(.963f,.837f));

            var occhiello = Stile.Scritta(_radice.transform, Stile.Interfaccia, 14, Stile.Grafite,
                new Vector2(0.07f, 0.925f), new Vector2(0.5f, 0.965f));
            occhiello.text = "AMNESIA   /   GIORGIO LIPARI";
            Pulsante(_radice.transform, "TORNA AL GIOCO  [ESC]", new Vector2(.72f,.93f), new Vector2(.94f,.975f), Chiudi);

            var barra = new GameObject("linguette", typeof(RectTransform));
            barra.transform.SetParent(_radice.transform, false);
            var disposizione = barra.AddComponent<HorizontalLayoutGroup>();
            disposizione.spacing = 34f;
            disposizione.childForceExpandWidth = false;
            disposizione.childControlWidth = true;
            disposizione.childControlHeight = true;
            disposizione.childAlignment = TextAnchor.MiddleLeft;
            Stile.Ancora((RectTransform)barra.transform, new Vector2(0.07f, 0.855f), new Vector2(0.93f, 0.915f));

            Linguetta(barra.transform, Pagina.Tasche, "INVENTARIO", "I");
            Linguetta(barra.transform, Pagina.Paese, "MAPPA", "M");
            Linguetta(barra.transform, Pagina.Taccuino, "TACCUINO", "T");

            Stile.Filo(_radice.transform, new Vector2(0.07f, 0.845f), new Vector2(0.93f, 0.848f), Stile.Incavo);

            // Le tasche e il taccuino crescono: sette oggetti e trenta righe di
            // dichiarazioni non stanno in una schermata, e quello che non ci
            // sta va potuto scorrere invece che tagliato via.
            _foglio = Stile.Rullo(_radice.transform, Stile.Macchina, 16, Stile.Carta,
                new Vector2(0.09f, 0.13f), new Vector2(0.91f, 0.79f), out _rullo);

            CostruisciTasche();

            CostruisciLaPianta();

            _coda = Stile.Scritta(_radice.transform, Stile.Interfaccia, 14, Stile.Grafite,
                new Vector2(0.07f, 0.035f), new Vector2(0.93f, 0.075f));

            _radice.SetActive(false);
        }

        private void CostruisciLaPianta()
        {
            _pianta = new GameObject("paese", typeof(RectTransform));
            _pianta.transform.SetParent(_radice.transform, false);
            Stile.Ancora((RectTransform)_pianta.transform, new Vector2(0.05f, 0.115f), new Vector2(0.95f, 0.82f));

            // La carta sta dentro una feritoia: il paese e' piu' grande della
            // finestra, e una pianta che non si puo' spostare mostra meta' paese
            // e taglia l'altra.
            Stile.Riquadro(_pianta.transform, "fondo carta", new Color(.20f,.23f,.19f), Vector2.zero, new Vector2(.78f,1f));
            _vista = Stile.Feritoia(_pianta.transform, "vista", new Vector2(.008f,.015f), new Vector2(.772f,.985f));
            var titolo = Stile.Scritta(_pianta.transform, Stile.Interfaccia, 26, Stile.Carta, new Vector2(.81f,.83f), new Vector2(1f,.98f));
            titolo.text = "MAPPA";
            titolo.fontStyle = FontStyle.Bold;
            var legenda = Stile.Scritta(_pianta.transform, Stile.Macchina, 14, Stile.Carta, new Vector2(.81f,.48f), new Vector2(.99f,.81f));
            legenda.text = "ESPLORA IL PAESE\n\n<color=#66D1AD>◆</color>  La tua posizione\n\nTrascina la carta per esplorare i dintorni.";
            _scalaMappa = Stile.Scritta(_pianta.transform, Stile.Macchina, 14, Stile.Ottone, new Vector2(.81f,.40f), new Vector2(.99f,.47f));
            Pulsante(_pianta.transform, "−", new Vector2(.81f,.27f), new Vector2(.89f,.37f), () => CambiaZoom(1f / 1.25f));
            Pulsante(_pianta.transform, "+", new Vector2(.91f,.27f), new Vector2(.99f,.37f), () => CambiaZoom(1.25f));
            Pulsante(_pianta.transform, "DOVE SONO", new Vector2(.81f,.12f), new Vector2(.99f,.22f), InquadraIlGiocatore);
            var disegno = new GameObject("carta", typeof(RectTransform));
            disegno.transform.SetParent(_vista, false);
            // La foto dall'alto se c'e', altrimenti la vecchia pianta a griglia.
            var immagine = _gioco.MappaImmagine;
            disegno.AddComponent<RawImage>().texture =
                immagine != null ? (Texture)immagine : Pianta.Disegna(_gioco.Map);
            _carta = (RectTransform)disegno.transform;
            _carta.anchorMin = _carta.anchorMax = new Vector2(0.5f, 0.5f);

            if (immagine != null)
            {
                // Un pallino con nome per ogni personaggio piazzato: la casa la
                // dice dove sta la persona. Se ne sposti uno, il segno lo segue.
                foreach (var pair in _gioco.Corpi)
                {
                    if (pair.Value == null) continue;
                    var nome = pair.Key == "don_carlo" ? "Don Carlo" : _gioco.NomeDi(pair.Key).Split(' ')[0];
                    _didascalie.Add((Etichetta(nome), pair.Value.position));
                }
            }
            else
            {
                foreach (var luogo in _gioco.Map.Places)
                {
                    if (!Pianta.SiScrive(luogo.Key)) continue;
                    var r = luogo.Value;
                    var mondo = new Vector3((r.X + r.W / 2f) * _gioco.CellSize, 0f, -(r.Y + r.H / 2f) * _gioco.CellSize);
                    _didascalie.Add((Etichetta(Pianta.NomeDi(luogo.Key)), mondo));
                }
            }

            var segno = Stile.Riquadro(_carta, "io", Stile.Ottone,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            segno.sizeDelta = new Vector2(13f, 13f);
            segno.localRotation = Quaternion.Euler(0f, 0f, 45f);
            segno.gameObject.AddComponent<Outline>().effectColor = Stile.Notte;
            _iosono = segno;

            _pianta.SetActive(false);
        }

        /// La carta al passo giusto: alla prima apertura ci sta tutta, poi
        /// resta come l'hai lasciata.
        private void Ridisegna()
        {
            var immagine = _gioco.MappaImmagine;
            if (immagine != null)
            {
                float aspetto = (float)immagine.width / immagine.height;
                float alt = Mathf.Max(1f,_vista.rect.height) * _zoom / 12f;
                _carta.sizeDelta = new Vector2(alt * aspetto, alt);
            }
            else
            {
                _carta.sizeDelta = new Vector2(_gioco.Map.Width * _zoom, _gioco.Map.Height * _zoom);
            }
            var occupied = new List<Rect>();
            foreach (var (etichetta, mondo) in _didascalie)
            {
                etichetta.sizeDelta=new Vector2(Mathf.Clamp(etichetta.GetComponent<Text>().preferredWidth+10,32,150),16);
                var point = PostoSuCarta(mondo);
                _segnaposti[etichetta].anchoredPosition = point;
                var offsets = new[]{new Vector2(0,13),new Vector2(0,-13),new Vector2(0,29),new Vector2(0,-29),new Vector2(52,13),new Vector2(-52,13),new Vector2(52,-13),new Vector2(-52,-13)};
                foreach(var offset in offsets) {
                    var candidate=point+offset;
                    var bounds=new Rect(candidate-etichetta.sizeDelta*.5f-Vector2.one*3,etichetta.sizeDelta+Vector2.one*6);
                    bool overlap=false;
                    foreach(var previous in occupied) if(previous.Overlaps(bounds)) { overlap=true;break; }
                    etichetta.anchoredPosition=candidate;
                    if(!overlap) { occupied.Add(bounds);break; }
                }
                etichetta.gameObject.SetActive(_zoom >= 8f);
            }
            _scalaMappa.text = "ZOOM  " + Mathf.RoundToInt(_zoom / 12f * 100f) + "%";
            Trattieni();
        }

        private void CambiaZoom(float fattore)
        {
            _zoom = Mathf.Clamp(_zoom * fattore, ZoomMinimo, ZoomMassimo);
            Ridisegna();
        }

        /// Dove cade un punto del mondo sulla carta (in pixel, centro = 0,0).
        /// Con la foto usa la calibrazione del Bootstrap; senza, la vecchia
        /// griglia.
        private Vector2 PostoSuCarta(Vector3 mondo)
        {
            if (_gioco.MappaImmagine != null)
            {
                var uv = _gioco.MappaUV(mondo);
                return new Vector2((uv.x - 0.5f) * _carta.sizeDelta.x, (0.5f - uv.y) * _carta.sizeDelta.y);
            }
            var c = _gioco.CellaDi(mondo);
            return SulFoglio(c.X + 0.5f, c.Y + 0.5f);
        }

        /// Un'etichetta-nome sulla carta; torna il suo RectTransform.
        private RectTransform Etichetta(string nome)
        {
            var etichetta = Stile.Scritta(_carta, Stile.Macchina, 10, Stile.Carta,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            etichetta.alignment = TextAnchor.MiddleCenter;
            etichetta.horizontalOverflow = HorizontalWrapMode.Overflow;
            etichetta.verticalOverflow = VerticalWrapMode.Overflow;
            etichetta.text = nome;
            etichetta.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, .95f);
            var rect = etichetta.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(Mathf.Clamp(etichetta.preferredWidth+8,32,150),16f);
            etichetta.raycastTarget=true;
            var shadow=etichetta.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.65f);shadow.effectDistance=new Vector2(0,-1);
            etichetta.gameObject.AddComponent<EtichettaMappa>();
            var marker=Stile.Riquadro(_carta,"punto_"+nome,new Color(.72f,.79f,.75f),new Vector2(.5f,.5f),new Vector2(.5f,.5f));
            marker.sizeDelta=new Vector2(4,4);marker.GetComponent<Image>().raycastTarget=false;
            _segnaposti[rect]=marker;
            return rect;
        }

        /// La carta non si perde: si puo' spostare finche' un pezzo resta in
        /// vista, non oltre.
        private void Trattieni()
        {
            var mezza = new Vector2(_carta.sizeDelta.x, _carta.sizeDelta.y) * 0.5f;
            var finestra = new Vector2(_vista.rect.width, _vista.rect.height) * 0.5f;
            var margine = new Vector2(Mathf.Max(mezza.x - finestra.x, 0f), Mathf.Max(mezza.y - finestra.y, 0f));
            _carta.anchoredPosition = new Vector2(
                Mathf.Clamp(_carta.anchoredPosition.x, -margine.x, margine.x),
                Mathf.Clamp(_carta.anchoredPosition.y, -margine.y, margine.y));
        }

        private Vector2 SulFoglio(float x, float y) => new Vector2(
            (x - _gioco.Map.Width / 2f) * _zoom,
            (_gioco.Map.Height / 2f - y) * _zoom);

        private void Linguetta(Transform genitore, Pagina pagina, string testo, string tasto)
        {
            var go = new GameObject(testo, typeof(RectTransform));
            go.transform.SetParent(genitore, false);
            var scritta = Stile.Scritta(go.transform, Stile.Macchina, 19, Stile.Grafite, Vector2.zero, Vector2.one);
            scritta.alignment = TextAnchor.MiddleLeft;
            scritta.horizontalOverflow = HorizontalWrapMode.Overflow;
            scritta.text = $"{testo}  ({tasto})";
            scritta.fontStyle = FontStyle.Bold;

            // Il filo d'ottone sotto la linguetta aperta: la stessa firma del
            // pannello, e lo stesso significato — sei qui dentro.
            var filo = Stile.Filo(go.transform, new Vector2(0f, 0f), new Vector2(1f, 0.06f), Stile.Ottone);
            _sottolineature[pagina] = filo;

            go.AddComponent<LayoutElement>().preferredWidth = 220f;
            var bottone = go.AddComponent<Button>();
            bottone.targetGraphic = scritta;
            bottone.onClick.AddListener(() => { _pagina = pagina; Mostra(); });
            _linguette[pagina] = scritta;
        }
    }
}
