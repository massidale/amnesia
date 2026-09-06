# Oggetti low poly 1987

Undici prefab separati, in metri, caricabili con `Resources.Load<GameObject>("Oggetti1987/" + itemId)`.
Gli ID corrispondono a `content/items.json`. `frase` non e' un oggetto fisico; `lapide` e' un memoriale fisso; il taccuino personale mantiene la propria schermata.

I prefab contengono geometria, materiali e collider, non logica di raccolta. Le istanze del B-17 ricevono `OggettoRaccoglibile`: quaderno sul ripiano e cassetta al piano inferiore. Il braccialetto e' un prefab annidato nella cassetta, sopra carta ripiegata; scarpine e fermaglio sono parti decorative del suo contenuto, non nuovi ID narrativi.

Il registro non e' nel magazzino: si ottiene attraverso la storia, come giacca e fogli. Quando sono posseduti, tutti usano il proprio modello nell'inventario.

## Rigenerazione

Menu Unity: Amnesia > San Rocco 1987 > Crea oggetti e aggiorna B-17.
Aggiorna prefab, miniature e scena SanRocco1987 senza ricostruire terreno o personaggi. La generazione completa del villaggio include gli stessi oggetti.

Menu di verifica: Verifica oggetti in Play. Prova collisori dalla normale altezza del giocatore, raccolta singola e anteprime non vuote/ruotabili; salva immagini in `artifacts/sopralluogo/oggetti`.

## Interazione

Puntare l'oggetto e premere E. La cassetta si apre alla prima interazione e si raccoglie alla successiva; il braccialetto puo' essere preso direttamente oppure dalla cassetta gia' in inventario. Trascinare il modello nell'inventario per ruotarlo, anche sul retro. Le iniziali EV sono sul lato interno della piastrina.

La proprieta' e l'apertura sono conservate nel World della sessione e sopravvivono ai dialoghi e ai viaggi. Questo non introduce un nuovo sistema di salvataggio su disco.
