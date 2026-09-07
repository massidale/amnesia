using System.Text;
using Amnesia.Dialogue;
using Amnesia.Game;
using UnityEngine;
using UnityEngine.UI;

namespace AmnesiaUnity
{
    /// La conversazione a schermo: il nome di chi hai davanti, quello che vi
    /// siete detti, gli oggetti che puoi mettergli sul banco, e la riga in cui
    /// scrivi.
    ///
    /// E' costruita in codice perche' finche' il look non e' deciso una scena
    /// salvata e' solo una cosa in piu' che si rompe in silenzio.
    public sealed class Pannello : MonoBehaviour
    {
        /// Quanto indietro si puo' riavvolgere. Tutta la conversazione: la
        /// prova, in questo gioco, e' spesso una frase detta tre scambi fa.
        private const int TurniMostrati = 200;

        private const float DurataAvviso = 5f;

        public bool Aperto { get; private set; }

        private Bootstrap _gioco;
        private Text _detto;
        private Text _chi;
        private Text _stato;
        private Text _invito;
        private InputField _campo;
        private GameObject _radice;
        private RectTransform _fila;
        private ScrollRect _rullo;
        private string _con = "";
        private bool _inAttesa;
        private Text _attesa;
        private float _inizioAttesa;
        private static readonly string[] FasiAttesa = { "Sta rispondendo.  ", "Sta rispondendo.. ", "Sta rispondendo..." };
        private Button _richiesta;
        private Text _testoRichiesta;
        private string _oggettoRichiesto = "";
        private float _avvisoFino;

        private void Start()
        {
            _gioco = FindFirstObjectByType<Bootstrap>();
            Costruisci();
            if (!string.IsNullOrEmpty(_gioco.Problema))
            {
                _stato.text = _gioco.Problema;
                _stato.color = Stile.Ruggine;
                _radice.SetActive(true);
            }
        }

        private void Costruisci()
        {
            var canvas = Stile.Tela("interfaccia", 0);
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventi = new GameObject("eventi");
                eventi.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventi.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            _radice = Stile.Riquadro(canvas.transform, "pannello", Stile.Notte,
                new Vector2(0.05f, 0.045f), new Vector2(0.95f, 0.44f)).gameObject;
            // Il filo d'ottone: l'unico ornamento, e vuol dire «sei qui dentro».
            Stile.Filo(_radice.transform, new Vector2(0f, 0.985f), new Vector2(1f, 1f), Stile.Ottone);

            _chi = Stile.Scritta(_radice.transform, Stile.Macchina, 20, Stile.Ottone,
                new Vector2(0.035f, 0.855f), new Vector2(0.7f, 0.965f));
            var azione = Stile.Riquadro(_radice.transform, "richiesta", Stile.Incavo,
                new Vector2(0.70f, 0.855f), new Vector2(0.965f, 0.965f));
            _testoRichiesta = Stile.Scritta(azione, Stile.Macchina, 14, Stile.Carta, Vector2.zero, Vector2.one);
            _testoRichiesta.alignment = TextAnchor.MiddleCenter;
            _testoRichiesta.resizeTextForBestFit = true;
            _testoRichiesta.resizeTextMinSize = 10;
            _testoRichiesta.resizeTextMaxSize = 14;
            _richiesta = azione.gameObject.AddComponent<Button>();
            _richiesta.targetGraphic = azione.GetComponent<Image>();
            _richiesta.onClick.AddListener(() => Manda("[richiedi: " + _oggettoRichiesto + "]"));

            _detto = Stile.Rullo(_radice.transform, Stile.Libro, 23, Stile.Carta,
                new Vector2(0.035f, 0.30f), new Vector2(0.965f, 0.84f), out _rullo);

            var barra = new GameObject("oggetti", typeof(RectTransform));
            barra.transform.SetParent(_radice.transform, false);
            var disposizione = barra.AddComponent<HorizontalLayoutGroup>();
            disposizione.spacing = 8f;
            disposizione.childForceExpandWidth = false;
            disposizione.childForceExpandHeight = true;
            disposizione.childAlignment = TextAnchor.MiddleLeft;
            _fila = Stile.Ancora((RectTransform)barra.transform,
                new Vector2(0.035f, 0.205f), new Vector2(0.965f, 0.285f));

            var riga = Stile.Riquadro(_radice.transform, "riga", Stile.Incavo,
                new Vector2(0.035f, 0.075f), new Vector2(0.965f, 0.19f));
            var scritto = Stile.Scritta(riga, Stile.Libro, 23, Stile.Carta,
                new Vector2(0.012f, 0f), new Vector2(0.99f, 1f));
            scritto.alignment = TextAnchor.MiddleLeft;
            scritto.supportRichText = false;

            _campo = riga.gameObject.AddComponent<InputField>();
            _campo.textComponent = scritto;
            _campo.lineType = InputField.LineType.SingleLine;
            _campo.caretColor = Stile.Ottone;
            _campo.customCaretColor = true;
            _attesa = Stile.Scritta(riga, Stile.Macchina, 18, Stile.Ottone,
                new Vector2(0.012f, 0f), new Vector2(0.99f, 1f));
            _attesa.alignment = TextAnchor.MiddleLeft;
            _attesa.raycastTarget = false;
            _attesa.gameObject.SetActive(false);
            // onEndEdit scatta anche quando il campo perde il fuoco: senza il
            // filtro sull'invio, cliccare altrove manderebbe una battuta.
            _campo.onEndEdit.AddListener(testo =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    Manda(testo);
                }
            });

