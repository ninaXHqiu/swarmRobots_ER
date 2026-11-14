// Checks the Physio core (heart rate from PPG, skin conductance, markers, CSV) with Node, no browser or sensor needed.
//   node tools/test-physio.mjs
import { createRequire } from 'node:module';
const require = createRequire(import.meta.url);
const { Physio, HeartRateEstimator, SimulatedSource, EdaFilter } = require('../Assets/StreamingAssets/physio/physio.js');
if (Physio._channel) { Physio._channel.close(); Physio._channel = null; }

let failed = 0;
const check = (name, ok, detail) => { console.log((ok ? 'ok   ' : 'FAIL ') + name + (detail ? '  ' + detail : '')); if (!ok) { failed++; } };

// a seeded random source so the test gives the same answer every time
function rng(seed) { let s = seed >>> 0; return () => { s = (s * 1664525 + 1013904223) >>> 0; return s / 4294967296; }; }

function runHr(bpm, invert, seconds, fs) {
  const est = new HeartRateEstimator();
  const out = [];
  const sim = new SimulatedSource((s) => {}, { fs: fs, bpm: bpm, rand: rng(7), invert: invert });
  let t = 0; sim.push = (s) => { const r = est.push(s.ppg, t, 1 / fs); out.push(r); };
  for (let i = 0; i < seconds * fs; i++) { t += 1 / fs; sim.step(); }
  const last = out.slice(-fs * 5).map((r) => r.bpm).filter((b) => b !== null);
  return { bpm: last.length ? last.reduce((a, b) => a + b, 0) / last.length : null, beats: est.beats };
}

for (const bpm of [55, 72, 90, 120, 150]) {
  const r = runHr(bpm, false, 40, 64);
  check('heart rate ' + bpm + ' bpm', r.bpm !== null && Math.abs(r.bpm - bpm) <= 3, 'estimated ' + (r.bpm && r.bpm.toFixed(1)) + ', beats ' + r.beats);
}
for (const bpm of [60, 100]) {
  const r = runHr(bpm, true, 40, 64);
  check('heart rate ' + bpm + ' bpm, waveform upside down', r.bpm !== null && Math.abs(r.bpm - bpm) <= 3, 'estimated ' + (r.bpm && r.bpm.toFixed(1)));
}
{
  const r = runHr(72, false, 40, 128);
  check('heart rate 72 bpm at 128 Hz sampling', r.bpm !== null && Math.abs(r.bpm - 72) <= 3, 'estimated ' + (r.bpm && r.bpm.toFixed(1)));
}
{   // a flat signal gives no heart rate
  const est = new HeartRateEstimator(); let last = null; let t = 0;
  for (let i = 0; i < 64 * 20; i++) { t += 1 / 64; last = est.push(2000 + 0.01 * Math.sin(i), t, 1 / 64); }
  check('no heart rate from a flat signal', last.bpm === null);
}
{   // the heart rate follows a change
  const est = new HeartRateEstimator(); const sim = new SimulatedSource(() => {}, { fs: 64, bpm: 70, rand: rng(3) });
  let t = 0, last = null; sim.push = (s) => { last = est.push(s.ppg, t, 1 / 64); };
  for (let i = 0; i < 64 * 30; i++) { t += 1 / 64; sim.step(); }
  sim.setBpm(100);
  for (let i = 0; i < 64 * 30; i++) { t += 1 / 64; sim.step(); }
  check('heart rate follows a change from 70 to 100', last.bpm !== null && Math.abs(last.bpm - 100) <= 4, 'estimated ' + (last.bpm && last.bpm.toFixed(1)));
}
{   // skin conductance: smooth, slow level, difference
  const f = new EdaFilter(); let o;
  for (let i = 0; i < 64 * 60; i++) { o = f.push(4, 1 / 64); }
  const base = o.phasic;
  for (let i = 0; i < 64 * 2; i++) { o = f.push(6, 1 / 64); }
  check('skin conductance: a jump shows in the fast part', o.phasic - base > 0.5, 'phasic ' + o.phasic.toFixed(2));
}
{   // recording, markers, CSV
  Physio.rows = []; Physio.source = 'simulated';
  Physio.startRecording();
  Physio.beginRoutine('C99', 'Test routine');
  Physio._push({ ppg: 2000, eda: 4.5 });
  Physio.sectionStart('ring, "quoted"');
  Physio.endRoutine();
  Physio.stopRecording();
  const lines = Physio.csv().trim().split('\n');
  check('csv has a header and rows', lines.length >= 6 && lines[0].startsWith('type,epoch_ms,t_s,source,routine,section'), lines.length + ' lines');
  check('csv keeps the routine label', lines.some((l) => l.includes('C99')));
  check('csv escapes quotes', lines.some((l) => l.includes('"section: ring, ""quoted"""')));
  check('csv has a sample row with a value', lines.some((l) => l.startsWith('sample,') && l.includes(',4.5,')));
}
process.exit(failed ? 1 : 0);
