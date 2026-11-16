// The monitor: live view of Physio (heart rate, skin conductance, markers) and a viewer for recorded CSV files.
// Runs in the accordion under the Control screen (?embedded=1) or in its own tab; talks to the page that runs
// Unity over a BroadcastChannel, so both must be the same site.
(function () {
  'use strict';
  const $ = (id) => document.getElementById(id);
  if (/embedded=1/.test(location.search)) { document.body.classList.add('embedded'); }
  $('where').textContent = document.body.classList.contains('embedded') ? '' : '(its own tab)';

  // ---------------------------------------------------------------- a small line plotter
  function plot(canvas, series, opt) {
    const ctx = canvas.getContext('2d'), W = canvas.width, H = canvas.height, pad = { l: 44, r: 12, t: 8, b: 30 };
    const css = getComputedStyle(document.body);
    const fg = css.getPropertyValue('--fg'), muted = css.getPropertyValue('--muted'), accent = css.getPropertyValue('--accent');
    ctx.clearRect(0, 0, W, H); ctx.font = '11px system-ui'; ctx.lineWidth = 1;
    let x0 = opt.xmin, x1 = opt.xmax, y0 = opt.ymin, y1 = opt.ymax;
    if (y0 === undefined || y1 === undefined) {
      let lo = Infinity, hi = -Infinity;
      for (const s of series) for (const p of s.pts) { if (Number.isFinite(p[1])) { lo = Math.min(lo, p[1]); hi = Math.max(hi, p[1]); } }
      if (!Number.isFinite(lo)) { lo = 0; hi = 1; }
      const m = (hi - lo) * 0.1 || 1; y0 = lo - m; y1 = hi + m;
    }
    const X = (x) => pad.l + (x - x0) / (x1 - x0 || 1) * (W - pad.l - pad.r);
    const Y = (y) => H - pad.b - (y - y0) / (y1 - y0 || 1) * (H - pad.t - pad.b);
    // routine / section bands
    for (const b of opt.bands || []) { ctx.fillStyle = b.color; ctx.globalAlpha = 0.12; ctx.fillRect(X(b.from), pad.t, Math.max(1, X(b.to) - X(b.from)), H - pad.t - pad.b); ctx.globalAlpha = 1; }
    ctx.fillStyle = muted; ctx.strokeStyle = muted; ctx.globalAlpha = 0.4;
    for (let i = 0; i <= 4; i++) { const y = y0 + (y1 - y0) * i / 4; ctx.beginPath(); ctx.moveTo(pad.l, Y(y)); ctx.lineTo(W - pad.r, Y(y)); ctx.stroke(); }
    ctx.globalAlpha = 1;
    for (let i = 0; i <= 4; i++) { const y = y0 + (y1 - y0) * i / 4; ctx.fillText(y.toFixed(Math.abs(y1 - y0) < 10 ? 1 : 0), 2, Y(y) + 4); }
    // clock-time axis: x is seconds, opt.epoch0 is the wall-clock second that x = 0 corresponds to
    if (opt.epoch0 !== undefined) {
      const span = x1 - x0, steps = [1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 900, 1800, 3600, 7200];
      const step = steps.find((v) => span / v <= 8) || 7200, z = (n) => String(n).padStart(2, '0');
      const tz = new Date().getTimezoneOffset() * 60;   // tick on local clock multiples
      ctx.fillStyle = muted; ctx.strokeStyle = muted; ctx.textAlign = 'center';
      for (let w = Math.ceil((x0 + opt.epoch0 - tz) / step) * step; w - opt.epoch0 + tz <= x1; w += step) {
        const x = X(w - opt.epoch0 + tz); if (x < pad.l + 20 || x > W - pad.r - 20) { continue; }
        const d = new Date((w + tz) * 1000);
        ctx.globalAlpha = 0.4; ctx.beginPath(); ctx.moveTo(x, pad.t); ctx.lineTo(x, H - pad.b + 3); ctx.stroke(); ctx.globalAlpha = 1;
        ctx.fillText(z(d.getHours()) + ':' + z(d.getMinutes()) + ':' + z(d.getSeconds()), x, H - 4);
      }
      ctx.textAlign = 'left';
    }
    canvas._geo = { x0: x0, x1: x1, W: W, pad: pad, series: series, epoch0: opt.epoch0, unit: opt.unit || '' };
    for (const s of series) {
      ctx.strokeStyle = s.color || fg; ctx.lineWidth = 1.5; ctx.beginPath(); let pen = false;
      for (const p of s.pts) { if (!Number.isFinite(p[1]) || p[0] < x0 || p[0] > x1) { pen = false; continue; } const x = X(p[0]), y = Y(p[1]); if (pen) { ctx.lineTo(x, y); } else { ctx.moveTo(x, y); pen = true; } }
      ctx.stroke();
    }
    ctx.strokeStyle = accent; ctx.fillStyle = accent; ctx.lineWidth = 1;
    // markers: a line plus a short number (full name is in the Markers list); numbers stagger so they never overlap
    let lastX = -1e9, row = 0;
    for (const m of opt.markers || []) {
      if (m.t < x0 || m.t > x1) { continue; }
      const x = X(m.t); ctx.beginPath(); ctx.moveTo(x, pad.t); ctx.lineTo(x, H - pad.b); ctx.stroke();
      row = (x - lastX < 22) ? (row + 1) % 4 : 0; lastX = x;
      ctx.fillText('#' + m.id, x + 3, pad.t + 10 + row * 11);
    }
  }

  function clock(sec) { const d = new Date(sec * 1000), z = (n) => String(n).padStart(2, '0'); return z(d.getHours()) + ':' + z(d.getMinutes()) + ':' + z(d.getSeconds()); }
  function tooltip(canvas) {
    canvas.addEventListener('mousemove', (ev) => {
      const g = canvas._geo; if (!g || !g.series.length) { return; }
      const r = canvas.getBoundingClientRect(), px = (ev.clientX - r.left) / r.width * g.W;
      const x = g.x0 + (px - g.pad.l) / (g.W - g.pad.l - g.pad.r) * (g.x1 - g.x0);
      let best = null;
      for (const p of g.series[0].pts) { if (Number.isFinite(p[1]) && (!best || Math.abs(p[0] - x) < Math.abs(best[0] - x))) { best = p; } }
      canvas.title = best ? (clock(best[0] + g.epoch0) + '  (' + Math.round(best[0] - g.x0) + ' s into the window)  ' + best[1].toFixed(2) + ' ' + g.unit) : '';
    });
  }
  ['cPpg', 'cHr', 'cEda', 'rHr', 'rEda'].forEach((id) => tooltip($(id)));

  // ---------------------------------------------------------------- live
  const hist = [];                 // samples {t, ppg, eda, hr}
  const markers = [];              // {t, text}
  const HIST_SECS = 300;
  let isConn = false, latest = null, beatTimer = null, edaSmooth = null, clockOff = Date.now() / 1000 - performance.now() / 1000;
  const ch = typeof BroadcastChannel !== 'undefined' ? new BroadcastChannel('swarm-physio') : null;
  if (!ch) { $('status').textContent = 'This browser has no BroadcastChannel, so the monitor cannot follow the other page.'; }
  function send(m) { if (ch) { ch.postMessage(m); } }
  function applyStatus(m) {
    if (m.status !== undefined) { $('status').textContent = m.status; }
    if (m.connected !== undefined) { $('dot').className = 'dot' + (m.recording ? ' recording' : (m.connected ? ' on' : '')); }
    if (m.connected !== undefined) { isConn = m.connected; }
    $('recinfo').textContent = isConn || m.recording ? 'Logging everything since connect' : 'Logging starts when a sensor connects';
    if (m.routine !== undefined) { $('routine').textContent = m.routine || '-'; }
  }
  function addSamples(list) {
    for (const s of list) { hist.push(s); }
    const tEnd = hist.length ? hist[hist.length - 1].t : 0;
    while (hist.length && hist[0].t < tEnd - HIST_SECS) { hist.shift(); }
  }
  function show(l) {
    if (!l) { return; }
    latest = l;
    $('hr').textContent = l.hrValid ? Math.round(l.hr) : '--';
    $('eda').textContent = Number.isFinite(l.edaSmooth) ? l.edaSmooth.toFixed(2) : '--';
  }
  if (ch) {
    ch.onmessage = (ev) => {
      const m = ev.data || {};
      if (m.clockOffset !== undefined) { clockOff = m.clockOffset; }
      if (m.type === 'history') { hist.length = 0; addSamples(m.samples || []); applyStatus(m); show(m.latest); markers.length = 0; for (const k of m.markers || []) { markers.push(k); } renderMarkers(); }
      else if (m.type === 'batch') {
        addSamples(m.samples || []); show(m.latest); applyStatus({ recording: m.recording, routine: m.routine });
        $('section').textContent = m.section || '-';
        if ((m.samples || []).some((s) => s.beat)) { const h = $('heart'); h.classList.add('beat'); clearTimeout(beatTimer); beatTimer = setTimeout(() => h.classList.remove('beat'), 120); }
      }
      else if (m.type === 'status') { applyStatus(m); }
      else if (m.type === 'marker') { markers.push(m.marker); if (markers.length > 500) { markers.shift(); } renderMarkers(true); }
      else if (m.type === 'marker-update') { const k = markers.find((x) => x.id === m.marker.id); if (k) { k.text = m.marker.text; } renderMarkers(); }
    };
    send({ type: 'hello' });
  }
  function renderMarkers(toEnd) {
    const box = $('markers'); box.textContent = '';
    for (const k of markers) {
      const d = document.createElement('div'); d.className = k.text ? '' : 'unnamed';
      d.textContent = '#' + k.id + '  ' + clock(k.epochMs / 1000) + '  ' + (k.text || '(unnamed, click to name)');
      d.title = 'Click to name or rename';
      d.onclick = () => { const n = prompt('Name for marker #' + k.id + ':', k.text); if (n !== null) { send({ type: 'cmd', cmd: 'rename-marker', id: k.id, name: n }); k.text = n.trim(); renderMarkers(); } };
      box.appendChild(d);
    }
    if (toEnd) { box.scrollTop = box.scrollHeight; }
  }
  $('mk').onclick = () => send({ type: 'cmd', cmd: 'marker' });
  $('bl').onclick = () => send({ type: 'cmd', cmd: 'marker', text: 'baseline' });
  document.addEventListener('keydown', (e) => { if ((e.key === 'm' || e.key === 'M') && !e.repeat && !e.ctrlKey && !e.metaKey && !e.altKey) { send({ type: 'cmd', cmd: 'marker' }); } });
  $('dl').onclick = () => { const l = prompt('Experiment name or label for this file (leave empty for the default name):', ''); if (l !== null) { send({ type: 'cmd', cmd: 'download', label: l }); } };

  function draw() {
    if (hist.length) {
      const tEnd = hist[hist.length - 1].t;
      plot($('cPpg'), [{ pts: hist.filter((s) => s.t > tEnd - 10).map((s) => [s.t, s.ppg]) }], { xmin: tEnd - 10, xmax: tEnd, epoch0: clockOff });
      const hrPts = [], edaPts = [];
      let step = Math.max(1, Math.floor(hist.length / 900));
      for (let i = 0; i < hist.length; i += step) { const s = hist[i]; hrPts.push([s.t, s.hr]); edaPts.push([s.t, s.eda]); }
      const xmin = Math.max(tEnd - HIST_SECS, hist[0].t);
      plot($('cHr'), [{ pts: hrPts, color: '#dc2626' }], { xmin: xmin, xmax: tEnd, ymin: 40, ymax: 160, markers: markers, epoch0: clockOff, unit: 'bpm' });
      plot($('cEda'), [{ pts: edaPts, color: '#2563eb' }], { xmin: xmin, xmax: tEnd, markers: markers, epoch0: clockOff, unit: 'uS' });
    }
    requestAnimationFrame(() => setTimeout(draw, 100));
  }
  draw();

  // ---------------------------------------------------------------- look at a recording
  function parseCsv(text) {
    const rows = []; let cur = [], f = '', q = false;
    for (let i = 0; i < text.length; i++) {
      const c = text[i];
      if (q) { if (c === '"') { if (text[i + 1] === '"') { f += '"'; i++; } else { q = false; } } else { f += c; } }
      else if (c === '"') { q = true; } else if (c === ',') { cur.push(f); f = ''; } else if (c === '\n') { cur.push(f); rows.push(cur); cur = []; f = ''; } else if (c !== '\r') { f += c; }
    }
    if (f.length || cur.length) { cur.push(f); rows.push(cur); }
    return rows;
  }
  $('file').onchange = async (ev) => {
    const file = ev.target.files[0]; if (!file) { return; }
    const rows = parseCsv(await file.text()); const head = rows.shift(); const ix = {}; head.forEach((h, i) => { ix[h] = i; });
    const num = (r, k) => { const v = parseFloat(r[ix[k]]); return Number.isFinite(v) ? v : NaN; };
    let epoch0 = 0, label = ''; const hr = [], eda = [], marks = [], bands = []; let tMax = 0, routineStart = null, routineName = '', n = 0;
    for (const r of rows) {
      if (r.length < head.length) { continue; }
      if (r[ix.type] === 'meta') { label = (r[ix.marker] || '').replace(/^experiment:\s*/, ''); continue; }
      const t = num(r, 't_s'); if (!Number.isFinite(t)) { continue; } if (!epoch0 && num(r, 'epoch_ms') > 0) { epoch0 = num(r, 'epoch_ms') / 1000 - t; } tMax = Math.max(tMax, t);
      if (r[ix.type] === 'sample') { n++; if (n % 8 === 0) { hr.push([t, num(r, 'hr_bpm')]); eda.push([t, num(r, 'eda_us')]); } }
      else if (r[ix.type] === 'marker') {
        const text = r[ix.marker]; marks.push({ t: t, text: text, id: marks.length + 1 });
        if (text.startsWith('routine start')) { routineStart = t; routineName = text.replace('routine start: ', ''); }
        else if (text === 'routine end' && routineStart !== null) { bands.push({ from: routineStart, to: t, color: bands.length % 2 ? '#2563eb' : '#c2410c' }); routineStart = null; }
      }
    }
    if (routineStart !== null) { bands.push({ from: routineStart, to: tMax, color: '#c2410c' }); }
    $('fileinfo').textContent = (label ? '[' + label + '] ' : '') + file.name + ': ' + Math.round(tMax) + ' s, ' + marks.length + ' markers, shaded = routine';
    plot($('rHr'), [{ pts: hr, color: '#dc2626' }], { xmin: 0, xmax: tMax, ymin: 40, ymax: 160, markers: marks, bands: bands, epoch0: epoch0, unit: 'bpm' });
    plot($('rEda'), [{ pts: eda, color: '#2563eb' }], { xmin: 0, xmax: tMax, markers: marks, bands: bands, epoch0: epoch0, unit: 'uS' });
  };
})();
