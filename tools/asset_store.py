#!/usr/bin/env python3
"""Importa i pacchetti dell'Asset Store senza aprire l'editor.

Un `.unitypackage` e' un tar.gz in cui ogni cartella ha per nome un GUID e
contiene tre file: `asset` (il file vero), `asset.meta` (che porta il GUID) e
`pathname` (dove va). Ricostruire l'albero e' esattamente quello che fa il
bottone *Import*, e farlo da qui vuol dire che il paese si rimonta da zero
senza che nessuno debba ricordarsi quali caselle aveva spuntato.

Le mesh comprate NON stanno in git: la licenza dell'Asset Store non permette di
ridistribuirle, e centoquaranta mega di binari nella storia non si tolgono piu'.
Quello che sta in git e' questa ricetta.

    python3 tools/asset_store.py

Serve aver gia' scaricato i tre pacchetti da Package Manager -> My Assets.
"""
import os
import pathlib
import shutil
import sys
import tarfile

CACHE = pathlib.Path.home() / "Library/Unity/Asset Store-5.x"
PROGETTO = pathlib.Path(__file__).resolve().parent.parent / "Unity"

PACCHETTI = [
    ("RPG Poly Pack - Lite",
     "Gigel3d/3D ModelsEnvironmentsLandscapes/RPG Poly Pack - Lite.unitypackage"),
    ("Nature Free",
     "Polytope Studio/3D ModelsEnvironments/"
     "Low Poly Environment - Nature Free - LOWPOLY MEDIEVAL FANTASY SERIES.unitypackage"),
    ("Church 3D",
     "AndreiCG/3D ModelsEnvironmentsFantasy/Church 3D.unitypackage"),
]

# `Resources.Load` legge SOLO da una cartella che si chiama Resources, quindi i
# prefab che il codice nomina vanno spostati li'. Mesh, materiali e texture
# restano dove sono: un prefab li riferisce per GUID, non per percorso. Copiare
# invece di spostare romperebbe tutto — due file con lo stesso GUID.
GRUPPI = {
    "natura": [
        "PT_Fruit_Tree_01_green", "PT_Fruit_Tree_01_apples", "PT_Fruit_Tree_01_dead",
        "PT_Fruit_Tree_01_plums", "PT_Fruit_Tree_01_stump", "PT_Pine_Tree_03_green",
        "PT_Pine_Tree_03_dead", "PT_Pine_Tree_03_logs", "PT_Generic_Shrub_01_green",
        "PT_Generic_Shrub_01_dead", "PT_High_Grass_02_v1", "PT_Grass_02", "PT_Grass_02_v1",
        "PT_Poppy_02", "PT_Generic_Rock_01", "PT_Menhir_Rock_02", "PT_River_Rock_Pile_02",
    ],
    "arredo": [
        "rpgpp_lt_barrel_01", "rpgpp_lt_barrel_02", "rpgpp_lt_crate_01", "rpgpp_lt_crate_02",
        "rpgpp_lt_crate_03", "rpgpp_lt_sack_01", "rpgpp_lt_sack_02", "rpgpp_lt_sack_02_set",
        "rpgpp_lt_basket_01", "rpgpp_lt_basket_02", "rpgpp_lt_bench_wood_01",
        "rpgpp_lt_bench_wood_02", "rpgpp_lt_box_wood_01", "rpgpp_lt_log_wood_01",
        "rpgpp_lt_log_wood_02a", "rpgpp_lt_bucket_01", "rpgpp_lt_vase_01", "rpgpp_lt_vase_02",
        "rpgpp_lt_jug_01", "rpgpp_lt_trough_01", "rpgpp_lt_ladder_01", "rpgpp_lt_package_01",
        "rpgpp_lt_hanger_clothes_01", "rpgpp_lt_rake_01", "rpgpp_lt_broom_01",
        "rpgpp_lt_stones_01", "rpgpp_lt_flower_01", "rpgpp_lt_flower_02",
    ],
    # La montagna e' un muro di cubi grigi finche' non ci si appoggia sopra dei
    # sassi: sono novecentosessantuno celle, ed e' la prima cosa che il giocatore
    # vede alzando gli occhi.
    "roccia": [
        "rpgpp_lt_rock_01", "rpgpp_lt_rock_02", "rpgpp_lt_rock_03",
        "rpgpp_lt_rock_small_01", "rpgpp_lt_rock_small_02", "rpgpp_lt_rocks_tiny_01",
        "rpgpp_lt_hill_small_01", "rpgpp_lt_hill_small_02", "rpgpp_lt_mountain_01",
    ],
    # Gli steccati separano l'orto dalla strada. Sono la cosa che fa leggere una
    # fila di case come un paese invece che come edifici messi vicini.
    "steccato": [
        "rpgpp_lt_fence_wood_01a", "rpgpp_lt_fence_wood_01b",
        "rpgpp_lt_fence_wood_01_corner_a", "rpgpp_lt_fence_wood_02a",
        "rpgpp_lt_fence_wood_02b", "rpgpp_lt_fence_wood_02c",
    ],
    "prato": [
        "rpgpp_lt_grass_small_01a", "rpgpp_lt_grass_small_01b", "rpgpp_lt_bush_01",
        "rpgpp_lt_bush_02", "rpgpp_lt_flower_03", "rpgpp_lt_plant_01", "rpgpp_lt_plant_02",
        "rpgpp_lt_terrain_grass_01", "rpgpp_lt_terrain_grass_02",
    ],
    "cielo": ["rpgpp_lt_sky_01", "rpgpp_lt_cloud_01", "rpgpp_lt_cloud_02"],
    "insegne": [
        "rpgpp_lt_well_01", "rpgpp_lt_wagon_01", "rpgpp_lt_awning_standing_01a",
        "rpgpp_lt_awning_standing_01b", "rpgpp_lt_banner_01a", "rpgpp_lt_banner_01b",
        "rpgpp_lt_shed_wood_01", "rpgpp_lt_bird_house_01",
    ],
}


