# Taccuino e dichiarazioni: verifica del 6 settembre 2026

## Aggiornamento: catalogo limitato alle convinzioni

Su richiesta dell’utente, eliminate le 12 dichiarazioni prive di una convinzione collegata e i relativi grants. Il catalogo attuale contiene 32 dichiarazioni, una per ciascuna delle 32 annotazioni non iniziali; restano due annotazioni iniziali. Nessuna delle dichiarazioni eliminate era usata da una condizione di progressione. Le conoscenze e le reazioni degli NPC restano disponibili nel dialogo senza chiamate a strumenti. Aggiornata la regola per esplicitarlo e sincronizzati i dati Unity. I 53 test mirati passano dopo questa riduzione. Le sezioni successive e la prova Sonda descrivono la verifica precedente alla riduzione; i limiti del motore restano validi.

## Modifiche applicate

Aggiornati `content/amnesia/declarations.json`, `positions.json`, `taccuino.json`, la regola di uso di `dichiaro` e il richiamo contestuale nel prompt Anna A0. Le copie in Unity/Assets/StreamingAssets sono sincronizzate. Nessuna modifica al motore di gioco.

Il taccuino ha 34 righe, due iniziali. Ogni altra riga dipende da una singola dichiarazione. Sono separati parentela e morte presunta di Elena, mancato ritrovamento del corpo, esistenza del Circolo e versione delle uscite, presenza di Andrea e presenza di Giorgio, salvataggio nel 1966 e sopravvivenza attuale. I dettagli del banco e del voltarsi non compaiono con la sola ammissione dell'aggressione. Anna testimonia il ritorno di Matteo verso la cava: non sa dove fossero tutti gli altri. Nino non deduce che Giorgio non potesse raggiungere il bosco da solo. La giacca è trovata oggi, non conservata dal 1985.

La frase iniziale completa resta invariata, compresa la correzione dell'utente a sette parole: è già presente nell'avvio attuale. La nota sullo scambio fra Anna e Pietro richiede invece `frase_per_riconoscersi`. Il vecchio id è mantenuto per compatibilità, ma non descrive più una parola d'ordine. `avevano_una_frase` non fa comparire quel ricordo: le varianti di Rosa e degli altri contengono informazioni differenti.

## Chi può dichiarare

- Tutti gli abitanti di San Rocco conoscono i fatti pubblici: famiglia di Elena, versione della morte, corpo assente, morte e ruolo di Vittorio, Circolo, Andrea membro e versione delle gite. Non servono foto o frase per questi fatti. Per chi ha scala, i relativi grants sono nel gradino iniziale.
- Rosa e Piero possono registrare il fatto generale delle ricerche di Andrea. Solo Piero dispone dei nuovi id `andrea_cercava_elena` e `giorgio_continuo_ricerche`. La convinzione di Andrea non è una conferma che Elena sia viva.
- Il ricordo della frase completa fra Anna e Pietro è disponibile a Lidia, Marisa e Beppe dopo aver visto la fotografia. Anna lo conosce già: A0 lo concede, ma il prompt ne prescrive l'uso in risposta alla reazione contestuale della frase e nei seguiti. Nessuno scalino nuovo.
- Solo Anna dispone dell'ubicazione precisa del magazzino. Rimosso il grant di Laura: la sua scheda non contiene quella conoscenza.
- I riti sono raccontabili da Anna A1, Laura L1 e Matteo M1, dopo uno qualsiasi dei tre reperti previsti. Eliminati i vecchi `requires_shown: frase/braccialetto` ridondanti nelle dichiarazioni governate da queste scale. Anna e Matteo conoscono il procedimento; Laura testimonia promessa, consenso e propria assenza, senza testimoniare la fuga.
- Solo Matteo M1 e successivi può dichiarare `elena_viva`. Anna, Laura e Piero non possono confermarla attraverso il racconto del giocatore. Nino N1 richiede questa dichiarazione stabilita e solo allora trova e consegna la giacca.
- Rosa, Lidia, Marisa, Beppe e Piero possono registrare il riconoscimento della giacca solo dopo averla vista. Anna e Laura restano fonti del riconoscimento per oggetto, con il limite dello schema descritto sotto. Gino non la riconosce.
- Matteo M2 resta condizionato a `nino_giacca`; la confessione dell'aggressione richiede l'accusa diretta nel prompt. M3 consegna le due righe dopo `matteo_confessa`. Invariati i trigger della progressione.

