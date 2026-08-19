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

LARGHEZZA, ALTEZZA = 64, 56

ROCCIA, TERRENO, STRADA, PAVIMENTO, PORTA, SOTTOBOSCO, MURO, ALBERO, FERROVIA = \
    "^", ".", ",", "~", "+", '"', "#", "T", "="

# La strada e' la virgola e il terreno aperto e' il punto: e' la convenzione
# con cui il paese e' sempre stato disegnato, e la prova del quarto atto —
# quella che cerca la curva sotto la bottega — la legge cosi'.
LEGENDA = {".": True, ",": True, "~": True, "+": True, '"': True,
           "#": False, "T": False, "^": False, "=": False}

# La spina dorsale del paese, per punti: (riga, colonna di sinistra). Larga due
# caselle da cima a fondo e mai dritta per piu' di dieci righe — un paese di
# montagna segue il terreno, e una strada dritta si legge come un corridoio.
# Il gomito piu' importante e' quello sotto la bottega: da quello dipende una
# battuta di Nino e con essa un atto intero.
NODI = [(8, 34), (21, 34), (24, 40), (27, 40), (32, 27), (41, 27), (45, 33), (53, 33)]


def spina():
    righe = {}
    for (y0, x0), (y1, x1) in zip(NODI, NODI[1:]):
        for y in range(y0, y1 + 1):
            quota = (y - y0) / max(y1 - y0, 1)
            righe[y] = round(x0 + (x1 - x0) * quota)
    return righe


STRADA_X = spina()


def vuoto():
    return [[ROCCIA] * LARGHEZZA for _ in range(ALTEZZA)]


def riempi(g, x0, y0, x1, y1, simbolo):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if 0 <= x < LARGHEZZA and 0 <= y < ALTEZZA:
                g[y][x] = simbolo


def strada(g):
    """Due caselle di larghezza, piu' i raccordi dove la spina piega."""
    for y, x in sorted(STRADA_X.items()):
        riempi(g, x, y, x + 1, y, STRADA)
        prima = STRADA_X.get(y - 1)
        if prima is not None and prima != x:
            # Il gomito: senza, la strada si spezza in diagonale e non ci si passa.
            riempi(g, min(x, prima), y, max(x, prima) + 1, y, STRADA)


def stanza(g, x0, y0, x1, y1, porte):
    riempi(g, x0, y0, x1, y1, MURO)
    riempi(g, x0 + 1, y0 + 1, x1 - 1, y1 - 1, PAVIMENTO)
    for x, y in porte:
        g[y][x] = PORTA
    return {"x": x0 + 1, "y": y0 + 1, "w": x1 - x0 - 1, "h": y1 - y0 - 1}


def affacciata(g, x0, y0, x1, y1, verso, porta):
    """Una casa che guarda la strada, con l'aia davanti fino alla carreggiata.

    Nessuna casa di questo paese guarda il retro di un'altra: la porta sta sul
    muro rivolto alla via, e il battuto davanti la unisce alla carreggiata.
    """
    y = porta
    if verso == "levante":
        luogo = stanza(g, x0, y0, x1, y1, [(x1, y)])
        riempi(g, x1 + 1, y - 1, STRADA_X[y] - 1, y + 1, TERRENO)
    else:
        luogo = stanza(g, x0, y0, x1, y1, [(x0, y)])
        riempi(g, STRADA_X[y] + 2, y - 1, x0 - 1, y + 1, TERRENO)
    return luogo


