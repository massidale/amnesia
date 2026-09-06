# Amnesia — conoscenze, reazioni e progressione del giallo

Stato: specifica proposta, corretta il 6 settembre 2026 dopo il confronto sulle posizioni. Solo documentazione e prompt di bozza: nessuna installazione nel runtime. Questa revisione sostituisce la precedente architettura del documento, comprese le 34 varianti e le 22 scene.

## 1. Separazione conforme al codice attuale

**Conoscenze permanenti:** identità, parentele, ricordi e modo di reagire agli argomenti sono nel prompt dell'NPC. «Se Giorgio chiede di Elena…» è una direttiva di conversazione. Il motore non deve riconoscere quella domanda né cambiare scheda. Tutti gli abitanti conoscono i fatti pubblici; ognuno ha dettagli personali più profondi.

**Reazioni agli oggetti:** `PlayerInput.Parse` verifica `[mostra: oggetto]`; `ConversationSession` registra l'oggetto mostrato. `ReactionTable.Reazione` sceglie un testo da `reazioni.json`, per NPC/posizione/oggetto. `ContextBuilder` lo aggiunge come `<come_reagisci>`. Si riusa questa meccanica. Lidia e Marisa sanno chi è Anna anche senza fotografia; la foto fa aggiungere l'aneddoto di Anna e Pietro e la formula, senza sostituire la scheda.

**Posizioni narrative:** soltanto quando cambia davvero ciò che un personaggio è disposto a raccontare, accade un ritrovamento o si apre un accesso. Gli input e i prerequisiti sono verificati dal codice. Nessuna domanda libera, insistenza, tono o valutazione LLM crea una posizione. In particolare M2 viene selezionato dal possesso della giacca; la reazione all’accusa diretta vive nel prompt M2, che contiene già tutti i fatti. L'LLM interpreta le domande per rispondere, entro le conoscenze già disponibili.

Non si aggiungono un interprete LLM delle intenzioni, un classificatore dei resoconti, un validatore semantico come arbitro della progressione o una scaletta di script per ciascun argomento. Don Carlo mantiene una scheda unica: reagisce agli oggetti e ai racconti del giocatore, senza ricevere automaticamente notizie degli altri incontri.

## 2. Canone da aggiornare una volta sola


