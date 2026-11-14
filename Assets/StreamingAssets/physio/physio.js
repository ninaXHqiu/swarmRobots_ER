// Physio: the participant's physical signals in the browser (heart rate and skin conductance), the
// markers that say what the robots were doing, recording to CSV, and the live feed to the monitor tab.
//
// One Physio lives in the page that also runs Unity. Sources push samples into it:
//   - a Shimmer sensor (shimmer-source.js)        - the simulated sensor below (for testing without a device)
// Unity reads the latest values through Physio.jslib (Assets/Plugins/WebGL). The monitor page
// (monitor.html, in the accordion under the Control screen or in its own tab) listens on a BroadcastChannel.
(function (global) {
  'use strict';

  // ---------------------------------------------------------------- Heart rate from the PPG waveform
  // Finds beats in a PPG signal. The waveform may come out either way up, so two trackers (peaks up and
  // peaks down) run side by side and the one with the steadier beat intervals is believed.
  const MIN_PEAK = 8;            // raw ADC counts: a pulse smaller than this is noise, not a heartbeat
  const MAX_SPREAD = 0.35;       // beat intervals that vary more than this are not a heartbeat
  class PeakTracker {
    constructor(sign) {
      this.sign = sign;
      this.base = null; this.lp = 0; this.varEma = 0;
      this.y1 = 0; this.y2 = 0; this.t1 = 0;
      this.lastBeat = -1; this.ibis = [];
    }
    // x: signal value, t: seconds, dt: seconds since the last sample. Returns the time of a beat just found, or null.
    push(x, t, dt) {
      if (this.base === null) { this.base = x; }
      this.base += (x - this.base) * Math.min(1, dt / (0.9 + dt));          // slow baseline: removes drift
      let y = this.sign * (x - this.base);
      this.lp += (y - this.lp) * Math.min(1, dt / (0.06 + dt));             // smooth: removes noise
      y = this.lp;
      this.varEma += (y * y - this.varEma) * Math.min(1, dt / (3 + dt));    // typical size over the last seconds
      const threshold = Math.max(0.5 * Math.sqrt(Math.max(this.varEma, 1e-12)), MIN_PEAK);   // never smaller than the noise floor
      let beat = null;
      // the previous sample was a peak: it was above both neighbours and above the threshold
      if (this.y1 > this.y2 && this.y1 >= y && this.y1 > threshold && this.y1 > 0) {
        const tp = this.t1;
        if (this.lastBeat < 0 || tp - this.lastBeat > 0.33) {
          if (this.lastBeat >= 0) {
            const ibi = tp - this.lastBeat;
            if (ibi < 2.0) { this.ibis.push(ibi); if (this.ibis.length > 8) { this.ibis.shift(); } } else { this.ibis = []; }
          }
          this.lastBeat = tp; beat = tp;
        }
      }
      this.y2 = this.y1; this.y1 = y; this.t1 = t;
      return beat;
    }
    // steadiness of the recent beat intervals: lower is steadier (coefficient of variation)
    spread() {
      const a = this.ibis.slice(-6);
      if (a.length < 3) { return Infinity; }
      const m = a.reduce((s, v) => s + v, 0) / a.length;
      const sd = Math.sqrt(a.reduce((s, v) => s + (v - m) * (v - m), 0) / a.length);
      return sd / m;
    }
    bpm() {
      const a = this.ibis.slice(-5).sort((p, q) => p - q);
      if (a.length < 3) { return null; }
      const median = a[Math.floor(a.length / 2)];
      const bpm = 60 / median;
      return bpm >= 35 && bpm <= 200 ? bpm : null;
    }
  }

  class HeartRateEstimator {
    constructor() { this.up = new PeakTracker(1); this.down = new PeakTracker(-1); this.best = this.up; this.lastBeatT = -1; this.beats = 0; }
    // returns { beat: bool, bpm: number|null }
    push(x, t, dt) {
      const bu = this.up.push(x, t, dt), bd = this.down.push(x, t, dt);
      const su = this.up.spread(), sd = this.down.spread();
      this.best = sd < su ? this.down : this.up;
      const b = this.best === this.up ? bu : bd;
      let beat = false;
      if (b !== null) { beat = true; this.lastBeatT = b; this.beats++; }
      const bpm = this.best.spread() <= MAX_SPREAD ? this.best.bpm() : null;
      const fresh = this.lastBeatT >= 0 && t - this.lastBeatT < 3.0;
      return { beat: beat, bpm: fresh ? bpm : null };
    }
  }

  // ---------------------------------------------------------------- Skin conductance
  class EdaFilter {
    constructor() { this.smooth = null; this.tonic = null; }
    push(us, dt) {
      if (this.smooth === null) { this.smooth = us; this.tonic = us; }
      this.smooth += (us - this.smooth) * Math.min(1, dt / (1.0 + dt));      // smooth over about a second
      this.tonic += (us - this.tonic) * Math.min(1, dt / (30 + dt));         // slow level over about half a minute
      return { smooth: this.smooth, tonic: this.tonic, phasic: this.smooth - this.tonic };
    }
  }

  // ---------------------------------------------------------------- Simulated sensor
  // A stand-in for the real sensor: a PPG-like waveform at a heart rate that can be changed, with natural
  // beat-to-beat variation, and a skin conductance that drifts and jumps now and then.
  class SimulatedSource {
    constructor(push, opts) {
      opts = opts || {};
      this.push = push; this.fs = opts.fs || 64; this.bpm = opts.bpm || 72; this.phase = 0;
      this.eda = 4.0; this.n = 0; this.timer = null; this.nextScr = 6; this.scr = 0; this.rand = opts.rand || Math.random;
      this.invert = !!opts.invert;
    }
    setBpm(b) { this.bpm = Math.max(40, Math.min(180, b)); }
    step() {
      const dt = 1 / this.fs; this.n++;
      const t = this.n * dt;
      this.phase += dt * (this.bpm / 60) * (1 + 0.04 * Math.sin(t * 0.7) + 0.02 * (this.rand() - 0.5));
      const p = this.phase % 1;
      let v = Math.exp(-Math.pow((p - 0.18) / 0.07, 2)) + 0.35 * Math.exp(-Math.pow((p - 0.45) / 0.1, 2));   // pulse and a smaller second bump
      v = 2000 + 600 * v + 15 * (this.rand() - 0.5) + 40 * Math.sin(t * 0.3);
      if (this.invert) { v = 4000 - v; }
      if (t > this.nextScr) { this.scr = 1.2 + this.rand(); this.nextScr = t + 8 + 10 * this.rand(); }
      this.scr *= Math.exp(-dt / 3);
      this.eda += (4 + 0.5 * Math.sin(t * 0.05) - this.eda) * dt * 0.05;
      this.push({ ppg: v, eda: this.eda + this.scr });
    }
    start() { if (!this.timer) { this.timer = setInterval(() => this.step(), 1000 / this.fs); } }
    stop() { if (this.timer) { clearInterval(this.timer); this.timer = null; } }
  }

  // ---------------------------------------------------------------- Physio
  const Physio = {
    VERSION: 1,
    source: null,                // 'shimmer3r-ble' | 'shimmer3-serial' | 'simulated' | null
    connected: false,
    status: 'Not connected',
    latest: { t: 0, ppg: NaN, eda: NaN, edaSmooth: NaN, edaTonic: NaN, edaPhasic: NaN, hr: NaN, hrValid: false, beats: 0, lastBeatT: -1 },
    routine: '', section: '',
    recording: false, rows: [], recStart: 0,
    onStatus: null, onUpdate: null, onLog: null, logLines: [],
    _hr: new HeartRateEstimator(), _eda: new EdaFilter(), _lastT: null, _handle: null, _sim: null, _channel: null,
    _batch: [], _batchTimer: null, _hist: [], _simulatedBpm: 72,

    now() { return (typeof performance !== 'undefined' ? performance.now() : Date.now()) / 1000; },

    // ---- connecting
    // Plain-words reason for a failed connection (the browser's own messages are cryptic)
    explain(e) {
      const msg = e && e.message ? e.message : String(e);
      const name = e && e.name ? e.name : '';
      if (name === 'NotFoundError' || /cancel/i.test(msg)) { return 'No sensor was chosen. Switch the Shimmer on (green light blinking), click Connect again and pick it in the list.'; }
      if (name === 'SecurityError' || /secure context|permissions policy/i.test(msg)) { return 'The browser blocks Bluetooth on this page. Open the page from https:// or http://localhost, not from a file.'; }
      if (name === 'NetworkError' || /GATT|disconnected|connection attempt failed/i.test(msg)) { return 'The sensor did not accept the connection. Switch it off and on, make sure no other app or computer is connected to it, then try again. (' + msg + ')'; }
      if (/ACK|timeout|timed out/i.test(msg)) { return 'The sensor connected but did not answer its setup commands. Check the firmware (1.0.22 or newer for Shimmer3R) and try again. (' + msg + ')'; }
      return msg;
    },
    log(m) {
      const line = new Date().toLocaleTimeString() + '  ' + m;
      this.logLines.push(line); if (this.logLines.length > 60) { this.logLines.shift(); }
      if (this.onLog) { this.onLog(this.logLines); }
    },
    preload() {
      // Load the sensor code ahead of time, so the Bluetooth chooser opens right after the click (browsers only
      // allow it for a few seconds after a click)
      if (this._preloaded) { return; }
      this._preloaded = import(this._base() + 'shimmer-source.js').then(
        (m) => m.preloadSdk ? m.preloadSdk() : null).catch((e) => { this._preloaded = null; this.log('Could not load the sensor code: ' + (e && e.message || e)); });
    },
    async connect(kind) {
      await this.disconnect();
      this._reset();
      try {
        if (kind === 'shimmer3r-ble' && !(navigator && navigator.bluetooth)) {
          throw new Error('This browser has no Web Bluetooth. Use Chrome or Edge on a computer (not Safari, not Firefox).');
        }
        if (kind === 'shimmer3-serial' && !(navigator && navigator.serial)) {
          throw new Error('This browser has no Web Serial. Use Chrome or Edge on a computer.');
        }
        if (kind === 'simulated') {
          this._sim = new SimulatedSource((s) => this._push(s), { bpm: this._simulatedBpm });
          this._sim.start();
          this._handle = { name: 'Simulated sensor', stop: async () => { this._sim.stop(); } };
        } else {
          this._setStatus('Connecting...'); this.log('Connecting (' + kind + ')');
          if (this._preloaded) { await this._preloaded; }
          const mod = await import(this._base() + 'shimmer-source.js');
          this._handle = await mod.connectShimmer(kind, {
            status: (m) => { this.log(m); this._setStatus(m); },
            sample: (s) => this._push(s),
            closed: (why) => { this.connected = false; this._setStatus('Connection lost: ' + why); this._announce(); },
          });
        }
        this.source = kind; this.connected = true;
        this._setStatus('Connected: ' + this._handle.name + (kind === 'simulated' ? '' : ' (waiting for the first heartbeat...)'));
        this._announce();
      } catch (e) {
        this.connected = false; this.source = null;
        const why = this.explain(e);
        this.log('FAILED: ' + (e && e.message ? e.message : e));
        this._setStatus('Could not connect: ' + why);
        this._announce();
        throw e;
      }
    },
    async disconnect() {
      if (this._handle) { try { await this._handle.stop(); } catch (e) { /* ignore */ } }
      this._handle = null; this._sim = null; this.connected = false; this.source = null;
      this._setStatus('Not connected'); this._announce();
    },
    setSimulatedBpm(b) { this._simulatedBpm = b; if (this._sim) { this._sim.setBpm(b); } },
    _base() {
      if (typeof document !== 'undefined' && document.currentScript && document.currentScript.src) { return document.currentScript.src.replace(/[^/]*$/, ''); }
      return this._scriptBase || './';
    },
    _reset() { this._hr = new HeartRateEstimator(); this._eda = new EdaFilter(); this._lastT = null; this.latest = Object.assign({}, this.latest, { hr: NaN, hrValid: false, beats: 0, lastBeatT: -1 }); },
    _setStatus(m) { this.status = m; if (this.onStatus) { this.onStatus(m); } this._post({ type: 'status', status: m, connected: this.connected, source: this.source }); },

    // ---- samples in
    _push(s) {
      const t = this.now();
      const dt = this._lastT === null ? 1 / 64 : Math.min(0.5, Math.max(1e-4, t - this._lastT));
      this._lastT = t;
      const L = this.latest;
      let beat = false;
      if (Number.isFinite(s.ppg)) {
        const r = this._hr.push(s.ppg, t, dt);
        L.ppg = s.ppg; beat = r.beat;
        L.hrValid = r.bpm !== null; if (r.bpm !== null) { L.hr = r.bpm; }
        if (beat) { L.beats = this._hr.beats; L.lastBeatT = t; }
      }
      if (Number.isFinite(s.eda)) {
        const e = this._eda.push(s.eda, dt);
        L.eda = s.eda; L.edaSmooth = e.smooth; L.edaTonic = e.tonic; L.edaPhasic = e.phasic;
      }
      L.t = t;
      const row = { t: t, ppg: L.ppg, eda: L.eda, hr: L.hrValid ? L.hr : NaN, beat: beat ? 1 : 0 };
      if (this.recording) { this._record('sample', row, ''); }
      this._batch.push(row);
      if (this._batch.length > 4000) { this._batch.shift(); }
      this._hist.push(row); if (this._hist.length > 64 * 40) { this._hist.shift(); }
      if (this.onUpdate) { this.onUpdate(L); }
      if (!this._batchTimer && this._channel) { this._batchTimer = setTimeout(() => this._flush(), 100); }
    },
    _flush() {
      this._batchTimer = null;
      if (!this._batch.length) { return; }
      this._post({ type: 'batch', samples: this._batch, latest: this.latest, routine: this.routine, section: this.section, recording: this.recording });
      this._batch = [];
    },

    // ---- what Unity reads
    hr() { return this.latest.hrValid ? this.latest.hr : -1; },
    eda() { return Number.isFinite(this.latest.edaSmooth) ? this.latest.edaSmooth : -1; },
    edaPhasic() { return Number.isFinite(this.latest.edaPhasic) ? this.latest.edaPhasic : 0; },
    beatCount() { return this.latest.beats; },
    secondsSinceBeat() { return this.latest.lastBeatT < 0 ? -1 : this.now() - this.latest.lastBeatT; },

    // ---- markers and recording
    beginRoutine(code, name) { this.routine = code || ''; this.section = ''; this.marker('routine start: ' + (code || '') + ' ' + (name || '')); },
    endRoutine() { this.marker('routine end'); this.routine = ''; this.section = ''; },
    marker(text) {
      this._record('marker', null, text);
      this._post({ type: 'marker', t: this.now(), text: text, routine: this.routine });
    },
    sectionStart(name) { this.section = name; this.marker('section: ' + name); },
    startRecording() {
      this.rows = []; this.recStart = this.now(); this.recording = true;
      this._record('marker', null, 'recording start');
      this._setStatus(this.status); this._announce();
    },
    stopRecording() {
      if (!this.recording) { return; }
      this._record('marker', null, 'recording stop'); this.recording = false; this._announce();
    },
    _record(type, row, text) {
      if (!this.recording && !(type === 'marker' && text === 'recording start')) { return; }
      const t = row ? row.t : this.now();
      this.rows.push({
        type: type, epochMs: Date.now(), tS: +(t - this.recStart).toFixed(4), source: this.source || '', routine: this.routine, section: this.section,
        ppg: row ? row.ppg : '', eda: row ? row.eda : '', hr: row ? row.hr : '', beat: row ? row.beat : '', marker: text || '',
      });
    },
    csv() {
      const head = 'type,epoch_ms,t_s,source,routine,section,ppg_raw,eda_us,hr_bpm,beat,marker';
      const fmt = (v) => (v === '' || v === undefined || (typeof v === 'number' && !Number.isFinite(v))) ? '' : String(v);
      const esc = (v) => { const s = fmt(v); return /[",\n]/.test(s) ? '"' + s.replace(/"/g, '""') + '"' : s; };
      return [head].concat(this.rows.map((r) => [r.type, r.epochMs, r.tS, r.source, r.routine, r.section, r.ppg, r.eda, r.hr, r.beat, r.marker].map(esc).join(','))).join('\n') + '\n';
    },
    downloadCsv() {
      const name = 'physio_' + new Date().toISOString().replace(/[:.]/g, '-').slice(0, 19) + '.csv';
      const blob = new Blob([this.csv()], { type: 'text/csv' });
      const a = document.createElement('a'); a.href = URL.createObjectURL(blob); a.download = name;
      document.body.appendChild(a); a.click(); setTimeout(() => { URL.revokeObjectURL(a.href); a.remove(); }, 1000);
      return name;
    },

    // ---- the monitor tab
    monitorUrl() { return this._base().replace(/\/$/, '') + '/monitor.html'; },
    openMonitor() { return window.open(this.monitorUrl(), 'swarm-physio-monitor'); },
    _announce() { this._post({ type: 'status', status: this.status, connected: this.connected, source: this.source, recording: this.recording, routine: this.routine }); },
    _post(msg) { if (this._channel) { try { this._channel.postMessage(msg); } catch (e) { /* ignore */ } } },
    _startChannel() {
      if (typeof BroadcastChannel === 'undefined' || this._channel) { return; }
      this._channel = new BroadcastChannel('swarm-physio');
      this._channel.onmessage = (ev) => {
        const m = ev.data || {};
        if (m.type === 'hello') { this._post({ type: 'history', samples: this._hist.slice(-64 * 30), latest: this.latest, status: this.status, connected: this.connected, source: this.source, recording: this.recording, routine: this.routine }); }
        else if (m.type === 'cmd') {
          if (m.cmd === 'record-start') { this.startRecording(); }
          else if (m.cmd === 'record-stop') { this.stopRecording(); }
          else if (m.cmd === 'download') { this.downloadCsv(); }
          else if (m.cmd === 'connect') { this.connect(m.kind).catch(() => {}); }
          else if (m.cmd === 'disconnect') { this.disconnect(); }
          else if (m.cmd === 'sim-bpm') { this.setSimulatedBpm(m.bpm); }
        }
      };
    },
  };

  // where this script was loaded from (for the module import and the monitor page)
  if (typeof document !== 'undefined' && document.currentScript && document.currentScript.src) { Physio._scriptBase = document.currentScript.src.replace(/[^/]*$/, ''); }
  Physio._startChannel();
  global.Physio = Physio;
  if (typeof window !== 'undefined' && typeof document !== 'undefined') { window.addEventListener('DOMContentLoaded', () => Physio.preload()); }
  if (typeof module !== 'undefined' && module.exports) { module.exports = { Physio, HeartRateEstimator, PeakTracker, EdaFilter, SimulatedSource }; }
})(typeof window !== 'undefined' ? window : globalThis);
