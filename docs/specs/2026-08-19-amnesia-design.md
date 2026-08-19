# AMNESIA — design

> Spec meccanico. La storia sta in `2026-08-19-amnesia-storia.md`, le tabelle in `2026-08-19-amnesia-contenuto.md`.

Amnesia riusa il motore di Valnera senza modificarne l'architettura. Aggiunge una meccanica sola — **il confronto** — e sottrae tutto il livello mercantile.

## Cosa Amnesia toglie a Valnera

**Scambi, offerte, denaro: via.** Niente `[dai:]`, niente `[offro: N monete]`, niente `accept_offer`, `propose_terms`, `respond_to_proposal`, niente listino, niente borsa. Giorgio non compra niente da nessuno e non ha una lira in tasca che conti.

Resta **un solo canale dal giocatore al personaggio**:

```
[mostra: <oggetto>]
```

Questo semplifica e non indebolisce: la proprietà di sicurezza del motore — *i modelli possono mentire a parole ma non possono muovere la merce* — vive in `PlayerInput`, che resta l'unico a decidere cosa il giocatore possiede davvero. Con un canale solo, quella superficie si riduce invece di allargarsi.

**Gli oggetti che un personaggio consegna a Giorgio** (le due righe di Matteo, la busta di Don Carlo, la scatola di Rosa) non passano da una trattativa: **li concede il motore quando un predicato è soddisfatto.** Non c'è niente da negoziare e quindi niente da simulare. Un personaggio che non vuole dare una cosa non la dà — non perché rifiuta un'offerta, ma perché il predicato non è vero.

## Cosa Amnesia toglie: i numeri

**Via anche il livello sociale numerico.** `fiducia`, `sospetto`, `paura`, `fastidio` come scalari, e con essi `appraise_turn` con i suoi dieci assi.

La ragione non è la semplicità: è che **non li legge più niente.** In Valnera quei numeri esistevano per una cosa sola — decidere se Giorgio firmava — e ogni cancello era una soglia. In Amnesia nessun cancello dice `fiducia > 0.6`: i cancelli sono predicati su dichiarazioni registrate e oggetti mostrati.

Contatori che nessuno interroga non sono peso morto innocuo: **sono una tentazione.** Il primo cancello scritto di fretta li userà, e il gioco si ritrova «convinci la barra» dentro un progetto che aveva deciso di non esserlo. E `appraise_turn` costa token a ogni singolo turno per produrre numeri destinati a un registro che nessuno guarda.

### Ma la memoria resta, ed è quello che tiene in piedi il gioco

Tolto tutto, un personaggio tratterebbe il turno 1 e il turno 30 allo stesso modo: uno che il giocatore ha insultato cinque volte lo saluterebbe come la prima. È lì che un gioco di conversazione diventa un distributore automatico con delle facce sopra, ed è **il vero rischio del «film giocabile»** — molto più della linearità dei cancelli.

Quello che si tiene quindi non è *la relazione*: è **cosa c'è stato fra quei due**, in forma di fatti.

> Al posto di `fiducia: 0.31 · paura: 0.22 · sospetto: 0.40`, il blocco dice:
> *gli hai mostrato la frase e la fotografia · gli hai chiesto tre volte di Elena · è la quinta volta che torni · una volta gli hai dato del bugiardo.*

