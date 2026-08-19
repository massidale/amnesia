# AMNESIA — contenuto

> Tabelle. La storia sta in `2026-08-19-amnesia-storia.md`, il design in `2026-08-19-amnesia-design.md`.
> Questo documento è la fonte da cui si generano i dati: dichiarazioni, colonne epistemiche, scale di posizione, oggetti.

## Convenzioni

**Verità** è quello che sa il motore, mai il giocatore e mai il modello. Un personaggio che dice una cosa falsa **non sa di mentire**: nella sua scheda quella cosa è scritta come convinzione sincera, e nessun prompt gli dice mai il contrario.

- `V` — vera
- `F` — falsa, detta in buona fede
- `½` — vera in parte, e la parte falsa è quella che assolve chi la dice

**Precondizione** è ciò che deve essere già stabilito perché quella dichiarazione entri nel prompt del personaggio. Se la precondizione non è soddisfatta, **quella riga non esiste nel suo contesto** e non può sfuggirgli.

---

## 1. Le dichiarazioni

### Il Circolo

| id | testo canonico | chi | pre | ver |
|---|---|---|---|---|
| `circolo_esisteva` | C'era un gruppo che si trovava, si facevano chiamare il Circolo della Soglia. | chiunque, con la foto | — | V |
| `scampagnate` | Era una compagnia di amici. Si andava su alla cava a fare le scampagnate, roba di quando eravamo giovani. | Anna, Laura, Matteo | — | **F** |
| `avevano_una_frase` | Avevano una specie di detto fra loro. Qualcosa di una porta. | Nino, Rosa, Don Carlo, il paese | — | V |
| `non_erano_gite` | Non erano gite. Ci si trovava per altro. | Anna, Laura, Matteo | `frase_detta` | V |
| `padre_nel_circolo` | Tuo padre era dei nostri. Otto anni, forse nove. | Anna, Laura | `frase_detta` | V |
| `magazzino_dove` | La roba della cava l'hanno portata giù al deposito. Il magazzino in fondo, il diciassette. | Anna, Laura | `frase_detta` | V |
| `affitto` | Tuo padre l'affitto di un posto l'ha pagato per vent'anni. Non gli ho mai chiesto di cosa. | Rosa | — | V |

**La frase non la pronuncia nessun personaggio, mai.** `avevano_una_frase` dice che esisteva e ne cita una parola. Giorgio ce l'ha per intero sul taccuino dalla prima scena. Il collegamento è un **confronto**, e lo fa il giocatore.

### Elena, e la versione del paese

| id | testo canonico | chi | pre | ver |
|---|---|---|---|---|
| `versione_paese` | È stata una disgrazia. Erano andati su a vedere la cava, la montagna è venuta giù, e per quella creatura è stato un attimo. | **tutti**, nessuno escluso | — | **½** |
| `elena_figlia_vittorio` | Era la figlia di Vittorio Valli, quello che comandava. | chiunque | — | V |
| `corpo_mai_trovato` | Non l'hanno mai tirata fuori. Hanno murato la galleria e ci hanno messo la lapide. | chiunque | — | V |

`versione_paese` è **il coro**. Va citata alla lettera da ogni bocca, marcata nella scheda come modo di dire — *«questa cosa in paese si dice così, con queste parole, da vent'anni»*. Le prime due proposizioni sono difendibili; **la terza è impossibile**, perché nessuno era là sotto. Il motore non la segnala mai: la nota il giocatore alla terza bocca.

### Il rito

| id | testo canonico | chi | pre | ver |
|---|---|---|---|---|
| `il_rito` | Per far tornare qualcuno di là, qualcuno di qua deve passare la soglia e tornare indietro. | il quaderno di Vittorio | `magazzino_aperto` | V |
| `sacrificio_per_vittorio` | Doveva servire per Vittorio. Era morto in primavera, e lui era quello che poteva tornare. | Anna | `braccialetto_mostrato` | V |
| `elena_prescelta` | Era lei che doveva passare. Serviva il sangue. | Anna, Laura | `braccialetto_mostrato` | V |
| `laura_non_cera` | Io lassù non c'ero. Sono rimasta a casa. | Laura | `braccialetto_mostrato` | V |
| `laura_assoggettata` | Ero assoggettata a mio marito. Non giudicarmi. | Laura | `braccialetto_mostrato` | **½** |
| `anna_solo_matteo` | Quando sono scesa in paese, alla cava era rimasto solo Matteo. | Anna | `braccialetto_mostrato` | V |
| `padre_nella_cava` | Tuo padre quella notte c'era. | Anna; il registro | `frase_detta` | V |
| `io_ero_con_lui` | E c'eri anche tu. Ti aveva portato su, non aveva nessuno a cui lasciarti. | Anna; il registro | `padre_nella_cava` | V |