            _stato = Stile.Scritta(_radice.transform, Stile.Macchina, 13, Stile.Grafite,
                new Vector2(0.035f, 0.012f), new Vector2(0.965f, 0.065f));

            // Fuori dal pannello: si legge camminando.
            _invito = Stile.Scritta(canvas.transform, Stile.Macchina, 17, Stile.Carta,
                new Vector2(0.2f, 0.45f), new Vector2(0.8f, 0.51f));
            _invito.alignment = TextAnchor.MiddleCenter;

            _radice.SetActive(false);
        }

        public void Apri(string npcId)
        {
            if (!_gioco.Session.PuoParlare(npcId))
            {
                Avviso("Wanda non ha ancora autorizzato l'ingresso.");
                return;
            }
            FindFirstObjectByType<Menu>()?.Chiudi();
            _con = npcId;
            Aperto = true;
            _radice.SetActive(true);
            _chi.text = _gioco.NomeDi(npcId);
            _invito.text = "";
            // Chi alza la testa per primo. Il saluto entra nel registro della
            // conversazione, quindi si scrive prima di trascrivere.
            _gioco.Accoglienza.Apri(_gioco.Session.World, _gioco.Session.Log, npcId);
            Targhette();
            // Riaprire una conversazione la ritrova dov'era: e' il registro del
            // personaggio, non una finestra che si svuota chiudendola.
            Trascrivi();
            _stato.text = "invio per parlare   ·   rotella per rileggere   ·   /oggetti   /taccuino   ·   esc per andartene";
            _campo.text = "";
            _campo.ActivateInputField();
        }

        /// Chi hai davanti, prima di aprire bocca. In un paese di milleduecento
        /// anime le facce si conoscono tutte: andare in giro a chiedere «lei chi
        /// e'?» a uno per uno e' una cosa che Giorgio non farebbe mai.
        public void Suggerimento(string testo)
        {
            if (Aperto || Time.time < _avvisoFino)
            {
                return;
            }
            _invito.color = Stile.Carta;
            _invito.text = testo;
        }

        /// Quello che succede fuori dalla conversazione: una porta che non si
        /// apre, una serratura che gira, della roba che cambia di mano.
        public void Avviso(string testo, bool storto = false)
        {
            _invito.color = storto ? Stile.Grafite : Stile.Carta;
            _invito.text = testo;
            _avvisoFino = Time.time + DurataAvviso;
        }

        private void Update()
        {
            if (_inAttesa) AggiornaAttesa(Time.unscaledTime - _inizioAttesa);
            // La rotella scorre solo dove sta il puntatore; questi funzionano
            // anche con le mani sulla tastiera, che e' dove stanno mentre parli.
            if (Aperto && _rullo != null)
            {
                if (Input.GetKey(KeyCode.PageUp))
                {
                    _rullo.verticalNormalizedPosition += Time.deltaTime * 0.8f;
                }
                else if (Input.GetKey(KeyCode.PageDown))
                {
                    _rullo.verticalNormalizedPosition -= Time.deltaTime * 0.8f;
                }
            }
            if (Aperto && Input.GetKeyDown(KeyCode.Escape) && !_inAttesa)
            {
                Aperto = false;
                _radice.SetActive(false);
            }
            if (!Aperto && Time.time > _avvisoFino && _avvisoFino > 0f)
            {
                _invito.text = "";
                _avvisoFino = 0f;
            }
        }

        private async void Manda(string riga)
        {
            if (_inAttesa || string.IsNullOrWhiteSpace(riga))
            {
                return;
            }
            if (riga.TrimStart().StartsWith("/"))
            {
                Comando(riga.Trim());
                _campo.text = "";
                _campo.ActivateInputField();
                return;
            }
            _campo.text = "";
            ImpostaAttesa(true);
            _stato.text = "";

            TurnResult turno;
            try
            {
                turno = await _gioco.Session.TakeTurnAsync(_con, riga);
            }
            catch (System.Exception errore)
            {
                // Un turno che esplode non deve lasciare il pannello muto: il
                // mondo non si e' mosso, e il giocatore ha diritto di saperlo.
                _stato.color = Stile.Ruggine;
                _stato.text = $"non ha risposto — {errore.GetType().Name}";
                ImpostaAttesa(false);
                _campo.ActivateInputField();
                return;
            }

            if (turno.IsOk)
            {
                // DIAGNOSTICA: perche' ogni tanto lo schermo resta muto. Un turno
                // riuscito con Reply vuota e FinishReason "length" + reasoning
                // lungo = il modello ha finito il budget ragionando e il testo
                // non e' mai uscito.
                var vuota = string.IsNullOrWhiteSpace(turno.Reply);
                var diag = $"[turno] npc={_con} ok=1 reply.len={turno.Reply.Length} " +
                    $"finish={turno.FinishReason} compTokens={turno.CompletionTokens} " +
                    $"reasoning.len={turno.ReasoningLength} " +
                    $"dichiara=[{string.Join(",", turno.Declared)}] " +
                    $"rifiuta=[{string.Join(",", turno.RefusedDeclarations)}]";
                if (vuota)
                {
                    UnityEngine.Debug.LogWarning("RISPOSTA VUOTA — " + diag);
                }
                else
                {
                    UnityEngine.Debug.Log(diag);
                }
                Trascrivi();
                Targhette();
                _stato.color = Stile.Grafite;
                _stato.text = vuota
                    ? $"(muto) finish={turno.FinishReason}, reasoning={turno.ReasoningLength}, token={turno.CompletionTokens}"
                    : Coda(turno);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[turno] npc={_con} ok=0 code={turno.Code} msg={turno.Message}");
                _stato.color = Stile.Ruggine;
                _stato.text = $"non ha risposto — {turno.Message}";
            }
            ImpostaAttesa(false);
            _campo.ActivateInputField();
        }

        private void ImpostaAttesa(bool attiva)
        {
            _inAttesa = attiva;
            _attesa.gameObject.SetActive(attiva);
            _campo.interactable = !attiva;
            _richiesta.interactable = !attiva;
            _fila.gameObject.SetActive(!attiva);
            if (attiva)
            {
                _inizioAttesa = Time.unscaledTime;
                AggiornaAttesa(0f);
            }
        }

        private void AggiornaAttesa(float secondi)
        {
            _attesa.text = FasiAttesa[Mathf.FloorToInt(Mathf.Max(0f, secondi) / .4f) % FasiAttesa.Length];
        }

        /// Gli oggetti che hai addosso, uno per targhetta. Cliccarne una scrive
        /// il tag nella riga invece di mandarlo: mostrare una cosa non e' una
        /// mossa a se', e' una cosa che si fa *mentre* si dice qualcosa — ed e'
        /// il giocatore a decidere cosa.
        private void Targhette()
        {
            var richieste = ConsegneNarrative.Richiedibili(_gioco.Session.World, _con);
            _oggettoRichiesto = richieste.Count > 0 ? richieste[0] : "";
            _richiesta.gameObject.SetActive(_oggettoRichiesto.Length > 0);
            _testoRichiesta.text = _oggettoRichiesto == "fotografia" ? "Chiedi la fotografia" : "Chiedi il messaggio";
            foreach (Transform vecchia in _fila)
            {
                Destroy(vecchia.gameObject);
            }
            foreach (var oggetto in _gioco.Taccuino.Tasche())
            {
                if (oggetto.Id == "taccuino" || oggetto.Id == "frase") continue;
                var nome = string.IsNullOrEmpty(oggetto.Name) ? oggetto.Id : oggetto.Name;
                var targhetta = Stile.Riquadro(_fila, oggetto.Id, Stile.Incavo, Vector2.zero, Vector2.one);
                var testo = Stile.Scritta(targhetta, Stile.Macchina, 14, Stile.Carta, Vector2.zero, Vector2.one);
                testo.alignment = TextAnchor.MiddleCenter;
                testo.text = nome;

                targhetta.gameObject.AddComponent<LayoutElement>().preferredWidth = testo.preferredWidth + 22f;
                var bottone = targhetta.gameObject.AddComponent<Button>();
                bottone.targetGraphic = targhetta.GetComponent<Image>();
                bottone.onClick.AddListener(() => Inserisci(nome));
            }
        }

        private void Inserisci(string nome)
        {
            var scritto = _campo.text.TrimEnd();
            _campo.text = (scritto.Length == 0 ? "" : scritto + " ") + $"[mostra: {nome}] ";
            _campo.ActivateInputField();
            _campo.caretPosition = _campo.text.Length;
        }

        /// Le tasche e il taccuino, senza spendere un turno e senza passare dal
        /// modello: sono roba del motore, e chiedere a un personaggio cosa hai
        /// in tasca sarebbe chiederlo alla persona sbagliata.
        private void Comando(string riga)
        {
            if (riga.StartsWith("/ogg"))
            {
                _detto.text = Inventario.Oggetti(_gioco);
            }
            else if (riga.StartsWith("/tac"))
            {
                _detto.text = Inventario.Righe(_gioco);
            }
            else
            {
                _detto.text = "/oggetti    cosa hai in tasca\n/taccuino   cosa ti hanno detto";
            }
            InFondo();
            _stato.color = Stile.Grafite;
            _stato.text = "scrivi per tornare alla conversazione";
        }

        /// La conversazione come la ricorda il personaggio. La fonte e' il
        /// registro del dominio e non una lista tenuta a parte: quella che il
        /// giocatore legge deve essere la stessa cosa che rientra nel prompt il
        /// turno dopo, o si finisce a discutere di una battuta che il modello
        /// non ha mai avuto davanti.
        private void Trascrivi()
        {
            var scritto = new StringBuilder();
            foreach (var battuta in _gioco.Session.Log.Recent(_con, TurniMostrati * 2))
            {
                if (battuta.Role == ChatRole.User)
                {
                    // La didascalia del motore, in ottone: e' l'unica voce del
                    // pannello che non appartiene a nessuno dei due: non l'hai
                    // detta tu e non l'ha detta lui. Constata cosa hai messo sul
                    // banco — perche' mostrare e' un gesto, e un gesto che non
                    // lascia traccia a schermo e' un gesto che il giocatore non
                    // sa se ha fatto.
                    if (!string.IsNullOrEmpty(battuta.Didascalia))
                    {
                        scritto.Append("<size=17><color=#C0983F>")
                            .Append(battuta.Didascalia)
                            .Append("</color></size>\n");
                    }
                    // Le tue parole in grafite: sul foglio le hai scritte tu.
                    // Il testo e' gia' passato dal sanificatore, quindi non puo'
                    // contenere marcatori suoi.
                    if (battuta.Content.Length > 0)
                    {
                        scritto.Append("<color=#8C8577><i>— ").Append(battuta.Content).Append("</i></color>\n");
                    }
                }
                else
                {
                    scritto.Append(battuta.Content).Append('\n');
                }
            }
            _detto.text = scritto.ToString().Trim('\n');
            InFondo();
        }

        /// Dopo ogni battuta si torna in fondo, dov'e' l'ultima cosa detta. Da
        /// li' si riavvolge con la rotella; la posizione la tiene il rullo, non
        /// noi, cosi' chi sta rileggendo non viene riportato in basso a forza.
        private void InFondo()
        {
            Canvas.ForceUpdateCanvases();
            if (_rullo != null)
            {
                _rullo.verticalNormalizedPosition = 0f;
            }
        }

        /// Cosa il motore ha registrato. A schermo perche' questa e' una scena
        /// di prova e serve a vedere la meccanica lavorare.
        private string Coda(TurnResult turno)
        {
            var ora = Amnesia.Time.WorldClock.Format(turno.Minute);
            var registrato = turno.Declared.Count > 0 ? "   ·   annotato: " + string.Join(", ", turno.Declared) : "";
            var rifiutato = turno.RefusedTags.Count > 0 ? "   ·   azione non disponibile: " + string.Join(", ", turno.RefusedTags) : "";
            var ricevuto = turno.Received.Count > 0 ? "   ·   ricevuto: " + string.Join(", ", turno.Received) : "";
            return ora + registrato + rifiutato + ricevuto;
        }
    }
}
