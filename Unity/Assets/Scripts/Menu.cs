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
    public sealed class Menu : MonoBehaviour
    {
        private const int Zoom = 14;

        private enum Pagina { Tasche, Paese, Taccuino }

        public bool Aperto { get; private set; }

        private Bootstrap _gioco;
        private Transform _giocatore;
        private GameObject _radice;
        private Text _foglio;
        private GameObject _pianta;
        private RectTransform _carta;
        private RectTransform _iosono;
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
            _foglio.transform.parent.gameObject.SetActive(_pagina != Pagina.Paese);
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
                var scelta = linguetta.Key == _pagina;
                linguetta.Value.color = scelta ? Stile.Carta : Stile.Grafite;
                _sottolineature[linguetta.Key].gameObject.SetActive(scelta);
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
            var canvas = Stile.Tela("menu", 10);
            _radice = Stile.Riquadro(canvas.transform, "pagina", Stile.Velo, Vector2.zero, Vector2.one).gameObject;

            var occhiello = Stile.Scritta(_radice.transform, Stile.Macchina, 13, Stile.Grafite,
                new Vector2(0.07f, 0.925f), new Vector2(0.5f, 0.965f));
            occhiello.text = "PAUSA";

            var barra = new GameObject("linguette", typeof(RectTransform));
            barra.transform.SetParent(_radice.transform, false);
            var disposizione = barra.AddComponent<HorizontalLayoutGroup>();
            disposizione.spacing = 34f;
            disposizione.childForceExpandWidth = false;
            disposizione.childAlignment = TextAnchor.MiddleLeft;
            Stile.Ancora((RectTransform)barra.transform, new Vector2(0.07f, 0.855f), new Vector2(0.93f, 0.915f));

            Linguetta(barra.transform, Pagina.Tasche, "In tasca", "I");
            Linguetta(barra.transform, Pagina.Paese, "Il paese", "M");
            Linguetta(barra.transform, Pagina.Taccuino, "Il taccuino", "T");

            Stile.Filo(_radice.transform, new Vector2(0.07f, 0.845f), new Vector2(0.93f, 0.848f), Stile.Incavo);

            var feritoia = Stile.Feritoia(_radice.transform, "foglio",
                new Vector2(0.07f, 0.09f), new Vector2(0.93f, 0.825f));
            _foglio = Stile.Scritta(feritoia, Stile.Macchina, 16, Stile.Carta, Vector2.zero, Vector2.one);

            CostruisciLaPianta();

            var coda = Stile.Scritta(_radice.transform, Stile.Macchina, 13, Stile.Grafite,
                new Vector2(0.07f, 0.035f), new Vector2(0.93f, 0.075f));
            coda.text = "esc riprende   ·   tab e frecce cambiano pagina";

            _radice.SetActive(false);
        }

        private void CostruisciLaPianta()
        {
            _pianta = new GameObject("paese", typeof(RectTransform));
            _pianta.transform.SetParent(_radice.transform, false);
            Stile.Ancora((RectTransform)_pianta.transform, new Vector2(0f, 0.09f), new Vector2(1f, 0.825f));

            var larghezza = _gioco.Map.Width * Zoom;
            var altezza = _gioco.Map.Height * Zoom;

            // La cornice: un bordo intorno alla carta, che la stacca dal nero e
            // dice dove finisce il paese.
            var cornice = Stile.Riquadro(_pianta.transform, "cornice", Stile.Grafite,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            cornice.sizeDelta = new Vector2(larghezza + 4f, altezza + 4f);

            var disegno = new GameObject("carta", typeof(RectTransform));
            disegno.transform.SetParent(cornice, false);
            disegno.AddComponent<RawImage>().texture = Pianta.Disegna(_gioco.Map);
            _carta = Stile.Ancora((RectTransform)disegno.transform, Vector2.zero, Vector2.one);
            _carta.offsetMin = new Vector2(2f, 2f);
            _carta.offsetMax = new Vector2(-2f, -2f);

            foreach (var luogo in _gioco.Map.Places)
            {
                var rettangolo = luogo.Value;
                var etichetta = Stile.Scritta(_carta, Stile.Macchina, 12, Stile.Carta,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                etichetta.alignment = TextAnchor.MiddleCenter;
                etichetta.horizontalOverflow = HorizontalWrapMode.Overflow;
                etichetta.verticalOverflow = VerticalWrapMode.Overflow;
                etichetta.text = Pianta.NomeDi(luogo.Key);
                var rect = etichetta.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(170f, 16f);
                rect.anchoredPosition = SulFoglio(
                    rettangolo.X + rettangolo.W / 2f, rettangolo.Y + rettangolo.H / 2f);
            }

            var segno = Stile.Riquadro(_carta, "io", Stile.Ottone,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            segno.sizeDelta = new Vector2(9f, 9f);
            _iosono = segno;

            _pianta.SetActive(false);
        }

        private Vector2 SulFoglio(float x, float y) => new Vector2(
            (x - _gioco.Map.Width / 2f) * Zoom,
            (_gioco.Map.Height / 2f - y) * Zoom);

        private void Linguetta(Transform genitore, Pagina pagina, string testo, string tasto)
        {
            var go = new GameObject(testo, typeof(RectTransform));
            go.transform.SetParent(genitore, false);
            var scritta = Stile.Scritta(go.transform, Stile.Macchina, 19, Stile.Grafite, Vector2.zero, Vector2.one);
            scritta.alignment = TextAnchor.MiddleLeft;
            scritta.horizontalOverflow = HorizontalWrapMode.Overflow;
            scritta.text = $"{testo}  ({tasto})";

            // Il filo d'ottone sotto la linguetta aperta: la stessa firma del
            // pannello, e lo stesso significato — sei qui dentro.
            var filo = Stile.Filo(go.transform, new Vector2(0f, 0f), new Vector2(1f, 0.06f), Stile.Ottone);
            _sottolineature[pagina] = filo;

            go.AddComponent<LayoutElement>().preferredWidth = scritta.preferredWidth + 4f;
            var bottone = go.AddComponent<Button>();
            bottone.targetGraphic = scritta;
            bottone.onClick.AddListener(() => { _pagina = pagina; Mostra(); });
            _linguette[pagina] = scritta;
        }
    }
}
