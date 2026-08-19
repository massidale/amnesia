using System.Threading.Tasks;
using System.Text;
using Amnesia.Dialogue;
using Amnesia.Game;
using UnityEngine;
using UnityEngine.UI;

namespace AmnesiaUnity
{
    /// La conversazione a schermo. Costruita in codice perche' finche' il look
    /// non e' deciso una scena salvata e' solo una cosa in piu' che si rompe.
    public sealed class Pannello : MonoBehaviour
    {
        /// Quanti scambi restano a schermo. Il pannello e' basso apposta: se ci
        /// stesse tutta la conversazione, il giocatore rileggerebbe invece di
        /// guardare in faccia chi ha davanti.
        private const int TurniMostrati = 6;

        public bool Aperto { get; private set; }

        private Bootstrap _gioco;
        private Text _detto;
        private Text _chi;
        private Text _invito;
        private Text _stato;
        private InputField _campo;
        private GameObject _radice;
        private string _con = "";
        private bool _inAttesa;

        private void Start()
        {
            _gioco = FindFirstObjectByType<Bootstrap>();
            Costruisci();
            if (!string.IsNullOrEmpty(_gioco.Problema))
            {
                _stato.text = _gioco.Problema;
                _radice.SetActive(true);
            }
        }

        private void Costruisci()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = new GameObject("interfaccia").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("eventi");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            _radice = new GameObject("pannello");
            _radice.transform.SetParent(canvas.transform, false);
            var sfondo = _radice.AddComponent<Image>();
            sfondo.color = new Color(0.05f, 0.05f, 0.06f, 0.88f);
            var rect = _radice.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.06f, 0.04f);
            rect.anchorMax = new Vector2(0.94f, 0.40f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            _chi = Etichetta(_radice.transform, font, 23, new Vector2(0.03f, 0.86f), new Vector2(0.97f, 0.99f));
            _chi.color = new Color(0.88f, 0.82f, 0.66f);
            _detto = Etichetta(_radice.transform, font, 20, new Vector2(0.03f, 0.34f), new Vector2(0.97f, 0.84f));
            _detto.color = new Color(0.93f, 0.91f, 0.86f);
            _detto.alignment = TextAnchor.LowerLeft;
            _stato = Etichetta(_radice.transform, font, 15, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.16f));
            _stato.color = new Color(0.60f, 0.58f, 0.54f);

            var riga = new GameObject("riga");
            riga.transform.SetParent(_radice.transform, false);
            var rigaImg = riga.AddComponent<Image>();
            rigaImg.color = new Color(1f, 1f, 1f, 0.06f);
            var rigaRect = riga.GetComponent<RectTransform>();
            rigaRect.anchorMin = new Vector2(0.03f, 0.17f);
            rigaRect.anchorMax = new Vector2(0.97f, 0.32f);
            rigaRect.offsetMin = rigaRect.offsetMax = Vector2.zero;

            var testo = Etichetta(riga.transform, font, 20, new Vector2(0.01f, 0f), new Vector2(0.99f, 1f));
            testo.alignment = TextAnchor.MiddleLeft;
            testo.supportRichText = false;

