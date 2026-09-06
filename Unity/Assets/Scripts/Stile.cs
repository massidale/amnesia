using UnityEngine;
using UnityEngine.UI;

namespace AmnesiaUnity
{
    /// Interfaccia low poly: un solo font, superfici piatte e contrasti netti.
    public static class Stile
    {
        public static readonly Color Notte = new Color(.08f, .11f, .14f, .98f);
        public static readonly Color Velo = new Color(.035f, .05f, .065f, .98f);
        public static readonly Color Carta = new Color(.91f, .95f, .96f);
        public static readonly Color Grafite = new Color(.63f, .71f, .76f);
        public static readonly Color Ottone = new Color(.40f, .82f, .68f);
        public static readonly Color Ruggine = new Color(.92f, .48f, .34f);
        public static readonly Color Incavo = new Color(.26f, .34f, .39f, .65f);
        public static readonly Color Pannello = new Color(.13f, .18f, .22f);
        public static readonly Color Bordo = new Color(.30f, .39f, .44f);

        private static Font _interfaccia;
        public static Font Interfaccia => _interfaccia != null
            ? _interfaccia
            : _interfaccia = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Alias per i chiamanti esistenti: dialoghi, menu e HUD usano lo stesso font.
        public static Font Macchina => Interfaccia;
        public static Font Libro => Interfaccia;

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

        /// Un testo che si puo' scorrere. Serve alla conversazione: le battute
        /// vecchie non spariscono, si riavvolgono — e in un gioco in cui la
        /// prova e' una frase detta tre scambi fa, poterla rileggere mentre si
        /// parla non e' comodita', e' la meccanica.
        public static Text Rullo(Transform genitore, Font carattere, int corpo, Color colore,
            Vector2 min, Vector2 max, out ScrollRect rullo)
        {
            var contenitore = new GameObject("rullo", typeof(RectTransform));
            contenitore.transform.SetParent(genitore, false);
            Ancora((RectTransform)contenitore.transform, min, max);
            rullo = contenitore.AddComponent<ScrollRect>();

            var vista = Feritoia(contenitore.transform, "vista", Vector2.zero, Vector2.one);
            var testo = Scritta(vista, carattere, corpo, colore, Vector2.zero, Vector2.one);
            var rect = testo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            testo.verticalOverflow = VerticalWrapMode.Overflow;
            var misura = testo.gameObject.AddComponent<ContentSizeFitter>();
            misura.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            rullo.viewport = vista;
            rullo.content = rect;
            rullo.horizontal = false;
            rullo.vertical = true;
            rullo.movementType = ScrollRect.MovementType.Clamped;
            rullo.scrollSensitivity = 28f;
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
