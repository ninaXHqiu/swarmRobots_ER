using System.Collections;
using System.Linq;
using UnityEngine;

// LLB_resonance_v1 (D01), 3:20. Introduction: the messenger (LLBPlan). Then:
//   1. The line breaks at once: every robot dashes off to its own random patch of the mat, already buzzing.
//   2. Hive: each buzzes about its patch, chaotic like a hive or flies, never pausing, for a long
//      while. How fast they buzz follows the participant's heart rate, within LLBPlan.HeartMargin of the
//      script (88 beats a minute, easing to 64).
//   3. They calm: the buzz slows into a slow float, and their movements become the same for all of them.
//   4. Floating, they gather into a flower and travel slowly round it once, in and out of its petals,
//      and it softens into a plain ring.
//   Closing (the same in every routine): slowly back into the line; everyone looks at the participant.
public class LLB_resonance_v1 : SwarmRoutine
{
    public override string Name { get { return "LLB_resonance_v1"; } }
    public override string Code { get { return "D01"; } }
    public override string Note { get { return "Chaotic hive, floating, flower"; } }
    public override int MinRobots { get { return 8; } }   // planned and checked for 8

    public override IEnumerator Run(RoutineContext ctx) { return LLBShow.Run(ctx, start => new Plan(start), 100, followHeart: true); }

    public class Plan : LLBPlan
    {
        const float RunUnitsPerSec = 55f, BuzzAmp = 27f, BuzzLo = 0.4f, BuzzHi = 0.9f, BuzzRise = 1.2f;
        const float FloatAmp = 16f, FloatLo = 0.04f, FloatHi = 0.11f, CalmSecs = 10f, SyncSecs = 12f;
        const float FormSecs = 12f, FigureR = 100f, Petals = 4f, Bloom = 0.3f, TurnSecs = 28f, SoftenSecs = 7f;
        const float ScriptHigh = 88f, ScriptLow = 64f;

        readonly Vector2[] cell;
        readonly float[] room, arriveAt;
        const float PatchGap = 108f;
        readonly int[] parity;
        readonly V2Show.WayToLine run;
        readonly Room runRoom;
        readonly LiveTempo tempo;
        readonly float runAt, hiveAt, calmAt, formAt, turnAt;
        readonly Show.Eased turn;

        public Plan(Vector2[] start) : base(start, Intro.Messenger)
        {
            // the figure's places are the closing ring's, so the figure only has to soften into it
            var place = V2Show.RingOrder(n);
            parity = place.Select(p => p % 2 == 0 ? 1 : -1).ToArray();
            // each robot's patch: straight out from its place in the figure, on a wide oval, some further, some nearer
            cell = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 d = LLB.Dir(RingDeg(i) + 36f * (Shapes.Hash(i, 7f) - 0.5f));
                float k = 0.62f + 0.5f * Shapes.Hash(i, 4f);
                Vector2 c = LLB.Centre + new Vector2(d.x * 150f * k, d.y * 112f * k) + 24f * new Vector2(Shapes.Hash(i, 5f) - 0.5f, Shapes.Hash(i, 6f) - 0.5f);
                cell[i] = new Vector2(Mathf.Clamp(c.x, 112f, 388f), Mathf.Clamp(c.y, 122f, 330f));
            }
            // push patches apart until every two are at least PatchGap apart (still scattered, never crowded)
            for (int round = 0; round < 200; round++)
            {
                for (int a = 0; a < n; a++)
                {
                    for (int b = a + 1; b < n; b++)
                    {
                        Vector2 d = cell[a] - cell[b]; float m = d.magnitude;
                        if (m >= PatchGap) { continue; }
                        Vector2 push = (m < 0.01f ? Vector2.right : d / m) * (PatchGap - m) * 0.5f;
                        cell[a] += push; cell[b] -= push;
                    }
                }
                for (int a = 0; a < n; a++) { cell[a] = new Vector2(Mathf.Clamp(cell[a].x, 112f, 388f), Mathf.Clamp(cell[a].y, 122f, 330f)); }
            }
            // each buzzes no further than leaves room for its nearest neighbour
            room = new float[n];
            for (int a = 0; a < n; a++)
            {
                float m = float.MaxValue;
                for (int b = 0; b < n; b++) { if (b != a) { m = Mathf.Min(m, (cell[a] - cell[b]).magnitude); } }
                room[a] = Mathf.Clamp((m - LLB.Keep - 4f) / 2f, 8f, BuzzAmp);
            }
            runAt = MiddleAt;
            run = LLB.Way(MiddleStarts(), cell, RunUnitsPerSec);
            hiveAt = runAt + run.Total;
            runRoom = new Room(n, (i, t) => run.Position(i, t - runAt), runAt, hiveAt + 6f, LLB.Keep, BuzzAmp);
            arriveAt = Enumerable.Range(0, n).Select(i => runAt + run.ArrivesAt(i)).ToArray();