1. Presente ottobre 1987. Frana del 14 ottobre 1966; Vittorio morto nella primavera del 1966; Andrea morto nel 1981; aggressione nell'autunno del 1985. Le età esistenti restano invariate.
2. Il deposito B-17 era già usato dal gruppo prima del crollo per lampade, corde e carte. Dopo il crollo vi furono raccolti gli effetti recuperati. B-17 è una stanza interna del fabbricato B, in fondo al corridoio; conservare la geografia delle scene attuali.
3. «Chi passa per primo tiene la porta» è un detto del gruppo, anche usato fra Anna e Pietro nelle faccende quotidiane. Non è una password per riconoscere sconosciuti. La frase ha sette parole; all'inizio Rosa ricorda solo «Chi passa per primo…».
4. Rosa restituisce il foglio e la chiave, e consegna un taccuino nuovo. La fotografia non era nelle tasche recuperate nel 1985. Una stampa è conservata da Don Carlo fra vecchie carte della parrocchia; Giorgio può aver consultato un'altra stampa nel 1985. Non raccontare più che Rosa gli restituisce la foto.
5. Decisione di normalizzazione proposta: la foto ritrae otto adulti, Andrea, Rosa, Matteo, Anna, Pietro, Vittorio, Laura e Giuseppe Boasso. È una foto pubblica del 1961, non la lista dei presenti al rito. Rosa presente nella foto non significa membro; Giorgio ed Elena non erano nati. Michele Aimar può essere membro senza apparire nella foto. Aggiornare insieme numero, roster e riconoscimenti, senza nomi improvvisati. Nessun nuovo NPC giocabile.
6. La versione pubblica: gite e pranzi fra amici, poi la frana; Elena è creduta morta ma non ritrovata. I membri difendono la versione, gli altri la riportano con diversi gradi di diffidenza. Una ripetizione non dimostra un crimine e non crea automaticamente una verità.
7. Piero era amico stretto di Andrea e sa delle sue ricerche dal 1974 al 1981. Andrea era convinto della sopravvivenza, non l'aveva dimostrata a Piero. Piero sa che Giorgio continuò la ricerca; non sa di Wanda, non conosce l'aggressore e non accusa Matteo all'inizio.
8. Il rito prevedeva discesa e ritorno della bambina. Il gruppo sapeva del pericolo, ma si raccontava che fosse santo. La frana interrompe il rito. Gli oggetti non provano la sopravvivenza: lo confessa Matteo.
9. Anna conosce il deposito, ma non sa prima del ritorno di Giorgio con gli oggetti quali effetti di Elena siano finiti nella cassetta lì conservata. Non possiede una testimonianza universale della fuga: ricorda di aver visto Matteo tornare verso l'imbocco, non può certificare la posizione di ogni altro partecipante.
10. Nino trova la giacca nel presente, durante un ritorno al punto del ritrovamento: era incastrata sotto un groviglio di rami sul bordo del castagneto, ora liberato durante il lavoro. È sporca, scolorita e riconoscibile dalla toppa già nota ai paesani. Non la conserva dal 1985 e non ha nascosto una prova all'ambulanza. Il momento del ritrovamento è un evento guidato che può avvenire soltanto dopo che Matteo ha pronunciato e registrato `elena_viva`. Nessuna condizione ulteriore: non occorrono il rito, un rifiuto di Matteo o una visita al don. Nino non conosce la confessione di Matteo: riceve soltanto il proprio evento di ritrovamento.
11. Matteo salvò Elena nel 1966; nel 1985 prima collaborò e dettò l'indirizzo, poi colpì Giorgio con una pietra e lo trascinò nel bosco, per paura delle conseguenze della verità. Mantenere questo motivo già scelto; non aggiungere complici, una nuova minaccia di Giorgio o premeditazione. Il motivo è già nel prompt dopo l’ammissione: chiedere «perché?» orienta la risposta, senza cambiare posizione.
12. Chivasso resta visitabile dall'inizio. Wanda non conosce il cast di San Rocco, la famiglia Lipari, il coma né il nome completo originale di Elena. Conosce Matteo, che aveva accudito da bambino. Nel prompt chiuso non riceve il suo nome: può chiedere una parola scritta da chi ha mandato Giorgio. La lettera, presente e mostrata, autorizza l'ingresso; né complimenti né il braccialetto sostituiscono la lettera.
13. Elena è adulta, lavora in cartoleria e decide come rispondere. Entrare non produce fiducia, gratitudine o perdono automatici. Non ha conoscenza autonoma della tragedia: reagisce a ciò che Giorgio le riferisce e distingue racconto e fatto verificato.

## 3. Percorso e luogo in cui scrivere ogni comportamento

