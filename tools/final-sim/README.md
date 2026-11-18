# Final routines: simulator and data for the app

The study's four final routines (1:55 each, 8 robots) were designed in a browser simulator. The app
plays them back exactly: `export.js` writes every robot's path to data files, and the Unity routines
in `Assets/Routines/Final` follow them.

| Button | Routine | Data file |
| --- | --- | --- |
| E01 / E02 / E03 | Resonance 1, GSR level 1 / 2 / 3: brief chaos, a racing heart that slows, three breaths | `res_heart_level1..3.txt` |
| E04 / E05 / E06 | Resonance 2, GSR level 1 / 2 / 3: brief chaos, a flower, round the petals, a heart that beats and turns | `res_flower_level1..3.txt` |
| E07 | Cheerleading 1: line to the middle, "here we are", pairs, turning heart, circle, spinning | `cl_heart.txt` |
| E08 | Cheerleading 2: line to the middle, the book, two fireworks, the finale | `cl_fireworks.txt` |

For a Resonance block, press the button of the level the experiment page shows after the stressor
(1 low, 2 medium, 3 high). Only the short chaos at the start changes with the level.

## How a routine runs (FinalShow)

1. Set-up, not timed: the robots go to their own line spots (the LLB line, robot 1 = messenger, the
   same robot every time) and turn to the participant.
2. The routine, 115 s. Robots that move follow their path with the same steering as the V2/LLB
   routines (forward or in reverse). Robots that only turn where they stand (looking at the
   participant or the hand, talking, spinning) are turned on the spot to the planned heading at
   the planned speed. Each section is written into the sensor recording as a marker.

## Change a routine

1. Edit `routines.js` (all four routines and the motion model are there).
2. `python3 build.py` and open `preview/index.html` to watch it (white cubes, lights button,
   GSR level for Resonance).
3. `node check.js`: closest distance, top speed (170 at most) and sideways motion for each.
4. `node export.js`: rewrites `Assets/Routines/Final/Resources/FinalRoutines/*.txt`.
5. `check/run.sh` (needs Mono): compiles the Unity code against stand-ins and tests every data file
   the way the app reads it.

Then commit; the Publish simulator workflow rebuilds the web app.

The motion model: a toio only drives forward or backward along its heading and turns on the spot.
In the simulator each robot gets a position and, where it matters, a facing; without a facing it
points where it drives. Robots never come closer than about 28 mat units (a cube is about 23 wide).