`laura_assoggettata` è l'unica `½` fra i vivi ed è la più delicata da scrivere: **è vera per tre quarti** — Vittorio l'aveva portata in quel gruppo, e dopo la sua morte è stato il gruppo a tenercela, in nome di lui. La parte falsa è l'implicazione: che non fosse libera **nemmeno mentre restava in cucina con la luce accesa**. Lei ci crede. Il gioco non arbitra mai.

### Matteo

| id | testo canonico | chi | pre | ver |
|---|---|---|---|---|
| `matteo_ero_gia_sceso` | Quella sera io ero già sceso quando è venuta giù. Ho saputo tutto la mattina dopo, come tutti. | Matteo | — | **F** |
| `matteo_la_porto_via` | L'ho sentita sotto le pietre. L'ho tirata fuori io. | Matteo | `confronto(anna_solo_matteo, matteo_ero_gia_sceso)` | V |
| `elena_viva` | È viva. È grande. Ha una vita, in un'altra città. | Matteo | `matteo_la_porto_via` | V |
| `matteo_non_dice_dove` | Lasciala stare. Ha una vita che non c'entra niente con noi. | Matteo | `elena_viva` | V |
| `matteo_cosa_ti_ricordi` | Senti… ma tu, di preciso, cosa ti ricordi? | Matteo | — | V |
| `matteo_mai_parlati` | Io e te non ci eravamo mai parlati. | Matteo | `matteo_rifiuta_il_foglio` | **F** |
| `matteo_confessa` | Quando ti sei girato ho visto tutto finire. Ho preso una pietra dal banco. Non ho deciso niente. | Matteo | `confronto(matteo_mai_parlati, segatura)` | V |

`matteo_cosa_ti_ricordi` è **il collante del quarto atto** e non ha precondizione: Matteo la fa **ogni volta**. Il taccuino la registra con la data, e alla quarta ricorrenza il giocatore ha una riga ripetuta in un taccuino che non ripete mai niente. È la stessa macchina del coro — lì si contano le bocche, qui le volte.

### Il 1985

| id | testo canonico | chi | pre | ver |
|---|---|---|---|---|
| `usciva_allegro` | Quella sera è uscito dopo cena. Era allegro, la prima volta in mesi. Diceva che andava da uno che sapeva. | Rosa | — | V |
| `segatura` | *(oggetto)* Nei risvolti dei pantaloni c'è la segatura. La giacca è consumata sulla schiena. | la scatola di Rosa | — | V |
| `referto` | *(oggetto)* Trauma occipitale. Escoriazioni su dorso e talloni. | la cartella clinica | — | V |
| `dove_lo_trovai` | Nel castagneto sopra la curva. Lì non ci si arriva camminando, e non ci si cade da soli. | Nino | — | V |
| `giovedi_sabato` | Sei venuto da me di giovedì. Il sabato ti hanno trovato. | Don Carlo | — | V |
| `nessuno_denuncio` | Si è detto che eri caduto. Poi si è smesso di dirlo. Nessuno ha mai denunciato niente. | il paese | — | V |

Tutti disponibili **dal primo atto**, e tutti privi di significato fino al quarto. È la regola del gioco: **niente è nascosto, tutto è illeggibile finché non lo sai leggere.**

### Il padre

| id | testo canonico | chi | pre | ver |
|---|---|---|---|---|
| `padre_cercava` | *(oggetto)* Cercava qualcuno che credeva vivo. I nomi sono raschiati. | il diario | — | V |
| `padre_veniva_da_me` | Tuo padre veniva qui. Spesso, negli ultimi anni. Di quello che mi ha detto non posso dirti niente. | Don Carlo | — | V |
| `busta_esiste` | C'è una cosa che tuo padre ha lasciato per te. Mi ha detto quando dartela: se un giorno fossi venuto a chiedere di quella bambina. | Don Carlo | — | V |
| `don_carlo_manda` | Non posso dirti niente. Va' a parlare con Matteo. | Don Carlo | `braccialetto_mostrato` | V |
| `wanda_una_persona_sola` | Di questa faccenda io parlo con una persona sola, e non è lei. | Wanda | — | V |

