import fs from 'node:fs';
import assert from 'node:assert/strict';
const root = new URL('../Unity/Assets/SanRocco1987/', import.meta.url);
const layout = JSON.parse(fs.readFileSync(new URL('layout.json',root),'utf8'));
for (const site of layout.sites.filter(s=>s.kind==='house')) {
  const prefab=fs.readFileSync(new URL(`Buildings/${site.id}.prefab`,root),'utf8');
  assert.ok(!prefab.includes('m_Name: insegna\n'), `${site.id}: insegna domestica non realistica`);
  assert.ok(prefab.includes('m_Name: libreria_parete\n'), `${site.id}: libreria assente`);
}
for (const id of ['rosa','matteo','anna','laura','don_carlo','nino','teresa','piero','marisa','beppe','lidia','gino','wanda','elena']) {
  const prefab=fs.readFileSync(new URL(`Characters/${id}.prefab`,root),'utf8');
  assert.ok(prefab.includes(`Id: ${id}\n`), `Identita non collegata: ${id}`);
}
const scene=fs.readFileSync(new URL('../Scenes/SanRocco1987.unity',root),'utf8');
assert.ok(scene.includes('m_Name: 03_Abitanti\n'), 'Abitanti non collocati');
console.log('Case prive di insegne, librerie e 14 identita NPC verificate; popolazione presente.');
