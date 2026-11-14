# Shimmer: the participant's body in the show

Two things, both in the browser page that already runs Unity and the robots:

1. **Feedback**: routines can read the participant's heart rate and skin conductance live and react (C19 Heart Ring does).
2. **Record**: every signal is saved with markers that say what the robots were doing, as a CSV, and can be drawn.

Version tag `without-shimmer` is the last commit before this work.

## What was added

| Part | Where | What it does |
|---|---|---|
| Sensor connection | `Assets/StreamingAssets/physio/shimmer-source.js` and `vendor/shimmer-web-sdk.esm.js` | Connects a Shimmer with Shimmer's own web SDK (v0.5.1, BSD licence, see `vendor/NOTICE.txt`). Turns on PPG and GSR at 128 Hz. |
| Signals, recording | `Assets/StreamingAssets/physio/physio.js` | Heart rate from the PPG, skin conductance, markers, CSV, simulated sensor, feed to the monitor. |
| Monitor | `Assets/StreamingAssets/physio/monitor.html`, `monitor.js` | Live heart rate, pulse wave, skin conductance and markers; look at a downloaded CSV. |
| Panel | `Assets/WebGLTemplates/webble/index.html` | A collapsible "Sensor and monitor" panel **under the whole Control screen**, with the monitor inside it and a button to open the monitor in its own tab. |
| Unity side | `Assets/Physio/Physio.cs`, `Assets/Plugins/WebGL/Physio.jslib` | `Physio.HeartRate`, `Physio.Eda`, beats, `Physio.Marker(...)`. |
| Markers | `SimRoutinePlayer.cs`, `RoutineRunner.cs`, `V2Show.cs` | Routine start/end, "opening done", "ending", and the named sections of C13 to C19. |
| First feedback routine | `V2HeartRing.cs` (C19) | The ring swells at every heartbeat and turns slower when the heart is faster. |
| Offline graph | `tools/plot_physio.py` | PNG of heart rate, skin conductance and pulse wave with the routine and its sections marked. |
| Test | `tools/test-physio.mjs` | `node tools/test-physio.mjs` checks heart rate (55 to 150 bpm, upside-down wave), a flat signal, skin conductance and the CSV. |

## Connecting a Shimmer

Needs **Chrome or Edge** (Safari has no Web Bluetooth or Web Serial) and a **Shimmer with a PPG + GSR set-up** (GSR+ unit, finger PPG probe and GSR electrodes).

1. Open the page (the published one, or the local build) and open **Sensor and monitor** under the Control screen.
2. **Shimmer3R**: switch it on, click **Connect Shimmer3R (Bluetooth LE)**, pick it in the browser window. Firmware 1.0.22 or newer is what Shimmer states for its web SDK.
3. **Classic Shimmer3**: first pair it in the computer's Bluetooth settings (it then appears as a serial port), click **Connect Shimmer3 (paired, serial)** and pick that port. This path is **not tested by us yet**.
4. No sensor at hand: **Use simulated sensor** gives a heart rate around 72 and a drifting skin conductance, enough to test everything.
5. Put the finger on the PPG probe. The first heart rate shows after a few seconds (it needs about 3 steady beats). A heart rate of "--" means no clear pulse: check the probe, keep the hand still.

Every browser asks the user to click and choose the device; that cannot be skipped. The robots (Web Bluetooth) and the Shimmer connect separately and both stay connected.

## Recording

- **Start recording** (panel, or in the monitor) before the routine; **Stop** after; **Download CSV**.
- Markers are written automatically while recording: `routine start: C17 v2 Ring Swap`, `opening done`, `section: swap 1`, `ending`, `routine end`.
- Columns: `type` (`sample` or `marker`), `epoch_ms`, `t_s` (seconds from the start of the recording), `source`, `routine`, `section`, `ppg_raw`, `eda_us` (microsiemens), `hr_bpm`, `beat` (1 on the sample where a beat was found), `marker`.
- Look at it in the monitor (**Look at a recording** > choose the CSV) or with `python3 tools/plot_physio.py recording.csv`.

## Using the body in a routine

```csharp
float bpm = Physio.HeartRateOr(72f);          // -1 / no sensor gives the fallback
float s = Physio.SecondsSinceBeatOr(t, 72f);  // time since the last real beat, or a 72 bpm metronome without a sensor
Physio.Section("my part");                     // a marker in the recording
```

Use smoothed trends, not single beats, for anything big: the pulse wave is noisy when the hand moves, and skin conductance reacts several seconds late. C19 Heart Ring keeps its movements small (about 10 mat units in 0.3 s) so the robots can follow.

## What is and is not verified

- Verified here with no device: the heart rate and skin conductance maths, the CSV, the plot script, the routines in the headless simulator (C19 follows its plan to within 17 units, ends in the line), and that the project compiles.
- **Not verified**: a real Shimmer. The connection code follows Shimmer's own PPG demo (`video-ppg`), but we have not run it against a unit. The classic Shimmer3 serial path and the PPG channel name on it are guesses to be checked. Shimmer calls the SDK pre-alpha.
- The monitor and the panel have been checked for syntax only, not opened in a browser.
- Bluetooth load: eight robots and a Shimmer in one browser may not all stay connected. Test with all of them before an experiment.

## People's data

Heart rate and skin conductance are personal data. Get consent, keep recordings without names (the CSV has none), store them somewhere private, and do not commit them to the repository.