| Passaggio | Dove vive il comportamento | Condizione di stato |
|---|---|---|
| Rosa introduce Elena, restituisce chiave/foglio e dà taccuino | Scheda unica; introduzione/consegna una volta | Prima interazione; nessuna domanda da riconoscere |
| Paese: Elena, Vittorio, Laura, Andrea, crollo, Circolo | Conoscenze pubbliche + prompt personali | Nessuna |
| Piero spiega Andrea e la ricerca proseguita da Giorgio | Scheda unica, disponibile subito | Nessuna; non conferma Elena viva |
| Il don parla del Circolo e offre la foto | Scheda unica; azione di consegna separata | Richiesta esplicita verificabile, proposta sotto |
| Foto a Lidia, Marisa o Beppe: persone e aneddoto completo | Reazione `sempre/fotografia` | Foto posseduta e realmente mostrata; nessuna nuova scheda |
| Formula ad Anna: ricordo di Pietro e magazzino | Reazione contestuale `frase`; scheda invariata | Parole riconosciute dal codice; nessun requisito di foto per Anna |
| «Quale magazzino?» / «Dove?» | Il contesto della reazione conserva già ubicazione, uso e chiave | Nessuna nuova posizione né domanda da classificare |
| Chiave ad Anna | Reazione `chiave_b17` e ricordo locale già condiviso | Chiave realmente mostrata; nessuna posizione della frase |
| Accesso al B-17 e raccolta | Meccanica dei luoghi e inventario | Chiave + informazione sull'ubicazione; vedi garanzia delle battute indispensabili |
| Reperti al don | Reazione agli oggetti; indica i membri e perché sentirne più di uno | Nessuna posizione del don |
| Reperti ad Anna | A0 → A1: confessione del rito | Un reperto pertinente realmente mostrato; nessuna frase come prerequisito |
| Reperti a Laura | L0 → L1 | Un reperto pertinente realmente mostrato; niente formula |
| Reperti a Matteo | M0 → M1 | Un reperto pertinente realmente mostrato; ammette salvataggio ed Elena viva |
| Racconto delle scoperte al don | Scheda unica, condizionali sul racconto ricevuto | Nessuna; un racconto non modifica il mondo |
| Nino trova la giacca, poi la consegna | N0 → N1 | Solo Matteo ha pronunciato `elena_viva`; consegna alla prossima visita |
| Giacca ai paesani competenti | Reazione `sempre/giacca` | Giacca mostrata; una fonte competente basta |
| Giorgio possiede la giacca | M1 → M2 immediato; eventuale reazione `M2/giacca` quando mostrata | Solo possesso, senza presentazione o riconoscimento esterno richiesti |
| Accusa dell’aggressione a Matteo | Il prompt M2 fa cedere Matteo | Nessuna nuova posizione; prima dell’accusa rimane evasivo |
| «Perché?» / «Che cosa successe quella sera?» | M2 contiene già la spiegazione | Nessuna nuova posizione; anche nella stessa domanda dell'accusa |
| Richiesta del messaggio per Wanda | M2 contiene già disponibilità e destinataria; consegna separata | Richiesta esplicita verificabile, dopo ammissione; non serve chiedere il motivo |
| Wanda prima/dopo lettera | W0 → W1 | Lettera autentica posseduta e mostrata; accesso fisico e dialogo Elena |
| Elena risponde e chiede spiegazioni | Scheda unica | Accesso autorizzato da Wanda; nessuna fiducia concessa dall'LLM |

I reperti che aprono le confessioni sono `quaderno_vittorio`, `cassetta_latta`, `braccialetto`. Il `registro` aiuta a leggere il deposito, ma non diventa da solo prova equivalente del rito. Anna, Laura e Matteo raccontano parti diverse; nessuna visita a tutti e tre è richiesta per far trovare la giacca a Nino. Piero e il don aiutano senza diventare controlli obbligatori del numero di fatti raccolti.

## 4. Le sole posizioni proposte

| Personaggio | Schede | Cambiamento reale |
|---|---|---|
| Rosa, Lidia, Marisa, Beppe, Teresa, Piero, Gino, Don Carlo, Elena | Una per ciascuno | Reazioni e normali approfondimenti, senza scale |
| Anna | A0, A1 | Versione pubblica → ammissione del rito; frase e magazzino sono contesto |
| Laura | L0, L1 | Versione pubblica → consenso al rito e promessa del ritorno |
| Matteo | M0, M1, M2 | Versione pubblica → salvataggio → giacca posseduta; M2 gestisce reticenza e confessione su accusa diretta |
| Nino | N0, N1 | Conosce il ritrovamento del 1985 → trova ora la giacca |
| Wanda | W0, W1 | Tiene chiusa la casa → riconosce il messaggio e apre |

Sono 20 schede per 14 NPC. La consegna di un oggetto non richiede automaticamente una variante: si può comunicare nel contesto il possesso/consegna corrente. Ogni variante rimane completa e conserva gli approfondimenti già disponibili.

La formula rimane un passaggio dell'indagine. Il suo completamento è scritto nelle reazioni alla fotografia, non in una scheda pubblica caricata dall'inizio. Ciò non limita la conoscenza di Anna come persona. Una volta raccontato l'aneddoto, le domande successive vi possono tornare: non occorre rimostrare la foto. Non si crea una posizione per memorizzare questa conversazione.

Anna ha soltanto la versione pubblica e quella dopo i reperti. La frase non compare nei prerequisiti della sua scala. Non introdurre un nuovo livello, un contatore di visite o una scheda equivalente con altro nome: il ricordo si aggiunge al contesto della conversazione. La foto è la via narrativa per trovare le parole, non un’autorizzazione che Anna controlla quando le sente.


### Guida dentro le risposte, senza nuove tappe