## Come arriva un fatto al taccuino

1. `ContextBuilder` prepara regole, scheda corrente, conoscenze comuni, conversazione e reazione all'oggetto/frase del turno.
2. `ConversationSession.TakeTurnAsync`, circa riga 138, aggiunge lo schema prodotto da `ToolCatalog.SchemasFor(EverSayable, SayableNow, TextOf)` alla richiesta al modello.
3. Nello strumento `dichiaro`, `parameters.properties.id.enum` contiene gli identificativi possibili per quel personaggio nell'arco della partita. La `description` della stessa proprietà contiene invece il glossario delle dichiarazioni ammesse adesso: `id: «text o variants[npc]»`.
4. Il modello scrive una battuta e può inviare `dichiaro({"id":"elena_viva"})`. `DeclarationService.Declare` rifiuta un id sconosciuto o non concesso ora. Le dichiarazioni accettate entrano nel `Register` insieme al parlante.
5. `Taccuino.Convinzioni` valuta `quando`: basta **una** delle dichiarazioni elencate con almeno una fonte registrata. È un OR, non una condizione sulla domanda, non una lista di requisiti da completare e non un'istruzione inviata al modello.
6. `sostituisce` barra la vecchia riga quando appare la nuova. Non elimina la testimonianza dal registro. `iniziale` rende una riga visibile subito.

`declarations.json` è il catalogo dei fatti registrabili. Non tutte le battute richiedono un id e non tutti gli id producono annotazioni. `text` definisce il contenuto, `variants` lo adatta alla voce del personaggio; una variante deve mantenere il significato necessario alle note collegate. `sources` identifica le fonti; per i personaggi con scala, però, i grants della scala sono autoritativi. `requires_shown` richiede tutti gli oggetti indicati per le dichiarazioni per oggetto. `truth` è metadato d'autore, non viene inviato come giudizio al modello e non sblocca niente. `counts_alone` stabilisce se basta una testimonianza per i requisiti di progressione: altrimenti servono due fonti indipendenti. Il taccuino annota già la prima, anche senza fatto stabilito.

## Limiti verificati, non risolti con nuovi scalini

- Il motore verifica gli id, non confronta semanticamente battuta e dichiarazione. La regola aggiornata impone di pronunciare l'intero fatto prima di dichiararlo. Le prove con risposte controllate verificano le condizioni deterministiche, non l'affidabilità di un LLM reale.
- Anna conosce già magazzino e ricordo; A0 li contiene nel glossario. La disponibilità contestuale resta una regola del prompt, non un blocco deterministico della chiamata. Analogamente M2 ammette `matteo_confessa` prima di interpretare un'accusa: la condotta è nel prompt, come richiesto.
- `EverSayable` prende solo i grants per chi ha scala; `SayableNow` aggiunge anche dichiarazioni per oggetto. Per Anna e Laura `giacca_e_di_matteo`, dopo l'esibizione, è accettato dal servizio ed esposto nel glossario ma manca nell'enum. Un fornitore che rispetta lo schema non può selezionarlo. Non è risolvibile coerentemente coi soli dati senza introdurre uno scalino artificiale o anticipare il riconoscimento. Servirebbe allineare i due insiemi nel motore.
- Gli identificativi dei futuri grants restano visibili nell'enum (non il loro testo esteso). I grants sono cumulativi: le ammissioni precedenti non vengono revocate e le vecchie versioni restano nel vocabolario. I prompt prescrivono di mantenere le confessioni già fatte.

## Verifica

- Build Sonda riuscita.
- 53 test mirati superati: taccuino, registro, dichiarazioni, sessione e catalogo strumenti. Aggiunti sette casi contro annotazioni anticipate. Corretto il test di integrità che confrontava il taccuino reale con una fixture storica delle dichiarazioni: ora confronta i due file reali.
- 19 turni Sonda superati, usando il motore reale con trasporto controllato, senza chiamate LLM. Esiti in `2026-09-06-taccuino-sonda.json`.
- Non rieseguita né dichiarata verde la suite completa: rimangono le incompatibilità con le vecchie aspettative narrative già documentate nell'intervento precedente.
