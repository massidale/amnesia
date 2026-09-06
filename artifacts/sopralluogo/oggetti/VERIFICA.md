# Verifica oggetti 3D - 6 settembre 2026

## Risultati

- 11 prefab separati e relative miniature generati in Unity, con geometria e materiali persistenti.
- 8/8 test NUnit PlaceService superati: requisiti porta, apertura senza raccolta, raccolta singola, rifiuto duplicati/estranei/oggetti di altri, stato conservato dopo serializzazione.
- Test Unity `OGGETTI_PLAY_OK`: modelli non vuoti e rotazione verificati sui pixel per tutti gli 11 asset; apertura cassetta; raccolta separata; recupero braccialetto dalla cassetta gia' posseduta.
- Raggiungibilita' verificata con lo stesso raycast del giocatore a 1,62 m di altezza, davanti agli scaffali. Il test ha trovato il bordo della cassetta davanti al braccialetto; corretto con un appoggio di carta ripiegata e ripetuto con esito positivo.
- Corretto il verso della cerniera; verificato che il coperchio aperto salga sopra il perno.
- Ispezionate le immagini renderizzate, corretti orientamento di fotografia/giacca e ordine delle righe del foglio. Iniziali EV sul lato interno del braccialetto. Inventario con anteprima grande e miniature reali.
- Controlli Node su villaggio, interni, NPC, depositi, Chivasso, scene giocabili e mappa stilizzata superati.
- Test Unity `PARTITA_OK` dopo andata e ritorno da Chivasso: sessione e proprieta' conservate; quaderno raccolto assente dalla scena al ritorno e cassetta non assegnata automaticamente.

## Limiti E Distinzioni

La suite NUnit generale ha dato 267 superati e 8 falliti: gli errori riguardano fixture/dichiarazioni e aspettative narrative non allineate ai contenuti correnti (fra cui `avevano_una_frase`, scale di Don Carlo/Wanda, conoscenze comuni). Non sono stati riscritti dati narrativi concorrenti per far passare questi test.

La proprieta' degli oggetti persiste nella sessione, non e' stato introdotto un salvataggio nuovo su disco. I test non chiamano il modello linguistico.

La frase non ha prefab fisico. Lapide fissa e taccuino personale non diventano oggetti raccoglibili nel magazzino. Registro, giacca e fogli mantengono i loro canali narrativi di acquisizione. Scarpe e fermaglio sono contenuto visivo della cassetta, non nuove voci del catalogo.