def paese():
    g = vuoto()
    luoghi = {}

    # --- Pian della Soglia, in cima, e la galleria murata -----------------
    riempi(g, 3, 1, 15, 3, TERRENO)
    luoghi["cava"] = {"x": 3, "y": 1, "w": 13, "h": 3}
    riempi(g, 6, 0, 12, 0, MURO)
    luoghi["galleria"] = {"x": 6, "y": 1, "w": 7, "h": 1}

    # Tre tornanti e un traverso: alla cava non ci si arriva per sbaglio, e
    # nessuno ci passa andando da qualche altra parte.
    riempi(g, 15, 2, 16, 4, STRADA)
    riempi(g, 16, 4, 27, 5, STRADA)
    riempi(g, 26, 5, 27, 7, STRADA)
    riempi(g, 27, 7, 35, 7, STRADA)

    # --- Il fondovalle, e il bosco che lo chiude in alto ------------------
    riempi(g, 2, 8, 61, 53, TERRENO)
    riempi(g, 4, 8, 30, 16, SOTTOBOSCO)
    riempi(g, 4, 8, 30, 8, ALBERO)
    riempi(g, 4, 16, 30, 16, ALBERO)
    riempi(g, 38, 8, 58, 14, SOTTOBOSCO)
    riempi(g, 38, 8, 58, 8, ALBERO)
    for y in range(8, 17):
        g[y][4] = ALBERO
    for y in range(8, 15):
        g[y][58] = ALBERO
    # Il castagneto col nome e' quello sopra la bottega: e' li' che nel 1985
    # hanno trovato Giorgio, ed e' li' che la battuta di Nino deve cadere.
    riempi(g, 38, 9, 47, 14, SOTTOBOSCO)
    luoghi["castagneto"] = {"x": 38, "y": 9, "w": 9, "h": 5}

    strada(g)

    # --- La bottega, sopra il gomito della strada -------------------------
    luoghi["bottega"] = affacciata(g, 38, 15, 46, 21, "ponente", 18)
    g[15][42] = PORTA          # la porta di dietro, che da' sul castagneto
    luoghi["casa_valli"] = affacciata(g, 24, 15, 31, 21, "levante", 18)

    # --- Il paese alto ----------------------------------------------------
    luoghi["panetteria"] = affacciata(g, 44, 25, 52, 31, "ponente", 28)
    luoghi["casa_ferro"] = affacciata(g, 16, 26, 24, 32, "levante", 29)

    # --- La piazza: la strada si allarga, ed e' li' che il paese si parla --
    riempi(g, 24, 33, 34, 39, TERRENO)
    luoghi["piazza"] = {"x": 24, "y": 34, "w": 9, "h": 4}
    luoghi["chiesa"] = affacciata(g, 12, 33, 22, 41, "levante", 37)
    luoghi["negozio"] = affacciata(g, 36, 34, 44, 39, "ponente", 36)

    # --- Il paese basso ---------------------------------------------------
    luoghi["bar"] = affacciata(g, 36, 41, 45, 47, "ponente", 44)
    luoghi["canonica"] = affacciata(g, 13, 43, 20, 49, "levante", 46)
    luoghi["casa_lipari"] = affacciata(g, 36, 48, 44, 54, "ponente", 51)

    riempi(g, 22, 51, 32, 52, TERRENO)
    for x in (23, 26, 29):
        g[52][x] = ALBERO
    luoghi["giardino"] = {"x": 22, "y": 51, "w": 10, "h": 1}

    # --- In fondo: la ferrovia, la stazione, il deposito ------------------
    riempi(g, 2, 53, 61, 53, TERRENO)
    luoghi["stazione"] = affacciata(g, 46, 49, 54, 54, "ponente", 52)
    riempi(g, 0, 55, 63, 55, FERROVIA)

    # Il deposito sta in fondo al paese, dall'altra parte da tutto: non si vede
    # dalla piazza, non ci si passa andando da nessuna parte, e non c'e' niente
    # che ce lo indichi. Dieci saracinesche uguali, una e' la diciassette, e
    # sotto c'e' una scala.
    riempi(g, 12, 51, 21, 52, TERRENO)
    luoghi["deposito"] = stanza(g, 2, 48, 11, 54, [(11, 51)])
    luoghi["magazzino_b17"] = {"x": 3, "y": 49, "w": 4, "h": 4}
    luoghi["seminterrato"] = {"x": 8, "y": 49, "w": 2, "h": 4}

    luoghi["strada"] = {"x": 27, "y": 32, "w": 2, "h": 9}

    # La strada si ripassa alla fine: la piazza, il giardino e le aie sono
    # stesi sopra il fondovalle e le mangiavano il selciato.
    strada(g)
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
    "player": (39, 52), "rosa": (41, 53), "matteo": (42, 18), "anna": (20, 29),
    "laura": (27, 18), "don_carlo": (16, 47), "nino": (42, 11),
    "teresa": (26, 51), "piero": (40, 44), "marisa": (39, 36),
    "beppe": (47, 28), "lidia": (42, 45), "gino": (30, 36),
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

    nomi = list(luoghi)
    for i, primo in enumerate(nomi):
        a = luoghi[primo]
        for secondo in nomi[i + 1:]:
            b = luoghi[secondo]
            if (a["x"] < b["x"] + b["w"] and b["x"] < a["x"] + a["w"]
                    and a["y"] < b["y"] + b["h"] and b["y"] < a["y"] + a["h"]
                    and {primo, secondo} not in ({"deposito", "magazzino_b17"},
                                                 {"deposito", "seminterrato"},
                                                 {"strada", "piazza"},
                                                 {"cava", "galleria"})):
                problemi.append(f"{primo} e {secondo} si sovrappongono")

    # Nessuna casa vuota: ogni stanza chiusa deve avere qualcuno dentro.
    abitate = {nome for nome, r in luoghi.items()
               for chi, (x, y) in SPAWN.items()
               if r["x"] <= x < r["x"] + r["w"] and r["y"] <= y < r["y"] + r["h"]}
    for nome in ("casa_lipari", "casa_ferro", "casa_valli", "bottega", "canonica",
                 "bar", "negozio", "panetteria"):
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