`busta_esiste` è **una serratura dichiarata**: Don Carlo dice al primo incontro che la busta c'è e a quale condizione. La condizione va letta stretta — *chiedere di quella bambina* significa chiedere di **una persona viva**, cioè `elena_viva` stabilita. Il prete non fa il difficile: fa esattamente quello che gli è stato chiesto.

### I quattro confronti, e i due ricordi

Nessuno di questi esce da una bocca. **Sono le uniche cose che il gioco non regala.**

| risultato | come si compone |
|---|---|
| la frase che ripeto è la loro | `taccuino_frase` × `avevano_una_frase` |
| Elena non è mai stata sepolta là sotto | `cassetta_di_latta` × `versione_paese` |
| quello che mio padre cercava era Elena | `padre_cercava` × `braccialetto` |
| Matteo mi ha mentito | `matteo_mai_parlati` × `segatura` |

| ricordo | quando |
|---|---|
| il buio di una stanza laterale, una lampada, voci più avanti, una corda che scende | dopo `io_ero_con_lui` |
| era mio padre a tenere la corda, e si è girato | nella galleria, dopo la lettera |
| la stessa parola, con la stessa voce, alle mie spalle | **dopo** `matteo_confessa`, mai prima |

I ricordi entrano nel registro con **fonte `memoria` e affidabilità sotto uno**: non sono mai la prima via su niente. Il terzo arriva un secondo troppo tardi per servire, e serve solo a far sentire a Giorgio una cosa che ha appena finito di ascoltare.

---

## 2. Le scale di posizione

La posizione governa **cosa il personaggio ha nel prompt**, mai cosa gli è permesso dire. Nessuna scheda contiene un'istruzione a nascondere.

### Matteo Sardi

| | dispone di | sale quando |
|---|---|---|
| **M0** | `scampagnate`, `versione_paese`, `matteo_ero_gia_sceso`, `matteo_cosa_ti_ricordi` | — |
| **M1** | + `circolo_esisteva`, `non_erano_gite`, `padre_nel_circolo` | `frase_detta` |
| **M2** | + `il_rito`, `elena_prescelta` | `braccialetto_mostrato` |
| **M3** | + `matteo_la_porto_via`, `elena_viva`, `matteo_non_dice_dove` | `confronto(anna_solo_matteo, matteo_ero_gia_sceso)` |
| **M4** | + `matteo_mai_parlati` | il giocatore chiede il foglio per Wanda e lui rifiuta |
| **M5** | + `matteo_confessa`, e concede il foglio | `confronto(matteo_mai_parlati, segatura)` |

**Matteo non dice mai dove abita Elena.** L'indirizzo Giorgio ce l'ha in tasca dalla prima scena, scritto di suo pugno sotto dettatura di quest'uomo due anni fa. Quello che Matteo ha e Giorgio no è **chi ci abita**, e non lo dà mai: lo scavalca il foglio per Wanda.

### Anna Ferro

| | dispone di | sale quando |
|---|---|---|
| **A0** | `scampagnate`, `versione_paese` | — |
| **A1** | + `circolo_esisteva`, `non_erano_gite`, `padre_nel_circolo`, `magazzino_dove` | `frase_detta` |
| **A2** | + `il_rito`, `sacrificio_per_vittorio`, `elena_prescelta`, `anna_solo_matteo`, `padre_nella_cava`, `io_ero_con_lui` | `braccialetto_mostrato` |

Anna è **membro, non ex**. Ci crede ancora, e la sua reticenza è riserbo su una cosa sacra, non vergogna — molto più difficile da smuovere di chi si protegge. In A2 racconta quelle serate **dall'interno**, con le parole di chi c'era e ci credeva.

`anna_solo_matteo` non è un'accusa e non deve mai suonare come tale: sta facendo l'elenco di chi c'era e chi no.

### Laura Valli

| | dispone di | sale quando |
|---|---|---|
| **L0** | `scampagnate`, `versione_paese`, `elena_figlia_vittorio` | — |
| **L1** | + `circolo_esisteva`, `non_erano_gite`, `padre_nel_circolo`, `magazzino_dove` | `frase_detta` |
| **L2** | + `elena_prescelta`, `laura_non_cera`, `laura_assoggettata` | `braccialetto_mostrato` |