def gia_in_resources():
    """I prefab gia' spostati sotto Resources non vanno ri-estratti.

    Rimetterli al loro posto originale creerebbe due file con lo stesso GUID, che
    per Unity non e' un doppione: e' un progetto rotto.
    """
    cartella = PROGETTO / "Assets" / "Resources" / "paese"
    return {p.name for p in cartella.rglob("*.prefab")} if cartella.exists() else set()


def estrai(nome, relativo, spostati):
    sorgente = CACHE / relativo
    if not sorgente.exists():
        print(f"  {nome}: non scaricato — Package Manager -> My Assets -> Download")
        return False
    with tarfile.open(sorgente, "r:gz") as pacco:
        dentro = {}
        for membro in pacco.getmembers():
            if "/" not in membro.name:
                continue
            guid, _, tipo = membro.name.partition("/")
            dentro.setdefault(guid, {})[tipo] = membro

        scritti = saltati = 0
        for parti in dentro.values():
            if "pathname" not in parti:
                continue
            rel = pacco.extractfile(parti["pathname"]).read().decode("utf8", "replace")
            rel = rel.split("\n")[0].strip()
            # Il progetto e' su built-in. Le varianti URP/HDRP sono pacchetti
            # annidati, e importarle e' l'unico modo di far diventare magenta il
            # verde.
            if "/URP/" in rel or "/HDRP/" in rel or rel.rsplit("/", 1)[-1] in spostati:
                saltati += 1
                continue
            fuori = PROGETTO / rel
            if "asset" not in parti:
                fuori.mkdir(parents=True, exist_ok=True)
            elif not fuori.exists():
                fuori.parent.mkdir(parents=True, exist_ok=True)
                fuori.write_bytes(pacco.extractfile(parti["asset"]).read())
                scritti += 1
            meta = pathlib.Path(str(fuori) + ".meta")
            if "asset.meta" in parti and not meta.exists():
                meta.write_bytes(pacco.extractfile(parti["asset.meta"]).read())
        print(f"  {nome}: {scritti} file, {saltati} gia' a posto o URP")
    return True


def in_resources():
    assets = PROGETTO / "Assets"
    trovati = {p.stem: p for p in assets.rglob("*.prefab")}
    for gruppo, nomi in GRUPPI.items():
        fuori = assets / "Resources" / "paese" / gruppo
        fuori.mkdir(parents=True, exist_ok=True)
        spostati, mancanti = 0, []
        for nome in nomi:
            if (fuori / f"{nome}.prefab").exists():
                spostati += 1
                continue
            sorgente = trovati.get(nome)
            if sorgente is None or not sorgente.exists():
                mancanti.append(nome)
                continue
            shutil.move(str(sorgente), str(fuori / sorgente.name))
            meta = pathlib.Path(str(sorgente) + ".meta")
            if meta.exists():
                shutil.move(str(meta), str(fuori / f"{sorgente.name}.meta"))
            spostati += 1
        coda = f"  MANCANTI: {', '.join(mancanti)}" if mancanti else ""
        print(f"  {gruppo}: {spostati}/{len(nomi)}{coda}")


if __name__ == "__main__":
    if not CACHE.exists():
        sys.exit(f"cache dell'Asset Store non trovata in {CACHE}")
    print("estraggo:")
    spostati = gia_in_resources()
    tutti = all([estrai(nome, rel, spostati) for nome, rel in PACCHETTI])
    print("in Resources/paese:")
    in_resources()
    print("\nfatto. Apri Unity/ e lascia che reimporti." if tutti
          else "\nqualche pacchetto manca: scaricalo e rilancia.")
