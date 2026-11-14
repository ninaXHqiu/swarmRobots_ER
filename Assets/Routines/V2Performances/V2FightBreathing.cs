using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// v2 Fight to Breathing Circle (C13).
//   1. The standard opening. Then a robot (slot 4) suddenly rushes out and fights the conductor (slot 3,
//      which stays where it is): front to front, pressing, one gives a little ground, then the other.
//   2. Two more (slots 1 and 6) come in from both sides to break it up, tug at it, and are drawn in:
//      a scrum of four, the whole cluster swinging round and drifting, never tidy.
//   3. The other four (0, 2, 5, 7) stand outside, unsure: each edges closer, hesitates, steps back,
//      each on its own timing.
//   4. Two of them (2 and 5) take hold of fighters 4 and 3 and pull them out slowly, with resistance:
//      the fighters are drawn back in part of the way twice before they give. Slots 1 and 6 press on
//      alone as a pair, then 0 and 7 pull those two out the same way.
//   5. The four pairs settle on one ring, which turns, breathes (large, slow) several times and then
//      opens and flattens into the line.
// The pairs sit round the ring in the order that flattens into the line without crossing (V2Show.RingOrder).
public class V2FightBreathing : SwarmRoutine
{
    public override string Name { get { return "v2 Fight to Breathing Circle"; } }
    public override string Code { get { return "C13"; } }
    public override string Note { get { return "Fight, helpers, breathing circle"; } }
    public override int MinRobots { get { return 8; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        yield return V2Show.Run(ctx, start => new Plan(start), 100);
    }

    public class Plan : V2Show.IPlan, V2Show.IHeading
    {
        const float AxisDeg = 0f;                  // the line from the conductor to the attacker
        const float RushAt = 3f, RushSecs = 1.1f;
        const float JoinAt = 13f, JoinSecs = 6f, TugAt = 19f, ScrumAt = 24f;
        static readonly int[] Fighters = { 3, 4, 2, 7 }, Helpers = { 1, 6, 0, 5 };   // helper k pulls fighter k
        static readonly float[] HelpFrom = { 36f, 36.5f, 54f, 54.8f };
        static readonly float[] PullFrom = { 46f, 47.2f, 64f, 65.4f }, PullTo = { 62f, 64.5f, 81f, 82.5f };
        const float HelpSecs = 7f, Flank = 46f, HelperLead = 1.5f;
        static readonly float[] GoAt = new float[8];
        static readonly Vector2[] Stand = new Vector2[8];
        static readonly float[][] Reactions = new float[8][];

        const float SectorRadius = 118f;
        const float RingFrom = 85f, FormSecs = 12f, TurnUpSecs = 6f, RingRate0 = 0.11f;
        const float WideRadius = 128f, SmallRadius = 52f, WidenSecs = 12f, BreathSecs = 20f;
        const int Breaths = 4;
        const float ReturnSecs = 12f, ExitRadius = 106f;
        static readonly Vector2 Centre = new Vector2(250f, 250f);

        readonly Vector2[] start, line;
        readonly float[] sectorFrom = new float[8], sectorTo = new float[8];
        readonly float breatheFrom, stopFrom, ringStop;
        readonly Show.Eased push;
        Show.Eased turnRate;
        Vector2 m0;
        public float Total { get; private set; }
        public float SeparateAt { get { return PullFrom[0]; } }
        public float RingAt { get { return RingFrom; } }
        public float BreatheAt { get { return breatheFrom; } }
        public float ReturnAt { get { return stopFrom; } }

        static Plan()
        {
            Stand[1] = new Vector2(105f, 310f); Stand[6] = new Vector2(410f, 310f);
            Stand[0] = new Vector2(90f, 170f); Stand[5] = new Vector2(395f, 395f);
            GoAt[1] = 19f; GoAt[6] = 19.5f; GoAt[5] = 20.5f; GoAt[0] = 21.5f;
            // (time, move toward the fight: + closer, - back): each its own hesitation
            Reactions[1] = new[] { 26f, 12f, 28.5f, -9f, 31f, 14f, 36f, -6f, 40f, 10f };
            Reactions[6] = new[] { 25f, 9f, 27f, -12f, 29f, 11f, 34f, -8f, 38f, 13f };
            Reactions[0] = new[] { 27f, 10f, 30f, -8f, 33f, 13f, 39f, -10f, 44f, 14f, 49f, -9f, 53f, 11f };
            Reactions[5] = new[] { 28f, 8f, 30f, -11f, 32f, 12f, 37f, -9f, 43f, 10f, 48f, -12f, 52f, 9f };
        }

        public Plan(Vector2[] start)
        {
            this.start = start;
            line = V2Show.OwnSpots(start.Length);
            m0 = start[3] + 21f * Dir(AxisDeg);
            push = new Show.Eased(0f, 0f, 4f, 0f, 5.5f, 0f, 6.7f, -8f, 7.6f, -8f, 9.4f, 7f, 10.2f, 7f, 12f, -9f, 12.8f, -9f,
                                  14.5f, 5f, 15.5f, 5f, 18f, -6f, 19f, -6f, 21f, 4f, 24f, 0f, 200f, 0f);
            var order = V2Show.RingOrder(8);
            for (int i = 0; i < 8; i++) { sectorTo[i] = (135f + 45f * order[i]) * Mathf.Deg2Rad; }
            for (int i = 0; i < 8; i++) { sectorFrom[i] = sectorTo[i]; }
            for (int k = 0; k < 4; k++)
            {
                int f = Fighters[k], h = Helpers[k];
                float side = Mathf.Sign(Mathf.DeltaAngle(sectorTo[f] * Mathf.Rad2Deg, sectorTo[h] * Mathf.Rad2Deg));
                sectorFrom[h] = sectorTo[f] + side * 25f * Mathf.Deg2Rad;
            }

            breatheFrom = RingFrom + FormSecs + WidenSecs;
            stopFrom = breatheFrom + Breaths * BreathSecs;
            ringStop = stopFrom + ReturnSecs;
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

        static Vector2 Dir(float deg) { return new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad)); }
        static float Wob(float t, float period, float phase) { return Mathf.Sin(2f * Mathf.PI * t / period + phase); }

        // ---------------- The cluster of fighters ----------------
        // how wild the scrum is: builds up after the joiners are in, calms as the first pair is pulled out
        float Wild(float t) { return Shapes.Ease((t - ScrumAt) / 5f) * (1f - 0.75f * Shapes.Ease((t - 42f) / 6f)); }

        Vector2 ClusterCentre(float t)
        {
            float w = Wild(t);
            Vector2 drift = new Vector2(12f * Wob(t, 5.3f, 0f) + 7f * Wob(t, 2.9f, 1f), 10f * Wob(t, 4.7f, 2f) + 6f * Wob(t, 3.1f, 0f));
            return m0 + Dir(AxisDeg) * push.At(t) + drift * w;
        }

        float Axis(float t)
        {
            float swing = 28f * Wob(t, 8.5f, 0f) + 12f * Wob(t, 4.1f, 1f);
            return AxisDeg + 9f * Wob(t, 9f, 0f) * Shapes.Ease((t - 6f) / 3f) + swing * Wild(t);
        }

        // where a fighter is within the cluster
        Vector2 Cluster(int slot, float t)
        {
            float a = Axis(t), w = Wild(t);
            Vector2 c = ClusterCentre(t);
            if (slot == 3) { return c - Dir(a) * (21f + 2f * w * Wob(t, 2.1f, 0f)); }
            if (slot == 4) { return c + Dir(a) * (21f + 2f * w * Wob(t, 2.3f, 1.7f)); }
            float side = slot == 7 ? 1f : -1f, ph = slot == 2 ? 0.4f : 2.2f;
            float calm = Shapes.Ease((t - 63f) / 6f);
            float r = 64f - 20f * Shapes.Ease((t - TugAt) / 6f) - 23f * calm + 3f * Wob(t, 2.3f, ph) * Shapes.Ease((t - ScrumAt) / 3f) * (1f - calm);
            return c + Dir(a + side * 90f) * r;
        }

        // before it is pulled out: where each fighter is
        Vector2 FighterPlace(int slot, float t)
        {
            if (slot == 4)
            {
                float w = Mathf.Clamp01((t - RushAt) / RushSecs);
                Vector2 bow = new Vector2(0f, 0f);
                return Vector2.Lerp(start[4], Cluster(4, t), Shapes.Ease(w)) + bow;
            }
            if (slot == 3) { return Cluster(3, t); }
            float j = Shapes.Ease((t - JoinAt) / JoinSecs);
            Vector2 sideBow = new Vector2(0f, (slot == 7 ? 18f : 0f) * Mathf.Sin(Mathf.PI * j));
            return Vector2.Lerp(start[slot], Cluster(slot, t), j) + sideBow;
        }

        // how far along its way out a fighter is: slow, with two partial returns to the fight
        float Pull(int k, float t)
        {
            float t0 = PullFrom[k], t1 = PullTo[k];
            float p = Shapes.Ease((t - t0) / (t1 - t0));
            p -= 0.22f * Show.Bump((t - (t0 + 0.28f * (t1 - t0))) / 4f);
            p -= 0.16f * Show.Bump((t - (t0 + 0.60f * (t1 - t0))) / 3.5f);
            return Mathf.Clamp01(p);
        }

        // ---------------- The onlookers ----------------
        Vector2 Onlooker(int slot, float t)
        {
            Vector2 p = Vector2.Lerp(start[slot], Stand[slot], Shapes.Ease((t - GoAt[slot]) / 5f));
            Vector2 toward = (new Vector2(268f, 305f) - Stand[slot]).normalized;
            Vector2 perp = new Vector2(-toward.y, toward.x);
            float[] ev = Reactions[slot];
            for (int k = 0; k + 1 < ev.Length; k += 2)
            {
                float bump = Show.Bump((t - ev[k]) / 3f);
                p += toward * ev[k + 1] * bump + perp * (ev[k + 1] < 0f ? 7f : 0f) * bump;
            }
            return p;
        }

        // ---------------- The ring ----------------
        float Ring(float t) { return turnRate.Integral(t) - turnRate.Integral(RingFrom); }

        float Radius(float t)
        {
            float r = Mathf.Lerp(SectorRadius, WideRadius, Shapes.Ease((t - RingFrom - FormSecs) / WidenSecs)) + 34f * (1f - Shapes.Ease((t - RingFrom) / FormSecs));
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
            int k = System.Array.IndexOf(Fighters, slot);
            if (k >= 0)
            {
                p = FighterPlace(slot, t);
                float pull = Pull(k, t);
                if (pull > 0f) { p = Vector2.Lerp(p, Sector(slot, t), pull) + new Vector2((k == 0 || k == 2 ? -1f : 1f) * 42f * Mathf.Sin(Mathf.PI * pull), 0f); }
            }
            else
            {
                k = System.Array.IndexOf(Helpers, slot);
                p = Onlooker(slot, t);
                int f = Fighters[k];
                float step = Shapes.Ease((t - HelpFrom[k]) / HelpSecs);
                if (step > 0f)
                {
                    Vector2 fp = FighterPlace(f, t);
                    Vector2 outward = k >= 2 ? new Vector2(k == 2 ? -1f : 1f, 0f) : (fp - ClusterCentre(t)).normalized;
                    if (outward.sqrMagnitude < 0.01f) { outward = Vector2.up; }
                    p = Vector2.Lerp(p, fp + outward * Flank, step);
                }
                float lead = Pull(k, t + HelperLead);
                if (lead > 0f)
                {
                    Vector2 fp = FighterPlace(f, t);
                    Vector2 outward = k >= 2 ? new Vector2(k == 2 ? -1f : 1f, 0f) : (fp - ClusterCentre(t)).normalized;
                    p = Vector2.Lerp(fp + outward * Flank, Sector(slot, t), lead) + new Vector2((k == 0 || k == 2 ? -1f : 1f) * 70f * Mathf.Sin(Mathf.PI * lead), 0f);
                }
            }
            if (t > stopFrom) { p = Vector2.Lerp(p, line[slot], Shapes.Ease((t - stopFrom) / ReturnSecs)); }
            return p;
        }

        public float Heading(int slot, float t) { return 0f; }
        public Vector2 LineSpotOf(int i) { return line[i]; }
        public Vector2 LineSpot(int i) { return line[i]; }
        public bool InLane(int slot, float t) { return false; }
    }
}
