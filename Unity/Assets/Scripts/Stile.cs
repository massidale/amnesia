using UnityEngine;
using UnityEngine.UI;

namespace AmnesiaUnity
{
    /// L'aspetto dell'interfaccia, in un posto solo.
    ///
    /// L'idea: quello che il giocatore ha davanti non e' un'interfaccia di
    /// gioco, e' l'incartamento di Giorgio. Un uomo che non si fida della
    /// propria memoria scrive tutto a macchina e lo tiene in una cartella: per
    /// questo il taccuino, i nomi e le etichette sono in Courier, e solo le
    /// voci della gente hanno il carattere di un libro.
    ///
    /// L'ottone e' la firma, e si spende una volta sola per schermata: e' il
    /// colore della targhetta legata alla chiave, e segna la cosa dentro cui sei
    /// adesso — il nome di chi hai davanti, la pagina aperta. Tutto il resto e'
    /// carta e grafite.
    public static class Stile
    {
        public static readonly Color Notte = new Color(0.071f, 0.067f, 0.055f, 0.95f);
        public static readonly Color Velo = new Color(0.043f, 0.043f, 0.039f, 0.97f);
        public static readonly Color Carta = new Color(0.902f, 0.875f, 0.800f);
        public static readonly Color Grafite = new Color(0.549f, 0.522f, 0.467f);
        public static readonly Color Ottone = new Color(0.753f, 0.596f, 0.247f);
        public static readonly Color Ruggine = new Color(0.549f, 0.290f, 0.196f);
        public static readonly Color Incavo = new Color(1f, 1f, 1f, 0.05f);

        private static Font _macchina;
        private static Font _libro;

        /// Courier Prime, per tutto cio' che e' scritto invece che detto.
        public static Font Macchina => _macchina != null
            ? _macchina
            : _macchina = Carattere("CourierPrime-Regular");

        /// Crimson Text, per le voci. Un carattere da libro perche' le battute
        /// sono l'unica cosa di questo gioco che non e' un documento.
        public static Font Libro => _libro != null
            ? _libro
            : _libro = Carattere("CrimsonText-Regular");

        private static Font Carattere(string nome)
        {
            var caricato = Resources.Load<Font>("font/" + nome);
            // Un carattere che non c'e' non deve lasciare l'interfaccia muta:
            // meglio brutta e leggibile che vuota.
            return caricato != null ? caricato : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static Canvas Tela(string nome, int ordine)
        {
            var canvas = new GameObject(nome).AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ordine;
            // Senza questo uGUI disegna il testo alla risoluzione di
            // riferimento e poi lo stira: le lettere vengono sgranate, e non e'
            // il carattere — e' l'atlante ridimensionato.
            canvas.pixelPerfect = true;
            var scala = canvas.gameObject.AddComponent<CanvasScaler>();
            scala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scala.referenceResolution = new Vector2(1280f, 720f);
            scala.matchWidthOrHeight = 0.5f;
            scala.referencePixelsPerUnit = 100f;
            scala.dynamicPixelsPerUnit = 2f;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Riquadro(Transform genitore, string nome, Color colore, Vector2 min, Vector2 max)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(genitore, false);
            go.AddComponent<Image>().color = colore;
            return Ancora(go.GetComponent<RectTransform>(), min, max);
        }

        /// Un filo d'ottone. E' l'unico ornamento che questa interfaccia si
        /// concede, e vuol dire una cosa sola: sei qui dentro.
        public static RectTransform Filo(Transform genitore, Vector2 min, Vector2 max, Color colore)
        {
            var filo = Riquadro(genitore, "filo", colore, min, max);
            return filo;
        }

        public static Text Scritta(Transform genitore, Font carattere, int corpo, Color colore, Vector2 min, Vector2 max)
        {
            var go = new GameObject("scritta");
            go.transform.SetParent(genitore, false);
            var testo = go.AddComponent<Text>();
            testo.font = carattere;
            testo.fontSize = corpo;
            testo.color = colore;
            testo.alignment = TextAnchor.UpperLeft;
            testo.horizontalOverflow = HorizontalWrapMode.Wrap;
            testo.verticalOverflow = VerticalWrapMode.Truncate;
            testo.lineSpacing = 1f;
            Ancora(go.GetComponent<RectTransform>(), min, max);
            return testo;
        }

        /// Un riquadro che taglia quello che esce. Serve dove il testo cresce e
        /// nessuno sa quanto: una conversazione lunga o un taccuino pieno
        /// finivano fuori dal pannello, sopra il paese.
        public static RectTransform Feritoia(Transform genitore, string nome, Vector2 min, Vector2 max)
        {
            var go = new GameObject(nome, typeof(RectTransform));
            go.transform.SetParent(genitore, false);
            go.AddComponent<RectMask2D>();
            return Ancora((RectTransform)go.transform, min, max);
        }

        /// Testo che cresce verso l'alto e viene tagliato in cima: l'ultima
        /// battuta sta sempre in fondo, dove l'occhio la cerca.
        public static Text Colonna(RectTransform feritoia, Font carattere, int corpo, Color colore)
        {
            var testo = Scritta(feritoia, carattere, corpo, colore, Vector2.zero, Vector2.one);
            var rect = testo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            testo.verticalOverflow = VerticalWrapMode.Overflow;
            testo.alignment = TextAnchor.LowerLeft;
            var misura = testo.gameObject.AddComponent<ContentSizeFitter>();
            misura.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return testo;
        }

        public static RectTransform Ancora(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
