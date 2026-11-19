# Stroop stress task

Live: https://ninaxhqiu.github.io/swarmRobots_ER/stroop/

Or open `public/index.html` in a browser (double-click it). No install or server needed.

Publishing: `tools/publish-pages.sh` copies `stroop/public/` (without `audio/originals/`) to `/stroop/` on the Pages site every time it publishes the simulator.

## Flow

1. Participant ID (typed by the researcher), then Start.
2. Welcome page.
3. Three sections, each:
   - the Stroop task page and the record page ("The highest record is 35. Your goal is to beat the record. Good luck!") in section 1 only, and the colours page. Before the Stroop in sections 2 and 3, a goal page shows the highest streak from any earlier Stroop: under the record it says how far from it; at or past the goal it says awesome and sets a new goal of best + 10 rounded up to a multiple of 5; between the record and their own goal it says how far from the prize; reaching their own goal gives the "Were you promised a prize? ... anything under $10!" page with a goal of best + 5 rounded down to a multiple of 5 (`progress()`, `RECORD`);
   - the Stroop: 3 minutes, 1.5 s per word, every word in a different ink from its name. The participant presses the first letter of the **ink colour**. The score (current streak) is top left; there is no CORRECT/WRONG screen, a wrong answer or timeout plays a wrong sound and the next word comes straight away. No answers are forced wrong;
   - TIME'S UP with the swarm smile under it shows on red for 1 s, then the red screen shrinks into the top-left score spot, which shows that Stroop's highest streak, and the eval appears with a 25 s countdown bar (SAM valence and arousal with the SAM pictures, STAI-6);
   - an instructions page (section 1: baseline; sections 2 and 3: "Now look at the robots.", where Next only appears after 2 minutes, `ROBOT_MS`), then the same eval again without a timer and "How do you feel now?";
   - while "Now look at the robots." is on screen, white noise plays (`public/audio/noise/white_noise.mp3`, 10 min, about -11 LUFS) until Next;
   - after section 2's last eval: a "We also have some questions" popup (dots appear one at a time, 10 s, no button);
   - after sections 1 and 2: "Ready for the next test?".

   Every eval question must be answered before Submit works. SAM pictures are in `public/image/`.
4. The 6th eval's Submit downloads one CSV, named `<ID>_<date>.csv` (to save straight into OneDrive, set the browser's download folder to a OneDrive-synced folder and turn off "ask where to save"), with one row per eval (6 rows): the time it was submitted, section, which eval, that section's highest Stroop streak, and the SAM and STAI-6 answers.
5. End page: fireworks into a heart, then a smile; Bye Bye! turns the screen off like an old TV.

Colour sets, one per section (`SETS`):
1. red, pink, blue, yellow, white, cyan
2. brown, orange, green, lime, cyan, magenta
3. rose, purple, violet, mint, grey, navy

## Sounds

Correct answers are silent. Wrong answers (including timeouts) play one of the `w_` clips in `public/audio/`, picked evenly at random.

Current clips: boo (full 4.4 s), buzzer (0.95 s), mocking laugh (original file). Long clips carry on into the next word; the next wrong answer cuts off whatever is still playing.

### Adding or trimming clips

Put clips in `public/audio/` named `w_<name>`. Any common format works (mp3, wav, m4a, ogg…). Then run:

```
npm run trim
```

This finds where the sound actually is in each clip (silence at either end is dropped), keeps at most the loudest 0.95 s, and brings every clip to the same perceived loudness (`LOUDNESS_LUFS`, -5) with a limiter so nothing distorts. Exceptions are set at the top of `trim.js`: `FULL_LENGTH` clips keep their whole sound (only silence at the ends is dropped), `AS_IS` clips are used exactly as downloaded. Saved as `<name>.mp3`; originals are kept in `public/audio/originals/`. It also updates the list of clips inside `index.html`, so run it after adding or removing files. Re-running is safe. Needs ffmpeg.
