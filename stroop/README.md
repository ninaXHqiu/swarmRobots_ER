# Stroop stress task

Live: https://ninaxhqiu.github.io/swarmRobots_ER/stroop/

Or open `public/index.html` in a browser (double-click it). No install or server needed.

Publishing: `tools/publish-pages.sh` copies `stroop/public/` (without `audio/originals/`) to `/stroop/` on the Pages site every time it publishes the simulator.

## Flow

1. Participant ID (typed by the researcher), then Start.
2. Welcome page.
3. Three sections, each:
   - the Stroop task page and the goal page ("Lowest score was 25! Your goal is to beat it!") in section 1 only, and the colours page;
   - the Stroop: 3 minutes, 1 s per word, every word in a different ink from its name. The participant presses the first letter of the **ink colour**. The score (current streak) is top left; there is no CORRECT/WRONG screen, a wrong answer or timeout plays a wrong sound and the next word comes straight away. A correct answer that would bring the streak to 5, 10, 15 or 20 is marked wrong with a 50, 65, 80 or 95% chance, and to 24 always (`FORCED_WRONG`), so 25 is never reached;
   - FAIL flashes, then the eval with a 25 s countdown bar (SAM valence and arousal with the SAM pictures, STAI-6);
   - an instructions page (section 1: baseline; sections 2 and 3: "Now look at the robots.", where Next only appears after 4 minutes, `ROBOT_MS`), then the same eval again without a timer and "How do you feel now?";
   - after sections 1 and 2: "Ready for the next test?".

   Every eval question must be answered before Submit works. SAM pictures are in `public/image/`.
4. The 6th eval's Submit uploads one CSV to the Google Drive folder (through the Apps Script web app in `UPLOAD_URL`), named `<ID>_<date>.csv`, with a row per Stroop word and a row per eval.
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

This finds where the sound actually is in each clip (silence at either end is dropped), keeps at most the loudest 0.95 s, and brings every clip to the same peak loudness so each one hits as a sudden burst. Exceptions are set at the top of `trim.js`: `FULL_LENGTH` clips keep their whole sound (only silence at the ends is dropped), `AS_IS` clips are used exactly as downloaded. Saved as `<name>.mp3`; originals are kept in `public/audio/originals/`. It also updates the list of clips inside `index.html`, so run it after adding or removing files. Re-running is safe. Needs ffmpeg.
