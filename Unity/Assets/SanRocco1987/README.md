# San Rocco 1987 - scena giocabile

Scena: `Assets/Scenes/SanRocco1987.unity`.

Le scene salvate sono configurate con il giocatore e il motore narrativo.
Edifici, natura e cava sono modelli provvisori, non asset artistici finali.
La prima rifinitura low poly comprende cornici, persiane, basamenti,
portale e rosone, tetti con intradosso, piazza lastricata, chiome sfaccettate
e creste montuose continue. Ogni fabbricato resta un prefab indipendente.
Gli interni sono arredati con modelli low poly: case, panificio, bar,
negozio, bottega, scuola, cooperativa, depositi, canonica e chiesa.
La stazione comprende sala d'attesa, sportello, pensilina e orologio.
Sono presenti 12 abitanti low poly, ciascuno con il componente `Personaggio`
e il proprio ID narrativo, collegato alle schede esistenti. Pose e routine
restano statiche; dialoghi, inventario, taccuino e porta B-17 usano il motore
del progetto. Le risposte LLM richiedono la configurazione OpenRouter in
`.env` alla radice del repository; non incorporare chiavi nelle build.

## Esplorazione

Aprire SanRocco1987 e premere Play. Il giocatore parte vicino a casa Lipari.
WASD: movimento. Mouse/frecce: visuale. E: parlare, porta o corriera.
I: inventario. T: taccuino. M: mappa della scena. Escape: pausa/chiudi dialogo.
V: prima/terza persona. Invio: invia la battuta nel dialogo.

La mappa del menu e' una piantina stilizzata, non una fotografia della scena:
sagome degli edifici, percorsi, bosco, cava, cimitero e binari. Le geometrie
sono ricavate dalle posizioni effettive. I nomi brevi degli NPC sono a 10 px,
con punti separati e un hover di colore e scala (+4%) senza muovere i punti.
`Aggiorna mappe stilizzate` aggiorna entrambe le carte senza rigenerare il paese.
`Verifica etichette mappa in Play` controlla dimensione, sovrapposizioni e hover.

## Organizzazione

- `layout.json`: posizioni in metri, dimensioni e orientamento degli edifici.
- `Buildings`: un prefab indipendente per edificio, piu' cava e cimitero.
- `Nature`: alberi e rocce originali low poly.
- `Characters`: 15 prefab individuali; `Previews/16_personaggi.png` li mostra insieme.
- `Meshes` e `Materials`: risorse persistenti riutilizzate dai prefab.
- `Previews`: immagini renderizzate da Unity, non illustrazioni della proposta.
- `validation.txt`: esito delle verifiche dell'ultima generazione riuscita.

Il pivot degli edifici e' al centro del pavimento. Spostare l'istanza del
prefab sposta l'edificio e i suoi figli. Terreno, strade e piazzole non si
aggiornano automaticamente quando si sposta un edificio in editor.

Il menu `Amnesia > San Rocco 1987 > Crea nuova scena completa`
rigenera entrambe le scene giocabili dal layout. `Crea fase 1 - mappa
esplorabile` produce invece la visita libera senza dialoghi.
La rigenerazione sovrascrive i prefab generati
e la scena SanRocco1987: duplicarla prima di apportare modifiche manuali
che si vogliono conservare. Le scene precedenti del progetto non vengono
modificate dal generatore.

## Relazioni narrative

Piazza centrale con chiesa a monte, bar a est, panificio e alimentari a ovest.
Casa Lipari vicino all'arrivo in paese. Nello scalo ci sono due fabbricati
indipendenti, `deposito_a` e `deposito_b`, con dieci locali ciascuno.
B-17 e' l'ultimo locale a destra del corridoio B, non un edificio autonomo.
La sua saracinesca conserva `PortaMarker.Id: magazzino_b17` e la stanza
il corrispondente `Luogo1987`: la chiave narrativa rimane `chiave_b17`.
Nella visita libera la serranda e' chiusa, perche' Bootstrap non e' attivo.
Bottega di Matteo sopra il tornante, uscita posteriore sul castagneto,
punto del ritrovamento fuori dal sentiero. Cava piu' a monte con ingresso
principale murato e accesso laterale. Memoriale della cava distinto dal
cenotafio di Elena al cimitero. Chivasso ha una scena separata, collegata
con la corriera nella visita libera.

Gli ingressi principali affacciano sulla piazza o sulle strade del villaggio.
La chiesa ha un solo portale sul sagrato; non ha un ingresso posteriore.
L'unica uscita sul retro degli edifici generati e' quella della bottega.
La generazione verifica sia le soglie sia i raccordi alla rete dei percorsi.

Il villaggio e' una porzione giocabile concentrata sui luoghi della storia,
non la riproduzione completa delle abitazioni di 1200 persone.

## Abitanti e case

Disposizione diurna coerente con le schede, non simulazione degli orari:

- Rosa e Laura sedute al tavolo delle rispettive case.
- Anna in casa Ferro; Don Carlo in canonica, in piedi.
- Matteo dietro il banco da lavoro, Beppe al laboratorio del panificio.
- Lidia dietro il bancone; Piero seduto al tavolino con il giornale.
- Marisa dietro il banco degli alimentari, Gino in piazza.
- Teresa sulla panchina della piazza vicina al bar, borsa in grembo; Nino nel castagneto.
- Wanda ed Elena hanno prefab pronti ma non sono collocate a San Rocco:
  abitano a Chivasso. Giorgio e' il prefab del protagonista (`Id: player`),
  non un secondo abitante da incontrare nella mappa.