**L'inferenza emotiva la fa il modello**, che è la cosa che sa fare meglio di qualunque formula. E quei fatti il motore li ha già: sono l'asse *cosa il personaggio sa che tu sai*, più il conteggio delle visite e le poche cose che vale la pena marcare (un'accusa diretta, un insulto, una promessa).

La paura di Matteo non è un numero da simulare: **è una funzione di cosa il giocatore gli ha messo davanti**, e quella lista esiste già.

### Cosa resta di `appraise_turn`

Quasi niente, e va scritto per evitare che rientri dalla finestra. Il perito serviva a convertire la qualità di un turno in delta di stato; senza stato da muovere, non serve.

Sopravvive **una cosa sola**, perché ha una conseguenza reale: che il giocatore sia stato **offensivo o aggressivo**. Un uomo a cui dai del bugiardo in faccia si comporta diversamente, e quello va marcato — non come scalare, ma come fatto nella lista di cosa c'è stato fra voi.

Tutto il resto — credibilità, risonanza, pressione, pertinenza, novità, tema, categoria di leva — esce.

### Il vincolo, e la concessione onesta

Amnesia è **più vincolata di Valnera**, ed è uno scambio voluto. Valnera era aperta, e il suo difetto era che si poteva vincere parlando bene senza che al giocatore importasse niente della storia. Amnesia è vincolata, e il suo difetto è che può sembrare su binari.

Quello che la salva non sono i cancelli, è dove *non* ci sono: **le due realizzazioni che contano non sono gated affatto.** Che la frase che Giorgio ripete è la loro, e che quello che suo padre cercava era Elena, non le dice nessun personaggio e non le sblocca nessun oggetto. Le fa il giocatore, o non succedono.

## Lo stato del gioco

Due modi ovvi di modellare la progressione sono tutti e due un passo indietro rispetto al motore che c'è già, e vanno scartati per iscritto perché sono le due strade in cui si ricasca per comodità.

**Prompt diversi per atto** rimette in piedi il *gating per istruzione* che l'architettura esiste apposta per evitare. Scrivere a un modello «adesso sei nell'atto 2, adesso puoi ammettere di essere stato alla cava» produce un narratore che recita una scaletta, e il giocatore lo sente. Peggio: se il giocatore arriva alla verità in un modo non previsto, il personaggio la nega comunque, perché l'atto non è avanzato.

**Oggetti trigger in ordine fisso** trasforma un gioco di conversazione in un'avventura a chiave-e-serratura. Se per parlare con Matteo del 1966 bisogna prima aver trovato il quaderno, il quaderno non è un indizio: è un lucchetto. E la prima volta che un giocatore trova la verità da solo e il gioco gli dice di no, ha smesso di giocare.

Dentro tutte e due c'è però la cosa giusta: i personaggi *devono* comportarsi diversamente man mano, e alcune cose *devono* arrivare in un ordine. La differenza è da cosa dipendono.

> **Lo stato del gioco non è un contatore. È l'insieme delle proposizioni che il giocatore ha stabilito — e, per ciascun personaggio, quali di quelle proposizioni gli sono state messe davanti.**

Nessun personaggio cambia perché il gioco è avanzato. Cambia perché **il giocatore** gli ha messo davanti qualcosa.

## Le tre cose che oggi si confondono

Il motore di Valnera tiene già la prima. Ad Amnesia servono tutte e tre, e vanno tenute separate:

| | Cos'è | Dove vive | Chi la scrive |
|---|---|---|---|
| **Cosa sa il personaggio** | La sua colonna epistemica. Non cambia quasi mai. | `KnowledgeService`, già esistente | l'autore |
| **Cosa ha stabilito il giocatore** | Le proposizioni che il gioco considera dimostrate. | registro nuovo, `world.flags` | il motore |
| **Cosa il personaggio crede che tu sappia** | Solo ciò che gli è stato messo davanti *da lui*. | asse nuovo, per coppia (npc, proposizione) | il motore |

**La terza è quella che fa funzionare Matteo**, ed è la cosa che oggi manca. Matteo si comporta in due modi radicalmente diversi a seconda che creda o no che Giorgio ricordi. E quella convinzione non nasce da un atto: nasce da cosa Giorgio gli ha detto e mostrato **in faccia**. Puoi sapere tutto della cava e restare, per lui, un ragazzo uscito dal coma che non ricorda niente — finché non gliene parli.

Questo dà «prompt diversi per stato» gratis, senza atti e senza scalette: il prompt di Matteo è diverso a ogni conversazione perché il blocco *ciò che sa di te* è diverso, e la sua scheda non è cambiata di una virgola.

## Come una proposizione si stabilisce

Serve una regola sola, deterministica, che il motore possa verificare:

**Una proposizione si stabilisce quando ha due sostegni indipendenti**, dove un sostegno è uno di questi tre:

1. un **oggetto** che la sostiene, presentato a qualcuno che l'ha riconosciuto;
2. una **dichiarazione** di un personaggio, registrata dal motore con lo strumento;
3. la **stessa dichiarazione da una seconda bocca** che non ha motivo di dire quella cosa.

Con l'eccezione dichiarata: **un personaggio che ammette una cosa contro il proprio interesse vale da solo.** Matteo che dice «l'ho portata via io» non ha bisogno di conferme.

È la regola delle «due vie non-mendaci» già scritta per la tabella epistemica, applicata a runtime. E ha il pregio di non avere niente da interpretare: il motore conta i sostegni.

## Le posizioni, invece degli atti

Ogni personaggio guardingo ha una **scala di posizioni**. Non è un atto globale: è locale a lui, e ogni gradino ha come precondizione un **predicato sulle proposizioni stabilite**, non un numero.

Matteo, per esempio:

| | Posizione | Si sale quando |
|---|---|---|
| **M0** | Cordiale. La versione del paese, detta da uno che c'era: non so cosa cercassero, ero andato via prima, la piccola è rimasta sotto. | — |
| **M1** | Ammette di essere stato lassù fino alla fine. | stabilito che il Circolo si riuniva alla cava **e** che lui era del Circolo |
| **M2** | Ammette il rito. Non la bambina: il rito. | stabilito cosa fosse la Stanza |
| **M3** | Ammette di aver sentito una voce sotto le pietre. | stabilita l'esistenza della cassetta di latta, cioè che una bambina era davvero là sotto |
| **M4** | Ammette di averla portata via, e dice **a chi**. | stabilito che Elena non è mai stata sepolta |

M4 non è «dice l'indirizzo»: l'indirizzo Giorgio ce l'ha in tasca dal primo minuto, scritto di suo pugno sotto dettatura di quest'uomo due anni fa. Quello che Matteo ha e Giorgio no è **chi ci abita** — che a quel numero civico c'è una donna che si chiama Wanda Cauda e la bambina che lui ci ha portato in braccio nel 1966.

È la differenza fra un nome di strada e una persona, ed è tutto il gioco: **le informazioni che contano non sono mai coordinate, sono sempre nomi.**

**La scheda di Matteo non cambia mai.** Cambia il blocco che il motore gli inietta ogni turno, e ogni posizione è scritta **in prima persona come convinzione sincera**, mai come istruzione. Non «adesso puoi ammettere il rito», ma quello che quest'uomo, oggi, con quella roba sul banco, ritiene di poter ancora tenere in piedi.

E — regola che vale doppio qui — **nessun prompt gli dice mai che sta mentendo.** M0 non è «menti sul rito»: è la versione che si racconta da ventun anni e a cui crede quasi. Un modello a cui chiedi di mentire scrive i segnali della menzogna. Un modello che esprime una convinzione scrive convinzione.

## Perché un personaggio non può rivelare troppo presto

Il timore è giusto e la risposta è più forte di una scaletta: **un LLM non rivela quello che non ha.**

Nessun personaggio riceve mai la storia intera. Ciascuno riceve la propria colonna, e la posizione corrente decide **quanta di quella colonna gli viene messa nel prompt di questo turno**. Nel prompt di Matteo a M0 la frase *ho sentito una voce sotto le pietre* non c'è. Non si trattiene: non ce l'ha.

Questo è il punto che va scritto a caratteri cubitali nello spec, perché è facilissimo sbagliarlo:

> **La posizione non governa cosa il personaggio può dire. Governa cosa il personaggio ha.**

Un personaggio a cui si scrive «sai X ma non dirlo ancora» prima o poi lo dice, o peggio: lo lascia trasparire, allude, fa la faccia di chi sa. Un personaggio che X non ce l'ha è semplicemente un uomo che non sa quella cosa, e recita quello.

**Il rischio residuo, quello vero, è l'inferenza.** Dai a un modello abbastanza fatti contigui e dedurrà quello mancante enunciandolo come se lo sapesse: Anna, che sa del rito e sa che Matteo era lassù, può arrivare da sola a «e se quella bambina non fosse mai stata sotto». Contro questo nessuna serratura serve a niente. Servono due cose, entrambe noiose e obbligatorie: **colonne magre** — si toglie dal prompt tutto ciò che non serve a *recitare*, non solo ciò che è segreto — e la **campagna di validazione**, che deve misurare esattamente questo su ogni personaggio.

## Le serrature vere

Serrature sì, e ce ne sono già tre o quattro nella storia. Con una regola sola:

> **Si chiude l'accesso fisico, mai un argomento di conversazione.**

Una porta chiusa è onesta: il giocatore vede la porta, capisce che gli manca qualcosa, e sa cosa sta cercando. Un argomento chiuso è il gioco che gli dice di no mentre finge di essere una persona.

E ogni serratura ha **più chiavi**, così non c'è mai un solo percorso:

| Serratura | Cosa blocca | Chiavi |
|---|---|---|
| **Sapere cos'è la frase** | il magazzino, e quindi tutto il primo atto | la frase ce l'ha già: gli manca **capire che serve a qualcosa**. **Don Carlo**, che ci ha predicato contro per dieci anni. **Nino**, che l'ha sentita al bar vent'anni fa. Oppure dirla a un membro e guardarlo in faccia. |
| **Il magazzino B-17** | il quaderno di Vittorio, il registro delle presenze, la cassetta di latta col braccialetto | la chiave d'ottone ce l'ha già: gli manca **dov'è**. Lo sanno in tre — **Anna**, **Laura** e **Matteo**. Le prime due lo dicono a chi pronuncia la frase. Matteo non lo dice a nessuno. |
| **Le carte di Andrea** | sette anni di ricerca — illeggibili finché non sai chi cercava | **ce le ha Rosa**, in una scatola, da due anni. Non le ha buttate e non le ha lette. Per averle deve decidere di smettere di proteggere suo marito — e non è un ritrovamento, è una conversazione che le costa. |
| **La busta di Andrea** | la lettera del padre, e con essa l'ultimo atto | **ce l'ha Don Carlo dal 1981**, con la condizione che Andrea gli ha dettato: *dalla a mio figlio se un giorno viene a chiedere di quella bambina*. È una serratura **dichiarata**: Don Carlo dice subito che esiste e qual è la condizione. Non c'è nessun'altra chiave, e non serve — la condizione la soddisfa chiunque giochi. |
| **La galleria murata** | il posto dove è successo, e il ricordo che Giorgio ci lascia dentro | **Nino**, che ha gli attrezzi e conosce la montagna. Oppure **Anna**, che sa del secondo imbocco che usava il Circolo per non farsi vedere dalla strada. |
| **La porta di Wanda** | Elena | **Matteo**, e nient'altro: due righe di suo pugno, oppure lui in persona. È l'unico di cui Wanda si sia mai fidata su questa faccenda, e per ottenerle serve sapere cosa ha fatto nel 1985. |

Quattro serrature, nessun ordine obbligato fra le prime tre.

**Una chiave, un mestiere.** È la regola che governa tutta la tabella, e la prima stesura la violava: il braccialetto apriva cinque cose diverse e quindi non ne apriva nessuna in particolare. Adesso ogni oggetto fa una cosa sola e riconoscibile:

| Chiave | Il suo unico mestiere |
|---|---|
| **La frase** | prova che sei già dentro la cosa → i membri smettono di fare la scampagnata |
| **Il braccialetto** | prova che in quel buco c'era una bambina → Laura, Anna e Don Carlo smettono di dire la versione |
| **La testimonianza di Anna** | mette Matteo da solo sopra quel buco → Matteo cede |
| **Nino e il foglio** | mettono Matteo con Giorgio la notte del 1985 → il quarto mistero |
| **Due righe di Matteo** | l'unica firma di cui Wanda si fidi → la porta |

**Chivasso non è una serratura.** L'indirizzo è uno dei tre oggetti con cui Giorgio si sveglia: un nome di via in un paese a due ore, scritto da lui, senza una spiegazione. Ci può andare il primo giorno.

E **deve valere la pena andarci il primo giorno**, altrimenti è un vicolo cieco travestito. Chi ci va presto non trova Elena — trova una donna di sessantasei anni che apre di dieci centimetri, sente il nome della via che lui le sta leggendo dal suo stesso foglio, e **si spaventa**. Non lo caccia perché è scortese: lo caccia perché ha paura, e la paura è un'informazione. Il giocatore torna a San Rocco senza Elena e con una cosa che prima non aveva: **a quell'indirizzo c'è qualcuno che ha qualcosa da perdere.**

È il cancello di comprensione di cui sopra. La distanza non è fisica: è sapere cosa hai in mano.

E le chiavi sono tutte **persone**. Nessuna è un documento da recuperare in un ufficio, nessuna è un oggetto nascosto sotto un tappeto. In un gioco il cui unico verbo è parlare, ogni serratura si apre convincendo qualcuno a dirti una cosa che sa.

## Gli atti

Non sono un contatore: sono il nome che diamo a **quello che il giocatore ha in mano**. Un atto finisce quando cambia la chiave, non quando scatta un flag.

Il ciclo è sempre lo stesso: **mostri una cosa, impari un nome, vai dal nome successivo.**

### Sinossi

| | Entri con | Esci con |
|---|---|---|
| **1 — La frase** | cinque parole, un nome, una foto, un indirizzo, una chiave | il braccialetto: in quel buco c'era una bambina |
| **2 — Il braccialetto** | la prova che c'era una bambina | tre strade che indicano lo stesso uomo |
| **3 — Matteo** | la testimonianza di Anna | Elena è viva, Wanda vuole lui, e lui dice di no |
| **4 — La bugia** | uno stallo, e una parola che gli è scappata | cinque pezzi che lasciano libero un posto solo |
| **5 — Il secondo scusa** | tutto tranne una confessione | la confessione, il ricordo, e due righe per Wanda |
| **6 — Le due porte** | due lettere in tasca | tuo padre, e Elena |
| **Epilogo — Le tre porte** | la verità intera | quello che decidi di farne |

## Il primo atto: la frase e il magazzino

### 1. Il capezzale scrive le prime due righe del taccuino

Rosa gli dà due cose, e non sa di dargli niente.

**La frase**, per intero. Gliel'ha sentita ripetere per mesi quando aveva tre anni e per settimane prima del coma — sono cinque parole, non si dimenticano.

**E un nome.** *Elena.* Anche quello lo ripeteva da bambino, e anche quello lei non ha mai collegato a niente: di Elene, in paese, ce n'erano quattro, e nessuno le ha mai detto che quella notte alla cava c'era una bambina.

Giorgio se le scrive sul taccuino. Sono le prime due righe, e sono **tutto il gioco in due righe**: una frase che apre le persone e un nome da cui parte tutto, e lui non sa cosa sia nessuna delle due.

**Esce dall'ospedale con la formula del Circolo in tasca e nessuna idea di cosa sia.** È il cancello migliore del gioco perché non è un cancello: è una cosa che ha già e non sa usare. Il primo atto non consiste nel *trovare* la chiave, ma nel **capire che quella cosa che dice da ventun anni apre le persone.**

Rosa gli dà anche, senza capire di darglielo, il terzo filo: quella targhetta d'ottone le dice qualcosa, perché **suo marito per vent'anni ha pagato l'affitto di un posto** e lei non ha mai chiesto di cosa. Non sa dove sia. Sa che esiste — ed è già più di quanto sappia Giorgio.

### 2. Chiedere di Elena non serve a niente, ed è il punto

Il nome è sul taccuino dal primo minuto, quindi il giocatore andrà a chiedere di Elena subito. Deve poterlo fare, e **non deve ottenere niente**.

Chiunque, in paese, gli racconta la stessa cosa: la bambina della cava, la frana del '66, la lapide. **La versione, parola per parola.** Non stanno nascondendo: è quello che sanno, ed è vera per due terzi.

Il giocatore raccoglie così, senza accorgersene, la prova più importante del gioco — la stessa frase impossibile da tre bocche diverse — mentre crede di star sbattendo contro un muro.

### 3. La fotografia apre il paese, e i membri hanno una scampagnata pronta

Giorgio riconosce due facce su undici. Mostrarla in giro è tutto il primo atto: ogni persona che ne riconosce una gli dà un nome, e ogni nome è una porta a cui bussare.

**Le conoscenze sono deliberatamente disuguali.** Chi non c'era sa poco e lo dice volentieri. Chi c'era ha una risposta pronta, la stessa da vent'anni:

> *Ah, quella. Era una compagnia di amici, si andava su a fare le scampagnate alla cava. Roba di quando eravamo giovani.*

**Era la copertura vera**, ed è quello che si sono raccontati anche fra loro. Non è una bugia che recitano: è la frase con cui hanno spiegato quelle serate alle mogli, ai figli, ai carabinieri e infine a se stessi. Un modello che la dice **non sta mentendo** — sta dicendo quello che quest'uomo dice sempre, ed è per questo che va scritta nella scheda come sua convinzione e non come istruzione a nascondere.

E quasi tutti in paese — anche chi del Circolo non ha mai fatto parte — sanno una cosa sola, la stessa: **che quelli lì avevano una frase**, che se la dicevano fra loro e che nessun altro ha mai saputo. È il pettegolezzo di vent'anni.

### 4. Nessuno gli dice la frase. Gli dicono che esiste

Questo è il meccanismo, e va scritto con precisione perché è delicato.

**Nessun personaggio pronuncia mai la formula.** Chi non era del Circolo non l'ha mai saputa. Chi c'era non la dice a uno che non è dei loro. Quello che il paese sa, e che ripete da vent'anni, è **una forma vaga con un dettaglio dentro**:

> *Avevano una specie di detto fra loro. Qualcosa di una porta.*

*Qualcosa di una porta.* È tutto. Ed è abbastanza, perché **il giocatore quella frase ce l'ha scritta sul taccuino dalla prima scena**, e contiene la parola *porta*.

Il collegamento non lo fa nessun personaggio. **Lo fa il giocatore**, ed è la prima cosa che questo gioco chiede di capire invece che di trovare — e per farla non serve nessun oggetto, solo aver ascoltato.

Chi non ci arriva ha comunque la strada di dietro: **provarla e guardare la faccia.** Un giocatore che la dice a un membro senza sapere cosa sta facendo ottiene lo stesso una reazione violenta, e impara guardando. Chi sperimenta viene premiato, e il gioco non deve spiegargli niente.

### 5. Non è una parola d'ordine

Questo va scritto chiaro perché è il punto in cui il primo atto può diventare una spy story.

Anna non sente il controsegno e apre il portone. Anna è una donna di cinquantotto anni che sente **cinque parole che non sente da ventun anni** uscire dalla bocca di un ragazzo appena uscito dal coma. Non gli parla perché è autorizzato. **Gli parla perché con uno che sa non ha più senso fare la scampagnata.**

E la sua reticenza non è vergogna. **Anna a quelle cose ci crede ancora.** Non parla del rito con un estraneo per la stessa ragione per cui non gli racconterebbe cosa ha detto in confessione: perché è sacro, non perché è sporco. È molto più difficile da smuovere della colpa — e molto più inquietante, perché il giocatore si accorge lentamente che l'unica persona disposta a raccontargli quelle serate gliele racconta **dall'interno**, con le parole di chi ci credeva e ci crede ancora.

**I membri rimasti in paese sono tre: Anna, Laura, Matteo.** Anna e Laura cedono alla frase e dicono dov'è il magazzino.

**Matteo no.** Matteo sente cinque parole che non sente da ventun anni uscire dalla bocca del ragazzo che ha creduto di ammazzare, e va nel panico. Non dà informazioni. Fa una cosa sola, e la fa ogni volta:

> *Chi te l'ha detta? … Cosa ti ricordi?*

**È la domanda di un uomo terrorizzato, e il giocatore non ha ancora nessun modo di sapere perché.** Non è un indizio: è una stonatura. Uno a cui reciti una vecchia preghiera dovrebbe chiederti dove l'hai sentita — non cosa ti ricordi.

E dirla a Laura è brutale in un altro modo: è la formula con cui hanno calato sua figlia nell'acqua.

### Cosa si può chiedere del padre, e quando

Un giocatore che guarda quella fotografia domanda subito di suo padre, e deve poter fare quella domanda dal primo minuto — semplicemente non gli risponde nessuno.

Prima della frase, Andrea è **un uomo perbene che è morto sei anni fa**, e chiunque tiene il punto: brava persona, un po' chiuso, gli dispiace tanto. Non stanno mentendo di più di quanto mentano su tutto il resto: è la scampagnata applicata a un morto.

Dopo la frase, no. **Anna e Laura sanno benissimo che Andrea era dei loro** — c'erano insieme per otto anni — e a uno che ha appena recitato la formula non ha più senso raccontare che il suo vecchio era un uomo tranquillo che andava a fare le gite.

**È la terza cosa che la frase sblocca**, e probabilmente la più dolorosa: non un luogo e non un rito, ma **suo padre**. Il giocatore va a chiedere del Circolo e si porta a casa una cosa su di lui.

Il registro, più avanti, non gli dirà niente di nuovo. Gli darà solo la firma.

### 6. Il magazzino chiude il primo atto — con le domande, non con le risposte

Vincolo, non dettaglio: **se il magazzino consegna Elena e il sacrificio, il secondo atto non ha più niente da scoprire parlando**, e il gioco diventa una caccia agli oggetti con dei figuranti intorno.

Tre oggetti, e **ognuno ha un mestiere solo**. Un oggetto che non smuove niente non è atmosfera: è rumore, e va tagliato.

**La cassetta di latta** — scarpine, un fermaglio, un braccialetto inciso *E.V.* Non spiega niente. Da questo momento **nessuno può più dire che lassù non c'era una bambina**, ed è la chiave del secondo atto.

**Il quaderno di Vittorio** — il rito scritto per esteso. Non dice chi, non dice quando, non nomina nessuno: dice **cosa doveva succedere**. Che per far tornare qualcuno di là, qualcuno di qua deve passare la soglia e tornare indietro. Il suo mestiere è uno: **trasforma «quelli lì facevano dei riti» in una cosa precisa e insopportabile**, e cambia la domanda del giocatore da *cosa combinavano* a *chi doveva passare*. Senza di lui il secondo atto non ha una domanda.

**Il registro delle presenze** — nomi di battesimo e iniziali, riunione per riunione, dal 1958. Il suo mestiere è **l'ultima pagina**: la sera di ottobre del 1966, con l'elenco di chi c'era. **A. L.** c'è. **L. V.** non c'è.

Fa due cose in una riga: **inchioda tuo padre a quella notte per iscritto** — non è più un'inferenza, è un documento — e **conferma Laura**, che quando dirà di non essere stata lassù starà dicendo la verità. È l'unico pezzo di carta di tutto il gioco che dà ragione a qualcuno.

Il sacrificio non è scritto da nessuna parte. Esce da una bocca, non da una carta — ed è il secondo atto.

## Il secondo atto: il braccialetto, e tre strade verso Matteo

Il braccialetto è il secondo passe-partout, e funziona su tutti quelli su cui la frase non bastava. **Tre persone cedono, e tutte e tre finiscono per indicare lo stesso uomo.**

### Laura

Confessa il rito, e la sua confessione è insieme vera e interessata: **lei nella cava non c'era.** È rimasta a casa. Si è pentita per ventun anni. Chiede a Giorgio di non giudicarla, e dice la cosa che si dice da ventun anni per riuscire ad alzarsi la mattina: **che era assoggettata a suo marito.**

Ed è vera per tre quarti — Vittorio l'aveva portata dentro quel gruppo, e dopo la sua morte è stato il gruppo a tenerla lì, in nome di lui. Ma resta il fatto che quella sera sapeva, e resta il fatto che non era assoggettata a nessuno mentre stava in cucina con la luce accesa.

**È l'unico personaggio la cui versione è vera e vile insieme**, e il gioco non deve arbitrare. Poi indica: se vuoi sapere cosa è successo lì dentro, chiedi ad Anna e a Matteo, che c'erano.

### Anna

Dice **per chi** era il sacrificio: per Vittorio. Che è la cosa che rende quella notte comprensibile invece che mostruosa — non stavano uccidendo una bambina, stavano richiamando un morto, e per farlo qualcuno doveva passare.

E dice una cosa che a lei sembra un dettaglio: che quando lei è scesa in paese, **alla cava era rimasto solo Matteo.**

**Non lo sta accusando.** Non le è mai passato per la testa. Sta ricordando chi c'era e chi non c'era, ed è l'unica testimonianza al mondo che mette quell'uomo da solo sopra un buco in cui c'era una bambina.

### E poi Anna dice la cosa che non stava dicendo a nessuno

Perché mentre ricorda chi c'era e chi non c'era, arriva anche a lui.

> *E c'eri anche tu, sai. Tuo padre ti aveva portato su. Non aveva nessuno a cui lasciarti.*

Lo dice come si dice una cosa ovvia, perché per lei lo è: a quel ragazzo è appena morto il padre da sei anni, sarà una consolazione sapere che erano insieme.

Il registro, quando il giocatore torna a guardarlo, ha **A. L.** e sotto, con la stessa mano, *+ il bambino.*

**È il primo ricordo che torna**, e torna come tornano tutti in questo gioco: una riga sul taccuino che il giocatore non ha scritto.

> *— il buio di una stanza laterale. Una lampada. Delle voci più avanti. Una corda che scende. (ricordo)*

Non c'è nessuna faccia. **Non ancora**: quella arriva alla fine, dopo la lettera, dentro la galleria. Qui il gioco dà al giocatore soltanto la cosa che ribalta tutta la sua posizione, e gliela dà a metà partita, con una signora che gli ha appena fatto un complimento:

**non stai indagando su una cosa successa ad altri. C'eri.**

### Don Carlo

Non può dire niente, e non dice niente. Ma è un uomo di sessantadue anni con due confessioni sullo stomaco — **quella di Andrea, e quella di Giorgio**, che è venuto da lui prima del coma e adesso non se lo ricorda.

Davanti alla fotografia il vincolo tiene e l'uomo no:

> *Io non posso parlare, figliolo. Non posso… Elena. Povera creatura. Elena…*

Non ha rivelato niente. Ha solo ripetuto un nome tre volte, ed è **peggio di una rivelazione**, perché il giocatore capisce che quest'uomo sa tutto e non lo dirà mai.

Davanti al braccialetto fa l'unica cosa che gli resta:

> *Non posso dirti niente. Va' a parlare con Matteo.*

**È la cosa più forte che possa fare un personaggio così:** non tradisce il segreto e ti manda dritto da chi te lo può dire. E la crudeltà supplementare è che una delle due confessioni che lo zittiscono è di Giorgio stesso — **c'è un uomo in paese che sa cosa Giorgio ha scoperto, e non può ridarglielo.**

### Perché tre strade e non una

Le tre portano tutte a Matteo, e nessuna è obbligatoria. Laura ce lo manda, Anna ce lo inchioda, Don Carlo ce lo spedisce. Un giocatore che manca Laura ci arriva con Anna; uno che non smuove nessuna delle due ci arriva col prete.

**È il criterio delle due vie non-mendaci applicato a un intero atto**, e vale la pena scriverlo: le strade non si sommano per potenza, si sostituiscono. Averle tutte e tre non ti dà di più — ti dà solo la stessa porta da tre corridoi diversi.

## Il terzo atto: Matteo confessa la metà buona

Il braccialetto su Matteo non basta: lui era là sotto, sa benissimo che c'era una bambina, e può reggere ancora un giro. **Quello che non può spiegare è Anna** — che quando lei è scesa in paese, alla cava era rimasto solo lui.

Non è un'accusa, è un dettaglio di una vecchia signora che ricorda chi c'era e chi no. Ed è l'unica cosa al mondo che mette quell'uomo da solo sopra un buco in cui c'era una bambina. Messa in bocca a Giorgio, non c'è versione che regga.

Matteo cede. Ma **prima si assicura di una cosa**, e il modo in cui se ne assicura è tutta la scena: continua a chiedergli cosa ricorda. Lo gira, lo rigira, lo chiede in tre modi diversi. E quando è convinto che quel ragazzo davvero non ricordi niente del 1985, parla.

Racconta la corsa giù per il sentiero. La voce sotto le pietre. Che l'ha tirata fuori. Che l'ha portata via **per proteggerla**, perché quella gente l'avrebbe ripresa. Che è viva, che è grande, che vive in un'altra città e ha una vita.

È tutto vero. È la cosa migliore che quest'uomo abbia fatto, e la racconta bene, e il giocatore in quel momento **gli vuole bene**.

Tace due cose. Una la conosce il giocatore: **dove.** *«Lasciala stare. Ha una vita che non c'entra niente con noi.»*

E l'altra è che ventiquattro mesi fa ha preso una pietra.

### Lo stallo, che è il perno di tutto il gioco

Il terzo atto finisce in un vicolo cieco, e **è voluto**.

Giorgio sa che Elena è viva. Ha l'indirizzo in tasca da sempre. Va a Chivasso — o ci è già andato — e Wanda gli dice l'unica cosa che ha da dirgli: **di questa faccenda parlo con una persona sola, e non è lei.**

Torna in bottega. Chiede a Matteo di venire, o almeno di scrivere due righe.

**E Matteo dice di no.**

Non è cattiveria e non è nemmeno un rifiuto secco: è che quell'uomo, se Elena viene a sapere, perde tutto — e finché non ci va nessuno, la cosa può ancora finire lì. *«Lasciala stare. Ha una vita.»*

E poi, incalzato, con la voce di uno che sta cercando di cambiare discorso, glielo chiede un'altra volta:

> *«…senti. Ma tu, di preciso, cosa ti ricordi?»*

### Il collante: la domanda che quest'uomo fa da tre atti

Qui non cambia direzione niente, ed è importante che non lo sembri.

L'obiettivo del giocatore resta identico dal terzo atto in poi: **arrivare a Elena.** L'unica strada passa da quell'uomo, quell'uomo ha detto di no, e quindi la domanda diventa quella che si fa chiunque davanti a un no:

> **cosa ho, io, su di lui?**

Non *chi mi ha aggredito* — quello è quello che il giocatore **trova**, non quello che cerca. Cerca una leva. E cercare una leva su un uomo, in un paese di milleduecento anime, vuol dire una cosa sola: **andare in giro a chiedere di lui.**

E c'è già una cosa che non torna, ed è lì dal primo atto.

**Matteo, ogni singola volta che si sono visti, gli ha chiesto cosa si ricorda.** Alla frase. Al braccialetto. Alla confessione. E adesso, mentre gli sta dicendo di no.

Sul taccuino quella riga non compare una volta: **compare quattro volte, sotto quattro date diverse.** È lo stesso meccanismo del coro del paese — solo che lì si contano le bocche e qui si contano le volte, e la struttura dati è la stessa.

Nessuno ha mai suggerito niente al giocatore. Ha soltanto **una riga ripetuta quattro volte** in un taccuino che non ripete mai niente, e la domanda gli viene da sola:

> *Perché a quest'uomo interessa tanto cosa mi ricordo?*

**Quella è la domanda che regge il quarto atto**, ed è una domanda su Matteo — cioè esattamente il filo che il giocatore stava già tirando. Non è un cambio di storia. È la stessa conversazione che continua.

E la risposta, ovviamente, sta nella sola cosa che Giorgio non si ricorda.

### La scatola di Rosa

Quando non si sa più dove andare si torna a casa. È l'unica cosa che un uomo di ventiquattro anni faccia con naturalezza, ed è dove il gioco mette la sua guida.

Rosa aspetta da due anni di poter parlare di quella notte con qualcuno che le risponda. Basta nominargliela — *cosa è successo quella sera?* — e tira fuori una scatola di cartone dall'armadio.

**Ci sono i vestiti che aveva addosso quando l'hanno trovato.** Li ha lavati e piegati, perché è quello che si fa, e non li ha buttati, perché è quello che si fa.

E addosso a quei vestiti ci sono due cose.

**La giacca è strappata sulla schiena.** Non tagliata: consumata, come si consuma una stoffa che viene trascinata su un terreno.

**Nei risvolti dei pantaloni c'è la segatura.**

Rosa non ci vede niente. Gliela indica come si indica una macchia: *«guarda che disastro, chissà dove eri stato».* **Non collega, come non ha mai collegato niente in ventun anni**, e consegna a suo figlio l'indizio più importante del gioco senza accorgersene.

A San Rocco c'è **un falegname.**

### Perché è questo l'indizio giusto

È in casa dalla prima scena, è raggiungibile in qualunque momento, e per tre atti **non vuole dire assolutamente niente.** Un giocatore che apre quella scatola nel primo atto vede dei vestiti sporchi. Lo stesso oggetto, dopo che Matteo ha detto di no, è una freccia.

È la stessa cosa che fa la frase, il braccialetto e il foglio con l'indirizzo: **il gioco non nasconde mai niente, mette tutto in vista e aspetta che il giocatore diventi capace di leggerlo.**

E non dice il nome di nessuno. Dice *segatura*. Il nome ce lo mette il giocatore, e lo può fare solo perché sa dal primo atto che quell'uomo lavora il legno.

### E il referto lo conferma

Seconda via indipendente, per chi vuole essere sicuro: **Giorgio ha due anni di cartella clinica su di sé.** Rosa ha le carte della dimissione, e all'ospedale di Cuneo c'è tutto il resto.

Trauma occipitale — **colpito da dietro**. Escoriazioni sul dorso e sui talloni — **trascinato**, e non per pochi metri.

L'unico testimone di quella notte che non ha mai avuto motivo di mentire è il suo stesso cranio.

Due sostegni indipendenti, la regola vale, e la proposizione si stabilisce: **quella sera qualcuno mi ha colpito da dietro in una falegnameria e mi ha trascinato nel bosco.**

Da lì la domanda viene da sola — *ci eravamo già parlati, io e te?* — e Matteo deve rispondere di no.

**Non ha scelta.** Ammettere quell'incontro significa dichiararsi l'ultima persona che ha visto quel ragazzo prima che lo trovassero in un bosco con la testa rotta. Quindi nega, e nega bene, perché lo fa da due anni.

**È l'unica bugia netta di tutto il gioco, e il motore la registra.**

## Il quarto atto: cosa ho su quest'uomo

L'obiettivo non è mai *scoprire chi mi ha aggredito*. È il no di Matteo, e come aggirarlo. Il giocatore va in giro a chiedere di lui — che è la cosa che fa chiunque debba convincere qualcuno — con in mano una sola stranezza: **questo tizio mi chiede da un mese cosa mi ricordo.**

E la sola cosa che Giorgio non si ricorda è la notte in cui gli hanno spaccato la testa.

Cinque pezzi, quattro dei quali sono conversazioni. **Nessuno di loro sa cosa sta consegnando**, e il giocatore, quando comincia, non sa cosa sta cercando: sta solo chiedendo in giro di un falegname che gli ha detto di no.

**Rosa.** Oltre alla scatola: l'ultima sera. Suo figlio è uscito dopo cena e non è tornato, e lei se la ricorda minuto per minuto perché se la racconta da due anni. Era **allegro** — la prima volta in mesi. Aveva detto che andava da uno che sapeva. **Non gli ha chiesto chi.** È l'ennesima volta che questa donna non chiede, e stavolta il giocatore capisce quanto le è costato.

**Nino.** Dove, e quando. Il castagneto sopra la curva: un posto in cui un uomo non ci arriva camminando e non ci cade da solo. E la curva, per chi conosce San Rocco, è **quella sotto la bottega.** Nino non ci vede niente di strano perché per lui è solo un posto; il giocatore che ha camminato per quel paese per tre atti ci vede una geografia.

**Don Carlo.** Non può dire una parola di quello che Giorgio gli ha confessato, e non la dice. Ma i giorni non sono un segreto: *sei venuto da me di giovedì. Il sabato ti hanno trovato.* Due giorni, e in mezzo una sola visita che qualcuno nega di aver ricevuto.

**Il foglio.** Scritto di suo pugno, e Giorgio non ricorda di averlo scritto: quindi è di prima del coma. E nessuno si scrive da solo l'indirizzo di una città in cui non è mai stato. **Qualcuno gliel'ha dettato**, e le persone al mondo che potevano dettarglielo sono una.

**Il paese.** Nel 1985 si è detto per due mesi che il ragazzo dei Lipari fosse caduto, o che l'avesse pestato uno di passaggio. Poi si è smesso di dirlo. Chiedendo in giro il giocatore scopre una cosa piccola e brutta: **nessuno ha mai denunciato niente**, perché era più comodo per tutti che fosse una caduta — e perché quel ragazzo, in paese, dava fastidio da anni con le sue domande.

Cinque pezzi. **Nessuno di loro dice il nome di Matteo.** Il nome ce lo mette il giocatore, ed è la seconda cosa che questo gioco chiede di capire invece che di trovare.

E vale la pena rileggere cosa ha fatto davvero il giocatore per arrivarci: **non ha mai smesso un attimo di cercare di arrivare a Elena.** Ha chiesto in giro di un uomo che gli aveva detto di no, e mentre chiedeva di lui ha scoperto una cosa su di sé.

**È così che gli arriva addosso**, e non perché a un certo punto abbia deciso di indagare sul proprio incidente. Un uomo non decide di indagare sul proprio incidente. Un uomo bussa a una porta finché non si apre, e non guarda mai cosa c'è dietro finché non l'ha aperta.

### La seconda uscita dallo stallo, che costa un'altra cosa

Un'unica strada per uscire da un vicolo cieco è un vicolo cieco con un corridoio. Ce ne vuole una seconda, e c'è, ed è terribile.

**Laura.**

Giorgio può smettere di lavorarsi Matteo e andare a dire a quella donna che sua figlia è viva.

Una madre a cui hanno appena detto una cosa del genere non è un problema che Matteo possa gestire. Va lei in bottega, e quello che gli chiede non è un favore. **Il foglio lo ottiene lei in dieci minuti**, con vent'anni di lutto addosso e niente da perdere.

Funziona, ed è più veloce. Ma il giocatore ha appena preso **la più grossa delle tre porte dell'epilogo e l'ha aperta a metà partita per convenienza**, senza sapere cosa ci fosse dietro — perché nessuno sa come reagisce una donna a cui si dice, dopo ventun anni, che la bambina che piange respira a due ore di corriera.

Sono due uscite e sono tutte e due brutte: **o dissotterri il tentato omicidio di te stesso, o scarichi la verità addosso a una donna e la mandi a combattere al posto tuo.**

Il gioco non dice mai quale sia quella giusta e non ne premia nessuna. Cambia solo con cosa esci dal quinto atto: dalla prima si esce con una confessione e un uomo che ti chiede scusa; dalla seconda si esce con un foglio, e senza aver mai saputo perché ti hanno trovato in un bosco.

**È la stessa scelta che hanno fatto tutti quelli di questa storia** — dire una cosa che fa male, o girarla a qualcun altro — e il gioco la mette in mano al giocatore senza avvisarlo che è quella.

## Il quinto atto: il secondo scusa

Giorgio torna in bottega. Non con una prova: con cinque cose che insieme lasciano un solo posto libero.

E Matteo, per la terza volta in vita sua, sceglie l'unica mossa che gli tiene il racconto in mano: **lo dice lui.**

Racconta tutto. Che quella sera aveva deciso di aiutarlo davvero, e ci credeva mentre gli dettava l'indirizzo. Che quando Giorgio si è girato per andarsene ha visto due secondi di futuro — il processo, il paese, quella ragazza che scopre da uno sconosciuto che le hanno rubato una vita. Che ha preso una pietra dal banco senza decidere niente. Che l'ha trascinato su nel castagneto convinto di aver ammazzato un uomo. Che per due anni ha aspettato di sapere se sarebbe morto, e poi se si sarebbe svegliato, e poi se si sarebbe ricordato.

E alla fine gli chiede scusa.

**È la seconda volta che glielo dice**, ed è la stessa parola, con la stessa voce, e vale altrettanto poco.

### Il ricordo arriva un secondo dopo, ed è il punto

Non prima. **Dopo.**

Il giocatore ha appena saputo tutto da una confessione — e a confessione finita, mentre quell'uomo dice *scusa* davanti a lui, **sul taccuino compare una riga che non ha scritto lui.**

> *— la stessa parola, con la stessa voce, alle mie spalle. (ricordo)*

Il ricordo **non rivela niente**: arriva un secondo troppo tardi per servire a qualcosa, e conferma una cosa che Giorgio ha appena finito di ascoltare.

Non serve a sapere. Serve a **sentire** — perché fino a quel momento era una storia raccontata da un altro, e adesso è una cosa che gli è successa. Il gioco non gli restituisce la memoria per farlo vincere: gliela restituisce quando non gli serve più a niente, che è esattamente come funziona.

E dà anche a Giorgio l'unica cosa che il quarto atto non poteva dargli: **la certezza che quell'uomo non stia raccontando anche questa a modo suo.**

### Non è una cutscene, ed è importante che non lo sia

Il gioco non toglie mai la parola al giocatore, e un ricordo che diventa un filmato è un ricordo che il giocatore subisce invece di avere.

La riga compare sul taccuino con la fonte «memoria» e affidabilità sotto uno, come tutti i ricordi di Giorgio — stessa struttura dati, stessa vista, stessa calligrafia di tutto il resto. E poi **non succede nient'altro**: nessuno reagisce, il gioco non commenta, il turno dopo tocca al giocatore come tutti gli altri turni.

**La rivelazione non è la scena. La scena è cosa ne fai.**

### E poi la lettera

A quel punto Giorgio gli chiede il foglio per Wanda.

Matteo lo scrive, e lo scrive **perché non gli costa niente** — nessun processo, nessuna denuncia, nessuno che lo venga a prendere. È sempre stato quello: la cosa giusta quando costava coraggio, quella mostruosa quando costava tutto, e adesso due righe che non costano niente.

Giorgio lo sa mentre tende la mano per prenderlo. **E lo prende lo stesso.**

Ed è per questo che la scelta dell'epilogo pesa davvero: **gli è servito.**

## Il filo del padre

È il filo che non corre parallelo agli altri: **corre sotto**, e affiora tre volte. E l'ordine in cui affiora è tutto, perché la stessa informazione data prima o dopo produce due giochi diversi.

La regola che lo governa: **prima si condanna Andrea, poi lo si riabilita, e poi si scopre che la riabilitazione non cancella niente.**

### Primo affioramento — la scatola, che non si può leggere

Fine del primo atto. Rosa tiene da due anni una scatola di carte di suo marito, che non ha buttato e non ha letto. Per averla deve decidere di smettere di proteggerlo, e non è un ritrovamento: è una conversazione che le costa.

Dentro non ci sono carte sciolte: c'è **un diario, e è mezzo cancellato.**

Non dall'acqua e non dal tempo. **L'ha cancellato lui.** Andrea scriveva e poi tornava indietro a raschiare via, riga per riga, per sette anni — un uomo che non è riuscito a lasciare le prove nemmeno a se stesso. È la stessa mano che non ha mai fatto la domanda a colazione.

Quello che sopravvive è preciso in un modo e vuoto nell'altro. **Sopravvivono i posti:** istituti, parrocchie, un paese cerchiato due volte, Roccavione. **Sopravvive il verbo:** cercare. **Sopravvive perfino la cosa più grossa** — che l'uomo che scriveva era convinto che **una persona data per morta fosse viva**, e che qualcuno l'avesse portata via.

**Non sopravvive un solo nome.** Quelli sono gli unici che ha raschiato via tutte le volte.

### Il giocatore capisce prima di Giorgio, ed è così che deve essere

Sì, questo dice quasi tutto — e va bene, perché **la tensione di questo gioco non è mai stata *è viva o no*.** È *chi lo sa, e cosa gli costa ammetterlo*.

Il diario dà al giocatore la cosa che gli manca di più nel secondo atto: **un'ipotesi.** Senza, va in giro a raccogliere confessioni senza sapere cosa sta cercando. Con, va a caccia di una conferma — che è un modo molto più attivo di giocare, e trasforma ogni conversazione in una verifica invece che in un'esplorazione a caso.

E l'escalation resta intera **proprio perché i nomi sono raschiati**, in tre gradini:

1. **il diario** — mio padre credeva che una persona data per morta fosse viva;
2. **il braccialetto** — quella persona è Elena;
3. **Matteo** — sì, ed è viva adesso.

Quando Matteo lo confessa, il giocatore fa 2+2 e la scena non perde niente: perché quello che uno aspetta a quel punto non è l'informazione, **è vedere se quell'uomo lo dice.**

### Secondo affioramento — il diario diventa leggibile, e non torna

Lo stesso diario, riletto dopo il braccialetto, dice una cosa sola: **Andrea Lipari ha passato gli ultimi sette anni della sua vita a cercare Elena Valli.**

Ed è qui che al giocatore si apre una cosa che non riesce a chiudere. Perché a questo punto ha anche l'ultima pagina del registro, con **A. L.** scritto sopra la data di quella notte.

**Un uomo che era dentro fino al collo e che poi ci ha buttato dieci anni per rimediare.** Il gioco non lo risolve, perché non si risolve.

### Terzo affioramento — la busta

Don Carlo non può dire una parola di quello che ha sentito in confessione, e non ne dirà nessuna. Ma nel 1981 Andrea non gli ha soltanto parlato: **gli ha lasciato una cosa.**

Una busta chiusa, con una condizione:

> *Dalla a mio figlio se un giorno viene a chiedere di quella bambina.*

**Un oggetto depositato non è un segreto confessato**, quindi il vincolo non c'entra e Don Carlo può parlarne liberamente — anzi lo dice subito, appena Giorgio si presenta: *c'è una cosa che tuo padre ha lasciato per te, e mi ha detto quando.*

È una serratura **visibile**, che è l'unico tipo onesto: il giocatore vede la busta dal primo atto e sa esattamente qual è la condizione. Deve solo diventare l'uomo che la soddisfa.

E la condizione va letta stretta, come la legge Don Carlo da sei anni: *chiedere di quella bambina* non significa nominare la morta che sta sulla lapide — quello lo fa mezzo paese. Significa **venire a chiedere di una persona viva.** Nel motore è il predicato più semplice di tutto il gioco: la busta si apre quando la proposizione «Elena è viva» è stabilita, cioè dopo Matteo. Il prete non sta facendo il difficile: sta facendo esattamente quello che gli è stato chiesto.

E la condizione che Andrea ha messo è la cosa più triste che abbia fatto in vita sua. Non «quando compie trent'anni», non «quando muoio». *Se viene a chiedere di quella bambina* — perché Andrea sapeva che se un giorno suo figlio fosse tornato a nominare Elena, avrebbe voluto dire una cosa sola. **Ha scritto una lettera alla versione di suo figlio che si ricorda**, e poi è morto senza sapere se sarebbe mai esistita.

### La lettera, e cosa non c'è dentro

Nella busta c'è tutto quello che Andrea sapeva: la voce di Roccavione, gli istituti, dove ha cercato e dove ha sbagliato, le due o tre cose che aveva capito e non era riuscito a provare. Sette anni consegnati a un ragazzo di diciotto anni che non li ha mai letti perché ha trovato il quaderno prima.

E c'è una richiesta di perdono, **il cui oggetto non è mai nominato.**

Andrea non scrive che teneva la corda. Non ci riesce sulla carta come non ci è riuscito a voce per quindici anni, e la lettera gira intorno a una cosa che non dice mai. Il giocatore la legge e capisce che quest'uomo si sta scusando di qualcosa di preciso — **e non sa di cosa.**

Poi Giorgio scende alla cava, entra nella galleria, e si ricorda.

**È l'ultima cosa che il gioco gli dà, e non gliela dà nessun personaggio:** una faccia che si gira sopra una corda, che è l'unica risposta possibile a una lettera che non nominava niente.

## Come si apre la porta di Wanda

Non con il braccialetto — quello ha già un mestiere e non deve farne cinque.

Wanda ha ventun anni di una sola paura: che qualcuno venga a riprendersi la ragazza. Un braccialetto non la tocca. **L'unica persona al mondo di cui si è fidata su questa faccenda è il ragazzo che gliel'ha portata in braccio nel 1966**, e la porta si apre per lui.

**La chiave è Matteo.** Nella forma minima: **due righe di suo pugno.** E qui il gioco rima con se stesso — due fogli scritti a mano, uno per ciascuno dei due uomini, a ventidue anni di distanza. Sul primo Matteo ha dettato un indirizzo e poi ha preso una pietra. Sul secondo scrive di lasciarlo entrare.

### Chi glielo dice che gli serve quello

**Wanda.** Ed è per questo che andare a Chivasso presto non è mai tempo perso.

Il giocatore ha quell'indirizzo dalla prima scena e prima o poi ci va, magari il primo giorno, magari senza sapere cosa sia. Wanda apre di dieci centimetri, si spaventa, e per togliersi di torno uno sconosciuto che le parla di una ragazza dice l'unica cosa che le viene:

> *Io di questa faccenda parlo con una persona sola, e non è lei.*

**Non fa il nome.** Non sa che quel ragazzo lo conosce, e comunque non lo direbbe. Ma il giocatore esce da lì con un obiettivo netto — *c'è un uomo di cui questa donna si fida, e mi serve lui* — e con un pezzo che gli resta in tasca per mezza partita senza poterlo usare.

E quando, atti dopo, Matteo confessa di essere stato lui a portarla via, **il pezzo si incastra da solo.** Due informazioni raccolte alle due estremità opposte del gioco che si chiudono l'una sull'altra senza che nessun personaggio le abbia mai messe in relazione. Il giocatore lo capisce prima di Giorgio, e non ha bisogno che nessuno glielo dica.

Serratura che dichiara la propria chiave: è l'unico tipo onesto, e questa lo fa dalla prima ora di gioco.

### Come si ottengono, quelle due righe

Alla fine del quinto atto, dopo la confessione. Non c'è nessun ricatto e non serve: a quel punto Matteo ha già detto tutto, e scrivere due righe è la sola cosa che gli resta per essere **quello che l'ha mandato** invece che quello che è stato scoperto.

### Le altre strade

Perché non ci sia un collo di bottiglia solo, il quinto atto si può forzare anche prima della confessione: minacciarlo di raccontarlo al paese, o di dirlo a Laura — su cui è vulnerabile in modo diverso, perché a Laura ha lasciato piangere una figlia viva per ventun anni.

Funzionano, e sono peggiori: **si ottiene lo stesso foglio da un uomo spaventato invece che da un uomo messo davanti a se stesso**, e non si sente mai il secondo *scusa*. Il gioco non lo dice mai e non penalizza niente. Semplicemente non è la stessa partita.

### Il buco: perché nel 1985 una pietra e nel 1987 una confessione?

È l'obiezione più seria che si possa fare a tutta la storia, e va risolta in modo che Matteo resti **una persona sola** e non due.

La risposta breve è che **non sta proteggendo la stessa cosa**, e non è mai stata la verità.

**Nel 1985 non ha nessun controllo.** Gli si presenta un uomo che dice *mi ricordo*: ha il quaderno, sa del rito, sa che lui c'era, e sta per andare a cercare Elena per raccontarle tutto. Quello che Matteo vede è la propria vita che finisce **in una versione che qualcun altro scriverà** — il processo, il paese, e soprattutto quella ragazza che scopre da uno sconosciuto che le è stata rubata una vita. Non gli resta un solo modo di uscirne che sia sopportabile.

E la pietra, nel 1985, è possibile perché **nessuno sapeva che quel ragazzo era in bottega**. Nel 1987 lo sa tutto il paese: c'è un tale appena uscito da due anni di coma che gira San Rocco a mostrare una fotografia a chiunque, e metà delle persone con cui ha parlato l'hanno già raccontato all'altra metà. **Quella strada è chiusa**, e Matteo lo sa dal primo giorno.

**Nel 1987 il controllo ce l'ha tutto lui**, e ci ha messo due anni ad accertarsene. Il ragazzo non ricorda, glielo ha chiesto in tre modi ogni volta che si sono visti, e il paese ormai sa metà della faccenda da solo. La domanda non è più *se* la storia uscirà: è **quale delle due metà**. E lui può scegliere.

Sceglie quella in cui è l'uomo che ha sentito una voce sotto le pietre ed è tornato indietro. **Non gli costa niente, ed è pure vera.**

E c'è la cosa che nessuno guarda mai: **per due anni quest'uomo si è creduto un assassino.** Nel 1966 poteva ancora dirsi di aver salvato una bambina; dal 1985 non poteva dirsi più niente. Poi quel ragazzo entra in bottega e cammina e parla. **Non ha ammazzato nessuno.** Quella non è una notizia, è un condono, e la confessione che segue non è coraggio: **è scarico.** Sta parlando con l'unica persona al mondo davanti a cui può assolversi gratis.

### Ma allora perché scrive il foglio per Wanda?

Perché è la stessa identica cosa, per la terza volta.

Impedire a Giorgio di arrivare a Elena valeva una pietra. Quando arriva la parola *scusa*, quello è finito: la storia arriverà a Elena comunque, con lui o senza di lui. Restano due versioni, e sono molto diverse.

**Senza il foglio**, Elena la sente da un estraneo, e Matteo è l'uomo che l'ha rubata e poi ha spaccato la testa a chi la cercava.
**Con il foglio**, Giorgio arriva a quella porta come **uno che manda lui.**

Scrivere due righe non è un cedimento e non è generosità: è **l'ultima leva rimasta per restare dentro la propria versione.** Preferisce essere quello che l'ha mandato piuttosto che quello che è stato scoperto.

### Il che dà a Matteo la sua unica riga

E chiude il personaggio, perché adesso i tre momenti sono lo stesso momento:

- **1966** — dice che è morta, e la storia resta sua.
- **1985** — prende una pietra, e la storia resta sua.
- **1987** — scrive due righe, e la storia resta sua.

> **Matteo non ha mai scelto una volta fra il bene e il male. Ha scelto tre volte quello che teneva il racconto in mano sua.**

È per questo che a vent'anni è un eroe e a trentanove un mostro senza essere cambiato di niente: **non è mai stata una questione morale, è sempre stata la stessa paura di essere visto.** E il giocatore, nell'epilogo, deve decidere cosa si fa con un uomo così — che non è cattivo, e non si è mai nemmeno posto la domanda.

### Come si impedisce ad Anna di dirlo prima

È il caso da manuale, e la risposta è quella giusta: **il fatto non sta nel suo testo finché non serve.**

Nella scheda di Anna non c'è scritto da nessuna parte «sai del sacrificio ma non dirlo finché non ti mostra il braccialetto». Una riga così, prima o poi, viene detta — o peggio, viene *alluso*: il modello fa la faccia di chi sa, e il giocatore capisce vent'anni prima del tempo.

Nella sua scheda **quella proposizione non esiste**. Anna, per tutto il tempo che precede il braccialetto, è una donna che ha perso il marito in una frana ed è fedele a una cosa in cui crede. Non le si chiede di nascondere niente, perché non ha niente in mano.

Nel motore è già tutto lì:

- il blocco `<conoscenze>` viene **ricostruito a ogni turno** da `KnowledgeService.context_for(npc)`;
- sbloccare il fatto è una `reveal_fact` sulla sua colonna;
- la scala di posizione è **una tabella di `reveal_fact` con dei predicati davanti**, non codice.

Un'accortezza sola, ma va scritta: **lo sblocco deve avvenire per una cosa successa davanti a lei nello stesso turno.** Le mostri il braccialetto, l'`<osservazione_motore>` dice che le stai mettendo davanti un braccialetto d'argento con delle iniziali, e nello stesso turno il fatto compare fra le sue conoscenze. Così il modello non sembra essersi *ricordato* qualcosa all'improvviso: sta reagendo a una cosa che ha appena visto, che è quello che succede davvero.

Regola generale, valida per tutti: **non si sblocca mai un fatto senza che nella stanza sia appena successo qualcosa che lo giustifichi.**

## Il taccuino di Giorgio

Un gioco di persuasione fallisce in modo pulito: l'altro dice di no e tu lo sai. **Un gioco di ricostruzione fallisce nel modo peggiore che esista: il giocatore si blocca senza sapere di essersi bloccato.** Valnera aveva le 18:00 e la sconfitta era leggibile. Qui non c'è niente del genere, e questo è il rischio di progettazione più serio di tutto Amnesia.

La soluzione è già dentro la finzione: **un uomo che ricostruisce la propria vita si scrive le cose.**

Il registro delle proposizioni del motore **è** il taccuino di Giorgio. Non un diario delle missioni scritto dall'autore: la stessa identica struttura dati che il motore usa per decidere le posizioni, mostrata così com'è.

Il giocatore ci legge cosa è stabilito, cosa ha un sostegno solo e quindi non conta ancora, e da chi viene ciascun sostegno. È contemporaneamente **lo stato interno del gioco, la lista di cose da fare, e un pezzo di caratterizzazione** — perché il taccuino di un uomo che non si fida della propria memoria è la cosa più triste e più utile che possa avere in tasca.

E rende possibile la mossa migliore del gioco: **quello che è scritto sul taccuino si può citare.** Una proposizione stabilita diventa una cosa che si mette in faccia a qualcuno, con `[cito:]`, e che quel qualcuno non può liquidare come una diceria.

## Le dichiarazioni

L'unità di stato del gioco. Ogni cosa che può far avanzare la trama è una **dichiarazione**: autoriale, con un identificativo, un testo canonico, chi può farla e a quali condizioni.

```
id:            padre_nella_cava
testo:         "Mio padre era alla cava la notte della frana."
fonti:         [registro_presenze, anna]
precondizione: frase_pronunciata          # per anna
sblocca:       io_ero_con_lui
```

**Il testo canonico non è la battuta.** Il modello scrive la sua prosa come gli pare, in carattere; quando ha effettivamente detto quella cosa chiama uno strumento a **vocabolario chiuso** con l'id, e il motore registra. È la stessa separazione di Valnera fra le parole e la merce, applicata all'informazione: **puoi dire quello che vuoi, ma solo il motore stabilisce che l'hai detto.**

Ordine di grandezza: **venticinque-trentacinque dichiarazioni** per l'intero gioco. Meno, e non c'è un'indagine; molte di più, e nessuno le troverà tutte.

### Il taccuino registra citazioni, mai conclusioni

È la regola che tiene in piedi la difficoltà, ed è facilissima da violare per comodità.

Sul taccuino finisce **solo ciò che è stato detto o visto**, attribuito a chi l'ha detto. Mai una sintesi, mai un'inferenza, e soprattutto **mai una domanda formulata dal gioco.**

> ✅ *Anna: «Quando sono scesa in paese, alla cava era rimasto solo Matteo.»*
> ❌ *Matteo è rimasto solo con Elena — potrebbe averla presa lui?*

La seconda riga è il gioco che gioca al posto tuo. Una sezione «dubbi» scritta dall'autore fa esattamente quello: prende il lavoro interessante — accorgersi — e lo consegna già fatto.

E c'è la seconda metà della regola, che è quella che rende difficile il gioco:

> **Il taccuino non segna mai niente come vero o falso.**

Ci finisce la versione del paese, ci finisce la scampagnata di Matteo, ci finisce *«ero assoggettata a mio marito»* di Laura. Tutto con lo stesso peso, la stessa calligrafia, nessun asterisco. **Su venticinque dichiarazioni, sei o sette sono false o mezze false, dette da gente che ci crede.**

### I dubbi li compone il giocatore: il confronto

E qui c'è il verbo che mancava.

Il giocatore prende **due righe del taccuino e le mette una accanto all'altra.** Il risultato è un oggetto — un **confronto** — che può portare in faccia a qualcuno esattamente come porta una fotografia.

```
confronto( anna_solo_matteo , matteo_ero_andato_via )
```

Meccanicamente è pulito: una coppia di identificativi, verificabile, niente da interpretare. **Ma il lavoro è tutto del giocatore**, perché la partita sta nello scegliere *quali due*.

E questo cambia il gioco in tre modi:

- **Le deduzioni migliori non le dice nessuno.** *«Quello che mio padre cercava era Elena»* non esce da nessuna bocca in tutto il gioco: si compone accostando il diario del padre al braccialetto. Lo stesso vale per *«la frase che ripeto è la loro»*: la si ottiene accostando la prima riga del taccuino al pettegolezzo del paese. **Le due realizzazioni più importanti del gioco sono entrambe confronti**, e nessun personaggio le pronuncia mai.
- **Si può sbagliare senza danno.** Accosti due righe che non c'entrano, la porti a qualcuno, e quello ti guarda come si guarda uno che ha detto una sciocchezza. Costa tempo, non progressione.
- **E accusare qualcuno smette di essere un'affermazione e diventa una prova.** Non dici a Matteo *«sei stato tu»*: gli metti davanti la sua frase e quella di Anna. Un modello può negare un'accusa; non può disdire due righe che il motore ha registrato.

### I tre confronti che reggono il gioco

**Il confronto del terzo atto** — quello che fa cedere Matteo:

> `matteo_ero_gia_sceso` · **Matteo:** *«Quella sera io ero già sceso quando è venuta giù. Ho saputo tutto la mattina dopo, come tutti.»*
> `anna_solo_matteo` · **Anna:** *«Quando sono scesa in paese, alla cava era rimasto solo Matteo.»*

Parlano dello stesso momento e non possono essere vere tutte e due. La prima è disponibile **dal primo atto** — è la versione che Matteo dà a chiunque da ventun anni — e il giocatore se la porta dietro per due atti senza sapere che gli servirà. La seconda arriva **solo dopo il braccialetto**, da una signora convinta di star ricordando un dettaglio senza importanza.

E qui c'è la cosa che fa di questa coppia il cuore del gioco: **quelle due frasi stanno in piazza da ventun anni.** Anna e Matteo si incontrano al mercato ogni settimana. Nessuno le ha mai accostate perché nessuno ha mai avuto motivo di fare quelle due domande di fila. **La prova non era nascosta: era solo non assemblata**, che è il tema del gioco scritto in forma di meccanica.

**Il confronto del quarto atto** — quello che smonta la bugia:

> `matteo_mai_parlati` · **Matteo:** *«Io e te non ci eravamo mai parlati.»*
> `foglio_di_mia_mano` · **oggetto:** il foglio con l'indirizzo di Chivasso è di sua mano, e Giorgio a Chivasso non è mai stato.

**Il coro** — che non è un confronto fra due, ma la stessa frase da tre bocche:

> `versione_del_paese` · **Rosa, Nino, il fornaio, chiunque:** *«…e per quella creatura è stato un attimo.»*

Non contraddice niente. È **impossibile**: non c'è un essere umano al mondo in condizione di sapere se quella bambina abbia sofferto. Il motore non lo segnala mai; lo nota il giocatore quando se la sente dire per la terza volta con le stesse identiche parole da gente che non si parla.

Sono due meccaniche diverse e vanno tenute distinte nei dati: **il confronto** accosta due dichiarazioni incompatibili, **il coro** conta quante bocche hanno detto la stessa dichiarazione parola per parola.

**E il coro ha un rischio tecnico che il confronto non ha:** i modelli parafrasano. Se ognuno la dice a modo suo, il coro non esiste — e con lui se ne va la prova migliore del gioco.

La soluzione è non chiedere al modello di ricordare una formulazione, ma di **citare un modo di dire.** Nella scheda non c'è scritto «di' questa frase»: c'è scritto che in paese quella cosa **si dice così**, con quelle parole, da vent'anni, come si ripete un proverbio. Riprodurre una citazione marcata come fissa è una cosa che i modelli fanno bene; ricordarsi una frase e non variarla è una cosa che fanno male.

E il motore verifica comunque sull'id della dichiarazione, non sulla stringa: **la formulazione identica è quello che il giocatore sente, il conteggio è quello su cui il gioco decide.** Se un modello sgarra di una parola, il coro regge lo stesso.

### I due fili, per esteso

**Il filo del padre** — quello che Giorgio ricostruisce a pezzi:

| Dichiarazione | Da dove |
|---|---|
| mio padre cercava qualcuno che credeva vivo | il diario mezzo cancellato |
| mio padre era del Circolo | Anna o Laura, dopo la frase |
| mio padre era alla cava quella notte | il registro, o Anna |
| **quello che cercava era Elena** | **nessuno. Confronto: diario × braccialetto** |
| quella notte c'ero anch'io | Anna, confermata dal registro |
| **era lui a tenere la corda** | **nessuno. Ricordo, nella galleria, dopo la lettera** |

**Il filo del Circolo:**

| Dichiarazione | Da dove |
|---|---|
| esisteva un gruppo che si chiamava così | la fotografia, chiunque |
| avevano una frase fra loro | pettegolezzo di paese |
| **la frase che ripeto io è la loro** | **nessuno. Confronto: taccuino × pettegolezzo** |
| il rito prevedeva che qualcuno passasse la soglia | il quaderno di Vittorio |
| il sacrificio era per Vittorio | Anna, dopo il braccialetto |
| Elena era quella che doveva passare | Anna o Laura, dopo il braccialetto |
| **Elena non è mai stata sepolta là sotto** | **confronto: cassetta di latta × versione del paese** |

**Le righe in grassetto sono quelle che non dice nessuno**, e sono le uniche che contano davvero. Il gioco distribuisce le altre con generosità: **è avaro solo delle conclusioni, e quelle non le vende — le fa comporre.**

## Come si guida il giocatore

È il problema di progettazione più serio di tutto Amnesia, e non si risolve con un diario delle missioni. Un'indagine in cui il giocatore si blocca senza sapere di essersi bloccato è un gioco rotto; un'indagine in cui una freccia gli dice dove andare non è più un'indagine.

**Il principio è uno solo:**

> **Il gioco non gli dice mai cosa fare. Gli mette accanto due cose che ha già, e lo lascia guardare.**

Guidare per **accostamento**, mai per istruzione. Tutto quello che segue è un modo di applicarlo.

### 1. Il taccuino ha due metà

*Cose dette* e ***gente da vedere*.**

La prima metà sono le dichiarazioni raccolte, attribuite, senza giudizio — comprese quelle false.

La seconda si riempie da sola con **i nomi che qualcuno ha pronunciato e con cui non hai ancora parlato**, ed è il motore dell'esplorazione. È gratis, e soprattutto **non è un obiettivo scritto da un autore**: è una lista che il giocatore si è fatto ascoltando, e la sente sua.

Quello che il taccuino **non** ha è una sezione «ipotesi» o «dubbi» compilata dal gioco. Quella parte la scrive il giocatore, accostando (vedi *Il confronto*), e se non l'ha ancora scritta è perché non ci è ancora arrivato — che è il gioco, non un difetto.

### 2. Le contraddizioni si mostrano accostate, non spiegate

Il taccuino non scrive mai «Matteo mente» — sarebbe una conclusione, e le conclusioni non sono sue. Le due righe stanno lì, in ordine di quando sono state raccolte:

> *Matteo: «Io e te non ci eravamo mai parlati.»*
> *Il foglio con l'indirizzo è di mia mano.*

Sta al giocatore accorgersene e **accostarle**. Se lo fa, ha in mano un confronto da portare in bottega; se non lo fa, quelle due righe restano due righe.

L'unico aiuto che il gioco si concede è di **non seppellirle**: le dichiarazioni che riguardano la stessa persona stanno vicine, perché è così che uno tiene un taccuino. Nient'altro. **Il gioco non ha detto niente, e chi capisce ha capito da solo** — è lo stesso trucco della frase e della porta nel primo atto, ed è la firma di questo gioco.

### 3. I personaggi si mandano l'un l'altro

La guida più naturale che esista in un gioco di conversazione è una persona che dice *questo chiedilo a un altro*, ed è anche quello che fanno le persone vere.

**Regola generale della tabella epistemica: ogni personaggio, oltre a quello che sa, sa chi ne sa di più.** Costa niente — è una colonna in più nei dati — e produce il rimando in modo spontaneo, senza che nessuno reciti un cartello.

Laura manda ad Anna e a Matteo. Don Carlo manda a Matteo. Anna manda a Nino per la montagna. Nino non manda a nessuno perché non conosce nessuno, ed è giusto così.

### 4. Rosa è la persona con cui si ragiona ad alta voce

Questo è il pezzo che mancava, ed è insieme il sistema di aiuto e un personaggio.

Rosa è sempre a casa, è sempre disponibile, non sa niente di niente, e **è l'unica persona al mondo che gli chieda come è andata.** Giorgio torna la sera e le racconta a che punto è — che è quello che fa un figlio di ventiquattro anni che vive con sua madre.

Il che significa che alla sua scheda si passa **una cosa che nessun altro personaggio riceve: le righe aperte del taccuino.** Non è una falla nell'isolamento epistemico: è che il giocatore gliele sta raccontando. Lei non ha nessun fatto da aggiungere e non ne aggiunge; **rimanda indietro i nomi.**

> *E quel Nino, l'hai più sentito? Diceva che ti aveva trovato lui.*

**Una madre come sistema di suggerimenti**, che non sa niente e che quindi non può rovinare niente. È gratis, è in carattere, ed è l'uso migliore che si possa fare di un modello linguistico: riformulare quello che ha appena sentito, sotto forma di preoccupazione.

E ha un costo emotivo che la mantiene un personaggio invece di un'interfaccia: **più lui avanza, meno lei vuole sentirlo.**

### 5. Il quarto atto è guidato perché i primi tre l'hanno insegnato

Non serve spiegare al giocatore come si smonta la frase di Matteo, perché a quel punto **ha già fatto tre volte esattamente quella cosa.** Il gioco ha insegnato il proprio verbo con casi facili — trova un secondo sostegno, accosta due righe, porta un oggetto a chi reagisce — e poi ne dà uno difficile.

Da qui la regola per la scrittura degli atti: **ogni atto insegna il meccanismo che l'atto dopo richiede.** Il primo insegna che una cosa che hai già può essere una chiave. Il secondo che un oggetto apre le persone. Il terzo che una testimonianza innocente può inchiodare qualcuno. Il quarto chiede tutte e tre insieme.

### 6. La mappa deve portare la geografia

Il pezzo di Nino funziona solo se il giocatore, sentendo *«il castagneto sopra la curva»*, sa dov'è quella curva — perché ci è passato venti volte per andare in bottega.

**Non è una nota di sceneggiatura, è un requisito di livello:** la bottega di Matteo e il punto in cui Nino ha trovato Giorgio devono essere visibilmente lo stesso pezzo di paese, e il giocatore deve averci camminato per tre atti prima che quella frase abbia senso. Se la mappa non ci arriva, quel pezzo non esiste.

### 7. La rete di sicurezza, che non dà informazioni

Se passano diverse conversazioni senza che si stabilisca niente, il gioco non manda nessun suggerimento e non apre nessun pannello. Fa una cosa sola: **rimette in cima al taccuino la domanda aperta più vecchia.**

Nessuna informazione nuova, nessuna freccia. Solo una cosa che il giocatore aveva già scritto e ha smesso di guardare — che è, di nuovo, quello che fa un uomo che rilegge i propri appunti.

## Ricordare invece di persuadere

L'intuizione è giusta e vale la pena dire perché.

In Valnera l'obiettivo è esterno — una firma — e la conversazione è lo strumento. Si può vincere parlando bene senza che al giocatore importi niente della storia. In Amnesia **la condizione di vittoria è la comprensione del giocatore**, e quella non si può forzare: non esiste un modo di essere abbastanza persuasivi da saltare il capire.

E sfrutta i modelli meglio, non solo diversamente. A un LLM chiedere *sii una persona con una versione, e tienila* è più vicino a quello che sa fare che *fatti manipolare lungo un asse di punteggio*.

Il rovescio, già detto sopra, è che ricordare non è più *strict* di persuadere: è più lasco, e il taccuino esiste per quello.

## Il vero cardine: le informazioni non passano a parole

Valnera ha risolto un problema di forma identica. **I modelli potevano dire che uno scambio era avvenuto, ma non potevano muovere la merce**: la roba passava solo attraverso gli strumenti, e solo se il motore lo consentiva.

Amnesia toglie la merce e applica la stessa regola **alle informazioni**.

> Un personaggio può dire quello che vuole. **Una cosa risulta detta solo se il motore l'ha registrata** — cioè se il modello ha chiamato `dichiaro(<id>)` e il predicato di quella dichiarazione era soddisfatto.

Il resto è prosa. Bella, in carattere, libera, e senza nessun effetto sullo stato del mondo.

Ne discende la simmetria con cui il gioco tratta le due direzioni:

| Direzione | Canale | Chi decide |
|---|---|---|
| giocatore → personaggio | `[mostra: <oggetto>]` e il **confronto** | `PlayerInput`: possiedi davvero quell'oggetto? hai davvero quelle due righe sul taccuino? |
| personaggio → giocatore | `dichiaro(<id>)`, e la concessione d'oggetto | il motore: il predicato è soddisfatto? |

**Niente trattativa in nessuna delle due direzioni.** Un personaggio che non deve dare una cosa non la dà perché non ce l'ha nel prompt, non perché rifiuta un'offerta.

E il gate non impedisce mai di *dire*. Il giocatore può andare da Laura al primo minuto e dirle che sua figlia è viva: quella donna sentirà un ragazzo appena uscito dal coma che dice una cosa insensata, e il motore non registrerà niente.

Che è precisamente la tesi del gioco: **dire una cosa vera senza avere niente in mano non muove nessuno.**

## La coerenza fra i prompt

Nove schede scritte in momenti diversi divergono. È il modo tipico in cui un gioco così si sfalda, e non si vede finché non ci si gioca: due personaggi che chiamano la cava con due nomi diversi, uno che parla come nel 1987 e uno come oggi, tre che hanno tre idee di quanto sia lontana Chivasso.

La regola è **una sola fonte per ogni cosa condivisa**, e le schede non la ripetono mai.

**Sta in `rules.md`, identico per tutti** — perché è il mondo, non il personaggio:

- l'epoca e il registro linguistico: 1987, italiano parlato di provincia, niente anglicismi, niente psicologia da manuale, il *lei* e il *tu* secondo la confidenza;
- la geografia e i nomi propri dei luoghi: San Rocco, Pian della Soglia, la Stanza, Roccavione, Chivasso, il deposito;
- le distanze e i tempi (Chivasso è due ore di corriera, e lo è per tutti);
- **la versione del paese, alla lettera**, marcata come modo di dire — è la cosa che più di ogni altra deve uscire identica da bocche diverse;
- i canali, la vincolatività degli strumenti, i limiti percettivi, lo stile.

**Sta nella scheda del personaggio, e solo lì:**

- chi è, che mestiere fa, come parla lui in particolare;
- cosa crede — comprese le convinzioni metafisiche, che devono **divergere fra loro**;
- la sua storia di copertura, scritta in prima persona come convinzione sincera;
- il suo rapporto con Giorgio e con gli altri.

Tre invarianti da verificare. Un grep le **segnala**, non le decide — provato sul campo: cercare i fatti condivisi duplicati fra schede produce sia le violazioni vere sia due persone che ricordano lo stesso anno, e a distinguerle bisogna guardarle. Vale come rete, non come cancello:

1. **Nessun fatto del mondo compare in due schede.** Se ce lo trovi, va in `rules.md`.
2. **Nessuna scheda contiene un'istruzione a nascondere.** Ciò che non deve uscire non sta nel prompt: lo governa la posizione.
3. **La versione del paese è citata alla lettera**, sempre con le stesse parole, e nessuna scheda ne contiene una variante.

E la coerenza va misurata, non sperata: la campagna di validazione deve far dire la stessa cosa a personaggi diversi e confrontare le uscite. **La deriva più costosa è quella metafisica** — un modello di frontiera scivola verso la spiegazione razionale su *tutti* i personaggi se ogni scheda non lo contrasta, e a quel punto Anna, che ci crede ancora, smette di essere inquietante e diventa una signora imbarazzata.

## Predisposizione al 3D

La presentazione andrà in **3D realistico a bassa risoluzione** — direzione artistica che costa poco a renderizzare e che si sposa con il 1987. L'engine resta Godot: il collo di bottiglia di questo gioco è la tabella epistemica e la scrittura, non il renderer, e le uniche righe che il passaggio al 3D obbliga a riscrivere sono quelle di `scenes/`, che andrebbero riscritte comunque.

Il dominio è già pronto e **non va toccato**:

- `src/` non dipende da nessun nodo e non sa niente di come il gioco venga disegnato;
- le posizioni vivono in `world.actors[<id>]["position"]` come celle `{x, y}`, e una cella è un'astrazione di navigazione, non di rendering: in 3D diventa una posizione sul piano con una scala, e la telecamera è affar suo;
- `Navigator` (`AStarGrid2D`) continua a valere: i personaggi camminano su un piano;
- il movimento è già funzione dei minuti di gioco e non dei fotogrammi.

Quello che va rispettato mentre si lavora al contenuto:

- **non scrivere altro codice di scena 2D.** Il client 2D esistente resta come banco di prova giocabile e non viene esteso;
- **la scena parla al dominio solo attraverso `SessionRunner.apply` / `advance` e `adopt_turn(result)`.** È già così, e resta l'unico seam;
- **nessun testo di contenuto deve descrivere l'inquadratura.** Le schede descrivono persone e stanze, mai riprese;
- il taccuino, il confronto e la selezione degli oggetti sono **UI**, e vanno progettati come un pannello che si può ridisegnare in 3D senza toccare la logica sotto.

Il client 3D è una tranche a sé, dopo la fetta verticale.

## Cosa cambia nel motore

Poco, e quasi tutto è additivo.

- **`src/knowledge/knowledge_service.gd`** — secondo asse: `witnessed(npc, proposizione)`, chi ha visto stabilire cosa. Stesse funzioni, chiave doppia.
- **Registro delle proposizioni** — nuovo, in `world.flags`: per ogni proposizione, i sostegni raccolti e da dove. Lo scrive solo il motore.
- **`src/dialogue/context_builder.gd`** — due blocchi nuovi nel prompt: `<posizione>` (la convinzione corrente, in prima persona) e `<cosa_sa_di_te>`.
- **Strumento nuovo a vocabolario chiuso** — con cui un personaggio dichiara di cedere su una proposizione. Vocabolario chiuso, quindi il motore sa esattamente cosa è stato ammesso; e l'esito è vincolante, come già oggi.
- **Tabella delle posizioni** — dati, non codice: una scala per personaggio guardingo, con predicato e testo. **Il `ContextBuilder` inietta solo la porzione di colonna epistemica che la posizione corrente concede** — è qui che si impedisce la rivelazione anticipata, non nelle istruzioni.
- **Serrature** — dati: quattro luoghi con più chiavi ciascuno, e il predicato di apertura.
- **Tabella delle dichiarazioni** — dati: id, testo canonico, fonti, precondizione, cosa sblocca. Venticinque-trentacinque righe, sei o sette false.
- **Strumento `dichiaro(id)`** — vocabolario chiuso, con cui un personaggio segnala al motore di aver effettivamente detto quella cosa. La prosa resta libera.
- **Il taccuino** — vista sul registro, in due metà: le dichiarazioni raccolte (senza giudizio di verità) e i nomi pronunciati e non ancora visitati. **Nessuna logica nuova, due query.**
- **Il confronto** — accostamento di due dichiarazioni scelte dal giocatore, che diventa un oggetto presentabile come una fotografia. È l'unica meccanica davvero nuova di tutto Amnesia, ed è quella su cui vive il gioco.
- **La colonna «chi ne sa di più»** — un campo in più per personaggio nella tabella epistemica, che produce i rimandi fra NPC senza scrivere una riga di codice.
- **Rosa riceve le righe aperte del taccuino** — l'unica eccezione all'isolamento epistemico di tutto il gioco, ed è giustificata: gliele sta raccontando lui. Non aggiunge fatti, rimanda indietro nomi.
- **Rilevatore di contraddizioni** — deterministico, confronta dichiarazioni registrate. È anche quello che accorge il giocatore quando tre bocche dicono la stessa frase impossibile.
- **Gli otto file con i nomi di Valnera cablati** (`flag_engine.gd`, `tool_executor.gd`, `context_builder.gd`, `tool_catalog.gd`, `session_runner.gd`, `epilogue.gd`, `prompt_library.gd`, `scenes/village.gd`) — vanno resi generici o riscritti per il cast nuovo. Lavoro noioso e già mappato.

