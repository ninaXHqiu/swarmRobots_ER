// Writes the final routines as data files for Unity (Assets/Routines/Final/Resources/FinalRoutines).
// The paths are exactly the ones the simulator page shows (routines.js), sampled at 10 frames a second.
//   node tools/final-sim/export.js
// Resonance routines are written once per skin-conductance level (low, medium, high = 1, 2, 3).
const fs = require('fs');
const path = require('path');
const S = require('./routines.js');

const OUT = path.join(__dirname, '..', '..', 'Assets', 'Routines', 'Final', 'Resources', 'FinalRoutines');
const STEP = 2;   // 20 fps in the simulator -> 10 fps in the file
fs.mkdirSync(OUT, { recursive: true });

const LEVELS = { low: 1, medium: 2, high: 3 };
for (const key of S.ROUTINES) {
  const levels = key.startsWith('res') ? Object.keys(LEVELS) : ['medium'];
  for (const lvl of levels) {
    const r = S.compute(key, lvl), c = S.check(r);
    const file = key + (key.startsWith('res') ? '_level' + LEVELS[lvl] : '') + '.txt';
    const lines = [
      '# Swarm study final routine, written by tools/final-sim/export.js from routines.js. Do not edit by hand.',
      'name ' + key + (key.startsWith('res') ? ' level ' + LEVELS[lvl] : ''),
      'robots ' + S.N,
      'fps ' + (S.FPS / STEP),
      'messenger ' + S.MSG,
      'check closest ' + c.min_distance + ' top_speed ' + c.max_speed,
    ];
    r.sections.forEach(([t, name]) => lines.push('section ' + t + ' ' + name));
    const frames = r.frames.filter((_, k) => k % STEP === 0);
    lines.push('frames ' + frames.length);
    // one line per frame: x y heading for each robot; x and y in tenths of a mat unit, heading in tenths of a degree
    for (const f of frames) lines.push(f.map(([x, y, h]) => Math.round(x * 10) + ' ' + Math.round(y * 10) + ' ' + Math.round(h * 10)).join(' '));
    fs.writeFileSync(path.join(OUT, file), lines.join('\n') + '\n');
    console.log(file, frames.length + ' frames', JSON.stringify(c));
  }
}
