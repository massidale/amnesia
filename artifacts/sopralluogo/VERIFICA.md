# Sopralluogo del 6 settembre 2026

Nota successiva: i video precedono la sostituzione della strada verso la
galleria con binari e sbarramento ferroviario. Per il confine aggiornato
consultare Unity/Assets/SanRocco1987/Previews/21_confine.png: sbarra,
cartello e collider sono all'inizio del percorso, sul lato del villaggio.

Scene salvate: SanRocco1987 e Chivasso1987. Unity 6000.5.9f1, Metal.

## Esiti

- Cinque verifiche Node superate: layout, interni, abitanti, depositi e Chivasso.
- SAN_ROCCO_APPEARANCE_OK: insegne e coperture della cava.
- CHIVASSO_OK: sei edifici visitabili, due identita, accessi e fermata.
- VIAGGIO_OK: prova in Play San Rocco -> Chivasso -> San Rocco.
- SOPRALLUOGO_OK: 2224 fotogrammi del villaggio e 600 di Chivasso.
- Materiali senza shader mancanti; percorsi camera degli ingressi senza intersezioni.

Le sequenze comprendono ingressi e panorami degli interni, giro aereo,
cava, cimitero, bosco, corridoio B, B-17, strada della galleria e piazza.
Gli indici nelle cartelle delle sequenze indicano gli intervalli esatti.
Video a 20 fps, 960 x 600. Le tavole contatto hanno eventuali riquadri
neri di riempimento, non fotogrammi neri della scena.

## Correzioni emerse

- Chiuso il raccordo scoperto fra accesso laterale e stanza della cava:
  dalle riprese si vedeva il cielo. Aggiunti volta, pareti e controllo verso l'alto.
- Allineato il piano di rotolamento delle ruote del treno ai binari.
- Elena ora guarda verso Wanda; camera della casa con due letti singoli.
- La cartoleria contiene carta, quaderni e strumenti da scrittura.
- Il mouse non ruota la visuale mentre il cursore e' libero.

## Limiti

Sopralluogo a camera programmata e ispezione di fotogrammi rappresentativi,
non playtest manuale esaustivo di ogni collisione o spazio tra mobili.
Il treno e' scenografico; il trasporto effettivo e' la corriera delle due
ore indicata dalla storia. Nessuna simulazione dell'orario delle 14:00.
Chivasso e' un quartiere interpretato, non una ricostruzione topografica.
Dialoghi, primo incontro alla porta di Wanda, progressione, apertura
narrativa del B-17 e salvataggio fra scene non sono integrati nella visita.

Resta un avviso Graphics Ring Buffer nella Scene View dell'editor.
L'instancing dei materiali e' abilitato, ma non elimina ogni avviso:
l'ottimizzazione e la profilazione prestazionale restano aperte.
