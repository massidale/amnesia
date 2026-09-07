# San Rocco: dettagli ispirati a Village

Intervento nella sola copia `/Users/massimo/dev_local/amnesia`.
Riferimento osservato tramite rendering di `Assets/Scenes/Village.unity`.
Ripresi vegetazione sfaccettata, densita' dei dettagli e raccordi fra case e verde;
non copiati edifici fantasy o asset acquistati.

## Scena salvata

- Layout, piazza, strade, edifici, interni, personaggi e collisioni preesistenti invariati.
- Livello `09_Dettagli_Village`: 149 alberi, 332 arbusti, 4612 ciuffi,
  pietre e chiazze irregolari, raggruppati in 72 mesh territoriali.
- Ogni edificio ha un figlio `Village_Dettagli`, che si sposta con il suo proprietario:
  vasi, fiori, plinti, pluviali, cassette e piccoli allestimenti secondo la destinazione.
- Una mesh decorativa per edificio: 92 renderer aggiuntivi in tutto, un materiale
  a colori di vertice, nessuno script per-frame o nuova collisione.
- Terreno visivo ricolorato con geometria identica e vertici duplicati accorpati.
  Mesh del collider originale conservata. Luce ambientale leggermente schiarita.
- Nuovi asset originali: circa 30 MB, senza importare nuovi pacchetti.

## Rigenerazione e verifiche

Menu `Amnesia / San Rocco 1987 / Applica dettagli stile Village`.
Il comando opera sulla scena salvata, non richiama `Crea nuova scena completa`.
Sostituisce solo il livello decorativo da lui generato; modifiche manuali dentro
quel livello vengono sovrascritte. Non usarlo per conservare ritocchi manuali
ai dettagli generati. Le anteprime sono in `Assets/SanRocco1987/Previews/village_*`.

- Controllate 8816 trasformazioni originali e l'identita' dei collider prima/dopo.
- Test geometrico sulle mesh stradali, anche prive di collider: nessun vertice
  decorativo basso invade i percorsi. Corretto il difetto iniziale dei ciuffi sull'asfalto.
- Controllo materiali, mesh non vuote e numero dei batch.
- Controllo visivo dei render a terra (ingresso, piazza, case) e dall'alto.
- Otto controlli statici delle scene superati; 293 test .NET superati.
- `AllineamentoNarrativoCheck.Run` superato in Play dopo il salvataggio.

Non e' un benchmark degli FPS: i batch limitano i renderer aggiuntivi,
ma le prestazioni dipendono dalla visuale e dalle impostazioni dell'editor.
