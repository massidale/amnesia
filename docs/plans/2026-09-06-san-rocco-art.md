# San Rocco - rifinitura low poly

Direzione approvata in conversazione: paese montano piemontese nel 1987,
autunno raccolto e leggermente malinconico, leggibile e non fantasy.
Layout, porte e progressione narrativa restano invariati.

## Architettura

Estendere Kit per geometrie arrotondate sfaccettate e testi con profondita.
Separare i dettagli estetici in SanRoccoArt.cs. Mantenere Build come
orchestratore e i prefab separati. Nessuna dipendenza aggiuntiva.

## Sequenza

- [x] Test Unity CheckPolish: soffitti, testo con depth test, creste continue,
  rosone, assenza di lampioni nei fabbricati; eseguirlo prima delle modifiche.
- [x] Correggere tetti privi di interno, testo visibile attraverso le pareti,
  lampioni nella chiesa, supporti delle panchine e orientamento della lavagna.
- [x] Aggiungere fronti finestrati e cornici, basamenti in pietra, tetti
  stratificati, portale e rosone della chiesa, piccoli arredi sulle facciate.
- [x] Sostituire le creste a blocchi con un rilievo continuo; chiome sfaccettate
  e sottobosco; pavimentazione della piazza a elementi e terreno variegato.
- [x] Chiudere la stanza della cava mantenendo gli imbocchi; ridurre la luce
  che attraversa i muri; calibrare ombre, luce autunnale e colori dei materiali.
- [x] Rigenerare in Unity, eseguire tutti i controlli di ingressi, percorsi,
  aspetto e rifinitura; vedere i render generali, piazza, cava e interni.
- [x] Verificare in Play mode che la scena parta, poi uscire senza salvare
  modifiche runtime. Documentare limiti residui e mostrare i render reali.

## Verifiche

`node tools/check-san-rocco.mjs` conserva gli orientamenti e gli ingombri.
Il generatore esegue ValidateBuildings, ValidatePaths, ValidateAccessConnections
e CheckEntrances. I menu Verifica scritte e cava e Verifica rifinitura leggono
la scena e devono terminare con SAN_ROCCO_APPEARANCE_OK e SAN_ROCCO_POLISH_OK.
La verifica visiva non viene sostituita dai controlli di geometria.

## Esito 2026-09-06

CheckPolish falliva sulla scena iniziale (soffitto assente); passa dopo la
rigenerazione, insieme a CheckAppearance. Layout: 20 luoghi senza overlap.
Validazione percorsi: 2304 campioni; 20 accessi collegati. Verificati render
di insieme, piazza, chiesa, insegna e imbocchi cava. Corretti anche insegna
coperta dalla pensilina e intradossi invisibili degli sporti. Smoke test Play
riuscito, non equivalente a un walkthrough manuale completo di tutti i luoghi.
