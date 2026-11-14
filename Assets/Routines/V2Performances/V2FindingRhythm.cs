using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// v2 Finding a Rhythm (C15).
//   1. After the standard opening each robot goes to a place of its own around the mat and starts a
//      path of its own: a small figure eight, a tilted one, an uneven two-lobed loop, an irregular
//      loop, a long S, a shallow zigzag, a loose clover, a small oval. Each at its own tempo.
//   2. Slowly the tempos pull together until all keep one beat and the same phase; the shapes stay
//      different, the pulse is shared.
//   3. The shapes open out into arcs that run into one ring; the ring turns, breathes (large, slow)
//      several times and then opens and flattens back into the line.
// The shapes are a starting idea and are smoothed: each is a curve with continuous speed.
public class V2FindingRhythm : SwarmRoutine
{
    public override string Name { get { return "v2 Finding a Rhythm"; } }
    public override string Code { get { return "C15"; } }
    public override string Note { get { return "Own paths, one beat, ring"; } }
    public override int MinRobots { get { return 8; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        yield return V2Show.Run(ctx, start => new Plan(start), 100);
    }

    public class Plan : V2Show.IPlan, V2Show.IHeading
    {
        const float GoSecs = 9f, SyncFrom = 22f, SyncSecs = 30f, RingFrom = 64f;
        const float SectorRadius = 118f, FormSecs = 12f, TurnUpSecs = 6f, RingRate0 = 0.11f;
        const float WideRadius = 128f, SmallRadius = 52f, WidenSecs = 12f, BreathSecs = 20f;
        const int Breaths = 4;
        const float ReturnSecs = 12f, ExitRadius = 106f;
        const float Beat = 2f * Mathf.PI / 12f;                                   // the shared tempo
        static readonly float[] Period = { 9.5f, 13f, 10.5f, 14.5f, 11f, 12.5f, 9f, 15f };
        static readonly float[] Size = { 30f, 34f, 36f, 32f, 38f, 36f, 34f, 30f };
        static readonly Vector2 Centre = new Vector2(250f, 250f);

        readonly Vector2[] start, line;
        readonly float[] sectorTo = new float[8], phase0 = new float[8];
        readonly float breatheFrom, stopFrom, ringStop;
        readonly Show.Eased syncRamp;
        Show.Eased turnRate;
        public float Total { get; private set; }
        public float RingAt { get { return RingFrom; } }
        public float BreatheAt { get { return breatheFrom; } }
        public float ReturnAt { get { return stopFrom; } }

        public Plan(Vector2[] start)
        {
            this.start = start;
            line = V2Show.OwnSpots(start.Length);
            var order = V2Show.RingOrder(8);
            for (int i = 0; i < 8; i++) { sectorTo[i] = (135f + 45f * order[i]) * Mathf.Deg2Rad; }
            syncRamp = new Show.Eased(0f, 0f, SyncFrom, 0f, SyncFrom + SyncSecs, 1f, 1000f, 1f);
            // initial phases chosen so that every robot is in the same phase of the beat once the tempos have met
            float ts = SyncFrom + SyncSecs;
            for (int i = 0; i < 8; i++)
            {
                float w = 2f * Mathf.PI / Period[i];
                phase0[i] = Mathf.Repeat(Beat * ts - w * ts - (Beat - w) * syncRamp.Integral(ts), 2f * Mathf.PI);
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

        // the eight shapes, as unit curves of the phase u
        static Vector2 Shape(int k, float u)
        {
            float s = Mathf.Sin(u);
            switch (k)
            {
                case 0: return new Vector2(0.8f * s, 0.5f * Mathf.Sin(2f * u));                                   // small figure eight
                case 1: { Vector2 e = new Vector2(s, 0.55f * Mathf.Sin(2f * u)); float a = 0.6f; return new Vector2(e.x * Mathf.Cos(a) - e.y * Mathf.Sin(a), e.x * Mathf.Sin(a) + e.y * Mathf.Cos(a)); }   // tilted figure eight
                case 2: return new Vector2(Mathf.Cos(u) + 0.35f * Mathf.Cos(2f * u) - 0.2f, 0.8f * s);            // uneven two-lobed loop
                case 3: return new Vector2(s + 0.22f * Mathf.Sin(3f * u), Mathf.Cos(u) + 0.2f * Mathf.Sin(2f * u)); // irregular loop
                case 4: return new Vector2(s, 0.6f * Mathf.Sin(Mathf.PI * s));                                    // long S
                case 5: return new Vector2(s, 0.3f * Mathf.Sin(3f * Mathf.PI * s));                               // shallow zigzag
                case 6: { float r = 0.62f + 0.38f * Mathf.Cos(3f * u); return new Vector2(r * Mathf.Cos(u), r * Mathf.Sin(u)); }   // loose clover
                default: return new Vector2(0.9f * Mathf.Cos(u), 0.55f * s);                                      // small oval
            }
        }

        Vector2 Cell(int slot) { return Centre + new Vector2(150f * Mathf.Cos(sectorTo[slot]), 140f * Mathf.Sin(sectorTo[slot])); }

        Vector2 OwnPath(int slot, float t)
        {
            float w = 2f * Mathf.PI / Period[slot];
            float u = phase0[slot] + w * t + (Beat - w) * syncRamp.Integral(t);
            return Cell(slot) + Size[slot] * Shape(slot, u);
        }

        float Ring(float t) { return turnRate.Integral(t) - turnRate.Integral(RingFrom); }

        float Radius(float t)
        {
            float r = Mathf.Lerp(SectorRadius, WideRadius, Shapes.Ease((t - RingFrom - FormSecs) / WidenSecs));
            for (int b = 0; b < Breaths; b++) { r -= (WideRadius - SmallRadius) * Show.Bump((t - breatheFrom - b * BreathSecs) / BreathSecs); }
            return r + (ExitRadius - WideRadius) * Shapes.Ease((t - stopFrom) / (ReturnSecs * 0.5f));
        }

        Vector2 Sector(int slot, float t)
        {
            float angle = sectorTo[slot] + Ring(t);
            return Centre + Radius(t) * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        public Vector2 Position(int slot, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            float b = Shapes.Ease((t - RingFrom) / FormSecs);
            Vector2 own = OwnPath(slot, t);
            if (b > 0f) { own = Cell(slot) + (own - Cell(slot)) * (1f - b); }     // the shape dies away into the arc
            Vector2 p = own;
            float go = Shapes.Ease((t - 0.7f * slot) / GoSecs);
            if (go < 1f) { p = Vector2.Lerp(start[slot], own, go); }
            if (b > 0f) { p = Vector2.Lerp(p, Sector(slot, t), b); }
            if (t > stopFrom) { p = Vector2.Lerp(p, line[slot], Shapes.Ease((t - stopFrom) / ReturnSecs)); }
            return p;
        }

        public float Heading(int slot, float t) { return 270f; }
        public Vector2 LineSpot(int i) { return line[i]; }
        public bool InLane(int slot, float t) { return false; }
    }
}
