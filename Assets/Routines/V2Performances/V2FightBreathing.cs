using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// v2 Fight to Breathing Circle (C13).
//   1. Two robots meet head-on on one lane, fronts facing each other (>>>   <<<): sharp advances,
//      the other gives ground and pushes back, a few times, always facing, also backing up.
//   2. A second robot joins each side in the next lane: two rows facing each other, a few more
//      push-and-yield exchanges, the second robots a fraction behind the first.
//   3. The four others wait outside in a loose, uneven arrangement and react a little, one after
//      another, unsure: edge closer, hesitate, step back when one side lunges. Then each steps
//      beside the exposed flank of one fighter.
//   4. The helpers take their fighters out of the fight, diagonally, curving outward; the fighters
//      try to re-engage once or twice with shorter, slower pushes before giving way.
//   5. The four pairs reach neighbouring sectors of one ring, which starts to turn; the spacing
//      eases to 45 degrees; then the whole ring keeps turning and breathes (large, slow, same
//      radius for all), and finally opens and flattens into the line.
// Slots 0..7 from the left. Fighters: 1 (lower left), 2 (upper left), 5 (lower right), 6 (upper
// right). Helpers: 0 for 1, 4 for 2, 3 for 5, 7 for 6. The pairs sit round the ring in the order
// that flattens into the line without crossing (V2Show.RingOrder).
public class V2FightBreathing : SwarmRoutine
{
    public override string Name { get { return "v2 Fight to Breathing Circle"; } }
    public override string Code { get { return "C13"; } }
    public override string Note { get { return "Fight, helpers, breathing circle"; } }
    public override int MinRobots { get { return 8; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        // the fixed opening (the Messenger's introduction) and ending (all in the line, facing the participant)
        yield return V2Show.Run(ctx, start => new Plan(start), 100);
    }

    public class Plan : V2Show.IPlan, V2Show.IHeading
    {
        // ---- The fight ----
        const float LaneLow = 308f, LaneHigh = 272f;       // the two lanes of the confrontation
        const float Flank = 44f;                           // a helper stands this far beside its fighter
        static readonly int[] Lower = { 1, 5 }, Upper = { 2, 6 };
        const float Lag = 0.14f;                           // the second robot of a side reacts this late
        // (time, centre of the contest, distance between the two sides' robots); each step eases
        static readonly float[] Keys =
        {
            0f, 250f, 170f,   6f, 250f, 170f,
            7.0f, 262f, 82f,  7.6f, 262f, 82f,  8.6f, 252f, 94f,  9.1f, 238f, 62f,  9.6f, 238f, 62f,  10.8f, 250f, 106f,
            11.3f, 263f, 50f, 11.8f, 263f, 50f, 12.8f, 243f, 92f, 13.3f, 232f, 46f, 13.8f, 232f, 46f, 15f, 250f, 124f,
            21f, 250f, 124f, 22f, 265f, 72f, 22.6f, 265f, 72f, 23.6f, 252f, 98f, 24.2f, 236f, 58f, 24.8f, 236f, 58f, 26f, 250f, 112f,
            26.6f, 266f, 54f, 27.2f, 266f, 54f, 28.4f, 244f, 90f, 29f, 230f, 48f, 29.6f, 230f, 48f, 31f, 250f, 118f,
            31.6f, 262f, 62f, 32.2f, 262f, 62f, 33.4f, 250f, 102f, 34f, 238f, 56f, 34.6f, 238f, 56f, 36f, 250f, 124f,
            // trying to re-engage while being led away: shorter, slower
            42f, 250f, 124f, 43f, 258f, 84f, 43.8f, 258f, 84f, 45.2f, 250f, 112f, 46.4f, 244f, 90f, 47.4f, 244f, 90f, 49f, 250f, 128f, 60f, 250f, 150f
        };
        readonly Show.Eased contestM, contestD;
        const float EnterSecs = 5f, SecondEnterFrom = 16f, SecondEnterSecs = 5f;
        const float HelpFrom = 34f, HelpSecs = 6f;         // the helpers step in beside their fighters
        const float SeparateFrom = 40f, SeparateSecs = 22f, HelperLead = 2f;

        // ---- Onlookers: where they stand, and their reactions (when, how far toward the fight) ----
        static readonly Vector2[] Stand = new Vector2[8];
        static readonly int[] HelperOf = { 0, 4, 3, 7 };   // helpers of fighters 1, 2, 5, 6
        static readonly float[][] Reactions =
        {
            new[] { 14f, 12f, 17.5f, -8f, 22f, 14f, 26f, -12f, 30f, 10f },    // slot 0
            new[] { 16f, 9f, 20f, -10f, 24f, 12f, 28.5f, -6f, 32f, 12f },      // slot 4
            new[] { 18f, 11f, 21.5f, -7f, 25f, 14f, 29.5f, -10f, 33f, 8f },    // slot 3
            new[] { 15f, 10f, 19f, -9f, 23.5f, 13f, 27.5f, -8f, 31f, 11f },    // slot 7
        };

        // ---- The ring ----
        const float SectorRadius = 118f;
        const float RingFrom = 62f, FormSecs = 12f, TurnUpSecs = 6f, RingRate0 = 0.11f;
        const float WideRadius = 128f, SmallRadius = 52f, WidenSecs = 12f, BreathSecs = 20f;
        const int Breaths = 4;
        const float ReturnSecs = 12f, ExitRadius = 106f;
        static readonly Vector2 Centre = new Vector2(250f, 250f);

        readonly Vector2[] start, line;
        readonly float[] sectorFrom = new float[8], sectorTo = new float[8];   // each robot's angle on the ring: first uneven, then 45 degrees apart
        readonly float breatheFrom, stopFrom, ringStop;
        Show.Eased turnRate;
        public float Total { get; private set; }
        public float SeparateAt { get { return SeparateFrom; } }
        public float RingAt { get { return RingFrom; } }
        public float BreatheAt { get { return breatheFrom; } }
        public float ReturnAt { get { return stopFrom; } }

        static Plan()
        {
            Stand[0] = new Vector2(108f, 372f); Stand[4] = new Vector2(176f, 214f);
            Stand[3] = new Vector2(338f, 388f); Stand[7] = new Vector2(372f, 208f);
        }

        public Plan(Vector2[] start)
        {
            this.start = start;
            line = V2Show.OwnSpots(start.Length);
            var m = new List<float>(); var d = new List<float>();
            for (int k = 0; k < Keys.Length; k += 3) { m.Add(Keys[k]); m.Add(Keys[k + 1]); d.Add(Keys[k]); d.Add(Keys[k + 2]); }
            contestM = new Show.Eased(m.ToArray()); contestD = new Show.Eased(d.ToArray());

            // ring places by cycle: the order that flattens into the line (1 0 2 4 6 7 5 3, 45 degrees apart,
            // slot 1 lower left). Pairs start uneven: the helper a little way from its fighter.
            var order = V2Show.RingOrder(8);
            for (int i = 0; i < 8; i++) { sectorTo[i] = (135f + 45f * order[i]) * Mathf.Deg2Rad; }
            foreach (int f in new[] { 1, 2, 5, 6 }) { sectorFrom[f] = sectorTo[f]; }
            sectorFrom[0] = sectorTo[1] + 25f * Mathf.Deg2Rad;    // helper 0 beside fighter 1
            sectorFrom[4] = sectorTo[2] + 25f * Mathf.Deg2Rad;
            sectorFrom[7] = sectorTo[6] + 25f * Mathf.Deg2Rad;
            sectorFrom[3] = sectorTo[5] + 25f * Mathf.Deg2Rad;

            breatheFrom = RingFrom + FormSecs + WidenSecs;
            stopFrom = breatheFrom + Breaths * BreathSecs;
            ringStop = stopFrom + ReturnSecs;
            // The ring turns gently and, half way through the final return, stands at the angle from
            // which it flattens into the line without crossing: the turn rate is set to bring it there.
            float tMid = stopFrom + ReturnSecs / 2f;
            Show.Eased RateFor(float c) { return new Show.Eased(RingFrom, 0f, RingFrom + TurnUpSecs, RingRate0, breatheFrom, c, stopFrom, c, ringStop, 0f); }
            float unused; float opensAt = V2Show.BestAngle(order, 1, ExitRadius, line, out unused);
            float needed = Mathf.Repeat(opensAt - 135f * Mathf.Deg2Rad, 2f * Mathf.PI);
            float omega0 = RateFor(0f).Integral(tMid) - RateFor(0f).Integral(RingFrom);
            float perUnit = RateFor(1f).Integral(tMid) - RateFor(1f).Integral(RingFrom) - omega0;
            float chosen = 7f * Mathf.Deg2Rad;
            for (int k = 0; k < 40; k++)
            {
                float c = (needed + 2f * Mathf.PI * k - omega0) / perUnit;
                if (c >= 4f * Mathf.Deg2Rad && c <= 9f * Mathf.Deg2Rad) { chosen = c; break; }
            }
            turnRate = RateFor(chosen);
            Total = ringStop + 0.3f;
        }

        // ---------------- The contest on the lanes ----------------
        // x of a robot on the left side (facing right) or on the right side (facing left), t seconds in
        float X(bool leftSide, float t)
        {
            float m = contestM.At(t), d = contestD.At(t);
            return leftSide ? m - d / 2f : m + d / 2f;
        }

        static bool IsLeft(int slot) { return slot == 1 || slot == 2; }
        static bool IsFighter(int slot) { return slot == 1 || slot == 2 || slot == 5 || slot == 6; }
        static bool IsSecond(int slot) { return slot == 2 || slot == 6; }
        static float LaneOf(int slot) { return IsSecond(slot) ? LaneHigh : LaneLow; }

        // A fighter's place on its lane (before it is led away)
        Vector2 OnLane(int slot, float t)
        {
            float tt = IsSecond(slot) ? t - Lag : t;
            return new Vector2(X(IsLeft(slot), tt), LaneOf(slot));
        }

        Vector2 Fighter(int slot, float t)
        {
            Vector2 lane = OnLane(slot, t);
            if (!IsSecond(slot))
            {
                Vector2 firstLane = new Vector2(X(IsLeft(slot), 5f), LaneLow);
                if (t < EnterSecs) { return Vector2.Lerp(start[slot], firstLane, Shapes.Ease(t / EnterSecs)); }
            }
            else if (t < SecondEnterFrom + SecondEnterSecs)
            {
                Vector2 laneNow = new Vector2(OnLane(slot, t).x, LaneHigh);
                return Vector2.Lerp(start[slot], laneNow, Shapes.Ease((t - SecondEnterFrom) / SecondEnterSecs));
            }
            return lane;
        }

        // ---------------- The onlookers ----------------
        Vector2 Onlooker(int slot, float t)
        {
            int idx = System.Array.IndexOf(HelperOf, slot);
            float[] ev = Reactions[idx];
            // to its stand, one after another
            float go = 0.5f + 1.2f * idx;
            Vector2 p = Vector2.Lerp(start[slot], Stand[slot], Shapes.Ease((t - go) / 5f));
            Vector2 toward = (new Vector2(250f, 290f) - Stand[slot]).normalized;
            Vector2 perp = new Vector2(-toward.y, toward.x);
            for (int k = 0; k + 1 < ev.Length; k += 2)
            {
                // a small move toward (+) or away (-) over a second, then settling back over two
                float u = t - ev[k];
                float bump = Show.Bump(u / 3f);
                p += toward * ev[k + 1] * bump + perp * (ev[k + 1] < 0f ? 7f : 0f) * bump;
            }
            return p;
        }

        // The flank beside its fighter: a helper follows the fighter's lane
        Vector2 FlankOf(int fighter, float t)
        {
            Vector2 f = OnLane(fighter, t);
            float side = IsSecond(fighter) ? -1f : 1f;   // upper fighters: from above; lower: from below
            return f + new Vector2(0f, side * Flank);
        }

        int FighterOf(int helper)
        {
            int[] f = { 1, 2, 5, 6 };
            return f[System.Array.IndexOf(HelperOf, helper)];
        }

        // ---------------- Leading apart, the ring ----------------
        float Ring(float t) { return turnRate.Integral(t) - turnRate.Integral(RingFrom); }

        float Radius(float t)
        {
            float r = Mathf.Lerp(SectorRadius, WideRadius, Shapes.Ease((t - RingFrom - FormSecs) / WidenSecs));
            for (int b = 0; b < Breaths; b++) { r -= (WideRadius - SmallRadius) * Show.Bump((t - breatheFrom - b * BreathSecs) / BreathSecs); }
            return r + (ExitRadius - WideRadius) * Shapes.Ease((t - stopFrom) / (ReturnSecs * 0.5f));
        }

        Vector2 Sector(int slot, float t)
        {
            float even = Shapes.Ease((t - RingFrom) / FormSecs);
            float angle = Mathf.LerpAngle(sectorFrom[slot] * Mathf.Rad2Deg, sectorTo[slot] * Mathf.Rad2Deg, even) * Mathf.Deg2Rad + Ring(t);
            return Centre + Radius(t) * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        public Vector2 Position(int slot, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            Vector2 p;
            bool fighter = IsFighter(slot);
            if (fighter)
            {
                p = Fighter(slot, t);
                float away = Shapes.Ease((t - SeparateFrom) / SeparateSecs);
                if (away > 0f) { p = Vector2.Lerp(p, Sector(slot, t), away); }
            }
            else
            {
                p = Onlooker(slot, t);
                int f = FighterOf(slot);
                float step = Shapes.Ease((t - HelpFrom) / HelpSecs);
                if (step > 0f) { p = Vector2.Lerp(p, FlankOf(f, t), step); }
                // it leads: out ahead of its fighter, curving outward
                float lead = Shapes.Ease((t - SeparateFrom + HelperLead) / SeparateSecs);
                if (lead > 0f)
                {
                    Vector2 outward = (FlankOf(f, t) - new Vector2(250f, 290f)).normalized;
                    p = Vector2.Lerp(p, Sector(slot, t), lead) + outward * 22f * Mathf.Sin(Mathf.PI * lead);
                }
            }
            if (t > stopFrom) { p = Vector2.Lerp(p, line[slot], Shapes.Ease((t - stopFrom) / ReturnSecs)); }
            return p;
        }

        // fighters face each other while on their lanes, also when backing up
        public float Heading(int slot, float t) { return IsLeft(slot) ? 0f : 180f; }

        public Vector2 LineSpotOf(int i) { return line[i]; }
        public Vector2 LineSpot(int i) { return line[i]; }
        public bool InLane(int slot, float t)
        {
            if (!IsFighter(slot)) { return false; }
            float from = IsSecond(slot) ? SecondEnterFrom + SecondEnterSecs : EnterSecs + 0.5f;
            return t >= from && t < SeparateFrom + 4f;
        }
    }
}
