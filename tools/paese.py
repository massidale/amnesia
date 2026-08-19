"""San Rocco di Valdieri, disegnato da una pianta invece che a mano.

La mappa e' un file di caratteri, e battere a mano trentadue righe da
quarantotto e' il modo migliore per ritrovarsi con una casa vuota, una porta
che non da' da nessuna parte e un paese in due pezzi. Qui i luoghi si
dichiarano, il disegno si compone, e alla fine si verifica: ogni casa ha
qualcuno dentro, ogni posto e' raggiungibile a piedi, e il deposito sta dove
non ci si arriva per caso.

Si rilancia con: python3 tools/paese.py
"""
import io
import json
import os
from collections import deque

LARGHEZZA, ALTEZZA = 48, 32

ROCCIA, TERRENO, STRADA, PAVIMENTO, PORTA, SOTTOBOSCO, MURO, ALBERO, FERROVIA = \
    "^", ".", ",", "~", "+", '"', "#", "T", "="

# La strada e' la virgola e il terreno aperto e' il punto: e' la convenzione
# con cui il paese e' sempre stato disegnato, e la prova del quarto atto —
# quella che cerca la curva sotto la bottega — la legge cosi'.
LEGENDA = {".": True, ",": True, "~": True, "+": True, '"': True,
           "#": False, "T": False, "^": False, "=": False}


def vuoto():
    return [[ROCCIA] * LARGHEZZA for _ in range(ALTEZZA)]


def riempi(g, x0, y0, x1, y1, simbolo):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if 0 <= x < LARGHEZZA and 0 <= y < ALTEZZA:
                g[y][x] = simbolo


def stanza(g, x0, y0, x1, y1, porte):
    """Quattro muri, un pavimento e le porte che le si dicono."""
    riempi(g, x0, y0, x1, y1, MURO)
    riempi(g, x0 + 1, y0 + 1, x1 - 1, y1 - 1, PAVIMENTO)
    for x, y in porte:
        g[y][x] = PORTA
    # Il rettangolo del luogo e' il *dentro*: chi ci sta, ci sta dentro.
    return {"x": x0 + 1, "y": y0 + 1, "w": x1 - x0 - 1, "h": y1 - y0 - 1}


def paese():
    g = vuoto()
    luoghi = {}

    # --- Pian della Soglia: la cava in cima, e la galleria murata ----------
    riempi(g, 3, 1, 15, 2, TERRENO)
    luoghi["cava"] = {"x": 3, "y": 1, "w": 13, "h": 2}
    # La galleria e' murata: ci si arriva davanti e non si passa. Il luogo e' lo
    # spiazzo davanti al muro — un posto che il gioco nomina e nessuno apre.
    riempi(g, 5, 0, 10, 0, MURO)
    luoghi["galleria"] = {"x": 5, "y": 1, "w": 6, "h": 1}
    # Il sentiero non sale dritto: fa un tornante sulla cengia, come tutti i
    # sentieri di montagna. Salire alla cava deve costare piu' che attraversare
    # il paese, o Pian della Soglia e' dietro l'angolo e non e' mai stato dietro
    # l'angolo per nessuno.
    riempi(g, 13, 5, 14, 11, STRADA)
    riempi(g, 13, 4, 22, 4, STRADA)
    riempi(g, 22, 2, 22, 4, STRADA)
    riempi(g, 16, 2, 22, 2, STRADA)

    # --- Il castagneto, che sta sopra la curva ----------------------------
    riempi(g, 8, 5, 30, 10, SOTTOBOSCO)
    riempi(g, 8, 5, 30, 5, ALBERO)
    riempi(g, 8, 10, 19, 10, ALBERO)
    riempi(g, 23, 10, 30, 10, ALBERO)
    for y in range(5, 11):
        g[y][8] = ALBERO
        g[y][30] = ALBERO
    riempi(g, 13, 5, 14, 10, STRADA)
    # Il rettangolo con il nome sta sopra la bottega: e' quello il castagneto di
    # cui parla Nino. Gli alberi vanno oltre, come vanno oltre i boschi.
    luoghi["castagneto"] = {"x": 17, "y": 6, "w": 8, "h": 4}

    # Il fondovalle. Le case stanno su un prato, non incastrate nella roccia:
    # la montagna chiude il paese in cima e ai lati, e non ci passa in mezzo.
    riempi(g, 2, 11, 46, 30, TERRENO)

    # --- La strada alta, la curva, la bottega ------------------------------
    riempi(g, 2, 12, 45, 12, STRADA)
    riempi(g, 20, 10, 22, 11, SOTTOBOSCO)
    # La curva: la strada piega qui, e sopra c'e' il bosco. E' la geografia da
    # cui dipende il quarto atto — chi ha camminato per il paese deve
    # riconoscere «il castagneto sopra la curva» senza che glielo spieghino.
    riempi(g, 21, 10, 21, 12, STRADA)
    luoghi["bottega"] = stanza(g, 17, 13, 24, 18, [(20, 18), (21, 13)])
    riempi(g, 20, 19, 20, 20, STRADA)

    # --- Le due case sulla strada alta ------------------------------------
    luoghi["casa_ferro"] = stanza(g, 2, 14, 8, 19, [(5, 19)])
    riempi(g, 5, 20, 5, 20, STRADA)
    # Casa Lipari e la bottega a duecento metri: e' scritto nella storia, e va
    # scritto nella mappa — meta' di quello che succede dipende da quanto e'
    # corta questa strada.
    luoghi["casa_lipari"] = stanza(g, 33, 14, 40, 19, [(36, 19)])
    riempi(g, 36, 20, 36, 20, STRADA)

    # --- La strada del paese ----------------------------------------------
    riempi(g, 2, 20, 45, 21, TERRENO)
    riempi(g, 2, 20, 45, 20, STRADA)

    # --- Il bar, la chiesa, la canonica, casa Valli ------------------------
    luoghi["casa_valli"] = stanza(g, 2, 22, 9, 27, [(5, 22)])
    riempi(g, 5, 21, 5, 21, STRADA)
    luoghi["chiesa"] = stanza(g, 11, 22, 19, 27, [(15, 22)])
    riempi(g, 15, 21, 15, 21, STRADA)
    luoghi["canonica"] = stanza(g, 21, 22, 27, 27, [(24, 22)])
    riempi(g, 24, 21, 24, 21, STRADA)
    luoghi["bar"] = stanza(g, 41, 22, 46, 27, [(43, 22)])
    riempi(g, 43, 21, 43, 21, STRADA)

    # --- La piazza e il giardino ------------------------------------------
    riempi(g, 29, 21, 39, 29, TERRENO)
    luoghi["piazza"] = {"x": 29, "y": 21, "w": 11, "h": 4}
    for x in (30, 33, 36, 39):
        g[25][x] = ALBERO
    luoghi["giardino"] = {"x": 29, "y": 26, "w": 11, "h": 4}

    # --- La stazione e, in fondo alla massicciata, il deposito -------------
    # La stradina bassa: dalla piazza dietro la chiesa, e poi in fondo.
    riempi(g, 11, 28, 29, 28, TERRENO)
    riempi(g, 11, 29, 20, 29, TERRENO)
    luoghi["stazione"] = stanza(g, 21, 29, 28, 31, [(24, 29)])
    riempi(g, 29, 30, 39, 30, TERRENO)
    riempi(g, 29, 31, 47, 31, FERROVIA)
    # Il deposito sta in fondo, dalla parte opposta a tutto il resto: non si
    # vede dalla piazza, non ci si passa andando da nessuna parte, e per
    # arrivarci bisogna sapere che c'e'. Dentro, dieci saracinesche uguali.
    luoghi["deposito"] = stanza(g, 2, 27, 10, 31, [(10, 29)])
    luoghi["magazzino_b17"] = {"x": 4, "y": 28, "w": 5, "h": 2}

    luoghi["strada"] = {"x": 2, "y": 20, "w": 44, "h": 1}
    return g, luoghi


