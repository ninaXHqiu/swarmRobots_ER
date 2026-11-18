/* Swarm study: the four final routines (2:00, 8 parts of about 15 s) and a toio-like motion model.
   Mat coordinates as in the Unity project: x to the right, y down, mat 45..455,
   participant at the bottom-centre edge, line at y = 352, robot 1 = messenger.

   Motion model: a toio drives only forward or backward along its heading and turns on the
   spot. Each routine gives, for every robot and moment, a position and optionally a facing
   (deg). Without a facing the robot points where it drives (forward; backward only where the
   routine allows it). With a facing it turns toward it, at most MAX_TURN deg/s. */
(function (root) {
  'use strict';
  const N = 8, FPS = 20, TOTAL = 115, MAX_TURN = 540;
  const INTRO = 25, SHIFT = 5;   // real time = design time / 1.2 in the intro, design time - 5 after it
  const design = t => t < INTRO ? t * 30 / INTRO : t + SHIFT;
  const real = T => T < 30 ? T * INTRO / 30 : T - SHIFT;
  const P = { x: 250, y: 455 };
  const LINE_Y = 352, SP = 36, MSG = 1;
  const HAND = { x: 140, y: 455 };
  const VISIT = { x: 215, y: 426 };          // where the messenger stops, a little toward the hand
  const TALK = { x: 215, y: 394 };           // straight back from there, just in front of the line
  const UP = -90;

  /* ---------- vectors and timing ---------- */
  const V = (x, y) => ({ x, y });
  const add = (a, b) => V(a.x + b.x, a.y + b.y);
  const sub = (a, b) => V(a.x - b.x, a.y - b.y);
  const mul = (a, s) => V(a.x * s, a.y * s);
  const len = a => Math.hypot(a.x, a.y);
  const lerp = (a, b, u) => V(a.x + (b.x - a.x) * u, a.y + (b.y - a.y) * u);
  const c01 = u => Math.max(0, Math.min(1, u));
  const ease = u => { u = c01(u); return u * u * (3 - 2 * u); };
  const easeOut = u => { u = c01(u); return 1 - (1 - u) * (1 - u); };
  const lin = u => c01(u);
  const R2D = 180 / Math.PI;
  const ang = (a, b) => Math.atan2(b.y - a.y, b.x - a.x) * R2D;
  const dir = d => V(Math.cos(d / R2D), Math.sin(d / R2D));
  const on = (c, d, r) => add(c, mul(dir(d), r));
  const prog = (t, t0, t1, f) => (f || ease)((t - t0) / (t1 - t0));
  const slot = i => V(250 + (i - 3.5) * SP, LINE_Y);
  const faceP = p => ang(p, P);
  const norm = a => ((a % 360) + 360) % 360;
  function hash(i, s) { const x = Math.sin(i * 127.1 + s * 311.7) * 43758.5453; return x - Math.floor(x); }
  function wander(i, salt, t, lo, hi) {
    let x = 0, y = 0;
    for (let k = 0; k < 3; k++) {
      const fx = lo + (hi - lo) * hash(i, salt + 11.1 * k), fy = lo + (hi - lo) * hash(i, salt + 23.7 * k + 5);
      const px = 6.283 * hash(i, salt + 31.3 * k + 2), py = 6.283 * hash(i, salt + 47.9 * k + 3);
      x += Math.sin(6.283 * fx * t + px) / 2; y += Math.sin(6.283 * fy * t + py) / 2;
    }
    return V(x / 1.5, y / 1.5);
  }
  const bump = (f, c, w) => Math.abs(f - c) < w ? Math.pow(Math.cos(Math.PI * (f - c) / (2 * w)), 2) : 0;
  function breath(s) { if (s <= 0) return 0; const u = s % 10; return u < 4 ? ease(u / 4) : 1 - ease((u - 4) / 6); }

  /* ---------- assignment: least total squared travel, with optional rules ---------- */
  const PERMS = {};
  function perms(n) {
    if (PERMS[n]) return PERMS[n];
    const out = [], a = [...Array(n).keys()];
    (function rec(k) { if (k === n) { out.push(a.slice()); return; } for (let j = k; j < n; j++) { [a[k], a[j]] = [a[j], a[k]]; rec(k + 1); [a[k], a[j]] = [a[j], a[k]]; } })(0);
    return (PERMS[n] = out);
  }
  function assign(from, to, allowed) {
    let best = null, bestC = Infinity;
    for (const p of perms(from.length)) {
      let c = 0, ok = true;
      for (let i = 0; i < p.length; i++) {
        if (allowed && !allowed(i, p[i])) { ok = false; break; }
        const d = sub(from[i], to[p[i]]); c += d.x * d.x + d.y * d.y;
        if (c >= bestC) { ok = false; break; }
      }
      if (ok) { bestC = c; best = p.slice(); }
    }
    return best;
  }
  /* closest two robots come on straight simultaneous trips */
  function tripGap(from, to) {
    let g = Infinity;
    for (let u = 0; u <= 1.0001; u += 0.05) for (let a = 0; a < from.length; a++) for (let b = a + 1; b < from.length; b++)
      g = Math.min(g, len(sub(lerp(from[a], to[a], u), lerp(from[b], to[b], u))));
    return g;
  }
  const ids = [...Array(N).keys()];

  /* ---------- shapes ---------- */
  const C = V(250, 225);
  /* the heart: tip (k = 0, toward the participant), lower sides, widest points, lobe tops, cleft */
  const HEART_UV = [[0, 0.98], [0.53, 0.33], [0.86, -0.32], [0.46, -0.82], [0, -0.56], [-0.46, -0.82], [-0.86, -0.32], [-0.53, 0.33]];
  const HEART = HEART_UV.map(([u, v]) => V(C.x + 78 * u, C.y + 78 * v));
  /* the path round the heart: the landmarks with the tip and the cleft softened a little,
     so the robots round them instead of snapping */
  const HEART_SMOOTH = HEART.map((p, k) => (k === 0 || k === 4) ? lerp(p, C, 0.08) : p);
  /* a smooth closed path through the heart's landmarks: u in [0, 8), integer u = landmark */
  function heartAt(u) {
    u = ((u % 8) + 8) % 8;
    const k = Math.floor(u), f = u - k, p = q => HEART_SMOOTH[(q + 8) % 8];
    const p0 = p(k - 1), p1 = p(k), p2 = p(k + 1), p3 = p(k + 2), f2 = f * f, f3 = f2 * f;
    const cr = (a, b, c, d) => 0.5 * (2 * b + (-a + c) * f + (2 * a - 5 * b + 4 * c - d) * f2 + (-a + 3 * b - 3 * c + d) * f3);
    return V(cr(p0.x, p1.x, p2.x, p3.x), cr(p0.y, p1.y, p2.y, p3.y));
  }
  /* Turning round the heart: offset along the outline (landmark steps) from t0 to t1, ending exactly on
     `steps`. Speed declines from 1 to `endSpeed` and swells by `swell` on each beat (lub-dub); beat period
     from T0 to T1. Never stops before t1. */
  function heartTurn(t0, t1, steps, T0, T1, swell, endSpeed) {
    const n = Math.round((t1 - t0) * FPS), cum = [0];
    let ph = 0;
    for (let k = 1; k <= n; k++) {
      const u = k / n, T = T0 + (T1 - T0) * ease(u);
      ph += 1 / (T * FPS);
      const f = ph % 1, beat = bump(f, 0.1, 0.12) + 0.6 * bump(f, 0.36, 0.12);
      const base = 1 - (1 - endSpeed) * ease(u);
      cum.push(cum[k - 1] + base * (1 + swell * beat) * Math.min(1, k / (0.8 * FPS)));
    }
    const total = cum[n];
    return t => t <= t0 ? 0 : t >= t1 ? steps : steps * cum[Math.round((t - t0) * FPS)] / total;
  }
  /* the flower: a four-petal curve, petal tips at 95, between petals at 35 */
  const rose = th => on(C, th, 65 + 30 * Math.cos(4 * th / R2D));

  /* ---------- Empathic Resonance chaos ---------- */
  const INTENSITY = {
    low: { W: 160, V: 25, lo: 0.5, hi: 1.1, D: 18 },
    medium: { W: 260, V: 45, lo: 0.7, hi: 1.6, D: 26 },
    high: { W: 360, V: 65, lo: 0.9, hi: 2.0, D: 32 }
  };
  const CHAOS = [V(160, 175), V(250, 175), V(340, 175), V(205, 237), V(295, 237), V(160, 300), V(250, 300), V(340, 300)];
  /* the line breaks: one push each, at its own moment, then a second smaller one */
  function dashAt(i, t) {
    const t1 = 30.05 + 0.5 * hash(i, 1), t2 = 32 + 1.2 * hash(i, 2);
    const d = (tk, w) => { const u = t - tk; return u >= 0 && u < 0.5 ? w * Math.sin(Math.PI * u / 0.5) : 0; };
    return d(t1, 1.0) + d(t2, 0.6);
  }

  /* ---------- shared: introduction (0-30 s) ---------- */
  /* Robot 1 walks straight to the participant, looking at them the whole way; looks at them, at their
     hand, back at them; turns to the team and walks straight back to just in front of the line; talks.
     The others turn to it and answer, each in its own way and moment. Then everyone looks at the
     participant. */
  function intro(i, t) {
    const s = slot(i);
    if (i === MSG) {
      if (t < 2) return { p: s, face: faceP(s) };
      if (t < 8) return { p: lerp(s, VISIT, prog(t, 2, 8)), face: faceP(lerp(s, VISIT, prog(t, 2, 8))) };
      if (t < 9.5) return { p: VISIT, face: faceP(VISIT) };
      if (t < 11.5) return { p: VISIT, face: ang(VISIT, HAND) };           // looks at the hand
      if (t < 14.5) return { p: VISIT, face: faceP(VISIT) };               // back at them
      if (t < 15.2) return { p: VISIT, face: UP };                         // turns to the team
      if (t < 17.4) return { p: lerp(VISIT, TALK, prog(t, 15.2, 17.4)), face: null };
      if (t < 18) return { p: TALK, face: UP };
      if (t < 25) {                                                         // talks: two little shakes
        const sh = (t > 18.5 && t < 20.5) || (t > 21.5 && t < 23.5) ? 15 * Math.sin(6.283 * 3 * t) : 0;
        return { p: TALK, face: UP + sh };
      }
      if (t < 27.5) return { p: TALK, face: UP };
      return { p: TALK, face: faceP(TALK) };
    }
    if (t < 16.5) return { p: s, face: faceP(s) };
    const toM = ang(s, TALK);
    if (t < 27.5) {
      // answers: a nod, two nods, a little shake, a look at the participant and back
      const at = 21 + 4 * hash(i, 13), u = t - at, kind = i % 4;
      let p = s, f = toM;
      if (u > 0) {
        if (kind === 0 && u < 0.7) p = add(s, mul(dir(toM), 6 * Math.sin(Math.PI * u / 0.7)));
        if (kind === 1 && u < 1.4) p = add(s, mul(dir(toM), 5 * Math.abs(Math.sin(Math.PI * u / 0.7))));
        if (kind === 2 && u < 1) f = toM + 12 * Math.sin(6.283 * 3 * u);
        if (kind === 3 && u < 1.6) f = u < 0.9 ? faceP(s) : toM;
      }
      return { p, face: f, rev: true };
    }
    return { p: s, face: faceP(s) };
  }
  const START = ids.map(i => i === MSG ? TALK : slot(i));   // where everyone stands at 30 s

  /* ---------- shared: "¿Mejor?" (90-105 s) and closing (105-120 s) ---------- */
  function better(i, t, F) {
    const s = F[i], f = faceP(s);
    // everyone turns to the participant; then the messenger goes over, tilts once, waits and backs away
    if (i !== MSG || t < 92) return { p: s, face: f };
    if (t < 95.5) return { p: lerp(s, VISIT, prog(t, 92, 95.5)), face: null };
    if (t < 97.5) return { p: VISIT, face: faceP(VISIT) + 20 * Math.sin(6.283 * (t - 95.5) / 2) };
    if (t < 99.5) return { p: VISIT, face: faceP(VISIT) };
    if (t < 102.5) return { p: lerp(VISIT, s, prog(t, 99.5, 102.5)), face: null, rev: true };
    return { p: s, face: f };
  }
  function closing(i, t, F, L, delay) {
    const s = F[i], e = slot(L[i]), d0 = 105.3 + delay[i];
    if (t < d0) return { p: s, face: faceP(s) };
    if (t < d0 + 0.6) return { p: s, face: ang(s, e) };
    if (t < d0 + 7.5) return { p: lerp(s, e, prog(t, d0 + 0.6, d0 + 7.5)), face: null };   // slowly
    if (t < 114.6) return { p: e, face: null };
    return { p: e, face: faceP(e) };                                                     // everyone looks at the participant
  }

  /* ---------- Cheerleading: a new line that glides to the middle (30-40 s) ---------- */
  function lineUp(x, yTo) {
    const ln = ids.map(k => V(250 + (k - 3.5) * SP, 330));
    x.toLine = assign(START, ln);
    x.lineAt = i => ln[x.toLine[i]];
    x.lineIdx = i => x.toLine[i];
    x.midAt = i => V(x.lineAt(i).x, yTo);
  }
  function glide(i, t, x) {
    const a = START[i], b = x.lineAt(i), m = x.midAt(i);
    if (t < 30.5) return { p: a, face: ang(a, b) };
    if (t < 33.5) return { p: lerp(a, b, prog(t, 30.5, 33.5)), face: null };
    if (t < 34) return { p: b, face: UP };
    if (t < 40) return { p: lerp(b, m, prog(t, 34, 40)), face: UP };
    return null;
  }

  /* ---------- fireworks pieces ---------- */
  /* sparkle: after a burst each robot walks a tiny zigzag outward, each on its own rhythm.
     Driven like a toio: heading swings +-35 deg round `a`; returns the offset from the start. */
  const ZIG = 16;   // how far out the zigzag goes
  function zigzag(i, u, a, salt, dur) {
    const f = 1.8 + 1.4 * hash(i, salt), ph = 6.283 * hash(i, salt + 1), steps = 24;
    let p = V(0, 0);
    const uu = c01(u / dur) * dur, v = ZIG / dur / 0.93;
    for (let k = 1; k <= steps; k++) {
      const w = uu * k / steps, hd = a + 42 * Math.sin(6.283 * f * w + ph) * (1 - 0.4 * w / dur);
      p = add(p, mul(dir(hd), v * uu / steps));
    }
    return p;
  }

  /* how far (s) one robot runs behind: it slows down, then hurries to catch up */
  function lagOf(t, t0, peak) {
    if (t < t0) return 0;
    if (t < t0 + 2.2) return peak * ease((t - t0) / 2.2);
    return peak * (1 - ease((t - t0 - 2.2) / 2.2));
  }
  const LATE = 6;   // the robot that is late (not the messenger)

  /* turning on the spot with a smooth speed profile: up in `up` s, steady, down in `down` s; total `deg` */
  function spinTrap(t, t0, t1, deg, up, down) {
    const T = t1 - t0, vmax = deg / (T - up / 2 - down / 2), u = Math.max(0, Math.min(T, t - t0));
    if (u < up) return vmax * u * u / (2 * up);
    if (u < T - down) return vmax * (up / 2 + (u - up));
    const w = T - u; return deg - vmax * w * w / (2 * down);
  }

  const R = {};

  /* ===== Resonance 1: chaos → heart that races and slows → breathe ===== */
  R.res_heart = {
    title: 'Resonance 1 · corazón acelerado que respira', condition: 'Resonance', chaosEnd: 45,
    sections: [[0, '1 · Se dan cuenta, el mensajero se acerca'], [15, '2 · Habla con el equipo'], [30, '3 · Caos breve → forman el corazón'],
      [45, '4 · "Tu corazón está acelerado"'], [60, '5 · "Respira" (1 y 2)'], [75, '6 · "Respira" (3)'], [90, '7 · "¿Mejor?" (todos, luego el mensajero)'], [105, '8 · Regresan a la fila y lo miran']],
    desc: 'La fila se rompe: cada robot sale impulsado y se mueve errático unos 4 s (intensidad según el GSR). Sin dejar de temblar van a su lugar en el corazón; el corazón late rápido ("pum-pum") y se va frenando; luego respira 3 veces (4 s adentro, 6 s afuera). En la primera respiración el robot 6 se atrasa un poco y luego se apura a alcanzar a los demás.',
    prepare(I) {
      const toChaos = assign(START, CHAOS);
      const chaosAt = i => CHAOS[toChaos[i]];
      const toHeart = assign(ids.map(chaosAt), HEART, (i, j) => (i === MSG) === (j === 0));
      const ph = [0];
      for (let k = 1; k <= 15 * FPS; k++) { const t = 45 + k / FPS; const T = t < 52 ? 0.55 : 0.55 + 1.05 * ease((t - 52) / 7.5); ph.push(ph[k - 1] + 1 / (T * FPS)); }
      return { I, chaosAt, heartAt: i => HEART[toHeart[i]], ph };
    },
    mid(i, t, x) {
      if (t < 34.5) return { p: x.chaosAt(i), chaos: { noise: prog(t, 30, 30.8), dash: dashAt(i, t) } };
      if (t < 45) return { p: lerp(x.chaosAt(i), x.heartAt(i), prog(t, 34.5, 43.5)), chaos: { noise: 1 - lin((t - 34.5) / 9.5), dash: 0 } };
      const h = x.heartAt(i), out = ang(C, h);
      if (t < 60) {
        const f = x.ph[Math.min(x.ph.length - 1, Math.round((t - 45) * FPS))] % 1;
        const A = (t < 52 ? 0.09 : 0.09 - 0.05 * ease((t - 52) / 7.5)) * (1 - c01(t - 59)) * c01(t - 46);
        return { p: add(C, mul(sub(h, C), 1 + A * (bump(f, 0.08, 0.08) + 0.65 * bump(f, 0.3, 0.08)))), face: out, rev: true };
      }
      const tl = i === LATE ? t - lagOf(t, 61, 1.6) : t;   // one robot breathes a little late, then catches up
      return { p: add(C, mul(sub(h, C), 1 + 0.35 * breath(tl - 60))), face: out, rev: true };
    }
  };

  /* ===== Resonance 2: chaos → flower → round the petals → heart that beats and turns ===== */
  R.res_flower = {
    title: 'Resonance 2 · flor y corazón que late y gira', condition: 'Resonance', chaosEnd: 45,
    sections: [[0, '1 · Se dan cuenta, el mensajero se acerca'], [15, '2 · Habla con el equipo'], [30, '3 · Caos breve → se calman en una flor'],
      [45, '4 · Recorren la flor, entrando y saliendo de los pétalos'], [60, '5 · Corazón que late y gira'], [75, '6 · El corazón se calma'], [90, '7 · "¿Mejor?" (todos, luego el mensajero)'], [105, '8 · Regresan a la fila y lo miran']],
    desc: 'La fila se rompe en un caos breve (unos 4 s, intensidad según el GSR); el robot 6 se queda perdido un poco más y luego corre a su lugar. Forman una flor de cuatro pétalos y la recorren despacio, entrando y saliendo de los pétalos. La flor se vuelve corazón, que gira sin parar sobre su contorno y acelera un poquito en cada latido ("pum-pum"); el latido y el giro se van haciendo más lentos.',
    prepare(I) {
      const toChaos = assign(START, CHAOS);
      const chaosAt = i => CHAOS[toChaos[i]];
      const th0 = ids.map(k => 45 * k);
      const toRose = assign(ids.map(chaosAt), th0.map(rose));
      const thOf = i => th0[toRose[i]];
      const roseEnd = ids.map(i => rose(thOf(i) + 180));
      const toHeart = assign(roseEnd, HEART);
      const m = toHeart[MSG]; let steps = (8 - m) % 8; if (steps < 4) steps += 8;   // the messenger ends on the tip
      // the heart turns all the time; its speed swells on every beat ("pum-pum") and the beat slows
      const ofs = heartTurn(63.6, 90, steps, 0.85, 2.2, 0.6, 0.15);
      return { I, chaosAt, thOf, roseEnd, heartIdx: i => toHeart[i], ofs };
    },
    mid(i, t, x) {
      const th = x.thOf(i);
      if (t < 34.5) return { p: x.chaosAt(i), chaos: { noise: prog(t, 30, 30.8), dash: dashAt(i, t) } };
      if (t < 45) {
        const late = i === LATE ? lagOf(t, 38.5, 2.4) : 0;     // one robot stays lost a bit longer, then hurries
        return { p: lerp(x.chaosAt(i), rose(th), prog(t - late, 34.5, 43.5)), chaos: { noise: Math.max(1 - lin((t - 34.5) / 9.5), late / 3, 0.18), dash: 0 } };
      }
      if (t < 60) return { p: rose(th + 180 * ease((t - 45) / 15)), face: null };
      const k = x.heartIdx(i), r = x.roseEnd[i];
      if (t < 60.5) return { p: r, face: ang(r, HEART[k]) };
      if (t < 63.6) return { p: lerp(r, HEART[k], prog(t, 60.5, 63.6)), face: null };
      return { p: heartAt(k + x.ofs(t)), face: null };
    }
  };

  /* ===== Cheerleading 1: line glides → here we are → heart → circle turns → spinning ===== */
  R.cl_heart = {
    title: 'Cheerleading 1 · corazón y círculo', condition: 'Cheerleading',
    sections: [[0, '1 · Se dan cuenta, el mensajero se acerca'], [15, '2 · Habla con el equipo'], [30, '3 · Fila al centro y "¡aquí estamos!"'],
      [45, '4 · Bailan en parejas; el corazón gira'], [60, '5 · El corazón se abre en un círculo que gira'], [75, '6 · Giran en su eje'], [90, '7 · "¿Mejor?" (todos, luego el mensajero)'], [105, '8 · Regresan a la fila y lo miran']],
    desc: 'Pulso de 1 por segundo. Forman una fila que se desliza al centro; miran al participante y rebotan ("¡aquí estamos!"); bailan en parejas y van torpes y sincronizados a formar un corazón que gira sobre su contorno; el corazón se abre en un círculo que gira abriéndose y cerrándose, primero en un sentido y luego en el otro (de regreso van de reversa, sin dar la vuelta); giran en su eje con energía y luego más suave, sin detenerse.',
    prepare() {
      const x = {};
      lineUp(x, 225);
      const mids = ids.map(x.midAt), toHeart = assign(mids, HEART);
      x.heartAt = i => HEART[toHeart[i]];
      x.hIdx = i => toHeart[i];
      x.hTurn = heartTurn(54.5, 63.3, 4, 1, 1, 0.35, 1);         // half a turn round the heart
      const heartEnd = i => heartAt(toHeart[i] + 4);
      // the circle: the start angle that keeps the trips apart; the turns end with the messenger at the bottom
      let best = null;
      for (let th = 0; th < 45; th += 7.5) {
        const pos = ids.map(k => on(C, 90 + th + 45 * k, 70));
        const to = assign(ids.map(heartEnd), pos);
        const gap = tripGap(ids.map(heartEnd), ids.map(i => pos[to[i]]));
        if (!best || gap > best.gap) best = { gap, th, to };
      }
      x.cA = i => 90 + best.th + 45 * best.to[i];
      let net = norm(90 - x.cA(MSG)); if (net > 180) net -= 360;
      x.turn1 = Math.max(110, net + 110); x.turn2 = x.turn1 - net;   // clockwise, then back
      return x;
    },
    mid(i, t, x) {
      const g = glide(i, t, x); if (g) return g;
      const m = x.midAt(i);
      if (t < 40.6) return { p: m, face: faceP(m) };
      if (t < 45) { const f = ((t - 40.6) / 1.3) % 1; return { p: add(m, mul(dir(faceP(m)), 9 * (f < 0.5 && t < 44.5 ? Math.sin(Math.PI * f / 0.5) : 0))), face: faceP(m), rev: true }; }   // three bounces
      const k = x.lineIdx(i), mate = ids.find(j => x.lineIdx(j) === (k ^ 1)), mm = x.midAt(mate), cc = lerp(m, mm, 0.5);
      if (t < 45.5) return { p: m, face: ang(m, mm) };
      if (t < 49.5) return { p: on(cc, ang(cc, m) + 360 * ease((t - 45.5) / 4), 18), face: null };
      if (t < 50) return { p: m, face: faceP(m) };
      const h = x.heartAt(i);
      if (t < 54.5) {
        const u = (t - 50) / 4.5, d = sub(h, m), L = len(d) || 1, n = mul(V(-d.y, d.x), 1 / L);
        const K = [[0, 0], [0.35, 0.55], [0.45, 0.5], [0.8, 1.06], [0.95, 1], [1, 1]];
        let c = 1; for (let q = 1; q < K.length; q++) if (u <= K[q][0]) { c = K[q - 1][1] + (K[q][1] - K[q - 1][1]) * ease((u - K[q - 1][0]) / (K[q][0] - K[q - 1][0])); break; }
        return { p: add(add(m, mul(d, c)), mul(n, 4 * Math.sin(6.283 * 2 * u) * (1 - u))), face: null, rev: true };
      }
      // the heart turns round its outline, a little faster on every beat of the 1 s pulse
      const hk = x.hIdx(i);
      if (t < 63.3) return { p: heartAt(hk + x.hTurn(t)), face: null };
      const a0 = x.cA(i), c0 = on(C, a0, 70), h4 = heartAt(hk + 4);
      if (t < 66) return { p: lerp(h4, c0, prog(t, 63.3, 66)), face: null };
      if (t < 75) {
        const first = t < 70.5, u = first ? (t - 66) / 4.5 : (t - 70.5) / 4.5;
        const a = first ? a0 + x.turn1 * ease(u) : a0 + x.turn1 - x.turn2 * ease(u);
        return { p: on(C, a, 70 + 24 * Math.sin(Math.PI * c01(u))), face: null, rev: true };
      }
      const a = a0 + x.turn1 - x.turn2, p = on(C, a, 70);
      if (t < 75.4) return { p, face: a };
      return { p, face: a + spinTrap(t, 75.4, 89.6, 1440, 1.2, 2) };                // four turns: speed up, steady, slow down
    }
  };

  /* ===== Cheerleading 2: line glides → book → staggered fireworks → finale ===== */
  const G5 = V(168, 172), G3 = V(345, 245), C8 = V(250, 205), ORB = 75;   // each firework turns 75 deg as a ring
  R.cl_fireworks = {
    title: 'Cheerleading 2 · libro y fuegos artificiales', condition: 'Cheerleading',
    sections: [[0, '1 · Se dan cuenta, el mensajero se acerca'], [15, '2 · Habla con el equipo'], [30, '3 · Fila al centro; se pliega en dos filas'],
      [45, '4 · Las filas se abren como libro'], [60, '5 · Fuegos artificiales, uno y luego el otro'], [75, '6 · Gran final: explota, gira y explota otra vez'], [90, '7 · "¿Mejor?" (todos, luego el mensajero)'], [105, '8 · Regresan a la fila y lo miran']],
    desc: 'Forman una fila que se desliza al centro; las dos mitades se pliegan en dos filas paralelas y se abren y cierran como las hojas de un libro, en onda. Luego dos fuegos (5 y 3 robots, en diagonal): bolita → explotan y cada robot camina un zigzag pequeñito a su propio ritmo (el chisporroteo) → giran juntos en círculo → cada uno gira en su lugar → bolita. El segundo va desfasado del primero. Gran final con los 8.',
    prepare() {
      const x = {};
      lineUp(x, 240);
      // the book: left and right halves, hinged next to the middle
      x.half = i => x.lineIdx(i) < 4 ? -1 : 1;
      x.dist = i => x.half(i) < 0 ? 3 - x.lineIdx(i) : x.lineIdx(i) - 4;            // 0 at the hinge
      x.hinge = i => V(250 + 18 * x.half(i), 240);
      // fireworks groups: the 5 leftmost of the line and the 3 rightmost
      x.g = i => x.lineIdx(i) < 5 ? 5 : 3;
      const members = g => ids.filter(i => x.g(i) === g);
      x.ballA = {}; x.ball = {}; x.toBallDelay = {};
      [5, 3].forEach(g => {
        const m = members(g), G = g === 5 ? G5 : G3, rb = g === 5 ? 26 : 19, n = m.length;
        const as = [...Array(n).keys()].map(k => -90 + 360 / n * k), ps = as.map(a => on(G, a, rb));
        const to = assign(m.map(i => V(x.lineAt(i).x, 240)), ps);
        m.forEach((i, k) => { x.ballA[i] = as[to[k]]; x.ball[i] = ps[to[k]]; });
        m.forEach(i => { x.ballWide = x.ballWide || {}; x.ballWide[i] = on(G, x.ballA[i], rb * 1.7); });
        m.slice().sort((a, b) => len(sub(x.ball[b], V(x.lineAt(b).x, 240))) - len(sub(x.ball[a], V(x.lineAt(a).x, 240)))).forEach((i, k) => { x.toBallDelay[i] = 0.2 * k; });
      });
      // finale ring: the messenger starts at the top, the half turn brings it to the bottom
      const ring = ids.map(k => on(C8, 90 + 45 * k, 44));
      x.ball755 = ids.map(i => this.burst(i, 75.5, x).p);
      x.at755 = ids.map(i => { const G = x.g(i) === 5 ? G5 : G3; return add(G, mul(sub(x.ball755[i], G), 1.6)); });
      // the messenger's spot in the ring decides how far the ring turns to bring it to the bottom
      // (clockwise up to 270 deg, or counter-clockwise for the last 45); keep the trips apart, then prefer a bigger turn
      let best = null;
      for (let jm = 1; jm < 8; jm++) {
        const to = assign(x.at755, ring, (i, j) => (i === MSG) === (j === jm));
        const gap = tripGap(x.at755, ids.map(i => ring[to[i]]));
        const net = norm(90 - (90 + 45 * jm)), turn = net <= 180 ? net : net - 360;
        const score = Math.min(gap, 34) * 10 + Math.abs(turn) / 100;
        if (!best || score > best.score) best = { score, to, turn };
      }
      x.ringA = i => 90 + 45 * best.to[i];
      x.finTurn = best.turn;
      return x;
    },
    /* one firework of 5 s from t0: ball → burst + crackle → turn together as a ring → each spins → ball */
    cycle(i, t, x, t0, centre, rot) {
      const g = x.g(i), rb = g === 5 ? 26 : 19, R = g === 5 ? 72 : 58, s = t - t0;
      const a = x.ballA[i] + rot;
      if (s < 0.5) return { p: on(centre, a, rb), face: a };
      if (s < 1.3) return { p: on(centre, a, rb + (R - rb) * easeOut((s - 0.5) / 0.8)), face: a };
      const R2 = R + ZIG;
      if (s < 2.2) return { p: add(on(centre, a, R), zigzag(i, s - 1.3, a, rot + g, 0.9)), face: null };
      if (s < 2.4) return { p: lerp(add(on(centre, a, R), zigzag(i, 0.9, a, rot + g, 0.9)), on(centre, a, R2), prog(s, 2.2, 2.4)), face: a + 90 };
      if (s < 3.5) return { p: on(centre, a + ORB * ease((s - 2.4) / 1.1), R2), face: null };
      const a2 = a + ORB;
      if (s < 4.4) return { p: on(centre, a2, R2), face: a2 + 360 * ease((s - 3.5) / 0.9) };
      return { p: on(centre, a2, R2 - (R2 - rb) * ease((s - 4.4) / 0.6)), face: a2, rev: true };
    },
    burst(i, t, x) {
      const g = x.g(i), G = g === 5 ? G5 : G3;
      const starts = g === 5 ? [63.5, 68.5] : [65.5, 70.5], rb = g === 5 ? 26 : 19;
      // while a ball waits for its turn it turns slowly, so nobody stands still
      const pre = 30 * (starts[0] - 63.45);
      if (t < starts[0]) return { p: on(G, x.ballA[i] + 30 * Math.max(0, t - 63.45), rb), face: t < 63.45 ? x.ballA[i] : null };
      if (t < starts[1]) return this.cycle(i, t, x, starts[0], G, pre);
      if (t < starts[1] + 5) return this.cycle(i, t, x, starts[1], G, pre + ORB);
      return { p: on(G, x.ballA[i] + pre + 2 * ORB + 30 * (t - starts[1] - 5), rb), face: null };
    },
    mid(i, t, x) {
      const gl = glide(i, t, x); if (gl) return gl;
      const H = x.hinge(i), d = 36 * x.dist(i), flat = x.half(i) < 0 ? 180 : 0, upA = x.half(i) < 0 ? 270 : -90;
      const lag = 0.15 * x.dist(i);
      const page = (t0, t1, a, b) => a + (b - a) * prog(t - lag, t0, t1);
      if (t < 60) {
        let A;
        if (t < 45) A = page(40, 44.6, flat, upA);                       // fold into two rows
        else if (t < 49) A = page(45, 48.5, upA, flat);                  // open like a book
        else if (t < 53) A = page(49, 52.5, flat, upA);                  // close
        else A = page(53, 59.3, upA, flat);                              // open again, slowly
        return { p: on(H, A, d), face: d === 0 ? A : null };
      }
      const g = x.g(i), fl = V(x.lineAt(i).x, 240), b = x.ball[i];
      // each goes to a roomy spot round its launch point, then they close into the ball
      const bd = x.toBallDelay[i], bw = x.ballWide[i];
      if (t < 60.2 + bd) return { p: fl, face: ang(fl, bw) };
      if (t < 62.0 + bd) return { p: lerp(fl, bw, prog(t, 60.2 + bd, 62.0 + bd)), face: null };
      if (t < 62.4 + bd) return { p: bw, face: x.ballA[i] };
      if (t < 63.45) return { p: lerp(bw, b, prog(t, 62.4 + bd, 63.45)), face: x.ballA[i], rev: true };
      if (t < 75.5) return t < 63.5 ? { p: b, face: x.ballA[i] } : this.burst(i, t, x);
      // finale: gather, burst + crackle, turn half way round together, spin, still
      const A = x.ringA(i), r0 = on(C8, A, 44), p0 = x.at755[i], G = x.g(i) === 5 ? G5 : G3;
      if (t < 76) return { p: lerp(x.ball755[i], p0, prog(t, 75.5, 76)), face: ang(G, p0) };
      if (t < 76.4) return { p: p0, face: ang(p0, r0) };
      if (t < 78.6) return { p: lerp(p0, r0, prog(t, 76.4, 78.6)), face: null };
      if (t < 79.1) return { p: r0, face: A };
      if (t < 79.9) return { p: on(C8, A, 44 + 56 * easeOut((t - 79.1) / 0.8)), face: A };
      const RF = 100 + ZIG;
      if (t < 80.9) return { p: add(on(C8, A, 100), zigzag(i, t - 79.9, A, 9, 1.0)), face: null };
      if (t < 81.1) return { p: lerp(add(on(C8, A, 100), zigzag(i, 1.0, A, 9, 1.0)), on(C8, A, RF), prog(t, 80.9, 81.1)), face: A + (x.finTurn >= 0 ? 90 : -90) };
      if (t < 84.3) return { p: on(C8, A + x.finTurn * ease((t - 81.1) / 3.2), RF), face: null };
      const A2 = A + x.finTurn;
      if (t < 84.5) return { p: on(C8, A2, RF), face: A2 };
      if (t < 85.6) return { p: on(C8, A2, RF), face: A2 + 360 * ease((t - 84.5) / 1.1) };
      // back into the ball and one more burst before "¿Mejor?"
      if (t < 86.6) return { p: on(C8, A2, RF - (RF - 44) * ease(t - 85.6)), face: A2, rev: true };
      if (t < 87) return { p: on(C8, A2, 44), face: A2 };
      if (t < 87.8) return { p: on(C8, A2, 44 + 56 * easeOut((t - 87) / 0.8)), face: A2 };
      if (t < 88.8) return { p: add(on(C8, A2, 100), zigzag(i, t - 87.8, A2, 19, 1.0)), face: null };
      return { p: add(on(C8, A2, 100), zigzag(i, 1.0, A2, 19, 1.0)), face: A2 };
    }
  };

  /* ---------- running a routine: positions → toio-like headings ---------- */
  function angDiff(a, b) { return ((((b - a) % 360) + 540) % 360) - 180; }
  function separate(pts, min) {
    for (let it = 0; it < 3; it++) for (let a = 0; a < pts.length; a++) for (let b = a + 1; b < pts.length; b++) {
      const d = sub(pts[b], pts[a]), l = len(d);
      if (l < min && l > 1e-6) { const m = mul(d, (min - l) / 2 / l); pts[a] = sub(pts[a], m); pts[b] = add(pts[b], m); }
    }
  }
  function compute(key, intensity) {
    const r = R[key], I = INTENSITY[intensity || 'medium'];
    const x = r.prepare.call(r, I);
    const F = ids.map(i => r.mid.call(r, i, 89.999, x).p);
    const L = assign(F, ids.map(slot));
    const rank = ids.slice().sort((a, b) => F[b].y - F[a].y);
    const delay = []; rank.forEach((i, k) => { delay[i] = k * 0.2; });
    const pose = (i, tr) => { const t = design(tr); return t < 30 ? intro(i, t) : t < 90 ? r.mid.call(r, i, t, x) : t < 105 ? better(i, t, F) : closing(i, t, F, L, delay); };
    const frames = [], h = ids.map(i => faceP(slot(i)));
    let prev = null, lastChaos = null;
    const n = Math.round(TOTAL * FPS);
    for (let k = 0; k <= n; k++) {
      const t = Math.min(k / FPS, TOTAL - 1e-6);
      const ps = ids.map(i => pose(i, t));
      let pts = ps.map(q => V(q.p.x, q.p.y));
      if (ps.some(q => q.chaos)) {
        // chaos: each robot drives like a toio (forward/back + turning), pulled to its spot, with noise
        pts = ps.map((q, i) => {
          const p = prev ? prev[i] : q.p, dt = 1 / FPS, c = q.chaos;
          const dist = len(sub(q.p, p));
          let e = angDiff(h[i], ang(p, q.p)), back = false;
          if (Math.abs(e) > 110 && dist < 70) { e = angDiff(h[i], ang(p, q.p) + 180); back = true; }
          const nz = c.noise;
          const w = I.W * nz * wander(i, 7.7, t, I.lo * 2, I.hi * 2.5).x + Math.max(-420, Math.min(420, 5 * e)) * Math.min(1, dist / 20);
          let v = I.V * nz * wander(i, 9.1, t, I.lo * 2, I.hi * 2.5).y + (back ? -1 : 1) * Math.min(130, 3 * dist) * Math.max(0, Math.cos(e / R2D));
          v += c.dash * I.D * 2.5;
          v = Math.max(-160, Math.min(160, v));
          h[i] += Math.max(-MAX_TURN, Math.min(MAX_TURN, w)) * dt;
          return add(p, mul(dir(h[i]), v * dt));
        });
        separate(pts, 32);
        if (prev) pts = pts.map((p, i) => { const d = sub(p, prev[i]), l = len(d), mx = 165 / FPS; return l > mx ? add(prev[i], mul(d, mx / l)) : p; });
        lastChaos = { t, pts: pts.slice() };
        frames.push(pts.map((p, i) => [Math.round(p.x * 10) / 10, Math.round(p.y * 10) / 10, Math.round(norm(h[i]) * 10) / 10]));
        prev = pts;
        continue;
      }
      if (lastChaos && t - lastChaos.t < 1.2) { const u = ease((t - lastChaos.t) / 1.2); pts = pts.map((p, i) => lerp(lastChaos.pts[i], p, u)); }
      for (let i = 0; i < N; i++) {
        let target = h[i];
        if (ps[i].face != null) target = ps[i].face;
        else if (prev) {
          const v = sub(pts[i], prev[i]);
          if (len(v) * FPS > 4) {
            const va = Math.atan2(v.y, v.x) * R2D;
            target = ps[i].rev && Math.abs(angDiff(h[i], va)) > 90 ? va + 180 : va;
          }
        }
        // smooth turning: the robot eases into its new heading instead of snapping at full speed
        // following its own path a robot turns calmly (at most 300 deg/s); asked to face somewhere or to spin,
        // it can turn faster (540 deg/s)
        const d = angDiff(h[i], target), step = (ps[i].face != null ? MAX_TURN : 300) / FPS;
        h[i] = h[i] + Math.max(-step, Math.min(step, d * 0.45));
      }
      frames.push(pts.map((p, i) => [Math.round(p.x * 10) / 10, Math.round(p.y * 10) / 10, Math.round(norm(h[i]) * 10) / 10]));
      prev = pts;
    }
    return { key, title: r.title, condition: r.condition, sections: r.sections.map(([a, b]) => [Math.round(real(a) * 10) / 10, b]), desc: r.desc, fps: FPS, frames };
  }

  /* ---------- checks ---------- */
  function check(res) {
    const f = res.frames, dt = 1 / FPS;
    let minD = Infinity, minDAt = 0, maxV = 0, maxVAt = 0, edge = Infinity, nearP = 0, side = 0, moving = 0, worstSide = 0, worstSideAt = 0;
    for (let k = 0; k < f.length; k++) {
      for (let a = 0; a < N; a++) {
        const [x, y, hd] = f[k][a];
        edge = Math.min(edge, x - 45, 455 - x, y - 45, 455 - y);
        if (a !== MSG) nearP = Math.max(nearP, y);
        for (let b = a + 1; b < N; b++) { const d = Math.hypot(f[k][b][0] - x, f[k][b][1] - y); if (d < minD) { minD = d; minDAt = k * dt; } }
        if (k > 0) {
          const vx = (x - f[k - 1][a][0]) / dt, vy = (y - f[k - 1][a][1]) / dt, sp = Math.hypot(vx, vy);
          if (sp > maxV) { maxV = sp; maxVAt = k * dt; }
          if (sp > 5) {
            moving++;
            const hr = hd / R2D, lat = Math.abs(-Math.sin(hr) * vx + Math.cos(hr) * vy);
            if (lat > 20) side++;
            if (lat > worstSide) { worstSide = lat; worstSideAt = k * dt; }
          }
        }
      }
    }
    return {
      min_distance: Math.round(minD * 10) / 10, at: Math.round(minDAt * 10) / 10,
      max_speed: Math.round(maxV), max_speed_at: Math.round(maxVAt * 10) / 10,
      closest_to_mat_edge: Math.round(edge), lowest_y_non_messenger: Math.round(nearP),
      sideways_share: moving ? Math.round(side / moving * 1000) / 10 + '%' : '0%', worst_sideways: Math.round(worstSide), worst_sideways_at: Math.round(worstSideAt * 10) / 10
    };
  }

  const API = { N, FPS, TOTAL, MSG, P, LINE_Y, ROUTINES: Object.keys(R), compute, check, INTENSITY, SHARED: [0, real(15), real(90), real(105)] };
  if (typeof module !== 'undefined' && module.exports) module.exports = API; else root.SWARM = API;
})(this);