### Don Carlo Bessone

| | dispone di | sale quando |
|---|---|---|
| **C0** | `avevano_una_frase`, `padre_veniva_da_me`, `busta_esiste`, `giovedi_sabato` | — |
| **C1** | + `don_carlo_manda` | `braccialetto_mostrato` |
| **C2** | consegna la busta | `elena_viva` |

**Il vincolo di Don Carlo non è una posizione: è una regola della sua scheda.** Di ciò che gli è arrivato in confessione — da Andrea e da Giorgio — non dice niente, mai, in nessuna posizione e sotto nessuna pressione. Più lo si spinge, più si chiude. È l'unico personaggio su cui incalzare è controproducente, e il giocatore deve accorgersene da solo.

Quello che **non** è coperto dal vincolo, e che quindi può dire: che Andrea veniva; i giorni; che esiste una busta e a quale condizione. Un oggetto depositato non è un segreto confessato.

### Rosa, Nino, Wanda

**Non hanno scale.** Non nascondono niente e non sanno niente da nascondere.

**Rosa** evita, e basta: è la prima persona in cui il giocatore incontra il meccanismo che regge tutto il paese. Non mente su niente. E ha la funzione che nessun altro ha — vedi *Rosa e le righe aperte* nel design.

**Nino** non ha niente da nascondere e del Circolo non gli importa. Sa una cosa fisica e la dice a chiunque gliela chieda.

**Wanda** ha una paura sola e una frase sola. Apre solo con due righe di Matteo.

---

## 3. Chi ne sa di più

La colonna che produce i rimandi fra personaggi senza scrivere codice. Ogni personaggio, oltre a quello che sa, sa **chi andare a cercare** su un argomento che non è suo.

| personaggio | manda a | su cosa |
|---|---|---|
| Rosa | Don Carlo | tuo padre, gli ultimi anni |
| Rosa | Nino | quella notte, chi ti ha trovato |
| Don Carlo | Anna | quello che facevano lassù |
| Don Carlo | Matteo | Elena, dopo il braccialetto |
| Anna | Laura | Elena e sua madre |
| Anna | Nino | la montagna, la galleria |
| Laura | Anna, Matteo | chi c'era davvero quella notte |
| Nino | nessuno | non conosce nessuno, e va bene così |
| Matteo | nessuno | manda via, non manda da qualcuno |
| Wanda | «una persona sola» | senza fare il nome |

**Wanda non fa il nome** ed è deliberato: non sa che quel ragazzo conosce Matteo, e comunque non lo direbbe. Il giocatore esce da Chivasso con un obiettivo netto e un pezzo che gli resta in tasca per mezza partita — e quando Matteo confessa di essere stato lui a portarla via, il pezzo si incastra da solo, senza che nessun personaggio abbia mai messo in relazione le due cose.

---

## 4. Gli oggetti

**Una chiave, un mestiere.** La prima stesura violava questa regola e il braccialetto ne faceva cinque.

| oggetto | faccia visibile | mestiere unico | dove |
|---|---|---|---|
| **la fotografia** | undici persone davanti alla cava, 1961; sul retro a matita *Circolo della Soglia* | apre il paese: ogni faccia riconosciuta è un nome, ogni nome è una porta | dal risveglio |
| **la frase** | cinque parole senza senso | prova che sei già dentro la cosa → i membri smettono con la scampagnata | dal risveglio, sul taccuino |
| **il foglio** | *Via Sant'Orsola 14 — Chivasso*, di sua mano | è di sua mano e lui a Chivasso non è mai stato: qualcuno gliel'ha dettato | dal risveglio |
| **la chiave B-17** | targhetta d'ottone | apre il magazzino, una volta saputo dov'è | dal risveglio |
| **il diario** | mezzo raschiato; sopravvivono i posti e il verbo, mai un nome | mio padre cercava qualcuno che credeva vivo | Rosa |
| **la scatola dei vestiti** | lavati e piegati; segatura nei risvolti, giacca consumata sulla schiena | c'è un falegname solo a San Rocco | Rosa |
| **la cartella clinica** | trauma occipitale, escoriazioni | colpito da dietro, e trascinato | Rosa / l'ospedale |
| **il quaderno di Vittorio** | il rito per esteso, nessun nome, nessuna data | trasforma «facevano dei riti» in *chi doveva passare* | magazzino |
| **il registro** | presenze dal 1958, nomi di battesimo e iniziali | l'ultima pagina: **A. L.** c'è, *+ il bambino*, **L. V.** non c'è | magazzino |
| **la cassetta di latta** | scarpine, un fermaglio, un braccialetto inciso *E. V.* | in quel buco c'era una bambina | magazzino |
| **la busta di Andrea** | chiusa, con una condizione scritta sopra | la lettera del padre | Don Carlo, su `elena_viva` |
| **le due righe di Matteo** | un foglio, la sua calligrafia | l'unica firma di cui Wanda si fidi | Matteo, in M5 |

