import fs from 'node:fs';
import assert from 'node:assert/strict';
const root='Unity/Assets/';
for(const name of ['SanRocco1987','Chivasso1987']) {
  const scene=fs.readFileSync(`${root}Scenes/${name}.unity`,'utf8');
  for(const script of ['Bootstrap','Giocatore','Pannello','Menu']) {
    const guid=fs.readFileSync(`${root}Scripts/${script}.cs.meta`,'utf8').match(/guid: (\w+)/)[1];
    assert.ok(scene.includes(`guid: ${guid},`),`${name}: manca ${script}`);
  }
  assert.ok(scene.includes('mappaAMano: 1'),`${name}: mappa non manuale`);
}
const scene=fs.readFileSync(`${root}Scenes/SanRocco1987.unity`,'utf8');
assert.ok(scene.includes('m_Name: panchina_teresa_piazza'),'Manca la panchina assegnata a Teresa');
console.log('Scene narrative con giocatore, interfaccia e panchina di Teresa verificate.');
