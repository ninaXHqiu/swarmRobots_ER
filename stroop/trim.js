// Trims every clip in public/audio/ to under 1 s for the task.
// It measures loudness every 50 ms, finds where the sound actually is (so
// silence at the start or end is dropped), and if the sound is longer than
// MAX_SEC keeps the loudest MAX_SEC window. Short fades avoid clicks.
// Each clip is brought to the same peak loudness (PEAK_DB).
// Writes <name>.mp3; originals are kept in public/audio/originals/. Needs ffmpeg.
// Safe to re-run: trimmed outputs are tagged, so they're re-cut from the
// original instead of being treated as new files.

const fs = require('fs');
const path = require('path');
const { execFileSync, spawnSync } = require('child_process');

const AUDIO = path.join(__dirname, 'public', 'audio');
const ORIGINALS = path.join(AUDIO, 'originals');
const MAX_SEC = 0.95;
const FRAME_SEC = 0.05;
const SILENCE_BELOW_PEAK_DB = 30; // quieter than peak minus this counts as silence
const FADE_IN_SEC = 0.015;
const FADE_OUT_SEC = 0.12;
const PEAK_DB = -1; // every clip is brought to the same peak, just under full scale
const TAG = 'stroop-trimmed';
const EXTS = ['.mp3', '.wav', '.m4a', '.ogg', '.aac', '.flac'];

const baseName = (f) => path.basename(f, path.extname(f));

function probe(file, entry) {
  return execFileSync('ffprobe', ['-v', 'error', '-show_entries', entry, '-of', 'csv=p=0', file]).toString().trim();
}

// RMS level in dB for each FRAME_SEC slice of the file.
function envelope(file) {
  const rate = 8000;
  const out = execFileSync('ffmpeg', [
    '-v', 'error', '-i', file, '-af',
    `aresample=${rate},asetnsamples=${rate * FRAME_SEC},astats=metadata=1:reset=1,` +
      'ametadata=mode=print:key=lavfi.astats.Overall.RMS_level:file=/dev/stdout',
    '-f', 'null', '-',
  ]).toString();
  return [...out.matchAll(/RMS_level=(\S+)/g)].map((m) => (m[1] === '-inf' ? -120 : parseFloat(m[1])));
}

// Loudest sample level in dB of the file after the given filter.
function peakDb(file, filter) {
  const res = spawnSync('ffmpeg', ['-i', file, '-af', `${filter},volumedetect`, '-f', 'null', '-']);
  const m = res.stderr.toString().match(/max_volume: (-?[\d.]+) dB/);
  return m ? parseFloat(m[1]) : PEAK_DB;
}

// Returns [start, end] in seconds of the part worth keeping.
function pickWindow(levels) {
  const peak = Math.max(...levels);
  const floor = peak - SILENCE_BELOW_PEAK_DB;
  const first = levels.findIndex((v) => v > floor);
  const last = levels.length - 1 - [...levels].reverse().findIndex((v) => v > floor);
  const span = last - first + 1;
  const win = Math.round(MAX_SEC / FRAME_SEC);
  if (span <= win) return [first * FRAME_SEC, (last + 1) * FRAME_SEC];

  // Sound is longer than the limit: take the loudest window, preferring the
  // earliest one that's within 1 dB of the best so we keep the attack.
  const energy = levels.map((db) => Math.pow(10, db / 10));
  const scores = [];
  for (let i = first; i + win <= last + 1; i++) {
    scores.push([i, energy.slice(i, i + win).reduce((a, b) => a + b, 0)]);
  }
  const best = Math.max(...scores.map((s) => s[1]));
  const [start] = scores.find((s) => 10 * Math.log10(s[1] / best) > -1);
  return [start * FRAME_SEC, (start + win) * FRAME_SEC];
}

fs.mkdirSync(ORIGINALS, { recursive: true });
const files = fs.readdirSync(AUDIO).filter((f) => EXTS.includes(path.extname(f).toLowerCase()));
if (!files.length) console.log('No audio files in public/audio/');

for (const f of files) {
  const name = baseName(f);
  const current = path.join(AUDIO, f);
  const kept = fs.readdirSync(ORIGINALS).find((o) => baseName(o) === name);
  let original;

  if (kept && probe(current, 'format_tags=comment') === TAG) {
    original = path.join(ORIGINALS, kept);
    fs.rmSync(current);
  } else {
    // A new file; it replaces any older original with the same name.
    if (kept) fs.rmSync(path.join(ORIGINALS, kept));
    original = path.join(ORIGINALS, f);
    fs.renameSync(current, original);
  }

  const [start, end] = pickWindow(envelope(original));
  const len = end - start;
  const cut = [`atrim=${start.toFixed(3)}:${end.toFixed(3)}`, 'asetpts=PTS-STARTPTS'];
  const gain = PEAK_DB - peakDb(original, cut.join(','));
  const filter = [
    ...cut,
    `volume=${gain.toFixed(2)}dB`,
    `afade=t=in:d=${FADE_IN_SEC}`,
    `afade=t=out:st=${Math.max(0, len - FADE_OUT_SEC).toFixed(3)}:d=${FADE_OUT_SEC}`,
  ].join(',');
  const out = path.join(AUDIO, `${name}.mp3`);
  execFileSync('ffmpeg', ['-y', '-v', 'error', '-i', original, '-af', filter, '-metadata', `comment=${TAG}`, '-q:a', '2', out]);
  const before = parseFloat(probe(original, 'format=duration')).toFixed(2);
  const after = parseFloat(probe(out, 'format=duration')).toFixed(2);
  console.log(`${name}: ${before}s, kept ${start.toFixed(2)}-${end.toFixed(2)}s -> ${after}s`);
}

// The page can't list a folder when opened as a file, so write the list of
// recorded sounds into it: w_* are wrong sounds, c_* are correct sounds.
const PAGE = path.join(__dirname, 'public', 'index.html');
const ids = fs.readdirSync(AUDIO).filter((f) => f.endsWith('.mp3')).map(baseName);
const list = (prefix) => JSON.stringify(ids.filter((id) => id.startsWith(prefix)));
const html = fs.readFileSync(PAGE, 'utf8');
const block = /(\/\/ SOUND_FILES:BEGIN.*\n)[\s\S]*?(\/\/ SOUND_FILES:END)/;
if (!block.test(html)) throw new Error('SOUND_FILES markers not found in public/index.html');
fs.writeFileSync(
  PAGE,
  html.replace(block, `$1const SOUND_FILES = {\n  wrong: ${list('w_')},\n  correct: ${list('c_')},\n};\n$2`),
);
console.log(`index.html: ${ids.filter((id) => /^[wc]_/.test(id)).length} sounds listed`);