Il braccialetto ricompare a Chivasso e **non apre più niente**: è solo la prima cosa che qualcuno abbia mai potuto dare a quella donna. L'ultimo oggetto del gioco non fa niente, e conta più di tutti.

**Il magazzino non contiene risposte** — vincolo, non dettaglio. Un archivio che chiude i fatti da solo cortocircuita un gioco il cui unico verbo è parlare. Il sacrificio non è scritto da nessuna parte: esce dalla bocca di Anna, davanti al braccialetto.

---

## 5. Le serrature

| serratura | chiavi |
|---|---|
| **sapere cos'è la frase** | ce l'ha già; gli manca capire che serve. Don Carlo · Nino · dirla a un membro e guardarlo in faccia |
| **il magazzino B-17** | la chiave d'ottone ce l'ha; gli manca **dov'è**. Anna · Laura (Matteo lo sa e non lo dice) |
| **la galleria murata** | Nino (attrezzi, conosce la montagna) · Anna (il secondo imbocco che usava il Circolo) |
| **la busta di Andrea** | chiave unica e **dichiarata**: `elena_viva`. Don Carlo annuncia condizione e serratura al primo incontro |
| **la porta di Wanda** | **Matteo**, e nient'altro: due righe di suo pugno, o lui in persona |

**Chivasso non è una serratura.** L'indirizzo è in tasca dal primo minuto e chi ci va presto trova una porta che si apre di dieci centimetri — e ne esce con `wanda_una_persona_sola`, che è il pezzo che gli servirà tre atti dopo. Andarci presto non è mai tempo perso.

**Lo stallo del terzo atto ha due uscite**, e sono tutte e due brutte:

1. **il 1985** — dissotterrare il tentato omicidio di sé stesso e usarlo come leva;
2. **Laura** — dirle che sua figlia è viva e lasciare che vada lei in bottega. Funziona, è più veloce, e apre a metà partita la più grossa delle tre porte dell'epilogo senza sapere cosa c'è dietro.

Il gioco non dice mai quale sia quella giusta e non premia nessuna delle due. Cambia solo con cosa si esce: dalla prima, una confessione e un uomo che chiede scusa; dalla seconda, un foglio, e senza aver mai saputo perché ti hanno trovato in un bosco.

---

## 6. Casi limite

- **Il giocatore dice a Laura che sua figlia è viva al primo minuto.** Legittimo, e non succede niente: una donna a cui un ragazzo appena uscito dal coma dice una cosa del genere sente un pazzo. Il motore non registra. Il gate non impedisce di dire: impedisce che dire funzioni.
- **Il giocatore pronuncia la frase senza sapere cos'è.** Funziona. Chi sperimenta viene premiato, e il gioco non deve spiegargli niente.
- **Il giocatore incalza Don Carlo.** Si chiude. Più pressione, meno accesso — è l'unico personaggio con questa curva, e va scoperto sbattendoci contro.
- **Il giocatore mostra il braccialetto prima della frase.** I membri sono ancora a posizione 0 e non hanno le righe del rito nel prompt: reagiscono a un oggetto che non sanno spiegare. Nessuna informazione esce, e nessuna occasione è bruciata.
- **Il giocatore accosta due righe che non c'entrano.** Gliela portano indietro come si guarda uno che ha detto una sciocchezza. Costa tempo, non progressione.
- **Il giocatore non trova mai la scatola dei vestiti.** Il quarto atto ha una seconda via indipendente (la cartella clinica) e una seconda uscita completa (Laura).
