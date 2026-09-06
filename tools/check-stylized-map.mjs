import fs from 'node:fs';
import assert from 'node:assert/strict';
for(const scene of ['SanRocco1987','Chivasso1987']) {
  const path=`Unity/Assets/SanRocco1987/Previews/mappa_${scene}.png`;
  assert.ok(fs.existsSync(path),`Mappa stilizzata mancante: ${scene}`);
  const guid=fs.readFileSync(path+'.meta','utf8').match(/guid: (\w+)/)[1];
  assert.ok(fs.readFileSync(`Unity/Assets/Scenes/${scene}.unity`,'utf8').includes(`guid: ${guid},`),'Scena ancora collegata alla fotografia');
}
const menu=fs.readFileSync('Unity/Assets/Scripts/Menu.cs','utf8');
assert.ok(menu.includes('EtichettaMappa'),'Etichette senza hover');
assert.ok(menu.includes('Stile.Macchina, 10,'),'Dimensione etichette non ridotta');
console.log('Mappe stilizzate collegate alle scene, nomi compatti e hover presenti.');
