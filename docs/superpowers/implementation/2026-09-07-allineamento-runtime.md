# Allineamento del runtime alla narrativa

Intervento autorizzato il 7 settembre 2026 nella sola copia
`/Users/massimo/dev_local/amnesia`. Supera i limiti del precedente lavoro
"solo prompt e scalini"; non modifica la copia iCloud.

## Comportamento applicato

- Nuova partita con inventario vuoto. Il primo saluto di Rosa restituisce
  foglio e chiave B-17 e consegna un taccuino nuovo, una sola volta.
- Prima pagina e convinzioni iniziali contengono solo "Chi passa per primo...".
  Il completamento viene annotato dopo la dichiarazione realmente udita.
- Don Carlo conserva la fotografia. L'azione "Chiedi la fotografia" usa
  `[richiedi: fotografia]`; il motore verifica l'interlocutore e impedisce duplicati.
- Foto, icona e reazioni indicano otto persone: Andrea, Rosa, Matteo, Anna,
  Pietro, Vittorio, Laura e Giuseppe Boasso. Sono "persone", non "otto adulti":
  mantenendo le eta' canoniche, Matteo aveva circa quindici anni nel 1961.
- Anna non anticipa B-17 nei grants iniziali. Ubicazione dichiarabile dopo frase
  completa oppure chiave mostrata; nessun nuovo scalino. Il contesto della frase
  resta disponibile nei seguiti usando lo stato gia' persistito degli oggetti mostrati.
- Nino trova la giacca nel presente dopo `elena_viva`; consegna nella conversazione
  riuscita. Rimosso il passaparola hardcoded sulla conservazione "da anni".
- M2 richiede il possesso della giacca, non la dichiarazione `nino_giacca`.
  M3 conserva l'ammissione registrata ma non consegna automaticamente la lettera.
  "Chiedi il messaggio" richiede `matteo_confessa` gia' registrata.
- Consegne esplicite atomiche col turno: timeout e risposte vuote non spostano
  oggetti. La prosa del giocatore o del modello non equivale all'azione.
- Wanda resta raggiungibile sulla soglia, seduta su una sedia. Porta fisica chiusa
  ed Elena non visibile/interagibile fino al possesso della lettera e alla sua
  presentazione a Wanda. Il controllo esiste anche nel dominio, non solo nella UI.
- Geografia del deposito, raccolta singola degli oggetti e viaggio disponibili
  come prima. DLL e StreamingAssets aggiornati tramite `sync-domain.sh`.

## Verifiche

- Suite .NET: 293 test superati; comprende il percorso completo fino a Elena
  e i casi negativi di accesso, consegna, inventario e richiesta contraffatta.
- Le fixture storiche restano per le prove del motore su scale sintetiche/precedenti.
  I test del canone e del percorso leggono i contenuti reali; la sincronizzazione
  viene confrontata con StreamingAssets, non con la trama storica delle fixture.
- Unity compila. `AllineamentoNarrativoCheck.Run` supera la prova in Play sulle
  due scene: inventario, saluto, taccuino, azione UI, foto a otto figure, viaggio,
  collisione dell'ingresso, accesso chiuso/aperto ed Elena.
- I test usano risposte controllate, senza chiamate AI a pagamento. Non garantiscono
  che un modello reale segua sempre le istruzioni sull'accusa o emetta ogni `dichiaro`.
  La transazione degli oggetti e le condizioni di accesso restano deterministiche.