Ogni NPC deve dare l’informazione di base alla prima domanda pertinente. Quando il suo contributo si esaurisce, il giocatore appare incerto o una scoperta apre una pista, aggiunge una direzione concreta: **un fatto, una persona, che cosa può chiarire**. Sono regole di risposta nel prompt, non condizioni che il motore deve classificare. Non serve aggiungere un rimando a ogni battuta e non si rimanda a qualcuno per ottenere una risposta che l’interlocutore conosce già.

| Contesto locale | Informazione da dare subito | Direzione motivata |
|---|---|---|
| Rosa introduce la ricerca | Gridavi Elena; oggetti e frammento | Marisa/Beppe conoscono le famiglie: chi era Elena? |
| Paese, domanda su Elena | Famiglia, crollo, corpo mai trovato | Piero era amico di Andrea: perché tuo padre dubitava? |
| Paese, domanda sul Circolo | Versione delle gite e dettaglio personale | Il don si opponeva al gruppo: che cosa lo preoccupava? |
| Piero, ricerca del padre | Andrea cercava e Giorgio continuò | Il don conosceva il Circolo a cui apparteneva Andrea |
| Foto mostrata | Riconoscimenti e aneddoto con formula completa | Anna condivideva quelle parole con Pietro: quale ricordo le riportano? |
| Frase detta ad Anna | Episodio di Pietro, magazzino, chiave di Andrea | Approfondire il luogo e mostrare la chiave, senza un giro da altri NPC |
| Reperti al don | Osservazione pertinente dell’oggetto | Anna per il gruppo, Laura per la famiglia, Matteo come partecipante |
| Anna/Laura ammettono il rito | La propria parte concreta, senza sapere universale | Anna vide Matteo tornare; Laura sa chi partecipava: sentire chi era lì |
| Scoperte riferite al don | Collegamento fra vecchia ricerca e coma | Nino sa dove e come fu trovato Giorgio; nessuna promessa di una giacca |
| Nino consegna giacca | Ritrovamento recente e luogo | Marisa/Beppe possono riconoscere di chi fosse |
| Giacca riconosciuta | Nome di Matteo e motivo del riconoscimento | Chiedere a lui di spiegare il ritrovamento, senza dichiararlo già colpevole |
| Matteo ammette aggressione | Ammissione netta | Lascia capire che può spiegare il perché; risponde se già chiesto |
| Wanda richiede garanzia | Due righe da chi conosce, non fiducia astratta | Il racconto del giocatore può portare lui o il don a proporre Matteo |

Se il giocatore arriva presto, riceve comunque fatti pertinenti e un appiglio disponibile. Se torna dopo aver seguito il consiglio, si parte dalla novità che racconta e non lo si rimanda allo stesso giro. La guida non suppone che tutti conoscano il diario globale. Le informazioni più profonde e i limiti restano specifici di ciascuno.

### Persistenza del ricordo della frase

`ContextBuilder` oggi aggiunge la reazione `frase` soltanto nel turno in cui `FraseDetta` è vero. Senza una scheda successiva, va mantenuto nei turni seguenti il contenuto contestuale già introdotto: episodio, ubicazione e chiave. Con il vincolo di soli prompt e scalini non aggiungiamo questo blocco tramite codice. Si usa lo storico già disponibile e si scrivono istruzioni coerenti per i seguiti. Il limite della finestra resta: non promettere memoria persistente aggiuntiva. Le dichiarazioni udite vengono annotate soltanto se pronunciate.

Per `magazzino_dove` esiste già il riconoscimento per oggetto/frase in `DeclarationService`: lasciare la dichiarazione fuori dai grants della scala di Anna per consentire il canale contestuale esistente. Il passaggio A0 → A1 riguarda solo i reperti e il rito. Non aggiungere nuovi id o un servizio di memoria.

## 5. Vincolo operativo: solo prompt e scalini

Ultima decisione dell’utente: usare esclusivamente le meccaniche già disponibili. Questa sezione sostituisce le precedenti proposte di estensione; non si autorizzano modifiche C#, nuovi comandi, pulsanti, classificatori, battute obbligatorie del motore, formati di salvataggio o memoria aggiuntiva. Rimane un lavoro di specifica prima dell’implementazione.

Si modificano le schede, le regole, i blocchi di conoscenza, i testi delle reazioni nella struttura esistente e `positions.json`. I testi canonici delle dichiarazioni fanno parte del contenuto inviato al modello e del fallback delle sue parole: vanno segnalati se contraddicono i nuovi prompt, senza introdurre nuovi id, fonti o logiche. I cataloghi editoriali precedenti non sono istruzioni per estendere il runtime.

