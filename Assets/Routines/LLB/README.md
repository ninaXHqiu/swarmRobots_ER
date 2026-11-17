# LLB routines

Six routines for the LLB study, planned for 8 robots, each exactly 3:00. They appear in the routine lists (simulator and Control screen) as D01 to D06.

| Label | Routine | In short |
|---|---|---|
| D01 | LLB_resonance_v1 | The line breaks into a chaotic hive, calms into a slow synchronised float, gathers into a flower and travels round it |
| D02 | LLB_resonance_v2 | Panic (circles, spins, bouncing), all pairs calm at once each its own way, pair dances, a gentle ring dance, then they turn to the participant and a flower opens and closes with calm breathing |
| D03 | LLB_resonance_v3 | The line glides back to the middle of the mat as a heart trace at the participant's heart rate, softens into a swell, curls into a flower that breathes twice |
| D04 | LLB_cheering_v1 | Desynchronised step back, loops into two circles, fireworks x3, one circle that turns opening and closing, gentle spins |
| D05 | LLB_cheering_v2 | Pairs talk, dance in their own places, swap partners, dance again, a heart that beats softly |
| D06 | LLB_cheering_v3 | Huddle introduction, two rows walking apart in a wave, a pulsing star turning clockwise, groups of 2-3 doing figures, fireworks, spins and small circles in sync |

## Shared frame

Each routine is one planned path per robot (`LLBPlan`), followed with V2Show's steering (`LLBShow`):

1. Set-up, not timed: every robot goes to its own spot in the line, a quarter of the mat in from the participant (`LLB.LineY`), 36 apart, facing the participant.
2. Introduction, one of two (`LLBPlan.Intro`, chosen in each routine's `Plan` constructor):
   - **Messenger** (D01-D05): robot 1 walks over, looks at the participant, at their hand, back at them, turns to the team, goes straight back to just in front of the line and "talks"; the others answer, each its own way and moment.
   - **Huddle** (D06): the same visit, then the messenger steps back into its place, the line curls into a huddle facing in, they argue, two or three look at the participant, and they argue again.
3. The routine's own middle part.
4. Closing, identical in all six: from a ring the robots slowly go back into the line, and everyone turns to look at the participant.

Set-up lines in `LLBPlan.cs`: `LLB.Messenger` (which robot), `LLB.HandSide` / `LLB.HandOffset` (where the Shimmer hand is), `LLBPlan.TotalSecs` (length).

## Heart rate (resonance only)

The resonance routines follow the participant's heart rate from the Shimmer (`PhysioHeart` in `LLBShow.cs`): implausible beats are ignored (outside 45-140 bpm, or more than 25 % from the recent median), the rate is the median of the last 11 good beats and changes at most 2 bpm a second; with no good beat for 8 s the routine eases back to its script. The heart rate only moves the tempo, at most 10 bpm from the routine's script (`LLBPlan.HeartMargin`), never where the robots go. With no sensor they run on their script. The rate used is written into the sensor recording every 5 s ("LLB heart ...").

## Lights

The "Lights" button in the bottom-right corner of the simulator and the Control screen cycles off / white / colours (one colour per robot) for every routine (`RoutineLeds.Mode`, remembered between sessions).

## Checking

`Tools > LLB > Check routines` in the Unity editor (or `-executeMethod LLBCheck.Run`) checks every plan without robots. The same check, plus previews, runs outside Unity in `tools/llb-sim`. With 8 robots all six pass: never closer than 34 mat units centre to centre (a cube is about 23 wide), top speed 160 units/s (the chaotic hive), at least 29 from the mat edge.
