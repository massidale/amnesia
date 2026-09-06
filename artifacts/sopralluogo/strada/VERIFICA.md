# Strada principale - 6 settembre 2026

## Correzione salvata

Il controllo iniziale ha rilevato 501 campioni con strade sovrapposte alla stessa quota, 4 sotto il terreno e 6 troppo vicini al terreno. Le immagini mostravano macchie triangolari negli incroci, dovute a z-fighting.

La geometria viene ora ritagliata sulle facce reali del terreno, con distacco costante: 8,5 cm per la via principale e 6 cm per i raccordi. Nessuna proiezione eseguita durante il gioco, nessun nuovo collisore. Conservati tracciato, bordi irregolari e rappezzi.

Controllo dopo la riparazione: 56.106 campioni, zero sotto il terreno, zero troppo vicini, zero sovrapposti alla stessa quota; distacco minimo 8,5 cm. Scena SanRocco1987 salvata. Generatore aggiornato per mantenere la correzione alle ricostruzioni successive.

## Prestazioni: verifica non conclusa

La camminata iniziale di 72 m ha mostrato oscillazione verticale nulla. Frame mediano 82,1 ms, p95 124,4 ms nell'Editor con Scene e Game aperti: non e' una misura di una build standalone.

Un esperimento di batching statico solo in Play non ha prodotto un confronto valido: il test usava il delta del frame di gioco dentro EditorApplication.update, che non ha necessariamente la stessa cadenza. La temporizzazione del test e' stata corretta; il successivo controllo UI di Unity ha iniziato a restituire ripetutamente timeout, anche dopo il reset del collegamento.

Nessun batching runtime e' stato aggiunto alla scena o al gioco. Il difetto grafico e' corretto e verificato; non si dichiara risolto il calo generale di FPS.