### Meccaniche da riusare

- `PromptLibrary` e `ConoscenzeBase`: schede e sapere comune.
- `ReactionTable` e `ContextBuilder`: testi contestuali degli oggetti e della frase, più storico attuale.
- `Frase.Detta`: riconoscimento attuale, senza cambiarne l’algoritmo. La formula non diventa una posizione.
- `PositionTable`: soltanto `grants`, `requires_shown`, `requires_any_shown`, `requires_declared`, `consegna`; scala contigua e cumulativa già esistente.
- `DeclarationService` e `dichiaro`: registrazione dei fatti e riconoscimenti contestuali già previsti. Nessun nuovo classificatore o verificatore semantico.

### Traduzione delle condizioni

| Comportamento desiderato | Configurazione con le meccaniche attuali |
|---|---|
| Anna reagisce alla frase senza scaglione | `reazioni.json`, voce `anna/A0/frase`; `magazzino_dove` fuori dai grants della scala perché è già dichiarazione contestuale per `frase` |
| Anna/Laura/Matteo ammettono davanti ai reperti | `requires_any_shown` sui reperti pertinenti; contenuti nella nuova scheda |
| Nino trova e consegna dopo Matteo | N1 con `requires_declared: [elena_viva]` e `consegna: [giacca]`; eliminare il requisito del rifiuto |
| Matteo entra in M2 quando è stata consegnata la giacca | Riutilizzare `requires_declared: [nino_giacca]`, già dichiarazione di Nino con `counts_alone: true`; niente presentazione o riconoscimento esterno richiesti |
| Matteo cede solo all’accusa | Istruzioni nel prompt M2, che contiene già il racconto; nessuna condizione d’accusa nella scala |

`nino_giacca` è il riscontro narrativo esistente della consegna, non un controllo diretto di inventario. Il codice trasferisce la giacca al termine del turno N1 anche se il modello non emette quella dichiarazione; quindi possesso e registrazione possono divergere. Il prompt di Nino deve pronunciare e dichiarare il ritrovamento/consegna, ma non possiamo chiamare questa equivalenza una garanzia del motore. Non aggiungere `requires_owned` o sostituire il requisito con giacca mostrata senza una decisione esplicita.

### Limiti da non nascondere

Alcuni punti della storia proposta non sono realizzabili esattamente con soli prompt/scalini:

1. La fotografia è nell’inventario iniziale hardcoded di `Bootstrap`. Un prompt del don non può toglierla o farla diventare un oggetto nuovo.
2. `Inventario` stampa la formula completa nella prima pagina. Cambiare il prompt di Rosa non modifica quel testo visibile.
3. `ConversationSession` aggiunge un passaparola hardcoded secondo cui Nino conserva qualcosa da anni. I nuovi prompt non eliminano quella nota contraddittoria. Anche il testo canonico corrente di `nino_giacca` contiene il vecchio ritrovamento.
4. `consegna` trasferisce gli oggetti al raggiungimento dello scalino dopo un turno riuscito; non riconosce una richiesta libera. La lettera dopo la sola confessione si potrebbe consegnare con uno scalino basato su `matteo_confessa`, ma sarebbe automatica: non fingere di soddisfare anche la richiesta esplicita. Foto su domanda del Circolo presenta lo stesso limite.
5. M2 da possesso esatto e memoria contestuale oltre lo storico non hanno un predicato o un’iniezione già disponibile. Usare i riscontri esistenti con i limiti sopra, senza inventare API.

Questi punti restano incompatibilità documentate, non attività autorizzate sul motore e non modifiche narrative già accettate. Non considerarli completati mediante sole istruzioni all’LLM.

## 6. Verifica entro il perimetro

Controllare schede e reazioni, grants, riferimenti esistenti e condizioni degli scalini. Provare i dialoghi con le meccaniche correnti: prima risposta utile, rimandi motivati, frase contestuale, confessioni distinte, accusa in M2, niente alibi inventati. Separare i controlli delle condizioni deterministiche dalla qualità e affidabilità delle risposte LLM.

Non modificare inizializzazione, UI, viaggio, accessi o salvataggi per completare questo lavoro. I requisiti narrativi incompatibili elencati sopra rimangono visibili nel piano.
