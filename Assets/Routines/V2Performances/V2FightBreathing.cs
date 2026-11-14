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
        const float LaneLow = 266f, LaneHigh = 230f;       // the two lanes of the confrontation: just above the conductor, which stays where it is
        const float Flank = 44f;                           // a helper stands this far beside its fighter
        static readonly int[] Lower = { 1, 5 }, Upper = { 2, 6 };
        const float Lag = 0.14f;                           // the second robot of a side reacts this late
        // (time, centre of the contest, distance between the two sides' robots); each step eases
        static readonly float[] Keys =
        {
            // coupled pair: the fronts stay together (centre distance 36: a small gap, no actual contact), both shift as one
            0f, 250f, 160f,   1.5f, 250f, 160f,   4.0f, 250f, 36f,   5.8f, 250f, 36f,
            7.4f, 276f, 36f,  8.2f, 276f, 36f,  10.4f, 224f, 36f,  11.2f, 224f, 36f,
            13.2f, 272f, 36f, 14.0f, 272f, 36f, 15.8f, 230f, 36f,  16.6f, 230f, 36f,  18f, 250f, 36f,
            // two against two, the same pushing carried on
            21f, 250f, 36f,   22.8f, 274f, 36f, 23.6f, 274f, 36f,  25.8f, 226f, 36f, 26.6f, 226f, 36f,
            28.6f, 272f, 36f, 29.4f, 272f, 36f, 31.4f, 232f, 36f,  32.2f, 232f, 36f,  34f, 250f, 36f,  38f, 250f, 36f,
            // being led away: shorter, slower pushes, then they give way
            41f, 250f, 36f, 43f, 262f, 36f, 44.2f, 262f, 36f, 47f, 244f, 38f, 48.2f, 244f, 38f, 51f, 250f, 60f, 55f, 250f, 110f, 60f, 250f, 150f
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
            Stand[0] = new Vector2(108f, 372f); Stand[4] = new Vector2(160f, 190f);
            Stand[7] = new Vector2(380f, 190f);   // (the conductor, slot 3, stays where it is)
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
            return new Vector2(X(IsLeft(slot), tt), LaneOf(slot)) + Scrum(IsLeft(slot), t);
        }

        Vector2 Fighter(int slot, float t)
        {
            Vector2 lane = OnLane(slot, t);
            if (!IsSecond(slot))
            {
                Vector2 firstLane = new Vector2(X(IsLeft(slot), 5f), LaneLow);
                if (t < EnterSecs)
                {
                    float w = Shapes.Ease(t / EnterSecs);
                    // slot 1 goes round above the robot standing in the line beside it
                    return Vector2.Lerp(start[slot], firstLane, w) + (slot == 1 ? new Vector2(0f, -40f * Mathf.Sin(Mathf.PI * w)) : Vector2.zero);
                }
            }
            else if (t < SecondEnterFrom + SecondEnterSecs)
            {
                Vector2 laneNow = new Vector2(OnLane(slot, t).x, LaneHigh);
                return Vector2.Lerp(start[slot], laneNow, Shapes.Ease((t - SecondEnterFrom) / SecondEnterSecs));
            }
            return lane;
        }

        // ---------------- The scrum ----------------
        // From the second exchange on the fight turns into a scrum: each side sways and surges as a
        // block (its two robots together, so they stay clear of each other), the two sides on different
        // irregular rhythms; the helpers circle their fighters looking for the flank. It dies away as
        // the helpers lead the fighters out.
        const float ChaosFrom = 21f, ChaosRampSecs = 7f;
        float ChaosWeight(float t)
        {
            return Shapes.Ease((t - ChaosFrom) / ChaosRampSecs) * (1f - Shapes.Ease((t - SeparateFrom - 6f) / (SeparateSecs - 6f)));
        }

        Vector2 Scrum(bool leftSide, float t)
        {
            float w = ChaosWeight(t);
            if (w <= 0f) { return Vector2.zero; }
            float a = 0f;
            float x = 5f * Mathf.Sin(2f * Mathf.PI * t / 1.9f + a) + 4f * Mathf.Sin(2f * Mathf.PI * t / 1.1f + 2f * a);
            float y = 5f * Mathf.Sin(2f * Mathf.PI * t / 2.6f + 1.3f + a) + 5f * Mathf.Sin(2f * Mathf.PI * t / 1.4f + a);
            return new Vector2(x, y) * w;
        }

        // ---------------- The onlookers ----------------
        Vector2 Onlooker(int slot, float t)
        {
            int idx = System.Array.IndexOf(HelperOf, slot);
            float[] ev = Reactions[idx];
            // to its stand, one after another
            // the messenger (slot 3) has to clear the fight's lane first; the others stay in the line until the first exchange is under way
            // the three others leave the line early, out of the fight's way; the conductor does not leave
            float go = 0.5f + 1.0f * idx;
            Vector2 stand = slot == 3 ? start[slot] : Stand[slot];
            Vector2 p = Vector2.Lerp(start[slot], stand, Shapes.Ease((t - go) / 5f));
            Vector2 toward = (new Vector2(250f, 258f) - stand).normalized;
            Vector2 perp = new Vector2(-toward.y, toward.x);
            for (int k = 0; k + 1 < ev.Length && slot != 3; k += 2)   // (the conductor stays still)
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
            // it circles round toward the outer side (away from the other team) while the scrum is on
            float w = ChaosWeight(t);
            float swing = w * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / 2.2f + 1.1f * fighter)) * 0.85f;
            float outward = IsLeft(fighter) ? -1f : 1f;
            return f + new Vector2(outward * Flank * Mathf.Sin(swing), side * Flank * Mathf.Cos(swing));
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
        // free path steering for everyone: a lane robot cannot correct sideways drift while it stands still
        // pressing, and the fighters still face each other because they only ever move along their axis
        public bool InLane(int slot, float t) { return false; }
    }
}
