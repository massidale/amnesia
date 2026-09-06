using System;
using System.Collections.Generic;
using System.Linq;
using Amnesia.Dialogue;
using UnityEngine;
using UnityEngine.UI;

namespace AmnesiaUnity
{
    public sealed partial class Menu
    {
        private GameObject _tasche;
        private RectTransform _griglia;
        private Text _dettaglio;
        private Text _nomeOggetto;
        private Text _conteggio;
        private Text _invito;
        private ScrollRect _dettaglioRullo;
        private string _oggettoScelto;
        private AnteprimaOggetto _anteprimaOggetto;
        private Button _apriCassetta;
        private readonly Dictionary<string, Image> _schede = new Dictionary<string, Image>();
        private static readonly Color Inchiostro = Stile.Carta;

        private static Button Pulsante(Transform parent, string label, Vector2 min, Vector2 max, Action action)
        {
            var rect = Stile.Riquadro(parent, label, Stile.Pannello, min, max);
            var border = rect.gameObject.AddComponent<Outline>();
            border.effectColor = Stile.Bordo;
            border.effectDistance = new Vector2(2f, -2f);
            var text = Stile.Scritta(rect, Stile.Macchina, 14, Stile.Carta, new Vector2(.04f,0f), new Vector2(.96f,1f));
            text.text = label;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors;
            colors.highlightedColor = new Color(.68f,1f,.86f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(() => action());
            return button;
        }

        private void CostruisciTasche()
        {
            _tasche = new GameObject("inventario", typeof(RectTransform));
            _tasche.transform.SetParent(_radice.transform, false);
            Stile.Ancora((RectTransform)_tasche.transform, new Vector2(.06f,.12f), new Vector2(.94f,.81f));
            _conteggio = Stile.Scritta(_tasche.transform, Stile.Macchina, 14, Stile.Ottone,
                new Vector2(0f,.90f), new Vector2(.48f,1f));
            var area = Stile.Riquadro(_tasche.transform, "oggetti in tasca", Color.clear,
                Vector2.zero, new Vector2(.49f,.89f));
            var viewport = Stile.Feritoia(area, "vista oggetti", Vector2.zero, Vector2.one);
            _griglia = new GameObject("schede", typeof(RectTransform)).GetComponent<RectTransform>();
            _griglia.SetParent(viewport, false);
            _griglia.anchorMin = new Vector2(0f,1f);
            _griglia.anchorMax = Vector2.one;
            _griglia.pivot = new Vector2(.5f,1f);
            _griglia.sizeDelta = Vector2.zero;
            var layout = _griglia.gameObject.AddComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            layout.spacing = new Vector2(12f,12f);
            var fitter = _griglia.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = _griglia;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f;

            var paper = Stile.Riquadro(_tasche.transform, "oggetto esaminato", Stile.Pannello,
                new Vector2(.53f,0f), Vector2.one);
            Stile.Filo(paper, new Vector2(0f,0f), new Vector2(.008f,1f), Stile.Ottone);
            var heading = Stile.Scritta(paper, Stile.Interfaccia, 14, Stile.Grafite,
                new Vector2(.06f,.92f), new Vector2(.94f,.98f));
            heading.text = "OGGETTO SELEZIONATO";
            _nomeOggetto = Stile.Scritta(paper, Stile.Interfaccia, 28, Inchiostro,
                new Vector2(.06f,.77f), new Vector2(.94f,.88f));
            _nomeOggetto.fontStyle = FontStyle.Bold;
            var previewArea = new GameObject("area modello", typeof(RectTransform));
            previewArea.transform.SetParent(paper, false);
            Stile.Ancora((RectTransform)previewArea.transform, new Vector2(.04f,.20f), new Vector2(.51f,.76f));
            var preview = new GameObject("modello 3D", typeof(RectTransform), typeof(RawImage));
            preview.transform.SetParent(previewArea.transform, false);
            var ratio = preview.AddComponent<AspectRatioFitter>();
            ratio.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            ratio.aspectRatio = 1;
            _anteprimaOggetto = preview.AddComponent<AnteprimaOggetto>();
            _apriCassetta = Pulsante(paper, "", new Vector2(.06f,.01f), new Vector2(.94f,.09f), ApriCassetta);
            _dettaglio = Stile.Rullo(paper, Stile.Interfaccia, 20, Inchiostro,
                new Vector2(.55f,.20f), new Vector2(.94f,.76f), out _dettaglioRullo);
            _invito = Stile.Scritta(paper, Stile.Interfaccia, 15, Stile.Grafite,
                new Vector2(.06f,.10f), new Vector2(.94f,.19f));
            _invito.resizeTextForBestFit = true;
            _invito.resizeTextMinSize = 12;
            _invito.resizeTextMaxSize = 15;
            _tasche.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!Aperto || _pagina != Pagina.Tasche) return;
            // CanvasScaler e layout possono cambiare misura dopo l'apertura o un resize.
            var layout = _griglia.GetComponent<GridLayoutGroup>();
            var width = Mathf.Max(100f, (_griglia.rect.width - 12f) / 2f);
            if (Mathf.Abs(layout.cellSize.x - width) > .5f)
                layout.cellSize = new Vector2(width, 132f);
        }

