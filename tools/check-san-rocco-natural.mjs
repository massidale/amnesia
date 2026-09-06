import fs from 'node:fs';
import assert from 'node:assert/strict';

const {sites} = JSON.parse(fs.readFileSync('Unity/Assets/SanRocco1987/layout.json', 'utf8'));
const source = fs.readFileSync('Unity/Assets/Editor/SanRoccoScene.cs', 'utf8');
const lipari = sites.find(s => s.id === 'casa_lipari');
const ferro = sites.find(s => s.id === 'casa_ferro');
assert.notEqual(lipari.yaw, ferro.yaw, 'Il campione deve interrompere il parallelismo delle case');
for (const s of [lipari, ferro]) {
  assert.ok(Math.abs(s.yaw + 90) <= 10, `${s.id}: porta ancora rivolta al paese`);
}
const road = source.match(/Ribbon\(p,"via_principale",new\[\]\{([^}]+)\}/)?.[1];
assert.ok(road, 'Tracciato principale esplicito e verificabile');
const points = [...road.matchAll(/P\((-?[\d.]+)f?,(-?[\d.]+)f?\)/g)].map(m => [+m[1], +m[2]]);
const approach = points.filter(p => p[1] < -18);
assert.ok(approach.some(p => p[0] < -2) && approach.some(p => p[0] > 2), 'Ingresso con andamento sinuoso');
for (let i = 1; i < approach.length; i++) {
  assert.ok(approach[i][1] > approach[i - 1][1], 'Nessun ripiegamento del percorso');
  assert.ok(Math.abs((approach[i][0] - approach[i - 1][0]) / (approach[i][1] - approach[i - 1][1])) < .5, 'Curve percorribili, senza gomiti stretti');
}
assert.ok(source.includes('MainRoadPoint(s.z)'), 'Gli accessi seguono la strada modificata');
assert.ok(source.includes('EntranceGardens();'), 'Arredo del tratto campione integrato nella generazione');
assert.ok(source.includes('NearRoute(point,4.5f)'), 'Vegetazione arretrata dai percorsi');
console.log('Campione naturale: orientamenti, curve, raccordi e margini verificati (controllo sorgenti, non playtest).');
