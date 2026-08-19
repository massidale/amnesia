# La scena di prova

Non e' il gioco: e' il minimo che serve a rispondere all'unica domanda da cui
questo progetto dipende — **una conversazione con un modello regge come momento
di gioco?** Ci si risponde con due primitive e nessun asset, e conviene
rispondersi prima di modellare un paese intero.

## Come si avvia

1. Apri il progetto (`Unity/`) con Unity 6000.5.9f1
2. Menu **Amnesia → Costruisci la scena di prova**
3. Play

WASD per camminare, tasto destro del mouse per girarsi, **E** vicino a qualcuno
per parlargli, **Esc** per andartene.

## Cosa serve prima

Un file `.env` nella radice del progetto — accanto a `Amnesia.slnx`, **non**
dentro `Unity/` — con dentro:

```
OPENROUTER_KEY=...
```

E' gitignorato. Se manca, il pannello lo dice all'avvio invece di fallire al
primo turno.

## Dopo ogni modifica al dominio

```
./sync-domain.sh
```

Ricompila il dominio, copia la DLL in `Assets/Plugins/` e il contenuto in
`Assets/StreamingAssets/`. Non e' automatico apposta: una copia che parte da
sola nasconde quale versione stai giocando.

## Perche' e' tutto costruito in codice

La scena salvata contiene tre oggetti vuoti. Il paese, le figure, le luci e
l'interfaccia nascono all'avvio leggendo `content/`.

Finche' il look non e' deciso, una scena e' una cosa che si rompe in silenzio
quando cambia un prefab — e il paese qui cambia ogni volta che si scrive
contenuto. Quando ci sara' una direzione artistica vera, questa scena si butta:
ha gia' fatto il suo mestiere.

## Cosa vedrai a schermo

Cubi per i muri, capsule per le persone, e sotto la riga di dialogo una coda
tecnica: l'ora, cosa il motore ha **registrato** in quel turno, e cosa hai
provato a mostrare senza averlo.

Quella coda e' impalcatura. Nel gioco vero il giocatore vede il taccuino, non
il nome interno di una dichiarazione — ma adesso serve a vedere la meccanica
lavorare mentre si parla.