        private void AggiornaTasche()
        {
            foreach (Transform child in _griglia)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            _schede.Clear();
            var items = _gioco.Taccuino.Tasche().Where(i => i.Id != "frase" && i.Id != "taccuino").ToList();
            _conteggio.text = "IN TASCA   /   " + items.Count + " OGGETTI";
            Canvas.ForceUpdateCanvases();
            _griglia.GetComponent<GridLayoutGroup>().cellSize = new Vector2(
                Mathf.Max(100f, (_griglia.rect.width - 12f) / 2f), 132f);
            foreach (var item in items)
            {
                var button = Pulsante(_griglia, "", Vector2.zero, Vector2.one, () => Esamina(item));
                _schede[item.Id] = button.GetComponent<Image>();
                DisegnaSimbolo(button.transform, item.Id);
                var label = Stile.Scritta(button.transform, Stile.Macchina, 14, Stile.Carta,
                    new Vector2(.06f,.04f), new Vector2(.94f,.35f));
                label.alignment = TextAnchor.MiddleCenter;
                label.text = NomeOggetto(item);
                label.raycastTarget = false;
            }
            var selected = items.FirstOrDefault(i => i.Id == _oggettoScelto) ?? items.FirstOrDefault();
            if (selected != null) Esamina(selected);
            else
            {
                _nomeOggetto.text = "Le tasche sono vuote";
                _anteprimaOggetto.gameObject.SetActive(false);
                _apriCassetta.gameObject.SetActive(false);
                _dettaglio.text = "Gli oggetti che raccogli o ricevi compariranno qui. Selezionane uno per osservarlo.";
                _invito.text = "Il taccuino conserva i tuoi appunti.";
            }
        }

        private static string NomeOggetto(ItemDefinition item) => string.IsNullOrEmpty(item.Name) ? item.Id : item.Name;

        private void Esamina(ItemDefinition item)
        {
            _oggettoScelto = item.Id;
            _anteprimaOggetto.gameObject.SetActive(true);
            _anteprimaOggetto.Mostra(item.Id, OggettoRaccoglibile.Aperta(_gioco), !_gioco.World.ItemOwners.ContainsKey("braccialetto"));
            _apriCassetta.gameObject.SetActive(item.Id == "cassetta_latta" && !_gioco.World.ItemOwners.ContainsKey("braccialetto"));
            _apriCassetta.GetComponentInChildren<Text>().text = OggettoRaccoglibile.Aperta(_gioco) ? "Raccogli il braccialetto" : "Apri la cassetta";
            foreach (var pair in _schede)
            {
                var selected = pair.Key == item.Id;
                pair.Value.color = selected ? new Color(.12f,.32f,.29f) : Stile.Pannello;
                pair.Value.GetComponent<Outline>().effectColor = selected ? Stile.Ottone : Stile.Bordo;
            }
            _nomeOggetto.text = NomeOggetto(item);
            _dettaglio.text = item.Visible + (string.IsNullOrEmpty(item.Description) ? "" : "\n\n" + item.Description);
            _invito.text = "Mostra in dialogo: [mostra: " + NomeOggetto(item) + "]";
            Canvas.ForceUpdateCanvases();
            _dettaglioRullo.verticalNormalizedPosition = 1f;
        }

        private void ApriCassetta()
        {
            if (_oggettoScelto != "cassetta_latta" || !_gioco.World.ItemOwners.TryGetValue("cassetta_latta", out var owner) || owner != "player") return;
            if (!OggettoRaccoglibile.Aperta(_gioco)) _gioco.World.Flags[OggettoRaccoglibile.CassettaAperta] = true;
            else _gioco.Porte.Raccogli(_gioco.World, "magazzino_b17", "braccialetto");
            AggiornaTasche();
        }

        // Piccoli pittogrammi disegnati con elementi UI: nessuna immagine di indizi inventati.
        private static void DisegnaSimbolo(Transform parent, string id)
        {
            var icon = Resources.Load<Texture2D>("Oggetti1987/Icone/" + id);
            if (icon != null)
            {
                var area = new GameObject("miniatura", typeof(RectTransform));
                area.transform.SetParent(parent, false);
                Stile.Ancora((RectTransform)area.transform, new Vector2(.1f,.37f), new Vector2(.9f,.95f));
                var picture = new GameObject("oggetto", typeof(RectTransform), typeof(RawImage));
                picture.transform.SetParent(area.transform, false);
                var fit = picture.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 1;
                picture.GetComponent<RawImage>().texture = icon;
                picture.GetComponent<RawImage>().raycastTarget = false;
                return;
            }
            void Line(float x, float y, float w, float h)
            {
                var line = Stile.Riquadro(parent, "segno", Stile.Carta, new Vector2(x,y), new Vector2(x+w,y+h));
                line.GetComponent<Image>().raycastTarget = false;
            }
            if (id == "chiave_b17")
            {
                Line(.25f,.57f,.17f,.035f); Line(.25f,.77f,.17f,.035f);
                Line(.25f,.57f,.025f,.23f); Line(.40f,.57f,.025f,.23f);
                Line(.42f,.67f,.33f,.035f); Line(.66f,.58f,.025f,.10f); Line(.73f,.58f,.025f,.10f);
            }
            else if (id == "braccialetto")
            {
                Line(.34f,.52f,.32f,.025f); Line(.34f,.82f,.32f,.025f);
                Line(.34f,.52f,.02f,.30f); Line(.64f,.52f,.02f,.30f);
                Line(.46f,.50f,.08f,.06f);
            }
            else
            {
                Line(.32f,.48f,.36f,.025f); Line(.32f,.86f,.36f,.025f);
                Line(.32f,.48f,.02f,.40f); Line(.66f,.48f,.02f,.40f);
                if (id == "fotografia")
                {
                    Line(.37f,.53f,.05f,.18f); Line(.47f,.53f,.05f,.22f); Line(.57f,.53f,.05f,.18f);
                }
                else
                {
                    Line(.39f,.74f,.21f,.02f); Line(.39f,.65f,.21f,.02f); Line(.39f,.56f,.14f,.02f);
                }
            }
        }
    }
}
