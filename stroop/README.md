# Stroop stress task

Live: https://ninaxhqiu.github.io/swarmRobots_ER/stroop/

Or open `public/index.html` in a browser (double-click it). No install or server needed.

Publishing: `tools/publish-pages.sh` copies `stroop/public/` (without `audio/originals/`) to `/stroop/` on the Pages site every time it publishes the simulator.

The participant presses the first letter of the **ink colour**, not the word:

| Key | Colour | Key | Colour |
|---|---|---|---|
| R | red | P | purple |
| B | blue | O | orange |
| G | green | W | white |
| Y | yellow | C | cyan |

- Start screen: number of trials (max 50) and countdown seconds. Space or the Begin button starts.
- Two intro screens: the task with an example, then the key list. The keys are not shown during trials.
- Each trial is scored on the key press; a wrong key or running out of time counts as Wrong.
- Top right: `streak / total questions`. The streak goes up by 1 for each correct answer and drops to 0 on a wrong one or a timeout.
- `CORRECT` / `WRONG` shows for 1 s, then the next trial.
- Esc pauses and resumes. While paused, Q quits to the start screen.
- At the end: the best streak and Export CSV (which logs the streak after every trial).

## Sounds

Correct answers are silent. Wrong answers (including timeouts) play one of the `w_` clips in `public/audio/`, picked evenly at random.

Current clips: boo (full 4.4 s), buzzer (0.95 s), mocking laugh (original file). Long clips carry on into the next trial; the next wrong answer cuts off whatever is still playing.

### Adding or trimming clips

Put clips in `public/audio/` named `w_<name>`. Any common format works (mp3, wav, m4a, ogg…). Then run:

```
npm run trim
```

This finds where the sound actually is in each clip (silence at either end is dropped), keeps at most the loudest 0.95 s, and brings every clip to the same peak loudness so each one hits as a sudden burst. Exceptions are set at the top of `trim.js`: `FULL_LENGTH` clips keep their whole sound (only silence at the ends is dropped), `AS_IS` clips are used exactly as downloaded. Saved as `<name>.mp3`; originals are kept in `public/audio/originals/`. It also updates the list of clips inside `index.html`, so run it after adding or removing files. Re-running is safe. Needs ffmpeg.
