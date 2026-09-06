import fs from 'node:fs';
import assert from 'node:assert/strict';
const root=new URL('../Unity/Assets/SanRocco1987/',import.meta.url);
const {sites}=JSON.parse(fs.readFileSync(new URL('layout.json',root),'utf8'));
assert.ok(sites.some(s=>s.id==='deposito_a'),'Manca edificio A');
assert.ok(sites.some(s=>s.id==='deposito_b'),'Manca edificio B');
assert.ok(!sites.some(s=>s.id==='magazzino_b17'),'B-17 non deve essere un edificio autonomo');
for(const id of ['deposito_a','deposito_b']) {
  const prefab=fs.readFileSync(new URL(`Buildings/${id}.prefab`,root),'utf8');
  assert.equal((prefab.match(/m_Name: unita_[AB]_/g)||[]).length,10,`${id}: attese dieci unita interne`);
  assert.ok(!prefab.includes('m_Name: insegna\n'),'Nessuna insegna commerciale sui depositi');
  if(id==='deposito_b') assert.ok(prefab.includes('Id: magazzino_b17\n'),'Identita B-17 conservata');
}
const bakery=fs.readFileSync(new URL('Buildings/panetteria.prefab',root),'utf8');
assert.ok(bakery.includes('m_Name: lettere_dipinte\n'),'Insegna fisica assente');
const scene=fs.readFileSync(new URL('../Scenes/SanRocco1987.unity',root),'utf8');
assert.ok(scene.includes('m_Name: confine_perimetrale_invisibile\n'),'Manca il confine invisibile completo');
for(const side of ['nord','sud','est','ovest'])
  assert.ok(scene.includes(`m_Name: muro_invisibile_${side}\n`),`Manca il confine ${side}`);
assert.ok(scene.includes('m_Name: orizzonte_continuo\n'),'Paesaggio esterno assente');
assert.ok(scene.includes('m_Name: galleria_uscita_valle\n'),'La strada deve finire in una galleria scenografica');
for (const name of ['ferrovia_dismessa_valle','rotaia_curva','traversina_valle','sbarramento_ferroviario','confine_galleria_invisibile','cartello_divieto_ferrovia']) {
  assert.ok(scene.includes(`m_Name: ${name}\n`),`Confine ferroviario mancante: ${name}`);
}
const blocks=scene.split(/^--- /m);
const gate=blocks.find(b=>b.includes('m_Name: sbarramento_ferroviario\n'));
const transformId=gate.match(/component: \{fileID: (\d+)\}/)[1];
const transform=blocks.find(b=>b.startsWith(`!u!4 &${transformId}\n`));
const position=transform.match(/m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}/);
assert.ok(Math.abs(Number(position[1]))<1 && Number(position[3])<-109 && Number(position[3])>-113,
  'Lo sbarramento deve essere all\'inizio del percorso, non alla galleria');
const narrative=JSON.parse(fs.readFileSync(new URL('../content/amnesia/luoghi.json',import.meta.url),'utf8'));
const bundled=JSON.parse(fs.readFileSync(new URL('StreamingAssets/amnesia/luoghi.json',new URL('../Unity/Assets/',import.meta.url)),'utf8'));
assert.deepEqual(narrative,bundled,'Luoghi distribuiti non sincronizzati');
const b17=narrative.places.magazzino_b17;
assert.equal(b17.requires_item,'chiave_b17','Chiave narrativa modificata');
assert.ok(b17.requires_declared.includes('magazzino_dove'),'Il requisito narrativo deve restare');
console.log('Depositi A/B, 20 unita, B-17 interno, insegne e orizzonte verificati.');
