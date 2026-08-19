using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AmnesiaUnity
{
    /// Il menu di pausa, con tre pagine: cosa hai in tasca, la pianta del paese,
    /// il taccuino.
    ///
    /// Stanno insieme perche' sono la stessa cosa vista da tre lati — un
    /// oggetto senza una riga che lo spieghi non vuol dire niente, una riga
    /// senza l'oggetto in mano non si puo' mettere in faccia a nessuno, e
    /// nessuna delle due serve se non sai dove sta la bottega.
    ///
    /// Il tempo qui non scorre e non c'e' niente da fermare: in Amnesia l'orologio
    /// avanza a ogni battuta, quindi la pausa e' esattamente cio' che sembra —
    /// il gioco che sta fermo ad aspettarti.
    public sealed class Menu : MonoBehaviour
    {
        private const int Zoom = 14;

        private enum Pagina { Tasche, Paese, Taccuino }

        public bool Aperto { get; private set; }

        private Bootstrap _gioco;
        private Transform _giocatore;
        private GameObject _radice;
        private Text _titolo;
        private Text _foglio;
        private GameObject _pianta;
        private RectTransform _carta;
        private RectTransform _iosono;
        private readonly Dictionary<Pagina, Text> _linguette = new Dictionary<Pagina, Text>();
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

        /// Le pagine girano con Tab e con le frecce, come in un menu di allora.
        public void Scorri(int verso)
        {
            _pagina = (Pagina)(((int)_pagina + verso + 3) % 3);
            Mostra();
        }

        private void Mostra()
        {
            _foglio.gameObject.SetActive(_pagina != Pagina.Paese);
            _pianta.SetActive(_pagina == Pagina.Paese);
            if (_pagina == Pagina.Tasche)
            {
                _foglio.text = Inventario.Oggetti(_gioco);
            }
            else if (_pagina == Pagina.Taccuino)
            {
                _foglio.text = Inventario.Righe(_gioco);
            }
            foreach (var linguetta in _linguette)
            {
                linguetta.Value.color = linguetta.Key == _pagina
                    ? new Color(0.95f, 0.90f, 0.76f)
                    : new Color(0.52f, 0.50f, 0.47f);
            }
        }

        private void Update()
        {
            if (!Aperto || _pagina != Pagina.Paese || _giocatore == null)
            {
                return;
            }
            var qui = _gioco.CellaDi(_giocatore.position);
            _iosono.anchoredPosition = SulFoglio(qui.X + 0.5f, qui.Y + 0.5f);
        }

        private void Costruisci()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = new GameObject("menu").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Sopra la conversazione: l'ordine fra due canvas pari e' quello di
            // creazione, cioe' una cosa che cambia da sola spostando una riga.
            canvas.sortingOrder = 10;
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvas.gameObject.AddComponent<GraphicRaycaster>();

            _radice = new GameObject("pagina");
            _radice.transform.SetParent(canvas.transform, false);
            _radice.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.05f, 0.95f);
            Riempi(_radice.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            _titolo = Scritta(_radice.transform, font, 24, new Vector2(0.06f, 0.90f), new Vector2(0.5f, 0.97f));
            _titolo.color = new Color(0.88f, 0.82f, 0.66f);
            _titolo.text = "Pausa";

            var barra = new GameObject("linguette");
            barra.transform.SetParent(_radice.transform, false);
            var disposizione = barra.AddComponent<HorizontalLayoutGroup>();
            disposizione.spacing = 26f;
            disposizione.childForceExpandWidth = false;
            disposizione.childAlignment = TextAnchor.MiddleLeft;
            Riempi(barra.GetComponent<RectTransform>(), new Vector2(0.06f, 0.83f), new Vector2(0.94f, 0.89f));

            Linguetta(barra.transform, font, Pagina.Tasche, "In tasca  (I)");
            Linguetta(barra.transform, font, Pagina.Paese, "Il paese  (M)");
            Linguetta(barra.transform, font, Pagina.Taccuino, "Il taccuino  (T)");

            _foglio = Scritta(_radice.transform, font, 18, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.81f));

            CostruisciLaPianta(font);

            var coda = Scritta(_radice.transform, font, 15, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.07f));
            coda.color = new Color(0.52f, 0.50f, 0.47f);
            coda.text = "Esc riprende  ·  Tab o le frecce cambiano pagina";

            _radice.SetActive(false);
        }

        private void CostruisciLaPianta(Font font)
        {
            _pianta = new GameObject("paese", typeof(RectTransform));
            _pianta.transform.SetParent(_radice.transform, false);
            Riempi((RectTransform)_pianta.transform, new Vector2(0f, 0.08f), new Vector2(1f, 0.81f));

            var disegno = new GameObject("carta");
            disegno.transform.SetParent(_pianta.transform, false);
            disegno.AddComponent<RawImage>().texture = Pianta.Disegna(_gioco.Map);
            _carta = disegno.GetComponent<RectTransform>();
            _carta.anchorMin = _carta.anchorMax = new Vector2(0.5f, 0.5f);
            _carta.sizeDelta = new Vector2(_gioco.Map.Width * Zoom, _gioco.Map.Height * Zoom);

            foreach (var luogo in _gioco.Map.Places)
            {
                var rettangolo = luogo.Value;
                var etichetta = Scritta(_carta, font, 13, Vector2.zero, Vector2.zero);
                etichetta.alignment = TextAnchor.MiddleCenter;
                etichetta.horizontalOverflow = HorizontalWrapMode.Overflow;
                etichetta.text = Pianta.NomeDi(luogo.Key);
                var rect = etichetta.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(170f, 18f);
                rect.anchoredPosition = SulFoglio(
                    rettangolo.X + rettangolo.W / 2f, rettangolo.Y + rettangolo.H / 2f);
            }

            var segno = new GameObject("io").AddComponent<Image>();
            segno.transform.SetParent(_carta, false);
            segno.color = new Color(0.96f, 0.86f, 0.52f);
            _iosono = segno.GetComponent<RectTransform>();
            _iosono.anchorMin = _iosono.anchorMax = new Vector2(0.5f, 0.5f);
            _iosono.sizeDelta = new Vector2(9f, 9f);

            _pianta.SetActive(false);
        }

        private Vector2 SulFoglio(float x, float y) => new Vector2(
            (x - _gioco.Map.Width / 2f) * Zoom,
            (_gioco.Map.Height / 2f - y) * Zoom);

        private void Linguetta(Transform genitore, Font font, Pagina pagina, string testo)
        {
            var go = new GameObject(testo);
            go.transform.SetParent(genitore, false);
            var scritta = go.AddComponent<Text>();
            scritta.font = font;
            scritta.fontSize = 19;
            scritta.alignment = TextAnchor.MiddleLeft;
            scritta.horizontalOverflow = HorizontalWrapMode.Overflow;
            scritta.text = testo;
            go.AddComponent<LayoutElement>().preferredWidth = scritta.preferredWidth + 6f;
            var bottone = go.AddComponent<Button>();
            bottone.targetGraphic = scritta;
            bottone.onClick.AddListener(() => { _pagina = pagina; Mostra(); });
            _linguette[pagina] = scritta;
        }

        private static Text Scritta(Transform genitore, Font font, int corpo, Vector2 min, Vector2 max)
        {
            var go = new GameObject("scritta");
            go.transform.SetParent(genitore, false);
            var testo = go.AddComponent<Text>();
            testo.font = font;
            testo.fontSize = corpo;
            testo.color = new Color(0.91f, 0.89f, 0.84f);
            testo.alignment = TextAnchor.UpperLeft;
            testo.horizontalOverflow = HorizontalWrapMode.Wrap;
            testo.verticalOverflow = VerticalWrapMode.Truncate;
            Riempi(go.GetComponent<RectTransform>(), min, max);
            return testo;
        }

        private static void Riempi(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
