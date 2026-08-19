"""Le facce del paese.

Le mesh dei personaggi sono di Kenney (CC0) e vanno benissimo: testa, braccia,
gambe e UV gia' fatte. Le texture no — sono un cast da gioco per bambini, con
tre robot e nessuna donna oltre i venticinque anni. Qui si tengono le mesh e si
ridipingono i vestiti nei colori di un paese di montagna nel 1987.

Si rilancia con: python3 tools/facce.py
"""
import os
import numpy as np
from PIL import Image

QUI = os.path.dirname(os.path.abspath(__file__))
FONTE = os.path.join(QUI, "..", "Unity/Assets/Resources/kenney/persone/Textures")
DESTINO = os.path.join(QUI, "..", "Unity/Assets/Resources/kenney/persone/facce")

# La pelle non si ridipinge insieme ai vestiti: mani e collo stanno nelle stesse
# isole UV delle maniche. Si campiona dalla faccia e si esclude per colore.
GUANCIA = (150, 190, 260, 230)   # x0, y0, x1, y1 dentro l'isola della testa
CAPELLI = (150, 4, 250, 48)
SFONDO = 40                       # sotto questa luminosita' l'atlante e' vuoto

# La pelle si sposta di poco: tre donne sulla stessa mesh devono restare tre
# persone, e in un paese di montagna non ci sono due carnagioni uguali.
CAST = {
    # id          mesh  busto      gambe      capelli    pelle
    "rosa":      ("e", "#8a7f74", "#4a4442", "#6b5f55", 1.00),
    "anna":      ("e", "#33302e", "#2a2725", "#9d968d", 0.90),
    "laura":     ("e", "#3f4a52", "#33383c", "#3a3029", 1.06),
    "matteo":    ("q", "#6d5540", "#4a4238", None,      0.97),
    "don_carlo": ("j", "#232120", "#232120", None,      1.00),
    "nino":      ("a", "#4e5741", "#3d4034", None,      0.94),
}


def rgb(esadecimale):
    e = esadecimale.lstrip("#")
    return np.array([int(e[i:i + 2], 16) for i in (0, 2, 4)], dtype=float)


def tavolozza(pixel, rect, tolleranza=16):
    """I colori piatti presenti in un rettangolo: Kenney dipinge a tinte unite."""
    x0, y0, x1, y1 = rect
    campione = pixel[y0:y1, x0:x1].reshape(-1, 3)
    colori = []
    for c in np.unique(campione, axis=0):
        if c.max() < SFONDO:
            continue
        if not any(np.abs(c - altro).max() <= tolleranza for altro in colori):
            colori.append(c.astype(float))
    return colori


def appartiene(pixel, colori, tolleranza=18):
    if not colori:
        return np.zeros(pixel.shape[:2], dtype=bool)
    return np.any([np.abs(pixel.astype(float) - c).max(axis=2) <= tolleranza for c in colori], axis=0)


def tingi(pixel, maschera, colore):
    """Ricolora tenendo l'ombreggiatura: il chiaro resta chiaro."""
    if not maschera.any():
        return
    zona = pixel[maschera].astype(float)
    luce = zona.max(axis=1) / 255.0
    media = max(luce.mean(), 0.01)
    fattore = np.clip(luce / media, 0.72, 1.28)[:, None]
    pixel[maschera] = np.clip(colore[None, :] * fattore, 0, 255).astype(np.uint8)


def riquadro_della_faccia(pixel, carnagione, testa):
    """Dove sta la faccia, misurato invece che indovinato.

    Serve perche' su alcune mesh il colore dei capelli e' *lo stesso* della
    pelle in ombra — a un punto di distanza su 255. Nessuna soglia puo'
    separarli, la posizione si'.
    """
    chiare = sorted(carnagione, key=lambda c: c.max())[-2:]
    faccia = appartiene(pixel, chiare) & testa
    if not faccia.any():
        return None
    righe, colonne = np.where(faccia)
    return colonne.min(), righe.min(), colonne.max(), righe.max()


def dipingi(mesh, busto, gambe, capelli, pelle):
    immagine = Image.open(os.path.join(FONTE, f"texture-{mesh}.png")).convert("RGBA")
    dati = np.array(immagine)
    pixel = dati[:, :, :3]
    h, w = pixel.shape[:2]

    carnagione = tavolozza(pixel, GUANCIA)
    maschera_pelle = appartiene(pixel, carnagione)
    vestibile = (pixel.max(axis=2) >= SFONDO) & ~maschera_pelle

    riga = np.arange(h)[:, None].repeat(w, axis=1)
    colonna = np.arange(w)[None, :].repeat(h, axis=0)
    testa = riga < 400
    inferiore = (riga >= 780) & (colonna >= 470)

    tingi(pixel, vestibile & ~testa & ~inferiore, rgb(busto))
    tingi(pixel, vestibile & inferiore, rgb(gambe))
    if capelli is not None:
        riquadro = riquadro_della_faccia(pixel, carnagione, testa)
        chioma = testa & appartiene(pixel, tavolozza(pixel, CAPELLI))
        if riquadro is not None:
            x0, y0, x1, y1 = riquadro
            chioma &= ~((colonna >= x0) & (colonna <= x1) & (riga >= y0) & (riga <= y1))
        tingi(pixel, chioma, rgb(capelli))
    if pelle != 1.0:
        pixel[maschera_pelle] = np.clip(pixel[maschera_pelle].astype(float) * pelle, 0, 255).astype(np.uint8)

    dati[:, :, :3] = pixel
    return Image.fromarray(dati)


if __name__ == "__main__":
    os.makedirs(DESTINO, exist_ok=True)
    for chi, scheda in CAST.items():
        dipingi(*scheda).save(os.path.join(DESTINO, f"{chi}.png"))
        print(f"{chi}: mesh {scheda[0]}")