def raggiungibili(g, partenza):
    visti = {partenza}
    coda = deque([partenza])
    while coda:
        x, y = coda.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            n = (x + dx, y + dy)
            if (n not in visti and 0 <= n[0] < LARGHEZZA and 0 <= n[1] < ALTEZZA
                    and LEGENDA[g[n[1]][n[0]]]):
                visti.add(n)
                coda.append(n)
    return visti


SPAWN = {
    "player": (35, 16), "rosa": (37, 17), "matteo": (20, 15), "anna": (5, 16),
    "laura": (5, 24), "don_carlo": (24, 24), "nino": (20, 8),
    "teresa": (33, 27), "piero": (43, 24), "marisa": (33, 22),
}


def verifica(g, luoghi):
    problemi = []
    partenza = SPAWN["player"]
    if not LEGENDA[g[partenza[1]][partenza[0]]]:
        problemi.append("il giocatore nasce dentro un muro")
        return problemi
    visti = raggiungibili(g, partenza)

    for chi, (x, y) in SPAWN.items():
        if (x, y) not in visti:
            problemi.append(f"{chi} in ({x},{y}) non e' raggiungibile a piedi")

    calpestabili = sum(1 for y in range(ALTEZZA) for x in range(LARGHEZZA) if LEGENDA[g[y][x]])
    if len(visti) != calpestabili:
        problemi.append(f"il paese e' in piu' pezzi: {len(visti)} celle su {calpestabili}")

    for nome, r in luoghi.items():
        if nome == "galleria":
            continue  # murata apposta
        celle = [(x, y) for y in range(r["y"], r["y"] + r["h"]) for x in range(r["x"], r["x"] + r["w"])]
        if not any(c in visti for c in celle):
            problemi.append(f"{nome} non si raggiunge")

    # Nessuna casa vuota: ogni stanza chiusa deve avere qualcuno dentro.
    abitate = {nome for nome, r in luoghi.items()
               for chi, (x, y) in SPAWN.items()
               if r["x"] <= x < r["x"] + r["w"] and r["y"] <= y < r["y"] + r["h"]}
    for nome in ("casa_lipari", "casa_ferro", "casa_valli", "bottega", "canonica"):
        if nome not in abitate:
            problemi.append(f"{nome} e' vuota")
    return problemi


if __name__ == "__main__":
    g, luoghi = paese()
    problemi = verifica(g, luoghi)
    for p in problemi:
        print("  ✗", p)
    if problemi:
        raise SystemExit(1)

    mappa = {
        "width": LARGHEZZA, "height": ALTEZZA, "legend": LEGENDA,
        "rows": ["".join(r) for r in g],
        "places": luoghi,
        "spawn": {chi: {"x": x, "y": y} for chi, (x, y) in SPAWN.items()},
    }
    qui = os.path.dirname(os.path.abspath(__file__))
    destino = os.path.join(qui, "..", "content", "village_map.json")
    io.open(destino, "w", encoding="utf-8").write(json.dumps(mappa, ensure_ascii=False, indent=2) + "\n")
    for r in mappa["rows"]:
        print(r)
    print("village_map.json scritto")
