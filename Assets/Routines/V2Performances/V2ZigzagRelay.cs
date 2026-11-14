using System.Collections;
using System.Linq;
using UnityEngine;

// v2 Zigzag Relay (C18).
//   1. After the standard opening four robots go to the top and four to the bottom, each four evenly
//      spaced, top and bottom staggered: a zigzag over the eight slots.
//   2. A relay runs along the zigzag from the top left to the bottom right: one robot after another
//      moves on to the next point of the zigzag, and the last robot goes back round, under the zigzag, to
//      the first point that the first robot has left. This repeats eight times (every robot comes back to
//      its own point) while the distance between top and bottom slowly shrinks.
//   3. The zigzag has become a line in the middle; a wave starts to run along it, several cycles, and
//      then calms and the row is the final line. No pauses between the parts.
public class V2ZigzagRelay : SwarmRoutine
{
    public override string Name { get { return "v2 Zigzag Relay"; } }
    public override string Code { get { return "C18"; } }
    public override string Note { get { return "Zigzag relay, then wave"; } }
    public override int MinRobots { get { return 8; } }

    public override IEnumerator Run(RoutineContext ctx)
    {
        yield return V2Show.Run(ctx, start => new Plan(start), 100);
    }

    public class Plan : V2Show.IPlan, V2Show.IHeading
    {
        const float A0 = 70f, A1 = 8f;                           // half the distance between top and bottom: at the start and at the end of the relay
        const float FormSecs = 8f;
        const int Cycles = 8;
        const float CycleSecs = 9.4f, StepSecs = 6f, Stagger = 0.25f, WrapAt = 1.5f, WrapSecs = 7.5f, BelowBy = 60f;
        const float WaveAmp = 38f, WavePeriod = 7f, WaveLength = 336f, WaveCycles = 5f, RampSecs = 6f, TaperSecs = 9f;
        const float RowY = 250f;

        readonly Vector2[] start, line;
        readonly float cyclesFrom, cyclesTo, waveFrom, waveTo, taperTo;
        public float Total { get; private set; }
        public float WaveAt { get { return waveFrom; } }
        public float ReturnAt { get { return taperTo; } }

        public Plan(Vector2[] start)
        {
            this.start = start;
            line = V2Show.OwnSpots(start.Length);
            cyclesFrom = FormSecs;
            cyclesTo = cyclesFrom + Cycles * CycleSecs;
            waveFrom = cyclesTo - 4f;
            waveTo = waveFrom + RampSecs + WaveCycles * WavePeriod;
            taperTo = waveTo + TaperSecs;
            Total = taperTo + 3f;
        }

        // half the distance between the rows at t
        float Amp(float t) { return Mathf.Lerp(A0, A1, Shapes.Ease((t - cyclesFrom) / (cyclesTo - cyclesFrom))) * (1f - Shapes.Ease((t - (cyclesTo - 3f)) / 8f)); }

        // point k of the zigzag at time t: even points on top, odd points at the bottom
        Vector2 Point(int k, float t) { return new Vector2(line[k].x, RowY + (k % 2 == 0 ? -1f : 1f) * Amp(t)); }

        float Wave(int slot, float t)
        {
            float k = 2f * Mathf.PI / WaveLength;
            float x = line[slot].x - 250f;
            float own = Shapes.Ease((t - waveFrom - 0.5f * slot) / RampSecs);
            float taper = 1f - Shapes.Ease((t - waveTo) / TaperSecs);
            return WaveAmp * own * taper * Mathf.Sin(k * x - 2f * Mathf.PI * (t - waveFrom) / WavePeriod);
        }

        // where robot `slot` is during the relay (it started at point `slot` and has moved c points along)
        Vector2 Relay(int slot, float t)
        {
            float rt = t - cyclesFrom;
            int c = Mathf.Clamp(Mathf.FloorToInt(rt / CycleSecs), 0, Cycles - 1);
            float lt = rt - c * CycleSecs;
            int k = (slot + c) % 8;                                  // its point at the start of this cycle
            if (rt >= Cycles * CycleSecs) { return Point(slot, t); }
            if (k < 7)
            {
                float u = Shapes.Ease((lt - Stagger * k) / StepSecs);
                return Vector2.Lerp(Point(k, t), Point(k + 1, t), u);
            }
            // the last robot goes back round, below the zigzag, to the first point
            float lin = Mathf.Clamp01((lt - WrapAt) / WrapSecs);
            float v = Mathf.Lerp(lin, Shapes.Ease(lin), 0.5f);     // a steadier pace: the way round is long
            float yb = RowY + Amp(t) + BelowBy;
            Vector2 a = Point(7, t), b = new Vector2(a.x, yb), d = Point(0, t), cc = new Vector2(d.x, yb);
            float l1 = (b - a).magnitude, l2 = (cc - b).magnitude, l3 = (d - cc).magnitude;
            float s = v * (l1 + l2 + l3);
            if (s <= l1) { return Vector2.Lerp(a, b, s / l1); }
            if (s <= l1 + l2) { return Vector2.Lerp(b, cc, (s - l1) / l2); }
            return Vector2.Lerp(cc, d, (s - l1 - l2) / l3);
        }

        public Vector2 Position(int slot, float t)
        {
            t = Mathf.Clamp(t, 0f, Total);
            if (t < cyclesFrom)
            {
                Vector2 to = Point(slot, cyclesFrom);
                return Vector2.Lerp(start[slot], to, Shapes.Ease((t - 0.3f * slot) / (FormSecs - 2.4f)));
            }
            Vector2 p = Relay(slot, t);
            if (t > waveFrom)
            {
                float e = Shapes.Ease((t - waveFrom) / 4f);
                p = new Vector2(Mathf.Lerp(p.x, line[slot].x, e), Mathf.Lerp(p.y, RowY, e) + Wave(slot, t));
            }
            return p;
        }

        public float Heading(int slot, float t) { return 270f; }
        public Vector2 LineSpot(int i) { return line[i]; }
        public bool InLane(int slot, float t) { return false; }
    }
}
