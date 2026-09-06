import fs from 'node:fs';
import assert from 'node:assert/strict';
const path = 'Unity/Assets/SanRocco1987/layout.json';
assert.ok(fs.existsSync(path), 'La nuova mappa deve avere un layout persistente');
const {sites} = JSON.parse(fs.readFileSync(path, 'utf8'));
const byId = Object.fromEntries(sites.map(s => [s.id, s]));
assert.equal(byId.chiesa.x, 0, 'Chiesa sul lato a monte della piazza');
for (const id of ['panetteria','negozio']) {
  assert.ok(byId[id].x < -15, `${id} sul lato ovest`);
  assert.equal(byId[id].yaw, -90, `${id} deve affacciarsi sulla piazza`);
}
assert.ok(byId.bar.x > 15, 'Bar sul lato est');
assert.equal(byId.bar.yaw, 90, 'Bar rivolto alla piazza');
for(const id of ['casa_lipari','casa_ferro','casa_piero','casa_ravera','casa_nino']) assert.ok(Math.abs(byId[id].yaw+90)<=10, `${id}: ingresso verso la viabilita del villaggio`);
for(const id of ['scuola','cooperativa','casa_peirano','deposito_a','deposito_b']) assert.equal(byId[id].yaw,90, `${id}: ingresso sul lato del paese`);
for(const id of ['stazione','casa_chiapello']) assert.equal(byId[id].yaw,180, `${id}: ingresso verso monte`);
const extent = s => {
  const yaw = (s.yaw || 0) * Math.PI / 180;
  const c = Math.abs(Math.cos(yaw)), n = Math.abs(Math.sin(yaw));
  return [c*s.w+n*s.d, n*s.w+c*s.d];
};
assert.equal(Object.keys(byId).length, sites.length, 'ID duplicati');
for (const id of ['casa_lipari','bottega','casa_valli','casa_ferro','bar','panetteria','negozio','chiesa','canonica','scuola','cooperativa','stazione','deposito_a','deposito_b','casa_nino','casa_teresa','casa_piero']) assert.ok(byId[id], `Manca ${id}`);
for (let i=0;i<sites.length;i++) for (let j=i+1;j<sites.length;j++) {
  const a=sites[i],b=sites[j];
  const [aw,ad]=extent(a),[bw,bd]=extent(b);
  assert.ok(Math.abs(a.x-b.x)>(aw+bw)/2+2 || Math.abs(a.z-b.z)>(ad+bd)/2+2, `Edifici sovrapposti: ${a.id}, ${b.id}`);
}
assert.ok(byId.bottega.z > byId.casa_lipari.z+70);
assert.ok(Math.hypot(byId.stazione.x-byId.deposito_a.x,byId.stazione.z-byId.deposito_a.z)<55);
assert.ok(byId.bottega.z>50 && byId.bottega.z<85, 'Bottega sotto il castagneto');
console.log(`Layout verificato: ${sites.length} luoghi, nessuna sovrapposizione, collegamenti narrativi validi.`);