            // the rest of the time: the hive takes 55 %, the float 45 %
            formAt = CloseAt - TurnSecs - FormSecs;
            float free = formAt - hiveAt;
            calmAt = hiveAt + 0.55f * free;
            turnAt = formAt + FormSecs;

            tempo = new LiveTempo(this, t => Mathf.Lerp(ScriptHigh, ScriptLow, Shapes.Ease((t - calmAt) / (formAt - calmAt))), runAt);
            var unit = new Show.Eased(turnAt - 4f, 0f, turnAt + 2f, 1f, CloseAt - 5f, 1f, CloseAt, 0f, CloseAt + 10000f, 0f);
            float rate = 360f / (unit.Integral(CloseAt) - unit.Integral(turnAt - 4f));
            turn = new Show.Eased(turnAt - 4f, 0f, turnAt + 2f, rate, CloseAt - 5f, rate, CloseAt, 0f, CloseAt + 10000f, 0f);

            Section(runAt, "the line breaks: everyone runs off");
            Section(hiveAt, "hive: erratic (tempo follows the heart rate)");
            Section(calmAt, "calming into a slow float, all together");
            Section(formAt, "gathering into a flower");
            Section(turnAt, "travelling round the flower, in and out of its petals");
        }

        // The buzz clock runs at the heart-following tempo (1 s of clock a second at 88 beats a minute)
        // (half from the clock, half from the heart-following tempo, so the heart rate moves it gently)
        float BuzzClock(float t) { return 0.5f * (t - runAt) + 0.5f * tempo.Beats(t) * 60f / ScriptHigh; }

        // The flower stands still; the robots travel round it, so each goes in and out of the petals
        Vector2 Figure(int i, float t)
        {
            float turned = turn.Integral(t) - turn.Integral(turnAt - 4f) - 360f;
            float soft = Shapes.Ease((t - (CloseAt - SoftenSecs)) / SoftenSecs);
            float deg = RingDeg(i) + turned;
            float petal = Mathf.Cos(Petals * (deg - RingDeg(0)) * Mathf.Deg2Rad);
            float r = Mathf.Lerp(FigureR * (1f + Bloom * petal), CloseR, soft);
            return LLB.On(LLB.Centre, deg, r);
        }

        protected override Vector2 Middle(int i, float t)
        {
            Vector2 p = t < hiveAt ? run.Position(i, t - runAt) : cell[i];
            if (t > formAt) { p = Morph(p, Figure(i, t), t, formAt, FormSecs); }
            // the buzz: where there is room while they run, then full; slowing into the float; the same for all
            // buzzing all the way: as much as there is room for while running, full as soon as it reaches its patch
            float hive = Mathf.Lerp(runRoom.At(i, t) * room[i] / BuzzAmp, room[i], Shapes.Ease((t - arriveAt[i] + 0.5f) / BuzzRise));
            float calm = Shapes.Ease((t - calmAt) / CalmSecs);
            float amp = Mathf.Lerp(hive, FloatAmp, calm) * (1f - Shapes.Ease((t - formAt) / FormSecs));
            if (amp <= 0f) { return p; }
            Vector2 buzz = LLB.Wander(i, 1f, BuzzClock(t), BuzzLo, BuzzHi);
            float sync = Shapes.Ease((t - calmAt - 2f) / SyncSecs);
            Vector2 drift = Vector2.Lerp(LLB.Wander(i, 9f, t, FloatLo, FloatHi), LLB.Wander(99, 9f, t, FloatLo, FloatHi), sync);
            return p + amp * Vector2.Lerp(buzz, drift, calm);
        }
    }
}
