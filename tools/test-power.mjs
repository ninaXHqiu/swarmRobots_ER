// Checks the robot power page logic with fake robots (no Bluetooth needed):  node tools/test-power.mjs
import { createRequire } from 'node:module';
const P = createRequire(import.meta.url)('../power/power.js');
let failed = 0;
const check = (name, ok, detail) => { console.log((ok ? 'ok   ' : 'FAIL ') + name + (detail ? '  ' + detail : '')); if (!ok) { failed++; } };
const eq = (a, b) => JSON.stringify(Array.from(a)) === JSON.stringify(Array.from(b));
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

check('100 is green', eq(P.colorFor(100), P.GREEN)); check('50 is green', eq(P.colorFor(50), P.GREEN)); check('49 is orange', eq(P.colorFor(49), P.ORANGE));
check('20 is orange', eq(P.colorFor(20), P.ORANGE)); check('19 is red', eq(P.colorFor(19), P.RED)); check('0 is red', eq(P.colorFor(0), P.RED));
check('light on bytes', eq(P.lightOn([1, 2, 3]), [3, 0, 1, 1, 1, 2, 3])); check('light off bytes', eq(P.lightOff(), [1]));

function fakeRobot(id, power) {
  const writes = []; const listeners = {}; let notify = null;
  const light = { writeValueWithoutResponse: async (b) => { writes.push(Array.from(b)); } };
  const battery = { readValue: async () => ({ getUint8: () => power }), startNotifications: async () => {}, addEventListener: (n, f) => { notify = f; } };
  const svc = { getPrimaryService: null, getCharacteristic: async (u) => (u === P.LIGHT ? light : battery) };
  const device = { id, name: 'toio-' + id, addEventListener: (n, f) => { listeners[n] = f; },
    gatt: { connect: async () => ({ getPrimaryService: async () => svc }) } };
  return { device, writes, setPower: (p) => notify({ target: { value: { getUint8: () => p } } }), drop: () => listeners.gattserverdisconnected() };
}
const fakes = [fakeRobot('a', 85), fakeRobot('b', 40), fakeRobot('c', 12)]; let next = 0;
const bluetooth = { requestDevice: async (o) => { if (!o.filters[0].services.includes(P.SERVICE)) { throw new Error('wrong filter'); } return fakes[next++ % 3].device; } };
const robots = []; const changes = []; const onChange = (r) => changes.push(r.name + ':' + r.power);
for (let i = 0; i < 3; i++) { await P.addRobot(bluetooth, robots, onChange); }
await sleep(20);
check('three robots listed', robots.length === 3 && robots.every((r) => r.connected));
check('exact power read', robots.map((r) => r.power).join() === '85,40,12', robots.map((r) => r.power).join());
check('85 shown green', eq(fakes[0].writes.at(-1), P.lightOn(P.GREEN)));
check('40 shown orange', eq(fakes[1].writes.at(-1), P.lightOn(P.ORANGE)));
check('12 shown red', eq(fakes[2].writes.at(-1), P.lightOn(P.RED)));
fakes[0].setPower(45); await sleep(20);
check('a robot whose power drops turns orange', robots[0].power === 45 && eq(fakes[0].writes.at(-1), P.lightOn(P.ORANGE)));
fakes[0].setPower(18); await sleep(20);
check('and red below 20', eq(fakes[0].writes.at(-1), P.lightOn(P.RED)));
// pressing the colour: purple flashes, then back to its level colour
fakes[1].writes.length = 0; const before0 = fakes[0].writes.length, before2 = fakes[2].writes.length;
await robots[1].flash();
const purples = fakes[1].writes.filter((w) => eq(w, P.lightOn(P.PURPLE))).length;
check('flash: three purple flashes on that robot only', purples === 3 && fakes[0].writes.length === before0 && fakes[2].writes.length === before2, 'purple writes ' + purples);
check('flash: ends back on its level colour', eq(fakes[1].writes.at(-1), P.lightOn(P.ORANGE)));
// a repeated press during a flash does not stack
fakes[2].writes.length = 0; robots[2].flash(); robots[2].flash(); await sleep(1500);
check('flash pressed twice does not double', fakes[2].writes.filter((w) => eq(w, P.lightOn(P.PURPLE))).length === 3);
// the same robot picked again is not added twice
fakes[0].device.id = 'a'; next = 0; await P.addRobot(bluetooth, robots, onChange);
check('same robot chosen again is not listed twice', robots.length === 3);
fakes[2].drop(); await sleep(5);
check('a dropped robot is marked not connected', robots[2].connected === false);
process.exit(failed ? 1 : 0);