            _campo = riga.AddComponent<InputField>();
            _campo.textComponent = testo;
            _campo.lineType = InputField.LineType.SingleLine;
            // onEndEdit scatta anche quando il campo perde il fuoco: senza il
            // filtro sull'invio, cliccare altrove manderebbe una battuta.
            _campo.onEndEdit.AddListener(riga =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    Manda(riga);
                }
            });

            // Fuori dal pannello, perche' si legge mentre si cammina.
            _invito = Etichetta(canvas.transform, font, 18, new Vector2(0.20f, 0.44f), new Vector2(0.80f, 0.50f));
            _invito.alignment = TextAnchor.MiddleCenter;
            _invito.color = new Color(0.93f, 0.91f, 0.86f, 0.80f);
            _invito.text = "";

            _radice.SetActive(false);
        }

        private static Text Etichetta(Transform genitore, Font font, int corpo, Vector2 min, Vector2 max)
        {
            var go = new GameObject("testo");
            go.transform.SetParent(genitore, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = corpo;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return text;
        }

        public void Apri(string npcId)
        {
            _con = npcId;
            Aperto = true;
            _radice.SetActive(true);
            // Riaprire una conversazione la ritrova dov'era: e' il registro del
            // personaggio, non una finestra che si svuota chiudendola.
            Trascrivi();
            _chi.text = _gioco.NomeDi(npcId);
            _invito.text = "";
            _stato.text = "invio per parlare, Esc per andartene";
            _campo.text = "";
            _campo.ActivateInputField();
        }

        /// Chi hai davanti, prima di aprire bocca. In un paese di milleduecento
        /// anime le facce si conoscono tutte: andare in giro a chiedere «lei chi
        /// e'?» a uno per uno e' una cosa che Giorgio non farebbe mai.
        public void Suggerisci(string npcId)
        {
            _invito.text = Aperto || string.IsNullOrEmpty(npcId) ? "" : $"E — parla con {_gioco.NomeDi(npcId)}";
        }

        private void Update()
        {
            if (Aperto && Input.GetKeyDown(KeyCode.Escape) && !_inAttesa)
            {
                Aperto = false;
                _radice.SetActive(false);
            }
        }

        private async void Manda(string riga)
        {
            if (_inAttesa || string.IsNullOrWhiteSpace(riga))
            {
                return;
            }
            _inAttesa = true;
            _campo.text = "";
            _stato.text = "…";

            TurnResult turno;
            try
            {
                turno = await _gioco.Session.TakeTurnAsync(_con, riga);
            }
            catch (System.Exception errore)
            {
                // Un turno che esplode non deve lasciare il pannello muto: il
                // mondo non si e' mosso, e il giocatore ha diritto di saperlo.
                _stato.text = $"non ha risposto ({errore.GetType().Name})";
                _inAttesa = false;
                _campo.ActivateInputField();
                return;
            }

            if (turno.IsOk)
            {
                Trascrivi();
                _stato.text = Coda(turno);
            }
            else
            {
                _stato.text = $"non ha risposto: {turno.Message}";
            }
            _inAttesa = false;
            _campo.ActivateInputField();
        }

        /// La conversazione come la ricorda il personaggio. La fonte e' il
        /// registro del dominio e non una lista tenuta a parte: quella che il
        /// giocatore legge deve essere la stessa cosa che rientra nel prompt il
        /// turno dopo, o si finisce a discutere di una battuta che il modello non
        /// ha mai avuto davanti.
        private void Trascrivi()
        {
            var scritto = new StringBuilder();
            foreach (var battuta in _gioco.Session.Log.Recent(_con, TurniMostrati * 2))
            {
                if (battuta.Role == ChatRole.User)
                {
                    scritto.Append("\n> ").Append(battuta.Content).Append('\n');
                }
                else
                {
                    scritto.Append(battuta.Content).Append('\n');
                }
            }
            _detto.text = scritto.ToString().TrimStart('\n');
        }

        /// Cosa il motore ha registrato. A schermo perche' questa e' una scena di
        /// prova e serve a vedere la meccanica lavorare; nel gioco vero il
        /// giocatore vedra' il taccuino, non questa riga.
        private string Coda(TurnResult turno)
        {
            var ora = Amnesia.Time.WorldClock.Format(turno.Minute);
            var registrato = turno.Declared.Count > 0 ? "  ·  registrato: " + string.Join(", ", turno.Declared) : "";
            var rifiutato = turno.RefusedTags.Count > 0 ? "  ·  non ce l'hai: " + string.Join(", ", turno.RefusedTags) : "";
            return ora + registrato + rifiutato;
        }
    }
}
