const S = require('./routines.js');
for (const key of S.ROUTINES) for (const lvl of (key.startsWith('res') ? ['low','medium','high'] : ['medium'])) {
  const t0 = Date.now(); const r = S.compute(key, lvl);
  console.log(key, lvl, (Date.now()-t0)+'ms', JSON.stringify(S.check(r)));
}
