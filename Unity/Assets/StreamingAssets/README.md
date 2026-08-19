# Il contenuto del mondo

Tre file, e nessuno dei tre ammette commenti. Quello che andrebbe scritto dentro
sta qui.

- `village_map.json` — San Rocco di Valdieri, ottobre 1987
- `routines.json` — dove sta ognuno, ora per ora
- `items.json` — il catalogo degli oggetti

Il formato di ciascuno lo decide il caricatore che lo legge — `VillageMap`,
`RoutineTable`, `ItemCatalog` — non questo documento: se le due cose divergono,
ha ragione il codice. Le prove stanno in `Amnesia.Domain.Tests/Content/`.

---

## `village_map.json`

Una griglia sola, 48 × 32, con gli interni e gli esterni disegnati sulla stessa
superficie. Non ci sono scene: una porta è una cella calpestabile in un muro, e
attraversarla è un passo come gli altri.

### La legenda

| segno | | |
|---|---|---|
| `.` | terra battuta, cortili, piazzali | si passa |
| `,` | strada e mulattiera | si passa |
| `~` | pavimento di un interno | si passa |
| `+` | soglia | si passa |
| `"` | sottobosco del castagneto | si passa |
| `#` | muro, muratura, monumento | non si passa |
| `T` | tronco, macchia, scarpata boscosa | non si passa |
| `^` | roccia, parete, cresta | non si passa |
| `=` | binari dello scalo | non si passa |

### La geografia che regge una battuta

Nino Bergesio dice di aver trovato Giorgio *«nel castagneto sopra la curva»*, e
non aggiunge altro perché per lui è solo un posto. La curva è quella **sotto la
bottega di Matteo**, e nessun personaggio lo dirà mai: deve dirlo la mappa.

Perciò, dall'alto verso il basso: il **castagneto** (x 20–25, y 8–9), la
**bottega** (x 21–27, y 12–14) che ci sta appoggiata sotto con la porta del
cortile che dà dritta nei castagni, e il **tornante** della strada alta subito
sotto la bottega, in colonna 22.

E soprattutto: la scarpata alberata in riga 17 chiude il paese verso l'alto per
tutta la sua larghezza, e il tornante è **l'unico varco**. Chi va da Matteo
passa di lì ogni volta, per tre atti, prima che quella frase abbia un senso.

Nel bosco si entra da due parti sole: dalla porta di dietro della bottega, e dal
varco ripido a levante (colonna 31). Camminando, in effetti, non ci si arriva.

### I minuti

Si cammina a **16 celle per minuto** (`MovementSystem.WalkCellsPerMinute`), e la
mappa è tarata su quello:

| da casa Lipari a | celle | minuti |
|---|---|---|
| la bottega | 24 | 1,5 |
| la piazza | 24 | 1,5 |
| casa Valli, in fondo a levante | 42 | 2,6 |
| il magazzino B-17, allo scalo | 45 | 2,8 |
| il castagneto | 31 | 1,9 |
| **Pian della Soglia, la cava** | **77** | **4,8** |
| la galleria murata, in fondo al piazzale | 87 | 5,4 |

Il paese si attraversa in tre minuti. Alla cava si **sale**: la mulattiera esce
dalla strada alta, prende la costa a levante del bosco e traversa sotto la
parete, e non c'è nessuna scorciatoia perché la roccia è tutta `^`. Andare su e
tornare giù costa dieci minuti di orologio, ed è una decisione, non un passaggio.

### I luoghi

Sedici rettangoli, e **nessuno si sovrappone a un altro**. `PlaceAt` renderebbe
il primo dichiarato, il che è una regola ottima da avere e pessima su cui
contare: un luogo dentro un altro fa arrivare qualcuno che non arriva mai, perché
il sistema di movimento cammina verso il centro del luogo e si ferma quando la
cella su cui è finito si chiama come la destinazione.

`cava` e `galleria` sono due cose diverse: il piazzale, e il corridoio in fondo
al quale c'è la muratura del 1966 (il `#` in 22,2). La lapide è il `#` in 17,3.
Il muro non è una porta chiusa a chiave: è un muro, e aprirlo è un fatto della
partita, non del disegno.

