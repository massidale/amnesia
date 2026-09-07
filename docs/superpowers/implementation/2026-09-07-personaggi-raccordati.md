# Personaggi raccordati e camminata di Giorgio

Lavoro nella copia locale `/Users/massimo/dev_local/amnesia`.
Aggiornati i 15 prefab conservando GUID, identita', posizioni in scena e pose sedute.
Nessuna mappa rigenerata, nessuna modifica alla copia iCloud.

- Volumi low poly con normali morbide per volti, capelli, mani e bacino.
- Braccia e gambe a capsule continue, con articolazioni gerarchiche e raccordi
  alle spalle, ai gomiti, al bacino e alle ginocchia. Polsini arretrati rispetto alle mani.
- Pose sedute adattate alla quota di sedie e panchina; gonne e grembiuli dedicati.
- Barbe/baffi, montature, acconciature, orecchini, spille, collane e dettagli da lavoro
  differenziati. Le scelte visive non aggiungono indizi o modificano la storia.
- Giorgio: rotazione visiva locale di 180 gradi, soltanto sul prefab con il nuovo
  componente, per raccordare il fronte -Z del modello al +Z del controller.
- `PassoPersonaggio` anima anche/ginocchia/spalle/gomiti dallo spostamento orizzontale
  effettivo in LateUpdate. Ritorno graduale al riposo, reset sui teletrasporti,
  cadenza limitata alle alte velocita'. Nessuna modifica alla velocita' del giocatore.
  Si tratta di animazione procedurale degli arti, non di un sistema di foot IK.

## Rigenerazione

`Amnesia / San Rocco 1987 / Aggiorna solo personaggi` salva i prefab e produce
le anteprime `personaggi_v2_*`, poi riapre SanRocco1987. Non rigenera edifici o terreno.
Le mesh morbide e le capsule sono condivise; nessun pacchetto esterno aggiunto.

## Controlli

- `PeopleRevisionCheck.Verify`: 15 identita', posa, articolazioni, mesh e un solo
  collider per prefab; alternanza delle gambe, arresto e teletrasporto.
- `VisualChecks.CheckPeopleAndRooms`: 12 abitanti nelle posizioni assegnate,
  Teresa sulla panchina e nessuna nuova intersezione del torace con gli arredi.
- `PeopleWalkPlayCheck.Run`: orientamento +Z, movimento tramite CharacterController,
  oscillazione degli arti e arresto in Play. I movimenti di test non vengono salvati.
  La prova usa un driver runtime e un timestep riproducibile di 60 Hz: il ciclo
  editor e i piccolissimi delta del batch non rappresentano la velocita' di gioco.
- Render frontali e di tre quarti; controllo di Teresa seduta nella scena reale.
- Revisione statica dei nuovi script senza rilievi sostanziali.