Le identita' sono quelle lette dal registro `Bootstrap.RegistraOggettiInScena`.
Nella modalita giocabile Bootstrap registra i modelli senza rigenerarli.
Gli NPC sono raccolti in
`03_Abitanti`, spostabili indipendentemente; spostare un edificio non sposta
automaticamente gli abitanti che contiene.

Le case non espongono cognomi sulle facciate. Le librerie e gli armadi
appoggiano alla parete cieca posteriore; divisori separano cucina e letto.
Le finestre laterali mantengono spazio libero davanti. Sedie del tavolo
orientate verso il piano; `posto_tavolo` e' il riferimento per la posa seduta.

## Controllo visivo

`Verifica rifinitura` controlla soffitti e intradossi, materiali dei testi
con depth test, rosone, creste e assenza di lampioni dentro gli edifici.
`Verifica scritte e cava` controlla orientamenti e accesso laterale.
`Verifica abitanti e case` controlla 12 ID univoci, pose, collisioni del
torace, librerie aderenti e arredi che invadono lo spazio delle finestre.
Ultima generazione: controlli superati, 2639 campioni sui percorsi e 20
ingressi collegati. Render ispezionati e avvio in Play verificato; resta
necessario un playtest manuale completo. Le finestre decorative frontali
non sono aperture attraversabili. La validazione verifica anche il corridoio
centrale degli interni; non certifica ogni spazio fra tutti gli arredi.
I depositi hanno controlli aggiuntivi dei corridoi completi e degli accessi
alle venti unita' con serrande temporaneamente disabilitate durante il test.
Il confine verifica 680 punti sulla cresta esterna, con pendenza maggiore
di 55 gradi, prima del bordo della mesh. L'apertura narrativa della serranda
B-17 non e' stata esercitata nella scena di visita libera.
Nel test Play l'editor ha segnalato `Graphics Ring Buffer space` durante
il rendering della Scene View. La scena si avvia e viene renderizzata, ma
resta da profilare e ottimizzare il numero di draw call; non e' una verifica
di prestazioni conclusa.

La piazza ha raggio 18 metri, pavimentazione ad anelli e quattro panchine
rivolte al centro. Strade con leggere ondulazioni, bordi irregolari e
rappezzi; terreno con colori interpolati secondo quota, bosco e prossimita'
agli edifici. Cassette, legna, bidoni e chiazze naturali completano il paese.

Le insegne delle attivita' sono tavole con cornici, chiodi e lettere
geometriche dipinte, illuminate dalla scena. Sui depositi ci sono soltanto
le lettere identificative A e B; i codici dei locali sono su targhette interne.
Il terreno prosegue oltre il paese con una fascia continua di colline.
Quattro collider invisibili chiudono l'intero perimetro giocabile: x da
-160 a 160, z da -111 a 250. Sono sovrapposti agli angoli ed estesi da
y=-100 a y=300, senza renderer. La fascia panoramica resta visibile oltre
il limite, ma non raggiungibile. Il controllo campiona ogni lato a tre quote.
Il paesaggio prosegue con
creste ripide. Il tracciato verso la montagna e' una ferrovia dismessa:
massicciata, traversine e rotaie curve arrivano a una galleria ferroviaria.
Una sbarra bianca e rossa e il cartello FERROVIA - ACCESSO VIETATO segnano
il limite all'inizio del percorso, lato villaggio. Il collider invisibile
in quel punto impedisce il passaggio, prima dei binari e della galleria;
la galleria non e' una transizione verso un'altra scena. Il lato paese
del tronco ha un respingente. Il viaggio a Chivasso resta in corriera.

## Chivasso e viaggio

`Assets/Scenes/Chivasso1987.unity` contiene un quartiere interpretato della
storia, non una ricostruzione topografica: stazione, caffe', cartoleria,
case arredate e casa di Wanda al pianterreno di Via Sant'Orsola 14.
Wanda e' seduta, Elena le e' rivolta; la camera ha due letti singoli.
Gli edifici e i mezzi sono prefab separati, spostabili.

Nella visita libera avvicinarsi alla corriera e premere E per partire.
Escape libera o riprende il mouse; a cursore libero e' disponibile anche
il pulsante di viaggio. La corriera collega le due scene in entrambi i
sensi, con una breve partenza e il passaggio narrativo di due ore.
La storia conserva la partenza delle 14:00: qui non e' simulato un orario.
Il regionale a Chivasso e' scenografico. I binari hanno respingenti;
lo scalo di San Rocco resta dismesso, coerente con la storia.

Il test `Verifica viaggio andata e ritorno` esegue il cambio delle scene
in Play e controlla visitatore e camera unici. `Registra sopralluogo delle
due scene` produce sequenze camera in `artifacts/sopralluogo`, comprese
viste interne, aeree e percorsi esterni. Non sostituisce un playtest
completo delle collisioni del giocatore.

Nella modalita giocabile il viaggio conserva in memoria la stessa sessione:
dialoghi, dichiarazioni, inventario e porte aperte, e aggiunge 120 minuti.
Non e' un salvataggio su disco: uscire da Play avvia poi una nuova partita.
Non sono aggiunte nuove routine degli NPC o una sequenza scenica dedicata
al primo incontro sulla soglia di Wanda. Le schede restano quelle originali.

## Passaggio a Blender

Conservare gli ingombri approvati e i pivot. Sostituire progressivamente le
geometrie provvisorie dentro i prefab, mantenendo scala in metri, aperture
e collider. Gli arredi attuali possono essere rifiniti o sostituiti;
rig dei personaggi e interazioni restano da fare.
