import fs from 'node:fs';
import assert from 'node:assert/strict';

const root = new URL('../Unity/Assets/', import.meta.url);
const scene = fs.readFileSync(new URL('Scenes/SanRocco1987.unity', root), 'utf8');
assert.ok(scene.includes('m_Name: piazza_circolare\n'), 'Manca la piazza circolare');
for (const [id, objects] of Object.entries({
  panetteria: ['forno_muratura', 'pagnotta'], bar: ['banco_bar', 'macchina_espresso'],
  stazione: ['biglietteria', 'pensilina_stazione', 'orologio_stazione'],
  casa_lipari: ['letto', 'cucina_smalto'], scuola: ['lavagna'], bottega: ['morsa'],
})) {
  const prefab = fs.readFileSync(new URL(`SanRocco1987/Buildings/${id}.prefab`, root), 'utf8');
  for (const name of objects) assert.ok(prefab.includes(`m_Name: ${name}\n`), `${id}: manca ${name}`);
}
assert.ok(scene.includes('m_Name: dettagli_villaggio\n'), 'Mancano gli oggetti diffusi');
assert.ok(fs.readFileSync('Unity/Assets/SanRocco1987/Buildings/cava_pian_della_soglia.prefab','utf8').includes('m_Name: volta_raccordo'),'La stanza laterale della cava deve avere una volta');
console.log('Piazza, interni, stazione e oggetti del villaggio presenti negli asset salvati.');