`deposito` è il piazzale dello scalo merci dietro la stazione; `magazzino_b17` è
il capannone che ci si apre sopra, quello della chiave d'ottone.

### Chi nasce dove

`spawn` dà la cella di partenza di ognuno. Ci sono **sette** attori: il
giocatore e Rosa in casa Lipari, Matteo in bottega, Laura in casa Valli, Anna in
casa Ferro, Don Carlo in canonica, Nino alla segheria.

**Wanda ed Elena non hanno una cella, ed è voluto.** Vivono a Chivasso, che non
sta su questa mappa e non deve starci: è una porta, non un secondo paese. Un
attore senza posizione non è da nessuna parte — è dove serve che sia — e
`Presence` è scritto apposta per reggerlo.

---

## `routines.json`

Un personaggio, una lista di voci `{from_minute, place}`, in minuti dalla
mezzanotte. `PlaceFor` tiene **l'ultima voce già scattata nell'ordine in cui il
file la elenca**: le voci vanno scritte in ordine crescente, e la tabella non
gira a mezzanotte, quindi l'ultima voce vale anche per tutte le ore successive.

Ha una routine solo chi si muove. Rosa esce per la spesa alle 8:30 e poi sta in
casa; Matteo è in bottega tranne che a pranzo e alla sera, quando è al bar; Don
Carlo dice messa alle 6:30 e sta in canonica il resto del giorno, tranne il tardo
pomeriggio; Anna va alla prima messa e poi si ferma in piazza; Laura passa in
chiesa a metà mattina, quando non c'è nessuno.

**Nino è l'unico che sparisce davvero.** Dalle sei a mezzogiorno è nel
castagneto, che è dove lavora — e cioè esattamente dove ha trovato Giorgio nel
1985. Chi lo cerca in paese la mattina non lo trova, e chi lo va a cercare lo
trova là.

Il movimento serve a far sentire il paese abitato e a rendere qualcuno
occasionalmente introvabile. Non è un rompicapo di orari: nessuna serratura di
questo gioco si apre trovando una persona in un certo minuto.

Chi ha una routine deve avere anche uno `spawn`, altrimenti non si muove e
nessuno lo segnala. Il giocatore non ne ha: si muove lui.

---

## `items.json`

Una lista di `ItemDefinition`: `id`, `visible`, `name`, `description`.

**`visible` è l'unica delle tre che entra in un prompt.** È quello che un uomo
vede quando gli si mette la cosa sul tavolo. `name` è l'etichetta
dell'inventario, `description` è scritta per il giocatore alla sua scrivania:
dentro la finzione nessuna delle due è percepibile da nessuno.

E quindi la regola che governa ogni riga di questo file:

> **la faccia visibile non contiene mai il segreto.**

Il braccialetto è *«d'argento sottile, inciso E. V.»*, non «il braccialetto di
Elena Valli». La segatura nei risvolti dei pantaloni è segatura, non «la prova
che era in una falegnameria». Le due righe di Matteo sono due righe a matita e
una firma, non «l'unica firma di cui Wanda si fidi». Quello che una cosa
**significa** esce dalla reazione di chi la guarda, mai dalla sua descrizione: una
faccia visibile che si spiega da sola è un enigma cancellato.

`ItemCatalog` non ha ancora un caricatore da disco — il file è scritto nella
forma che il suo costruttore prende, cioè una lista di `ItemDefinition`, e per
adesso lo deserializza la prova in `Amnesia.Domain.Tests/Content/`.

Il catalogo dice **cosa sono** le cose, non **di chi sono**: `ItemDefinition` non
ha un proprietario, perché chi possiede cosa è stato di partita e si salva col
resto. I quattro oggetti che Giorgio ha addosso al risveglio — `fotografia`,
`foglio_indirizzo`, `chiave_b17`, `taccuino` — glieli deve mettere in mano il
motore; questo file non può dirlo.

`frase` è un oggetto come gli altri e sta sulla prima pagina del taccuino. Non è
una cosa che si porta: è una cosa che si dice, e mostrarla significa pronunciarla
in faccia a qualcuno. È l'unico oggetto del gioco la cui faccia visibile è
esattamente il suo contenuto — cinque parole — e che è insieme innocuo e
insostenibile, a seconda di chi le ascolta.
