# Applicazione narrativa — contenuti e scalini

Resoconto storico del 6 settembre. Le incompatibilita' residue sotto elencate
sono state affrontate nell'intervento autorizzato del 7 settembre, descritto in
`2026-09-07-allineamento-runtime.md`.

Implementati prompt, conoscenze condivise, reazioni, saluti e testi canonici delle dichiarazioni. Nessuna modifica ai file C#, a id/fonti/condizioni delle dichiarazioni, catalogo oggetti, geografia, inizializzazione o taccuino.

Anna A0 resta invariata alla frase; la reazione fornisce il ricordo e il prompt disciplina quando pronunciare l’ubicazione. A1 confessa ai reperti. Laura L1 confessa ai reperti senza formula. Matteo M1 racconta il salvataggio; M2 dipende dalla dichiarazione `nino_giacca` e contiene la risposta all’accusa diretta. M3 è lo scalino di consegna dopo `matteo_confessa`, che permette di conservare la lettera con il motore attuale. Nino N1 dipende solo da `elena_viva`. Wanda W1 dipende dalla lettera mostrata. Gli altri NPC hanno scheda unica.

La lettera viene consegnata automaticamente dopo la confessione: non si attende una richiesta separata. Il possesso effettivo della giacca non è un predicato della scala: si usa il riscontro narrativo di Nino, con i limiti già documentati.

La foto rimane iniziale e con undici persone, coerente col catalogo esistente; i prompt non simulano una nuova consegna del don. Il completamento della frase rimane visibile nella UI attuale. Il codice conserva il vecchio passaparola su Nino e alcune righe del taccuino mantengono formulazioni precedenti: sono incompatibilità note fuori dal perimetro dei prompt/scalini, non risolte da questa applicazione. Il glossario delle dichiarazioni conserva gli id esistenti, anche dove il nome storico descrive la vecchia lettura.

## Verifiche eseguite

- Baseline prima delle modifiche: 260/260 test superati.
- Suite dopo le modifiche: 253/260 superati. I 7 fallimenti elencati sotto verificano contenuti o strutture narrative precedenti; test e fixture non sono stati modificati per rispettare il perimetro dei contenuti.
- Sonda del progetto, con risposte controllate: 27 turni completati. Verificati dichiarazioni ammesse/rifiutate, frase senza cambio di Anna, reperti a Laura senza formula, Nino senza oggetto prima della confessione di Matteo, giacca dopo la sola `elena_viva`, M2 prima di mostrare la giacca, nessuna lettera prima dell’ammissione, consegna dopo `matteo_confessa`, nessun duplicato, prompt di Wanda prima/dopo lettera e incontro con Elena.
- 29 file di contenuto modificati/aggiunti sincronizzati byte per byte con StreamingAssets; versioni obsolete eliminate da entrambe le copie.
- Hash dei 111 file C# confrontati con l’inizio del lavoro: invariati. Catalogo oggetti e luoghi invariati rispetto alla copia iniziale. Il taccuino non è stato modificato da questa attività: durante la verifica finale vi sono comparsi cambiamenti esterni, conservati e non copiati automaticamente nella versione Unity.
- Id, fonti, condizioni e counts_alone delle dichiarazioni invariati; modificati soltanto testi canonici e varianti. Nessun nuovo predicato negli scalini.
- Regole ridotte a 277 parole. I 21 profili includono anche M3 per la consegna della lettera.

Le risposte della Sonda sono controllate: questa prova verifica il flusso e il contenuto dei prompt assemblati, non certifica il comportamento di un modello LLM reale. Nessuna chiamata al provider effettuata.

### Test ancora riferiti alla versione precedente

- `DaUnMondoFreddoLaCatenaFinoAllUltimoGradinoDiMatteoSiPercorre`.
- `IQuattroSenzaScalaHannoQualcosaDaDire`.
- `LeFixtureSonoLaCopiaEsattaDelContenuto`.
- `LeCompareRicevonoLaVersioneDelPaese`.
- `ChiHaUnaPosizioneSuaNonLaRiceve`.
- `LeDueTabelleSiCaricanoESonoQuelleDelGiocoIntero`.
- `LaVersioneDelPaeseEUnCoroEEscePariPariDaOgniBocca`.

Le aspettative superate riguardano il coro verbatim, l’assenza di conoscenze condivise per i membri, la scala del don e l’assenza di quella di Wanda, il passaggio di Anna alla frase e l’identità dei contenuti con le fixture storiche. Non sono stati alterati il motore o i test per ripristinare quelle scelte narrative.



Nel motore attuale `EverSayable` per chi ha una scala include solo i grants della scala: lasciare `magazzino_dove` fuori la rendeva assente dall’enum del tool. Per conservare un percorso dichiarabile senza cambiare codice, il grant è in A0 e il prompt ne disciplina l’uso dopo la frase, come M2 disciplina l’ammissione dopo l’accusa. Questa disponibilità nel glossario è una limitazione della protezione dei segreti tramite soli contenuti, non una nuova condizione del motore.
